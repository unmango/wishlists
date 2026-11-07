using System.Net;
using System.Text.RegularExpressions;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>Finds the picture a shop page advertises for sharing (og:image, then twitter:image).</summary>
public static partial class PageImage
{
	// Most specific first.
	private static readonly string[] Keys = ["og:image:secure_url", "og:image", "og:image:url", "twitter:image", "twitter:image:src"];

	public static Uri? Find(string html, Uri page) {
		var found = new Dictionary<string, string>();
		foreach (Match tag in MetaTag().Matches(html)) {
			string? key = null, content = null;
			foreach (Match attribute in Attribute().Matches(tag.Value)) {
				var name = attribute.Groups["name"].Value.ToLowerInvariant();
				var value = attribute.Groups["dq"].Success ? attribute.Groups["dq"].Value
					: attribute.Groups["sq"].Success ? attribute.Groups["sq"].Value
					: attribute.Groups["bare"].Value;
				if (name is "property" or "name") key = value.ToLowerInvariant();
				else if (name == "content") content = WebUtility.HtmlDecode(value).Trim();
			}

			if (key is not null && !string.IsNullOrEmpty(content)) found.TryAdd(key, content);
		}

		foreach (var key in Keys) {
			if (found.TryGetValue(key, out var value)
			    && Uri.TryCreate(page, value, out var uri)
			    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)) {
				return uri;
			}
		}

		return null;
	}

	[GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
	private static partial Regex MetaTag();

	[GeneratedRegex("""(?<name>[\w:-]+)\s*=\s*(?:"(?<dq>[^"]*)"|'(?<sq>[^']*)'|(?<bare>[^\s"'>]+))""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
	private static partial Regex Attribute();
}
