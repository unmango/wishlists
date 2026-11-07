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

/// <summary>A thumbnail found on the page at <paramref name="Link" />, recorded in the background after the item was saved.</summary>
public sealed record ItemThumbnailFound(Guid ItemId, string Link, string ImageUrl);

public sealed record WishlistItem(
	Guid Id,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant,
	bool Removed = false,
	string? ThumbnailUrl = null);

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
				? x with {
					Name = e.Name,
					Link = e.Link,
					Price = e.Price,
					Notes = e.Notes,
					ReallyWant = e.ReallyWant,
					// The thumbnail belongs to the link: a new or cleared link takes it away.
					ThumbnailUrl = x.Link == e.Link ? x.ThumbnailUrl : null,
				}
				: x)
			.ToList(),
	};

	// A lookup that finishes after the link changed is for a link the item no longer has.
	public WishlistView Apply(ItemThumbnailFound e) => this with {
		Items = Items.Select(x => x.Id == e.ItemId && x.Link == e.Link ? x with { ThumbnailUrl = e.ImageUrl } : x).ToList(),
	};

	public WishlistView Apply(ItemRemoved e) => this with {
		Items = Items.Select(x => x.Id == e.ItemId ? x with { Removed = true } : x).ToList(),
	};
}
