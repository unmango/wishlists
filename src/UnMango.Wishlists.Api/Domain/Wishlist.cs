using LanguageExt.Common;

namespace UnMango.Wishlists.Api.Domain;

internal sealed record Wishlist : Entity
{
	private readonly List<Item> _items = [];

	public required string Name { get; init; }

	public required User Owner { get; init; }

	public IReadOnlyCollection<Item> Items => _items;

	public Result<Added> Add(Item item) {
		_items.Add(item);
		return new Added(item);
	}

	public static Wishlist From(Create req) => new() {
		Id = Guid.CreateVersion7(),
		Name = req.Name,
		Owner = req.Owner,
	};

	public record Create(string Name, User Owner);
	public record Added(Item Item);
}
