using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using UnMango.Wishlists.Api.Auth;

[assembly: AssemblyFixture(typeof(UnMango.Wishlists.Api.Tests.AppFixture))]

namespace UnMango.Wishlists.Api.Tests;

/// <summary>
/// Runs the API against a real Postgres. Set WISHLISTS_TEST_CONNECTION_STRING to use an existing
/// database; otherwise one is started with Testcontainers.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
	private PostgreSqlContainer? _container;
	private WebApplicationFactory<Program>? _factory;

	public async ValueTask InitializeAsync() {
		var connectionString = Environment.GetEnvironmentVariable("WISHLISTS_TEST_CONNECTION_STRING");
		if (string.IsNullOrEmpty(connectionString)) {
			_container = new PostgreSqlBuilder("postgres:18.6-trixie").Build();
			await _container.StartAsync();
			connectionString = _container.GetConnectionString();
		}

		_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
			.UseEnvironment("Testing")
			.UseSetting("ConnectionStrings:App", connectionString)
			.ConfigureTestServices(services => services
				.AddAuthentication(TestAuthHandler.Scheme)
				.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, null)));

		// Force startup so the schema exists before tests run.
		_ = _factory.Server;
	}

	public async ValueTask DisposeAsync() {
		if (_factory is not null) await _factory.DisposeAsync();
		if (_container is not null) await _container.DisposeAsync();
	}

	public IServiceProvider Services => _factory!.Services;

	/// <summary>A client with no signed-in member.</summary>
	public HttpClient Anonymous() => _factory!.CreateClient(new WebApplicationFactoryClientOptions {
		AllowAutoRedirect = false,
	});

	/// <summary>Signs a new family member in the same way Google sign-in does and returns a client for them.</summary>
	public async Task<(Guid MemberId, HttpClient Client)> JoinAsync(string name) {
		using var scope = Services.CreateScope();
		var signIn = scope.ServiceProvider.GetRequiredService<MemberSignIn>();
		var subject = Guid.NewGuid().ToString("N");
		var member = await signIn.FindOrJoinAsync(
			new GoogleProfile(subject, $"{name.ToLowerInvariant()}-{subject}@example.com", $"{name} Test", name, null),
			CancellationToken.None);

		var client = Anonymous();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.Scheme, member.Id.ToString());
		return (member.Id, client);
	}
}

internal sealed class TestAuthHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string Scheme = "Test";

	protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
		if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
		    || header.Scheme != Scheme
		    || header.Parameter is null) {
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var identity = new ClaimsIdentity([new Claim(MemberSignIn.MemberIdClaim, header.Parameter)], Scheme);
		return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
	}
}
