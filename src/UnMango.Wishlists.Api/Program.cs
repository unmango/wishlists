using Microsoft.OpenApi;
using UnMango.Wishlists.Api.Domain;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables("UNMANGO_");

builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddAuthorization()
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

// This just doesn't seem to work
if (app.Environment.IsProduction()) {
	app.MapStaticAssets();
}

List<PathString> excludedPrefixes = ["/api", "/auth", "/openapi"];
app.UseWhen(ctx => !excludedPrefixes.Any(ctx.Request.Path.StartsWithSegments), then => {
	if (app.Environment.IsDevelopment()) {
		var uri = app.Configuration.GetValue<string>("DevServerUri", "http://localhost:5173");
		Console.WriteLine($"DevServer URI: {uri}");
		then.UseSpa(x => x.UseProxyToSpaDevelopmentServer(uri));
	} else {
		// This isn't handling / for some reason
		then.UseStaticFiles();
	}
});

app.Run();
