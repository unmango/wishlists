using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using UnMango.Wishlists.Api.Domain;
using UnMango.Wishlists.Api.Extensions;
using Vite.AspNetCore;
using Marten;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables("UNMANGO_");
builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddAuthorization()
	.AddViteServices()
	.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

if (builder.Configuration.GetConnectionString("App") is { Length: > 0 } connectionString) {
	builder.Services
		.AddDbContext<AppDbContext>(options => options.UseNpgsql(
			connectionString,
			x => x.SetPostgresVersion(18, 0)))
		.AddMarten(options => options.Connection(connectionString))
		.UseLightweightSessions();

	builder.Services
		.AddIdentityApiEndpoints<User>()
		.AddEntityFrameworkStores<AppDbContext>();
}

var app = builder.Build();
app.MapOpenApi();
app.MapGroup("/auth").MapIdentityApi<User>();
var api = app.MapGroup("/api");

if (!app.Environment.IsDevelopment() && !app.Environment.IsOpenApiCodegen()) {
	api.RequireAuthorization();
}

var me = api.MapGroup("/me");
me.MapGet("/", () => TypedResults.Ok(new User("Test")));
api.MapGroup("/wishlists").MapWishlists();

if (app.Environment.IsDevelopment()) {
	app.UseWebSockets();
	app.UseViteDevelopmentServer(useMiddleware: true);
} else if (!app.Environment.IsOpenApiCodegen()) {
	app.UseStaticFiles();
}

if (!app.Environment.IsOpenApiCodegen() && app.Environment.IsDevelopment()) {
	app.Migrate<AppDbContext>();
}

app.Run();
