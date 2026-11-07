using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace UnMango.Wishlists.Api.Thumbnails;

public sealed record ThumbnailImage(string ContentType, byte[] Bytes);

/// <summary>
/// Looks at an item's link for a thumbnail, and downloads it. Every miss (an error, a timeout, a page
/// with no picture, something too big or not an image) comes back as null, never as an exception.
/// </summary>
public sealed class ThumbnailClient(HttpClient http)
{
	public const string HttpClientName = "Thumbnails";
	public const int MaxRedirects = 3;
	public const int MaxPageBytes = 1024 * 1024;
	public const int MaxImageBytes = 5 * 1024 * 1024;

	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

	/// <summary>Sets the honest User-Agent and the overall time limit.</summary>
	public static HttpClient Configure(HttpClient http) {
		http.Timeout = DefaultTimeout;
		http.DefaultRequestHeaders.UserAgent.ParseAdd("UnMango.Wishlists/1.0 (+https://github.com/unmango/wishlists)");
		return http;
	}

	/// <summary>The picture the page at <paramref name="link" /> advertises, if any.</summary>
	public Task<Uri?> FindAsync(string link, CancellationToken cancellationToken) =>
		Guard(cancellationToken, async ct => {
			var fetched = await GetAsync(link, "text/html,application/xhtml+xml", ct);
			if (fetched is not var (response, finalUri)) return null;
			using var _ = response;

			var mediaType = response.Content.Headers.ContentType?.MediaType;
			if (mediaType is not ("text/html" or "application/xhtml+xml")) return null;

			// The meta tags live in <head>, so a truncated page is fine.
			var (bytes, _) = await ReadAsync(response, MaxPageBytes, ct);
			return PageImage.Find(Decode(bytes, response.Content.Headers.ContentType?.CharSet), finalUri);
		});

	/// <summary>Downloads an image, only if it is an allowed raster format by both its content type and its bytes.</summary>
	public Task<ThumbnailImage?> DownloadAsync(string url, CancellationToken cancellationToken) =>
		Guard(cancellationToken, async ct => {
			var fetched = await GetAsync(url, "image/avif,image/webp,image/png,image/jpeg,image/gif", ct);
			if (fetched is not var (response, _)) return null;
			using var _ = response;

			var headers = response.Content.Headers;
			if (!ImageType.IsAllowed(headers.ContentType?.ToString())) return null;
			if (headers.ContentLength > MaxImageBytes) return null;

			var (bytes, truncated) = await ReadAsync(response, MaxImageBytes, ct);
			if (truncated) return null;

			return ImageType.Sniff(bytes) is { } contentType ? new ThumbnailImage(contentType, bytes) : null;
		});

	private async Task<T?> Guard<T>(CancellationToken cancellationToken, Func<CancellationToken, Task<T?>> fetch) where T : class {
		// HttpClient.Timeout stops at the headers; this covers reading the body too.
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		if (http.Timeout != Timeout.InfiniteTimeSpan) timeout.CancelAfter(http.Timeout);

		try {
			return await fetch(timeout.Token);
		} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
			return null;
		} catch (HttpRequestException) {
			return null;
		}
	}

	private async Task<(HttpResponseMessage Response, Uri Uri)?> GetAsync(string url, string accept, CancellationToken ct) {
		if (!TryHttpUri(null, url, out var uri)) return null;

		for (var redirects = 0; ; redirects++) {
			using var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.Accept.ParseAdd(accept);
			var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

			if (IsRedirect(response.StatusCode)) {
				var location = response.Headers.Location;
				response.Dispose();
				if (redirects == MaxRedirects || location is null || !TryHttpUri(uri, location.OriginalString, out uri)) {
					return null;
				}

				continue;
			}

			if (!response.IsSuccessStatusCode) {
				response.Dispose();
				return null;
			}

			return (response, uri);
		}
	}

	private static bool IsRedirect(HttpStatusCode status) => status is HttpStatusCode.MovedPermanently
		or HttpStatusCode.Found
		or HttpStatusCode.SeeOther
		or HttpStatusCode.TemporaryRedirect
		or HttpStatusCode.PermanentRedirect;

	private static bool TryHttpUri(Uri? baseUri, string value, out Uri uri) {
		var ok = baseUri is null
			? Uri.TryCreate(value, UriKind.Absolute, out uri!)
			: Uri.TryCreate(baseUri, value, out uri!);
		return ok && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
	}

	/// <summary>Reads at most <paramref name="max" /> bytes, and says whether there was more.</summary>
	private static async Task<(byte[] Bytes, bool Truncated)> ReadAsync(HttpResponseMessage response, int max, CancellationToken ct) {
		await using var stream = await response.Content.ReadAsStreamAsync(ct);
		var buffer = new byte[max + 1];
		var total = 0;
		while (total < buffer.Length) {
			var read = await stream.ReadAsync(buffer.AsMemory(total), ct);
			if (read == 0) break;
			total += read;
		}

		return total > max ? (buffer[..max], true) : (buffer[..total], false);
	}

	private static string Decode(byte[] bytes, string? charset) {
		Encoding encoding;
		try {
			encoding = charset is null ? Encoding.UTF8 : Encoding.GetEncoding(charset.Trim('"'));
		} catch (ArgumentException) {
			encoding = Encoding.UTF8;
		}

		return encoding.GetString(bytes);
	}
}
