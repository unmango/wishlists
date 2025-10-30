var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();

var api = app.MapGroup("/api");
var wishlists = api.MapGroup("/wishlists");
wishlists.MapGet("/", () => Results.Ok(new[] { "Sample Wishlist 1", "Sample Wishlist 2" }));

List<Func<HttpContext, bool>> spaExclusions = [
	context => !context.Request.Path.StartsWithSegments("/api"),
	context => !context.Request.Path.StartsWithSegments("/openapi")
];

app.UseWhen(ctx => spaExclusions.Aggregate(true, (acc, fn) => fn(ctx) && acc), then => {
	if (app.Environment.IsDevelopment()) {
		then.UseSpa(x => x.UseProxyToSpaDevelopmentServer("http://localhost:5173"));
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
