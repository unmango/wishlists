using Microsoft.EntityFrameworkCore;
using UnMango.Wishlists.Api.Domain;
using UnMango.Wishlists.Api.Middleware;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(WishlistSerializationContext.Default));

builder.Services
	.AddOpenApi()
	.AddDbContext<WishlistsContext>((services, options) => {
		var configuration = services.GetRequiredService<IConfiguration>();
		options.UseNpgsql(
			configuration.GetConnectionString("Wishlists"),
			x => x.SetPostgresVersion(18, 0));
	});

var app = builder.Build();
app.MapOpenApi();

var api = app.MapGroup("/api");
var wishlists = api.MapGroup("/wishlists");
wishlists.MapGet("/", () => Results.Ok(new[] { "Sample Wishlist 1", "Sample Wishlist 2" }));

List<PathString> excludedPrefixes = ["/api", "/openapi"];

app.UseWhen(ctx => !excludedPrefixes.Any(ctx.Request.Path.StartsWithSegments), then => {
	if (app.Environment.IsDevelopment()) {
		// then.UseSpa(x => x.UseProxyToSpaDevelopmentServer("http://localhost:5173"));
		then.UseWishlistsDevServer();
	} else {
		// This isn't handling / for some reason
		then.UseStaticFiles();
	}
});

// This just doesn't work
if (app.Environment.IsProduction()) {
	app.MapStaticAssets();
}

app.Run();
