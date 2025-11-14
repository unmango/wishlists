namespace UnMango.Wishlists.Api.Domain;

internal sealed record Item : Entity
{
	public required string Name { get; init; }

	public required string? Note { get; init; }

	public required bool Purchased { get; init; }

	public required Wishlist Wishlist { get; init; }
}
