using JasperFx;
using Microsoft.AspNetCore.Diagnostics;

namespace UnMango.Wishlists.Api.Api;

/// <summary>Two people changed the same list at once. The client should reload and try again.</summary>
internal sealed class ConcurrencyExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken) {
		if (exception is not ConcurrencyException) return false;

		context.Response.StatusCode = StatusCodes.Status409Conflict;
		return await problemDetails.TryWriteAsync(new ProblemDetailsContext {
			HttpContext = context,
			Exception = exception,
			ProblemDetails = { Title = "Someone else changed this at the same time. Reload and try again." },
		});
	}
}
