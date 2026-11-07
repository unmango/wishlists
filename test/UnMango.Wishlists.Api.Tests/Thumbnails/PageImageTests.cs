using UnMango.Wishlists.Api.Thumbnails;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

public sealed class PageImageTests
{
	private static readonly Uri Page = new("https://shop.example.com/products/socks");

	[Fact]
	public void Finds_og_image() {
		const string html = """
			<html><head>
			<meta property="og:title" content="Socks">
			<meta property="og:image" content="https://cdn.example.com/socks.jpg">
			</head></html>
			""";

		Assert.Equal(new Uri("https://cdn.example.com/socks.jpg"), PageImage.Find(html, Page));
	}

	[Fact]
	public void Handles_any_attribute_order_and_quotes() {
		const string html = "<META content='https://cdn.example.com/a.png' property='og:image' />";

		Assert.Equal(new Uri("https://cdn.example.com/a.png"), PageImage.Find(html, Page));
	}

	[Fact]
	public void Prefers_og_image_over_twitter_image() {
		const string html = """
			<meta name="twitter:image" content="https://cdn.example.com/twitter.png">
			<meta property="og:image:secure_url" content="https://cdn.example.com/og.png">
			""";

		Assert.Equal(new Uri("https://cdn.example.com/og.png"), PageImage.Find(html, Page));
	}

	[Fact]
	public void Falls_back_to_twitter_image() {
		const string html = """<meta name="twitter:image" content="https://cdn.example.com/twitter.png">""";

		Assert.Equal(new Uri("https://cdn.example.com/twitter.png"), PageImage.Find(html, Page));
	}

	[Fact]
	public void Resolves_relative_urls_and_decodes_entities() {
		const string html = """<meta property="og:image" content="/img/socks.jpg?w=400&amp;h=400">""";

		Assert.Equal(new Uri("https://shop.example.com/img/socks.jpg?w=400&h=400"), PageImage.Find(html, Page));
	}

	[Theory]
	[InlineData("""<meta property="og:image" content="javascript:alert(1)">""")]
	[InlineData("""<meta property="og:image" content="data:image/png;base64,AAAA">""")]
	[InlineData("""<meta property="og:image" content="file:///etc/passwd">""")]
	[InlineData("""<meta property="og:image" content="">""")]
	[InlineData("<p>No meta tags here</p>")]
	public void Ignores_missing_or_non_http_images(string html) {
		Assert.Null(PageImage.Find(html, Page));
	}
}
