using System.Net;
using System.Net.Sockets;
using System.Text;
using UnMango.Wishlists.Api.Thumbnails;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

public sealed class ThumbnailClientTests
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private const string Page = "https://shop.example.com/socks";
	private const string ImageUrl = "https://cdn.example.com/socks.png";

	private readonly FakeWeb _web = new();

	private ThumbnailClient Client() => new(ThumbnailClient.Configure(new HttpClient(_web)));

	[Fact]
	public async Task Finds_the_image_on_a_page() {
		_web.ShopPage(Page, ImageUrl);

		Assert.Equal(new Uri(ImageUrl), await Client().FindAsync(Page, Ct));
	}

	[Fact]
	public async Task Sends_an_honest_user_agent() {
		var http = ThumbnailClient.Configure(new HttpClient(_web));

		Assert.Contains("UnMango.Wishlists", http.DefaultRequestHeaders.UserAgent.ToString());
	}

	[Fact]
	public async Task Follows_up_to_three_redirects() {
		_web.Redirect(Page, "https://shop.example.com/r1")
			.Redirect("https://shop.example.com/r1", "/r2")
			.Redirect("https://shop.example.com/r2", "https://www.example.com/r3")
			.ShopPage("https://www.example.com/r3", "/socks.png");

		Assert.Equal(new Uri("https://www.example.com/socks.png"), await Client().FindAsync(Page, Ct));
	}

	[Fact]
	public async Task Gives_up_after_three_redirects() {
		_web.Redirect(Page, "https://shop.example.com/r1")
			.Redirect("https://shop.example.com/r1", "https://shop.example.com/r2")
			.Redirect("https://shop.example.com/r2", "https://shop.example.com/r3")
			.Redirect("https://shop.example.com/r3", "https://shop.example.com/r4")
			.ShopPage("https://shop.example.com/r4", ImageUrl);

		Assert.Null(await Client().FindAsync(Page, Ct));
		Assert.DoesNotContain("https://shop.example.com/r4", _web.Requests);
	}

	[Theory]
	[InlineData("ftp://shop.example.com/socks")]
	[InlineData("file:///etc/passwd")]
	public async Task Refuses_redirects_to_other_schemes(string target) {
		_web.Redirect(Page, target);

		Assert.Null(await Client().FindAsync(Page, Ct));
		Assert.Single(_web.Requests);
	}

	[Theory]
	[InlineData("ftp://shop.example.com/socks")]
	[InlineData("not a url")]
	public async Task Refuses_links_that_are_not_http(string link) {
		Assert.Null(await Client().FindAsync(link, Ct));
		Assert.Empty(_web.Requests);
	}

	[Fact]
	public async Task Ignores_pages_that_are_not_html() {
		_web.Route(Page, () => new HttpResponseMessage(HttpStatusCode.OK) {
			Content = new StringContent($"""<meta property="og:image" content="{ImageUrl}">""", Encoding.UTF8, "application/json"),
		});

		Assert.Null(await Client().FindAsync(Page, Ct));
	}

	[Fact]
	public async Task Reads_at_most_a_megabyte_of_html() {
		var padding = new string(' ', ThumbnailClient.MaxPageBytes);
		_web.Html(Page, $"""<html><head>{padding}<meta property="og:image" content="{ImageUrl}"></head></html>""");

		Assert.Null(await Client().FindAsync(Page, Ct));
	}

	[Fact]
	public async Task Errors_and_timeouts_find_nothing() {
		_web.Route(Page, () => throw new HttpRequestException("connection refused"));
		Assert.Null(await Client().FindAsync(Page, Ct));

		var slow = new HttpClient(new SlowHandler()) { Timeout = TimeSpan.FromMilliseconds(100) };
		Assert.Null(await new ThumbnailClient(slow).FindAsync(Page, Ct));
	}

	[Fact]
	public async Task Downloads_an_allowed_image() {
		_web.Image(ImageUrl, FakeWeb.Png, "image/png");

		var image = await Client().DownloadAsync(ImageUrl, Ct);

		Assert.NotNull(image);
		Assert.Equal("image/png", image.ContentType);
		Assert.Equal(FakeWeb.Png, image.Bytes);
	}

	[Fact]
	public async Task Refuses_svg() {
		_web.Image(ImageUrl, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), "image/svg+xml");

		Assert.Null(await Client().DownloadAsync(ImageUrl, Ct));
	}

	[Fact]
	public async Task Refuses_content_that_is_not_what_it_claims() {
		_web.Image(ImageUrl, "<html><script>alert(1)</script></html>"u8.ToArray(), "image/png");

		Assert.Null(await Client().DownloadAsync(ImageUrl, Ct));
	}

	[Fact]
	public async Task Refuses_images_over_five_megabytes() {
		var big = new byte[ThumbnailClient.MaxImageBytes + 1];
		FakeWeb.Png.CopyTo(big, 0);
		_web.Image(ImageUrl, big, "image/png");

		Assert.Null(await Client().DownloadAsync(ImageUrl, Ct));
	}

	[Fact]
	public async Task Refuses_failed_responses() {
		Assert.Null(await Client().DownloadAsync(ImageUrl, Ct));
	}

	[Theory]
	[InlineData("127.0.0.1")]
	[InlineData("localhost")]
	public async Task The_guarded_handler_never_connects_to_local_addresses(string host) {
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		using var http = new HttpClient(GuardedHandler.Create());
		var client = new ThumbnailClient(ThumbnailClient.Configure(http));

		Assert.Null(await client.FindAsync($"http://{host}:{port}/", Ct));
		Assert.Null(await client.DownloadAsync($"http://{host}:{port}/socks.png", Ct));
		Assert.False(listener.Pending());
	}

	private sealed class SlowHandler : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}
