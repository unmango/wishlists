namespace UnMango.Wishlists.Api.Domain;

internal sealed record Wishlist
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required User Owner { get; init; }

	public static Wishlist From(Create req) => new() {
		Id = Guid.CreateVersion7(),
		Name = req.Name,
		Owner = req.Owner,
	};

	public record Create(string Name, User Owner);
}
