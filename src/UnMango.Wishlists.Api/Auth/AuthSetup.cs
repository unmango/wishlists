using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UnMango.Wishlists.Api.Extensions;

namespace UnMango.Wishlists.Api.Auth;

internal static class AuthSetup
{
	private const string EmailVerifiedClaim = "email_verified";

	extension(IServiceCollection services)
	{
		public IServiceCollection AddWishlistsAuth(IConfiguration configuration, IWebHostEnvironment environment) {
			var section = configuration.GetSection(AuthOptions.Section);
			var google = section.GetSection("Google").Get<AuthOptions.GoogleOptions>() ?? new();

			services.Configure<AuthOptions>(section);
			services.AddSingleton<AllowedEmails>();
			services.AddScoped<MemberSignIn>();

			services.AddDataProtection().SetApplicationName("wishlists");

			services.AddAuthorization();
			var authentication = services
				.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(o => {
					o.Cookie.Name = "wishlists";
					o.Cookie.HttpOnly = true;
					o.Cookie.SecurePolicy = environment.IsDevelopment()
						? CookieSecurePolicy.SameAsRequest
						: CookieSecurePolicy.Always;
					o.Cookie.SameSite = SameSiteMode.Lax;
					o.ExpireTimeSpan = TimeSpan.FromDays(30);
					o.SlidingExpiration = true;

					// The front end signs in explicitly through /auth/login, so API calls get status codes, not redirects.
					o.Events.OnRedirectToLogin = ctx => {
						ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
						return Task.CompletedTask;
					};
					o.Events.OnRedirectToAccessDenied = ctx => {
						ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
						return Task.CompletedTask;
					};
				});

			if (google is not { ClientId.Length: > 0, ClientSecret.Length: > 0 }) {
				// Development uses /auth/dev-login instead. Anywhere else, nobody could sign in, so fail loudly.
				if (environment.IsProduction() && !environment.IsOpenApiCodegen()) {
					throw new InvalidOperationException("Auth:Google:ClientId and Auth:Google:ClientSecret are required.");
				}

				return services;
			}

			authentication.AddGoogle(o => {
					o.ClientId = google.ClientId;
					o.ClientSecret = google.ClientSecret;
					o.ClaimActions.MapJsonKey("picture", "picture");
					o.ClaimActions.MapCustomJson(EmailVerifiedClaim, user =>
						(user.TryGetProperty("email_verified", out var v) || user.TryGetProperty("verified_email", out v))
						&& v.ValueKind == JsonValueKind.True
							? "true"
							: "false");

					o.Events.OnTicketReceived = OnTicketReceived;
					o.Events.OnRemoteFailure = ctx => {
						ctx.Response.Redirect("/?signin=failed");
						ctx.HandleResponse();
						return Task.CompletedTask;
					};
				});

			return services;
		}

		public IServiceCollection PersistDataProtectionKeysToMarten() {
			services.AddSingleton<MartenXmlRepository>();
			services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
				new ConfigureOptions<KeyManagementOptions>(o =>
					o.XmlRepository = sp.GetRequiredService<MartenXmlRepository>()));

			return services;
		}
	}

	extension(IEndpointRouteBuilder endpoints)
	{
		public void MapWishlistsAuth(IWebHostEnvironment environment) {
			var auth = endpoints.MapGroup("/auth").ExcludeFromDescription();

			auth.MapGet("/login", async Task<Results<ChallengeHttpResult, ProblemHttpResult>> (
				[FromServices] IAuthenticationSchemeProvider schemes,
				[FromQuery] string? returnUrl
			) => await schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) is null
				? TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Google sign-in isn't configured.")
				: TypedResults.Challenge(
					new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) },
					[GoogleDefaults.AuthenticationScheme]));

			auth.MapPost("/logout", async Task<NoContent> (HttpContext context) => {
				await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				return TypedResults.NoContent();
			});

			if (environment.IsDevelopment()) {
				// Lets you sign in locally without Google credentials. Never mapped outside Development.
				auth.MapGet("/dev-login", async Task<Results<RedirectHttpResult, ValidationProblem>> (
					HttpContext context,
					[FromServices] MemberSignIn signIn,
					[FromQuery] string email,
					[FromQuery] string? name,
					[FromQuery] string? returnUrl,
					CancellationToken cancellationToken
				) => {
					if (string.IsNullOrWhiteSpace(email)) {
						return TypedResults.ValidationProblem(new Dictionary<string, string[]> {
							["email"] = ["Email is required."],
						});
					}

					email = email.Trim();
					var displayName = name ?? email.Split('@')[0];
					var member = await signIn.FindOrJoinAsync(
						new GoogleProfile($"dev:{email}", email, displayName, displayName, null),
						cancellationToken);

					await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, MemberSignIn.Principal(member));
					return TypedResults.LocalRedirect(SafeReturnUrl(returnUrl));
				});
			}
		}
	}

	internal static string SafeReturnUrl(string? returnUrl) =>
		returnUrl is ['/', not '/' and not '\\', ..] or "/" ? returnUrl! : "/";

	private static async Task OnTicketReceived(TicketReceivedContext ctx) {
		var services = ctx.HttpContext.RequestServices;
		var logger = services.GetRequiredService<ILogger<MemberSignIn>>();
		var principal = ctx.Principal!;
		var email = principal.FindFirstValue(ClaimTypes.Email);
		var verified = principal.FindFirstValue(EmailVerifiedClaim) == "true";

		if (!verified || !services.GetRequiredService<AllowedEmails>().Contains(email)) {
			logger.LogWarning("Rejected sign-in for {Email} (verified: {Verified})", email, verified);
			ctx.Response.Redirect("/?signin=denied");
			ctx.HandleResponse();
			return;
		}

		var member = await services.GetRequiredService<MemberSignIn>().FindOrJoinAsync(
			new GoogleProfile(
				principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
				email!,
				principal.FindFirstValue(ClaimTypes.Name) ?? email!,
				principal.FindFirstValue(ClaimTypes.GivenName),
				principal.FindFirstValue("picture")),
			ctx.HttpContext.RequestAborted);

		ctx.Principal = MemberSignIn.Principal(member);
	}
}
