using Microsoft.AspNetCore.Identity;
using UnMango.Wishlists.Api.Domain;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddAuthorization()
	.AddOpenApi()
	.AddDbContext<AppDbContext>(AppDbContext.Configure);

builder.Services
	.AddIdentityApiEndpoints<User>()
	.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();
app.MapOpenApi();
app.MapGroup("/auth").MapIdentityApi<User>();

var api = app.MapGroup("/api").RequireAuthorization();
var me = api.MapGroup("/me");
me.MapGet("/", () => Results.Ok(new User("Test")));
var wishlists = api.MapGroup("/wishlists");
wishlists.MapGet("/", () => Results.Ok(new[] { "Sample Wishlist 1", "Sample Wishlist 2" }));

List<PathString> excludedPrefixes = ["/api", "/auth", "/openapi"];

app.UseWhen(ctx => !excludedPrefixes.Any(ctx.Request.Path.StartsWithSegments), then => {
	if (app.Environment.IsDevelopment()) {
		then.UseSpa(x => x.UseProxyToSpaDevelopmentServer("http://localhost:5173"));
		// then.UseWishlistsDevServer();
	} else {
		// This isn't handling / for some reason
		// then.UseStaticFiles();
	}
});

// This just doesn't work
if (app.Environment.IsProduction()) {
	app.MapStaticAssets();
}

app.Run();
