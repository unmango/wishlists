using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UnMango.Wishlists.Api.Auth;

namespace UnMango.Wishlists.Api.Tests;

public sealed class AuthTests
{
	private static AllowedEmails Allowed(AuthOptions options) =>
		new(new StaticMonitor(options), NullLogger<AllowedEmails>.Instance);

	[Fact]
	public void Allowlist_matches_configured_emails_ignoring_case_and_spaces() {
		var allowed = Allowed(new AuthOptions { AllowedEmails = ["Ada@Example.com"] });

		Assert.True(allowed.Contains(" ada@example.com "));
		Assert.False(allowed.Contains("bob@example.com"));
		Assert.False(allowed.Contains(null));
		Assert.False(allowed.Contains(""));
	}

	[Fact]
	public void Allowlist_reads_the_secret_file_on_every_check() {
		var path = Path.GetTempFileName();
		try {
			File.WriteAllLines(path, ["# family", "", "ada@example.com"]);
			var allowed = Allowed(new AuthOptions { AllowedEmailsFile = path });

			Assert.True(allowed.Contains("ada@example.com"));
			Assert.False(allowed.Contains("# family"));
			Assert.False(allowed.Contains("bob@example.com"));

			File.AppendAllLines(path, ["bob@example.com"]);
			Assert.True(allowed.Contains("bob@example.com"));
		}
		finally {
			File.Delete(path);
		}
	}

	[Fact]
	public void Allowlist_denies_everyone_when_the_file_is_missing() {
		var allowed = Allowed(new AuthOptions { AllowedEmailsFile = "/does/not/exist" });

		Assert.False(allowed.Contains("ada@example.com"));
	}

	[Theory]
	[InlineData(null, "/")]
	[InlineData("", "/")]
	[InlineData("/", "/")]
	[InlineData("/lists/123", "/lists/123")]
	[InlineData("//evil.example.com", "/")]
	[InlineData("/\\evil.example.com", "/")]
	[InlineData("https://evil.example.com", "/")]
	public void Return_urls_must_stay_on_this_site(string? input, string expected) {
		Assert.Equal(expected, AuthSetup.SafeReturnUrl(input));
	}

	private sealed class StaticMonitor(AuthOptions value) : IOptionsMonitor<AuthOptions>
	{
		public AuthOptions CurrentValue => value;
		public AuthOptions Get(string? name) => value;
		public IDisposable? OnChange(Action<AuthOptions, string?> listener) => null;
	}
}
