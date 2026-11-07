using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>
/// Looks for a thumbnail whenever an item gets a new link, in the background on the async daemon.
/// It starts from the first event, so items saved before thumbnails existed are back-filled.
/// One attempt per link: misses aren't recorded or retried.
/// </summary>
public sealed class ThumbnailSubscription : SubscriptionBase
{
	private readonly ThumbnailClient _client;
	private readonly ILogger<ThumbnailSubscription> _logger;

	public ThumbnailSubscription(ThumbnailClient client, ILogger<ThumbnailSubscription> logger) {
		_client = client;
		_logger = logger;
		IncludeType<ItemAdded>();
		IncludeType<ItemEdited>();
	}

	public override async Task<IChangeListener> ProcessEventsAsync(
		EventRange page,
		ISubscriptionController controller,
		IDocumentOperations operations,
		CancellationToken cancellationToken
	) {
		foreach (var e in page.Events) {
			var (itemId, link) = e.Data switch {
				ItemAdded x => (x.ItemId, x.Link),
				ItemEdited x => (x.ItemId, x.Link),
				_ => (Guid.Empty, null),
			};

			if (link is null) continue;
			if (e.Data is ItemEdited && !await LinkChangedAsync(operations, e, itemId, link, cancellationToken)) continue;

			// Skip links the item no longer has, and items that already have a thumbnail.
			var wishlist = await operations.LoadAsync<WishlistView>(e.StreamId, cancellationToken);
			var item = wishlist?.FindCurrent(itemId);
			if (item is null || item.Link != link || item.ThumbnailUrl is not null) continue;

			var image = await _client.FindAsync(link, cancellationToken);
			_logger.LogInformation("Thumbnail lookup for item {ItemId}: {Result}", itemId, image is null ? "none" : "found");
			if (image is null) continue;

			// Appended in its own session, not through `operations`, so the inline WishlistView projection sees it.
			// If the daemon stops before saving its progress, the item already has a thumbnail next time and is skipped.
			try {
				await using var session = operations.DocumentStore.LightweightSession();
				session.Events.Append(e.StreamId, new ItemThumbnailFound(itemId, link, image.AbsoluteUri));
				await session.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				// Most likely the owner saved at the same moment. Losing one thumbnail beats stalling every later lookup.
				_logger.LogWarning(ex, "Couldn't record the thumbnail for item {ItemId}", itemId);
			}
		}

		return NullChangeListener.Instance;
	}

	/// <summary>An edit that keeps the same link keeps the thumbnail, so it doesn't look again.</summary>
	private static async Task<bool> LinkChangedAsync(
		IDocumentOperations operations,
		IEvent e,
		Guid itemId,
		string link,
		CancellationToken cancellationToken
	) {
		var before = await operations.Events.AggregateStreamAsync<WishlistView>(
			e.StreamId, version: e.Version - 1, token: cancellationToken);
		return before?.FindCurrent(itemId)?.Link != link;
	}
}
