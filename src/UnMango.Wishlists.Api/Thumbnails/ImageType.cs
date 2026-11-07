using System.Net.Http.Headers;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>The raster image formats a thumbnail may be. SVG is never allowed, since it can carry script.</summary>
public static class ImageType
{
	private static readonly HashSet<string> Allowed = ["image/jpeg", "image/png", "image/webp", "image/gif", "image/avif"];

	public static bool IsAllowed(string? contentType) =>
		MediaTypeHeaderValue.TryParse(contentType, out var parsed)
		&& parsed.MediaType is { } mediaType
		&& Allowed.Contains(mediaType.ToLowerInvariant());

	/// <summary>The content type the bytes really are, from their magic number, or null when it isn't an allowed format.</summary>
	public static string? Sniff(ReadOnlySpan<byte> bytes) {
		if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return "image/jpeg";
		if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return "image/png";
		if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8)) return "image/gif";
		if (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
		if (bytes.Length >= 12 && bytes[4..8].SequenceEqual("ftyp"u8)
		    && (bytes[8..12].SequenceEqual("avif"u8) || bytes[8..12].SequenceEqual("avis"u8))) return "image/avif";
		return null;
	}
}
