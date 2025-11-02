using Microsoft.AspNetCore.Identity;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class User : IdentityUser<Guid>
{
	public User() => Id = NewId();

	public User(string userName) : base(userName) => Id = NewId();

	private static Guid NewId() => Guid.CreateVersion7();
}
