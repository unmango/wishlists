using JasperFx;
using JasperFx.Events.Projections;
using Marten;
using UnMango.Wishlists.Api.Auth;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Storage;

/// <summary>How the app stores its events and views. Shared with the stored-events test, so both read events the same way.</summary>
public static class WishlistsStore
{
	/// <summary>Every view built from events. Each one is rebuilt on startup.</summary>
	public static readonly IReadOnlyList<Type> Views = [typeof(MemberView), typeof(WishlistView), typeof(PurchasesView)];

	public static void Configure(StoreOptions options, string connectionString) {
		options.Connection(connectionString);
		options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
		options.Projections.Snapshot<MemberView>(SnapshotLifecycle.Inline);
		options.Projections.Snapshot<WishlistView>(SnapshotLifecycle.Inline);
		options.Projections.Snapshot<PurchasesView>(SnapshotLifecycle.Inline);
		options.Schema.For<MemberView>().UniqueIndex(x => x.GoogleSubject);
		options.Schema.For<WishlistView>().Index(x => x.OwnerId);
		options.Schema.For<DataProtectionKey>().Identity(x => x.Id);
	}

	/// <summary>
	/// Throws the views away and replays every event into them, so they always match the events this release understands.
	/// Subscriptions like Thumbnails are left alone: they carry on from where they were.
	/// </summary>
	public static async Task RebuildViewsAsync(this IDocumentStore store, CancellationToken cancellationToken) {
		using var daemon = await store.BuildProjectionDaemonAsync();
		foreach (var view in Views) {
			await daemon.RebuildProjectionAsync(view, TimeSpan.FromMinutes(5), cancellationToken);
		}
	}
}

/// <summary>
/// Rebuilds the views before the app serves traffic or the async daemon starts. Registered ahead of every other hosted service.
/// With more than one replica, pods would rebuild over each other; revisit this then.
/// </summary>
internal sealed class RebuildViewsOnStartup(IDocumentStore store, ILogger<RebuildViewsOnStartup> logger) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken) {
		// Marten applies schema changes in a hosted service too, but this one has to run first.
		await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
		logger.LogInformation("Rebuilding views from events");
		await store.RebuildViewsAsync(cancellationToken);
		logger.LogInformation("Rebuilt views from events");
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
