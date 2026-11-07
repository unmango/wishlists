using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using UnMango.Wishlists.Api.Auth;

namespace UnMango.Wishlists.Api.Tests;

public sealed class LoginTests(AppFixture app)
{
	private static readonly CancellationToken Ct = CancellationToken.None;

	[Fact]
	public async Task Login_redirects_to_google_without_the_account_picker() {
		var response = await app.Anonymous().GetAsync("/auth/login?returnUrl=/", Ct);

		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.StartsWith("https://accounts.google.com/", response.Headers.Location!.ToString());
		Assert.DoesNotContain("prompt=", response.Headers.Location!.Query);
	}

	[Fact]
	public async Task Login_after_a_denial_asks_google_for_the_account_picker() {
		var response = await app.Anonymous().GetAsync("/auth/login?returnUrl=/&pickAccount=true", Ct);

		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.Contains("prompt=select_account", response.Headers.Location!.Query);
	}

	[Fact]
	public async Task Sign_in_cookie_outlives_the_browser_session() {
		var cookie = await SignInCookieAsync(AppFixture.AllowedEmail);

		Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData(AppFixture.AllowedEmail, true)]
	[InlineData("removed@example.com", false)]
	public async Task Sign_in_cookie_only_works_while_the_email_is_allowed(string email, bool expected) {
		var cookie = await SignInCookieAsync(email);

		using var scope = app.Services.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Request.Headers.Cookie = cookie.Split(';')[0];
		var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

		Assert.Equal(expected, result.Succeeded);
	}

	private async Task<string> SignInCookieAsync(string email) {
		using var scope = app.Services.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		var principal = new ClaimsPrincipal(new ClaimsIdentity(
			[
				new Claim(MemberSignIn.MemberIdClaim, Guid.NewGuid().ToString()),
				new Claim(ClaimTypes.Email, email),
			],
			CookieAuthenticationDefaults.AuthenticationScheme));

		await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

		return Assert.Single(context.Response.Headers.SetCookie, c => c!.StartsWith("wishlists="))!;
	}
}
