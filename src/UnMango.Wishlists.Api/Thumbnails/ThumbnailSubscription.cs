using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Marten.Subscriptions;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Thumbnails;

/// <summary>
/// Looks for a thumbnail whenever an item gets a new link, in the background on the async daemon.
/// It reads from the first event, so it back-fills items saved before the thumbnails feature.
/// One attempt per link: it neither records nor retries misses.
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

			Uri? image;
			try {
				image = await _client.FindAsync(link, cancellationToken);
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				// A surprise in one page mustn't pause the subscription and hold up every later lookup.
				_logger.LogWarning(ex, "Couldn't look up a thumbnail for item {ItemId}", itemId);
				continue;
			}

			_logger.LogInformation("Thumbnail lookup for item {ItemId}: {Result}", itemId, image is null ? "none" : "found");
			if (image is null) continue;

			// Appended in its own session, not through `operations`, so the inline WishlistView projection sees it.
			// If something stops the daemon before it saves its progress, the next run sees the thumbnail and skips the item.
			try {
				await using var session = operations.DocumentStore.LightweightSession();
				session.Events.Append(e.StreamId, new ItemThumbnailFound(itemId, link, image.AbsoluteUri));
				await session.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				// Most likely a concurrent write to the same stream. Losing one thumbnail beats holding up every later lookup.
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
