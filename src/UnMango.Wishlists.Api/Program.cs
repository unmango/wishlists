using Microsoft.OpenApi;
using UnMango.Wishlists.Api.Domain;
using UnMango.Wishlists.Api.Extensions;
using Vite.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables("UNMANGO_");
builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddAuthorization()
	.AddViteServices()
	.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1)
	.AddDbContext<AppDbContext>(AppDbContext.Configure);

builder.Services
	.AddIdentityApiEndpoints<User>()
	.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();
app.MapOpenApi();
app.MapGroup("/auth").MapIdentityApi<User>();
var api = app.MapGroup("/api").RequireAuthorization();

var me = api.MapGroup("/me");
me.MapGet("/", () => TypedResults.Ok(new User("Test")));

var wishlists = api.MapGroup("/wishlists");
wishlists.MapGet("/", () => TypedResults.Ok(new[] { "Sample Wishlist 1", "Sample Wishlist 2" }));

if (app.Environment.IsDevelopment()) {
	app.UseWebSockets();
	app.UseViteDevelopmentServer(useMiddleware: true);
} else {
	app.UseStaticFiles();
}

if (!app.Environment.IsOpenApiCodegen() && app.Environment.IsDevelopment()) {
	app.Migrate<AppDbContext>();
}

app.Run();
