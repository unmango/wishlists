using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

public sealed record WishlistCreated(Guid WishlistId, Guid OwnerId, string Name, Guid PurchasesId);

public sealed record WishlistRenamed(string Name);

/// <param name="Position">
/// The item's place in the order, counting every stored item (removed ones too) from 0 at the top.
/// Items that older releases stored have none, and applying them puts them at the bottom, so old wishlists keep oldest-first.
/// </param>
public sealed record ItemAdded(
	Guid ItemId,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant,
	int? Position = null);

public sealed record ItemEdited(
	Guid ItemId,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant);

public sealed record ItemRemoved(Guid ItemId);

/// <summary>
/// The owner rearranged their current items, top first. Applying it is part of the stored contract: it ignores ids that
/// aren't current items, puts current items it leaves out on top in their order, and keeps removed items in their slots.
/// </summary>
public sealed record ItemsReordered(IReadOnlyList<Guid> ItemIds);

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

	/// <summary>Finds an item even if the owner removed it.</summary>
	public WishlistItem? Find(Guid itemId) => Items.FirstOrDefault(x => x.Id == itemId);

	public static WishlistView Create(WishlistCreated e) =>
		new(e.WishlistId, e.OwnerId, e.Name, e.PurchasesId, []);

	public WishlistView Apply(WishlistRenamed e) => this with { Name = e.Name };

	public WishlistView Apply(ItemAdded e) {
		var item = new WishlistItem(e.ItemId, e.Name, e.Link, e.Price, e.Notes, e.ReallyWant);
		var index = Math.Clamp(e.Position ?? Items.Count, 0, Items.Count);
		return this with { Items = [..Items.Take(index), item, ..Items.Skip(index)] };
	}

	/// <summary>
	/// The event that puts current items in the order of <paramref name="itemIds" />. It drops ids that aren't current items and
	/// puts current items left out on top in their order: most likely the owner added them on another phone mid-reorder.
	/// </summary>
	public ItemsReordered Reorder(IEnumerable<Guid> itemIds) {
		var current = CurrentItems.Select(x => x.Id).ToList();
		var listed = itemIds.Distinct().Where(current.Contains).ToList();
		return new ItemsReordered([..current.Except(listed), ..listed]);
	}

	// Removed items keep their slots, so a gift-giver still sees them in their old places; current items fill the rest in the new order.
	public WishlistView Apply(ItemsReordered e) {
		var byId = CurrentItems.ToDictionary(x => x.Id);
		var slots = Items.Index().Where(x => !x.Item.Removed).Select(x => x.Index);
		var placed = slots.Zip(Reorder(e.ItemIds).ItemIds, (slot, id) => (slot, item: byId[id]))
			.ToDictionary(x => x.slot, x => x.item);
		return this with { Items = Items.Select((x, i) => placed.GetValueOrDefault(i, x)).ToList() };
	}

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
