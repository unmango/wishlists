namespace UnMango.Wishlists.Api.Api;

public sealed record Me(Guid Id, string Email, string DisplayName, string? AvatarUrl, Guid WishlistId);

public sealed record Member(Guid Id, string DisplayName, string? AvatarUrl, Guid WishlistId);

public sealed record ItemRequest(string Name, string? Link, decimal? Price, string? Notes, bool ReallyWant);

public sealed record RenameWishlist(string Name);

/// <summary>An item as its owner sees it. Deliberately has no bought fields.</summary>
public sealed record OwnItem(Guid Id, string Name, string? Link, decimal? Price, string? Notes, bool ReallyWant);

/// <summary>A wishlist as its owner sees it. Deliberately has no bought fields.</summary>
public sealed record OwnWishlist(Guid Id, string Name, IReadOnlyList<OwnItem> Items);

/// <summary>An item as a gift-giver sees it.</summary>
public sealed record GiftItem(
	Guid Id,
	string Name,
	string? Link,
	decimal? Price,
	string? Notes,
	bool ReallyWant,
	bool RemovedByOwner,
	Guid? BoughtBy,
	string? BoughtByName);

/// <summary>A wishlist as a gift-giver sees it.</summary>
public sealed record GiftWishlist(
	Guid Id,
	string Name,
	Guid OwnerId,
	string OwnerName,
	IReadOnlyList<GiftItem> Items);
