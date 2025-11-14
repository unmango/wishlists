namespace UnMango.Wishlists.Api;

internal sealed record User(Guid Id, string Name);

internal sealed record Wishlist(Guid Id, string Name, IEnumerable<Item> Items)
{
	public sealed record Created(Guid WishlistId, string Name);

	public sealed record ItemAdded(Guid WishlistId, string Name);

	public static Created Create(string name) => new(Domain.NewId(), name);

	public ItemAdded AddItem(string name) => new(Id, name);
}

internal sealed record Item(Guid Id, string Name);

internal static class Domain
{
	extension(User user)
	{
		public static User From(string name) => new(NewId(), name);
	}

	public static Guid NewId() => Guid.CreateVersion7();
}
