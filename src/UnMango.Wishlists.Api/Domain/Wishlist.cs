namespace UnMango.Wishlists.Api.Domain;

internal sealed record Wishlist
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required User Owner { get; init; }
}
