using Microsoft.Extensions.Options;

namespace UnMango.Wishlists.Api.Auth;

internal sealed class AllowedEmails(IOptionsMonitor<AuthOptions> options, ILogger<AllowedEmails> logger)
{
	public bool Contains(string? email) {
		if (string.IsNullOrWhiteSpace(email)) return false;

		var normalized = email.Trim();
		return Load().Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
	}

	private IEnumerable<string> Load() {
		var current = options.CurrentValue;
		var fromFile = current.AllowedEmailsFile is { Length: > 0 } path
			? ReadFile(path)
			: [];

		return current.AllowedEmails
			.Concat(fromFile)
			.Select(x => x.Trim())
			.Where(x => x.Length > 0 && !x.StartsWith('#'));
	}

	private string[] ReadFile(string path) {
		try {
			return File.ReadAllLines(path);
		}
		catch (IOException ex) {
			logger.LogError(ex, "Unable to read allowed emails from {Path}", path);
			return [];
		}
		catch (UnauthorizedAccessException ex) {
			logger.LogError(ex, "Unable to read allowed emails from {Path}", path);
			return [];
		}
	}
}
