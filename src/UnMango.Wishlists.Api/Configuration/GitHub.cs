namespace UnMango.Wishlists.Api.Configuration;

internal sealed record GitHub(string ClientId = "", string ClientSecret = "")
{
	public static GitHub Bind(IConfiguration configuration) {
		var options = new GitHub();
		configuration.Bind(options);
		return options;
	}
}
