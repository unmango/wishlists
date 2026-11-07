namespace UnMango.Wishlists.Api.Domain;

// Bought marks live in their own stream so reading a wishlist for its owner never loads them.
public sealed record PurchasesOpened(Guid PurchasesId, Guid WishlistId);

public sealed record ItemMarkedBought(Guid ItemId, Guid BuyerId);

public sealed record ItemUnmarked(Guid ItemId, Guid BuyerId);

public sealed record PurchasesView(Guid Id, Guid WishlistId, IReadOnlyDictionary<Guid, Guid> Buyers)
{
	public Guid? BuyerOf(Guid itemId) => Buyers.TryGetValue(itemId, out var buyer) ? buyer : null;

	public static PurchasesView Create(PurchasesOpened e) =>
		new(e.PurchasesId, e.WishlistId, new Dictionary<Guid, Guid>());

	public PurchasesView Apply(ItemMarkedBought e) => this with {
		Buyers = new Dictionary<Guid, Guid>(Buyers) { [e.ItemId] = e.BuyerId },
	};

	public PurchasesView Apply(ItemUnmarked e) {
		var buyers = new Dictionary<Guid, Guid>(Buyers);
		buyers.Remove(e.ItemId);
		return this with { Buyers = buyers };
	}
}
