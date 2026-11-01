using System.Net;

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
}
