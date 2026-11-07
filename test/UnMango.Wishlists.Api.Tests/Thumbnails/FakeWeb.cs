using System.Net;
using System.Text;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

/// <summary>A pretend internet: canned responses by absolute URL, 404 for anything else.</summary>
public sealed class FakeWeb : HttpMessageHandler
{
	private readonly Dictionary<string, Func<HttpResponseMessage>> _routes = new();
	private readonly List<string> _requests = [];

	public IReadOnlyList<string> Requests {
		get { lock (_requests) return _requests.ToList(); }
	}

	public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];

	public FakeWeb Route(string url, Func<HttpResponseMessage> respond) {
		lock (_routes) _routes[url] = respond;
		return this;
	}

	public FakeWeb Html(string url, string html) => Route(url, () => new HttpResponseMessage(HttpStatusCode.OK) {
		Content = new StringContent(html, Encoding.UTF8, "text/html"),
	});

	public FakeWeb ShopPage(string url, string imageUrl) =>
		Html(url, $"""<html><head><meta property="og:image" content="{imageUrl}"></head></html>""");

	public FakeWeb Image(string url, byte[] bytes, string contentType) => Route(url, () => {
		var content = new ByteArrayContent(bytes);
		content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
		return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
	});

	public FakeWeb Redirect(string from, string to) => Route(from, () => new HttpResponseMessage(HttpStatusCode.Found) {
		Headers = { Location = new Uri(to, UriKind.RelativeOrAbsolute) },
	});

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
		var url = request.RequestUri!.AbsoluteUri;
		lock (_requests) _requests.Add(url);
		Func<HttpResponseMessage>? respond;
		lock (_routes) _routes.TryGetValue(url, out respond);
		return Task.FromResult(respond?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.NotFound));
	}
}
