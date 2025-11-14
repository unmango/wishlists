using Marten;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using UnMango.Wishlists.Api;
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
	.AddSingleton(TimeProvider.System)
	.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

builder.Services.AddIdentityApiEndpoints<IdentityUser>();

_ = builder.Configuration.GetConnectionString("App") switch {
	null or "" => builder.Services,
	{ } connectionString => builder.Services
		.AddMarten(x => x.Connection(connectionString))
		.UseLightweightSessions()
		.Services
};

// TODO: https://github.com/openiddict/openiddict-samples/blob/dev/samples/Mimban/Mimban.Server/Program.cs
// builder.Services.AddOpenIddict()
// 	.AddClient(options => {
// 		options.UseAspNetCore();
// 		options.UseWebProviders()
// 			.AddGitHub(gh => gh.SetClientId("unmango"));
// 	});

var app = builder.Build();
app.MapOpenApi();
app.MapGroup("/auth").MapIdentityApi<IdentityUser>();
var api = app.MapGroup("/api");

if (!app.Environment.IsOpenApiCodegen() && !app.Environment.IsDevelopment()) {
	api.RequireAuthorization();
}

var me = api.MapGroup("/me");
me.MapGet("/", () => TypedResults.Ok(User.From("Test")));
api.MapWishlists();

if (app.Environment.IsDevelopment()) {
	app.UseWebSockets();
	app.UseViteDevelopmentServer(useMiddleware: true);
} else {
	app.UseStaticFiles();
}

if (!app.Environment.IsOpenApiCodegen() && app.Environment.IsDevelopment()) {
	// app.Migrate<AppDbContext>();
}

app.Run();
