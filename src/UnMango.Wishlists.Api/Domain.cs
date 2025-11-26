using System.Collections.Immutable;

namespace UnMango.Wishlists.Api;

internal record WishlistCreated(Guid UserId, string Name);

internal record Wishlist(string Name);

internal sealed record User(Guid Id, string Name, IImmutableList<Wishlist> Wishlists)
{
	public static User Apply(WishlistCreated created, User user) =>
		user with { Wishlists = user.Wishlists.Add(new(created.Name)) };
};

internal static class Api {
    extension(IEndpointRouteBuilder routes) {
        public void MapGetUser() {

        }
    }
}
