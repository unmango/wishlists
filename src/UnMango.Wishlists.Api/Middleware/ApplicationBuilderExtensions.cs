using System.Text;
using CliWrap;
using Microsoft.AspNetCore.SpaServices;

namespace UnMango.Wishlists.Api.Middleware;

internal static class ApplicationBuilderExtensions
{
	private static readonly Command Bun = new("bun");

	public static void UseWishlistsDevServer(this IApplicationBuilder builder) {
		var lifetime = builder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
		builder.UseSpa(spa => spa.UseWishlistsDevServer(lifetime));
	}

	private static void UseWishlistsDevServer(this ISpaBuilder builder, IHostApplicationLifetime lifetime) {
		builder.UseProxyToSpaDevelopmentServer(() => SpaUrlAsync(lifetime.ApplicationStopping));
	}

	private static async Task<Uri> SpaUrlAsync(CancellationToken cancellationToken) {
		var stdout = new StringBuilder();

		var command = await Bun.WithArguments("dev")
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
			.ExecuteAsync(cancellationToken);

		if (stdout.ToString().Contains("TODO"))
			throw new NotImplementedException();

		return new("");
	}
}
