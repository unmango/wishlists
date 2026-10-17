namespace UnMango.Wishlists.Api.Auth;

internal sealed class AuthOptions
{
	public const string Section = "Auth";

	/// <summary>Emails allowed to sign in, for development or small setups.</summary>
	public List<string> AllowedEmails { get; set; } = [];

	/// <summary>
	/// A file with one allowed email per line, usually mounted from a Kubernetes Secret.
	/// Blank lines and lines starting with '#' are ignored. Re-read on every sign-in.
	/// </summary>
	public string? AllowedEmailsFile { get; set; }

	public GoogleOptions Google { get; set; } = new();

	public sealed class GoogleOptions
	{
		public string? ClientId { get; set; }
		public string? ClientSecret { get; set; }
	}
}
