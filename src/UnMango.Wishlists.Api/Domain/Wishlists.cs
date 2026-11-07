using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

public sealed record WishlistCreated(Guid WishlistId, Guid OwnerId, string Name, Guid PurchasesId);

public sealed record WishlistRenamed(string Name);

public sealed record ItemAdded(
	Guid ItemId,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant);

public sealed record ItemEdited(
	Guid ItemId,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant);

public sealed record ItemRemoved(Guid ItemId);

public sealed record WishlistItem(
	Guid Id,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant,
	bool Removed = false);

public sealed record WishlistView(
	Guid Id,
	Guid OwnerId,
	string Name,
	Guid PurchasesId,
	IReadOnlyList<WishlistItem> Items)
{
	// Removed items are kept so a gift-giver who already bought one can still see it.
	[JsonIgnore]
	public IEnumerable<WishlistItem> CurrentItems => Items.Where(x => !x.Removed);

	public WishlistItem? FindCurrent(Guid itemId) => CurrentItems.FirstOrDefault(x => x.Id == itemId);

	public static WishlistView Create(WishlistCreated e) =>
		new(e.WishlistId, e.OwnerId, e.Name, e.PurchasesId, []);

	public WishlistView Apply(WishlistRenamed e) => this with { Name = e.Name };

	public WishlistView Apply(ItemAdded e) => this with {
		Items = [..Items, new WishlistItem(e.ItemId, e.Name, e.Link, e.Price, e.Notes, e.ReallyWant)],
	};

	public WishlistView Apply(ItemEdited e) => this with {
		Items = Items.Select(x => x.Id == e.ItemId
				? x with { Name = e.Name, Link = e.Link, Price = e.Price, Notes = e.Notes, ReallyWant = e.ReallyWant }
				: x)
			.ToList(),
	};

	public WishlistView Apply(ItemRemoved e) => this with {
		Items = Items.Select(x => x.Id == e.ItemId ? x with { Removed = true } : x).ToList(),
	};
}
