using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using Microsoft.AspNetCore.SpaServices;

namespace UnMango.Wishlists.Api.Middleware;

internal static partial class ApplicationBuilderExtensions
{
	private static readonly Command BunDev = new Command("bun")
		.WithArguments("dev");

	[GeneratedRegex(@"Local:\s+(?<url>.*)")]
	private static partial Regex LocalUrlPattern { get; }

	public static void UseWishlistsDevServer(this IApplicationBuilder builder)
		=> builder.UseSpa(spa => spa.UseWishlistsDevServer(
			builder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>(),
			builder.ApplicationServices.GetRequiredService<ILoggerFactory>()));

	private static void UseWishlistsDevServer(
		this ISpaBuilder builder,
		IHostApplicationLifetime lifetime,
		ILoggerFactory loggerFactory)
		=> builder.UseProxyToSpaDevelopmentServer(() =>
			SpaUrlAsync(
				loggerFactory.CreateLogger(nameof(BunDev)),
				lifetime.ApplicationStopping));

	private static async Task<Uri> SpaUrlAsync(ILogger logger, CancellationToken cancellationToken) {
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();

		logger.LogInformation("Starting bun dev server");
		var bunDevTask = BunDev
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(stderr))
			.ExecuteAsync(cancellationToken);

		logger.LogInformation("Waiting for the url");
		var urlTask = UrlAsync(stdout, cancellationToken);

		if (urlTask == await Task.WhenAny(bunDevTask, urlTask)) {
			var url = urlTask.Result;
			logger.LogInformation("Got bun dev url {Url}", url);
			return url;
		}

		if (logger.IsEnabled(LogLevel.Warning)) {
			logger.LogWarning(
				"bun task completed before a url was matched {Stdout} {Stderr} {Result}",
				stdout, stderr, await bunDevTask);
		}

		return new Uri("http://localhost:8080");
	}

	private static async Task<Uri> UrlAsync(StringBuilder stdout, CancellationToken cancellationToken) {
		var match = MatchLocalUrl(stdout);
		while (!match.Success && !cancellationToken.IsCancellationRequested) {
			await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
			match = MatchLocalUrl(stdout);
		}

		return new Uri(match.Groups["url"].Value);
	}

	private static Match MatchLocalUrl(StringBuilder builder)
		=> LocalUrlPattern.Match(builder.ToString());
}
