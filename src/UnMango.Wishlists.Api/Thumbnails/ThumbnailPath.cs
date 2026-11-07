using System.Security.Cryptography;
using System.Text;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Thumbnails;

public static class ThumbnailPath
{
	/// <summary>
	/// Where the front end loads an item's thumbnail. The version changes with the image, so a browser
	/// that cached the old one for a day still shows the new one straight away. The server ignores it.
	/// </summary>
	public static string? For(WishlistItem item) => item.ThumbnailUrl is { } url
		? $"/api/items/{item.Id}/thumbnail?v={Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..12]}"
		: null;
}
