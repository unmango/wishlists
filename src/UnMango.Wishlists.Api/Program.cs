using JasperFx;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using UnMango.Wishlists.Api;
using UnMango.Wishlists.Api.Api;
using UnMango.Wishlists.Api.Auth;
using UnMango.Wishlists.Api.Domain;
using UnMango.Wishlists.Api.Extensions;
using Vite.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables("UNMANGO_");
builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddProblemDetails()
	.AddExceptionHandler<ConcurrencyExceptionHandler>()
	.AddViteServices()
	.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1)
	.AddWishlistsAuth(builder.Configuration, builder.Environment);

// The app runs behind the cluster's ingress, which terminates TLS.
builder.Services.Configure<ForwardedHeadersOptions>(options => {
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();
});

if (builder.Configuration.GetConnectionString("App") is { Length: > 0 } connectionString) {
	builder.Services
		.AddMarten(options => {
			options.Connection(connectionString);
			options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
			options.Projections.Snapshot<MemberView>(SnapshotLifecycle.Inline);
			options.Projections.Snapshot<WishlistView>(SnapshotLifecycle.Inline);
			options.Projections.Snapshot<PurchasesView>(SnapshotLifecycle.Inline);
			options.Schema.For<MemberView>().UniqueIndex(x => x.GoogleSubject);
			options.Schema.For<WishlistView>().Index(x => x.OwnerId);
			options.Schema.For<DataProtectionKey>().Identity(x => x.Id);
		})
		.UseLightweightSessions()
		.ApplyAllDatabaseChangesOnStartup();

	builder.Services.PersistDataProtectionKeysToMarten();
}

var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapWishlistsAuth(app.Environment);
app.MapGroup("/api")
	.RequireAuthorization()
	.MapWishlistsApi();

if (app.Environment.IsDevelopment()) {
	app.UseWebSockets();
	app.UseViteDevelopmentServer(useMiddleware: true);
} else if (!app.Environment.IsOpenApiCodegen()) {
	app.UseStaticFiles();
}

app.Run();

public partial class Program;
