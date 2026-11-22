using JasperFx.Events.Daemon;
using Marten;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using UnMango.Wishlists.Api;
using UnMango.Wishlists.Api.Api;
using UnMango.Wishlists.Api.Auth;
using UnMango.Wishlists.Api.Extensions;
using UnMango.Wishlists.Api.Storage;
using UnMango.Wishlists.Api.Thumbnails;
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

// The cluster's ingress sits in front of the app and terminates TLS.
builder.Services.Configure<ForwardedHeadersOptions>(options => {
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownIPNetworks.Clear();
	options.KnownProxies.Clear();

	// Trust only the ingress's networks when they're configured; until then, trust any peer.
	var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
	foreach (var network in knownNetworks) {
		options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network.Trim()));
	}
});

// The SSRF guard checks every request to an item's link.
builder.Services.AddHttpClient<ThumbnailClient>(http => ThumbnailClient.Configure(http))
	.ConfigurePrimaryHttpMessageHandler(GuardedHandler.Create);

if (builder.Configuration.GetConnectionString("App") is { Length: > 0 } connectionString) {
	builder.Services
		.AddMarten(options => WishlistsStore.Configure(options, connectionString))
		.UseLightweightSessions()
		.ApplyAllDatabaseChangesOnStartup()
		// The async daemon handles thumbnail lookups. HotCold elects one leader, so only one pod looks things up.
		.AddSubscriptionWithServices<ThumbnailSubscription>(ServiceLifetime.Scoped, o => o.Name = "Thumbnails")
		.AddAsyncDaemon(DaemonMode.HotCold);

	builder.Services.PersistDataProtectionKeysToMarten();

	// First in line, so views are rebuilt before the daemon or the server starts. See docs/adr/0001-stored-events-are-contracts.md.
	builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, RebuildViewsOnStartup>());
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
	// Serve wwwroot/index.html at "/". Screens use hash routes, so the app needs no fallback route.
	app.UseDefaultFiles();
	app.UseStaticFiles();
}

app.Run();

public partial class Program;
