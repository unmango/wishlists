namespace UnMango.Wishlists.Api.Domain;

internal abstract record Entity
{
	public Guid Id { get; init; }

	public static Guid NewId() => Guid.CreateVersion7();
}
