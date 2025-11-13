namespace UnMango.Wishlists.Api.Domain;

internal sealed record Item
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required string Note { get; init; }

	public required bool Purchased { get; init; }
}
