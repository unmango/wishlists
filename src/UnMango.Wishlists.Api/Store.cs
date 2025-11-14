using Marten;

namespace UnMango.Wishlists.Api;

internal static class Store
{
	extension(Wishlist wishlist)
	{
		public static async Task<Guid> Create(IDocumentSession session, string name, CancellationToken cancellationToken) {
			var created = Wishlist.Create(name);
			var stream = session.Events.StartStream<Wishlist>(created);
			await session.SaveChangesAsync(cancellationToken);
			return stream.Id;
		}

		public static Task<IReadOnlyList<Wishlist>> List(IDocumentSession session, CancellationToken cancellationToken)
			=> session.Query<Wishlist>().ToListAsync(cancellationToken);

		public static Task<Wishlist?> Get(IDocumentSession session, Guid id, CancellationToken cancellationToken)
			=> session.Events.AggregateStreamAsync<Wishlist>(id, token: cancellationToken);
	}
}
