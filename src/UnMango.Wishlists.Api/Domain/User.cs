using System.Collections.Immutable;

namespace UnMango.Wishlists.Api.Domain;

public record WishlistCreated(Guid WishlistId, string Name);

internal sealed record User(Guid Id, IImmutableList<Wishlist> Wishlists)
{

	public static User Apply(WishlistCreated created, User user) => user with {
		Wishlists = user.Wishlists.Add(Wishlist.From(created, user.Id)),
	};
}
