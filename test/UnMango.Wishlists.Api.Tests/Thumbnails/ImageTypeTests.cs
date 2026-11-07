using UnMango.Wishlists.Api.Thumbnails;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

public sealed class ImageTypeTests
{
	public static TheoryData<byte[], string> Images => new() {
		{ [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0], "image/jpeg" },
		{ [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0], "image/png" },
		{ "GIF89a......"u8.ToArray(), "image/gif" },
		{ "GIF87a......"u8.ToArray(), "image/gif" },
		{ "RIFF\0\0\0\0WEBPVP8 "u8.ToArray(), "image/webp" },
		{ "\0\0\0\u001cftypavif\0\0\0\0"u8.ToArray(), "image/avif" },
	};

	[Theory]
	[MemberData(nameof(Images))]
	public void Sniffs_allowed_formats(byte[] bytes, string expected) {
		Assert.Equal(expected, ImageType.Sniff(bytes));
	}

	[Theory]
	[InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
	[InlineData("<?xml version=\"1.0\"?><svg/>")]
	[InlineData("<!doctype html><html></html>")]
	[InlineData("")]
	[InlineData("RIFF\0\0\0\0WAVEfmt ")]
	public void Rejects_everything_else(string content) {
		Assert.Null(ImageType.Sniff(System.Text.Encoding.UTF8.GetBytes(content)));
	}

	[Theory]
	[InlineData("image/jpeg", true)]
	[InlineData("image/png", true)]
	[InlineData("image/webp", true)]
	[InlineData("image/gif", true)]
	[InlineData("image/avif", true)]
	[InlineData("image/svg+xml", false)]
	[InlineData("text/html", false)]
	[InlineData(null, false)]
	public void Allows_only_raster_content_types(string? contentType, bool allowed) {
		Assert.Equal(allowed, ImageType.IsAllowed(contentType));
	}
}
