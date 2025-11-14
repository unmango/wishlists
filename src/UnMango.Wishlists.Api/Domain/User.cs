using Microsoft.AspNetCore.Identity;

namespace UnMango.Wishlists.Api.Domain;

internal sealed class User : IdentityUser<Guid>
{
	public User() => Id = Entity.NewId();

	public User(string userName) : base(userName) => Id = Entity.NewId();

	public IReadOnlyCollection<Wishlist> Wishlists { get; init; } = [];
}
