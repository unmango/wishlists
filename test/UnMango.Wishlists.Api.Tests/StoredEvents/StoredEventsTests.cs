using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marten;
using Marten.Events;
using Npgsql;
using UnMango.Wishlists.Api.Domain;
using UnMango.Wishlists.Api.Storage;

namespace UnMango.Wishlists.Api.Tests.StoredEvents;

/// <summary>
/// Stored events are contracts (docs/adr/0001-stored-events-are-contracts.md). <c>stored-events.json</c> holds events
/// exactly as Marten stored them, and every later release must still read them.
/// Never edit an example in that file: add a new one after it. The newest example of each type is the current shape.
/// </summary>
public sealed class StoredEventsTests(AppFixture app) : IAsyncLifetime
{
	private const string Schema = "stored_events";
	private static readonly CancellationToken Ct = CancellationToken.None;
	private static readonly Guid Ada = Guid.Parse("0195a000-0000-7000-8000-00000000a001");
	private static readonly Guid Bob = Guid.Parse("0195a000-0000-7000-8000-00000000b001");
	private static readonly Guid AdaWishlist = Guid.Parse("0195a000-0000-7000-8000-00000000a002");
	private static readonly Guid AdaPurchases = Guid.Parse("0195a000-0000-7000-8000-00000000a003");
	private static readonly Guid Socks = Guid.Parse("0195a000-0000-7000-8000-0000000000c1");
	private static readonly Guid Hat = Guid.Parse("0195a000-0000-7000-8000-0000000000c2");
	private static readonly Guid Scarf = Guid.Parse("0195a000-0000-7000-8000-0000000000c3");

	private static readonly JsonSerializerOptions Readable = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
	private static readonly StoredStream[] Streams = Load();
	private DocumentStore _store = null!;

	public async ValueTask InitializeAsync() {
		var connectionString = app.ConnectionString;
		await using (var connection = new NpgsqlConnection(connectionString)) {
			await connection.OpenAsync(Ct);
			await using var drop = new NpgsqlCommand($"drop schema if exists {Schema} cascade", connection);
			await drop.ExecuteNonQueryAsync(Ct);
		}

		_store = DocumentStore.For(options => {
			WishlistsStore.Configure(options, connectionString);
			options.DatabaseSchemaName = Schema;
		});
		await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
	}

	public async ValueTask DisposeAsync() => await _store.DisposeAsync();

	[Fact]
	public async Task Stored_events_rebuild_into_the_views() {
		await AppendRawAsync();

		await _store.RebuildViewsAsync(Ct);

		await using var session = _store.QuerySession();
		var ada = await session.LoadAsync<MemberView>(Ada, Ct);
		var bob = await session.LoadAsync<MemberView>(Bob, Ct);
		var wishlist = await session.LoadAsync<WishlistView>(AdaWishlist, Ct);
		var purchases = await session.LoadAsync<PurchasesView>(AdaPurchases, Ct);

		Assert.Equal(new MemberView(Ada, "100000000000000000001", "ada@example.com", "Ada Example", "https://example.com/ada.png"), ada);
		Assert.Equal(new MemberView(Bob, "100000000000000000002", "bob@example.com", "Bob Example", null), bob);

		Assert.NotNull(wishlist);
		Assert.Equal((Ada, "Ada's winter list", AdaPurchases), (wishlist.OwnerId, wishlist.Name, wishlist.PurchasesId));
		Assert.Equal<WishlistItem>([
			new(Socks, "Socks", "https://shop.example.com/socks", 12.50m, "Wool, size L", true, ThumbnailUrl: "https://cdn.example.com/socks.png"),
			new(Scarf, "Scarf", null, null, null, false),
			new(Hat, "Wool hat", "https://shop.example.com/hat", 20m, "Grey", true, Removed: true),
		], wishlist.Items);

		Assert.NotNull(purchases);
		Assert.Equal(AdaWishlist, purchases.WishlistId);
		Assert.Equal(new Dictionary<Guid, Guid> { [Socks] = Bob }, purchases.Buyers);
	}

	public static TheoryData<string> EventTypes() => new(AppliedEventTypes().Select(x => x.Name));

	[Theory]
	[MemberData(nameof(EventTypes))]
	public void Current_code_stores_events_as_the_newest_example(string name) {
		var type = AppliedEventTypes().Single(x => x.Name == name);
		var alias = ((EventGraph)_store.Options.Events).EventMappingFor(type).EventTypeName;
		var example = Streams.SelectMany(x => x.Events).LastOrDefault(x => x.Type == alias);
		Assert.True(example is not null,
			$"{name} is stored as '{alias}', and stored-events.json has no example of it. A new event needs an example; a renamed one needs MapEventType.");

		var serializer = _store.Options.Serializer();
		var read = serializer.FromJson(type, new MemoryStream(Encoding.UTF8.GetBytes(example.Data.ToJsonString())));
		var written = JsonNode.Parse(serializer.ToJson(read));

		Assert.True(JsonNode.DeepEquals(example.Data, written),
			$"{name} would now be stored as {written?.ToJsonString(Readable)}, not {example.Data.ToJsonString(Readable)}. "
			+ "If a field was added, add a new example after the old one. Never rename or remove a field (see ADR 0001).");
	}

	/// <summary>Every event a view applies. A new event type joins this list as soon as a view uses it.</summary>
	private static IEnumerable<Type> AppliedEventTypes() => WishlistsStore.Views
		.SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
		.Where(x => x.Name is "Create" or "Apply" && x.GetParameters().Length == 1)
		.Select(x => x.GetParameters()[0].ParameterType)
		.Distinct()
		.OrderBy(x => x.Name);

	private async Task AppendRawAsync() {
		await using var connection = new NpgsqlConnection(app.ConnectionString);
		await connection.OpenAsync(Ct);
		foreach (var stream in Streams) {
			await using var insertStream = new NpgsqlCommand(
				$"insert into {Schema}.mt_streams (id, type, version) values ($1, $2, $3)", connection) {
				Parameters = { new() { Value = stream.Id }, new() { Value = stream.Type }, new() { Value = (long)stream.Events.Length } },
			};
			await insertStream.ExecuteNonQueryAsync(Ct);

			for (var i = 0; i < stream.Events.Length; i++) {
				var e = stream.Events[i];
				await using var insertEvent = new NpgsqlCommand(
					$"""
					insert into {Schema}.mt_events (seq_id, id, stream_id, version, data, type, mt_dotnet_type, timestamp)
					values (nextval('{Schema}.mt_events_sequence'), gen_random_uuid(), $1, $2, $3::jsonb, $4, $5, now())
					""", connection) {
					Parameters = {
						new() { Value = stream.Id },
						new() { Value = (long)i + 1 },
						new() { Value = e.Data.ToJsonString() },
						new() { Value = e.Type },
						new() { Value = e.DotnetType },
					},
				};
				await insertEvent.ExecuteNonQueryAsync(Ct);
			}
		}
	}

	private static StoredStream[] Load() {
		var path = Path.Combine(AppContext.BaseDirectory, "StoredEvents", "stored-events.json");
		var file = JsonSerializer.Deserialize<StoredEventsFile>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web));
		return file!.Streams;
	}

	private sealed record StoredEventsFile(StoredStream[] Streams);

	private sealed record StoredStream(Guid Id, string Type, StoredEvent[] Events);

	private sealed record StoredEvent(string Type, string DotnetType, JsonObject Data);
}
