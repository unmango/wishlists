using Marten;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using UnMango.Wishlists.Api;
using UnMango.Wishlists.Api.Configuration;
using UnMango.Wishlists.Api.Extensions;
using Vite.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables("UNMANGO_");

builder.Services.ConfigureHttpJsonOptions(options => options
	.SerializerOptions.TypeInfoResolverChain
	.Add(AppSerializationContext.Default));

builder.Services
	.AddViteServices()
	.AddSingleton(TimeProvider.System)
	.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

_ = builder.Configuration.GetConnectionString("App") switch {
	null or "" => builder.Services,
	{ } connectionString => builder.Services
		.AddDbContext<AppDbContext>(options => {
			options.UseNpgsql(connectionString);
			options.UseOpenIddict();
		})
		.AddMarten(x => x.Connection(connectionString))
		.UseLightweightSessions()
		.Services,
};

// builder.Services.AddIdentityApiEndpoints<IdentityUser>();
// TODO: https://github.com/openiddict/openiddict-samples/blob/dev/samples/Mimban/Mimban.Server/Program.cs
builder.Services.AddOpenIddict()
	.AddCore(options => {
		options.UseEntityFrameworkCore()
			.UseDbContext<AppDbContext>();
	})
	.AddClient(options => {
		options.AllowAuthorizationCodeFlow();

		options.AddDevelopmentEncryptionCertificate()
			.AddDevelopmentSigningCertificate();

		options.UseAspNetCore();
		options.UseSystemNetHttp()
			.SetProductInformation(typeof(Program).Assembly);

		options.UseWebProviders()
			.AddGitHub(gh => {
				var (clientId, clientSecret) = GitHub.Bind(builder.Configuration.GetSection("GitHub"));
				gh.SetClientId(clientId).SetClientSecret(clientSecret).SetRedirectUri("callback/login/github");
			});
	})
	.AddServer(options => {
		options.SetAuthorizationEndpointUris("authorize")
			.SetTokenEndpointUris("token");

		options.AllowAuthorizationCodeFlow();

		options.AddDevelopmentEncryptionCertificate()
			.AddDevelopmentSigningCertificate();

		options.UseAspNetCore()
			.DisableTransportSecurityRequirement();
	});


builder.Services
	.AddAuthorization()
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie();

var app = builder.Build();

app.MapOpenApi();

// app.MapGroup("/auth").MapIdentityApi<IdentityUser>();
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
} else if (!app.Environment.IsOpenApiCodegen()) {
	app.UseStaticFiles();
}

if (!app.Environment.IsOpenApiCodegen() && app.Environment.IsDevelopment()) {
	await using var scope = app.Services.CreateAsyncScope();
	var context =  scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await context.Database.MigrateAsync();
}

app.Run();
