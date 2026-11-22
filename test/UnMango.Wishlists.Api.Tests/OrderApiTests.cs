using System.Net;
using System.Net.Http.Json;
using System.Text;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using UnMango.Wishlists.Api.Api;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Tests;

public sealed class OrderApiTests(AppFixture app)
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private const string Order = "/api/wishlists/mine/order";

	private static ItemRequest Item(string name) => new(name, null, null, null, ReallyWant: false);

	private static async Task<OwnItem> AddAsync(HttpClient client, string name) =>
		(await (await client.PostAsJsonAsync("/api/wishlists/mine/items", Item(name), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct))!;

	private static async Task<IEnumerable<string>> MineAsync(HttpClient client) =>
		(await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct))!.Items.Select(x => x.Name);

	private static async Task<Guid> WishlistIdAsync(HttpClient client) =>
		(await client.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;

	[Fact]
	public async Task A_new_item_goes_on_top() {
		var (_, ada) = await app.JoinAsync("Ada");

		await AddAsync(ada, "Socks");
		await AddAsync(ada, "Hat");

		Assert.Equal(["Hat", "Socks"], await MineAsync(ada));
	}

	[Fact]
	public async Task An_import_goes_on_top_in_file_order() {
		var (_, ada) = await app.JoinAsync("Ada");
		await AddAsync(ada, "Socks");

		await ada.PostAsJsonAsync("/api/wishlists/mine/items/import", new ImportRequest([Item("Hat"), Item("Scarf")]), Ct);

		Assert.Equal(["Hat", "Scarf", "Socks"], await MineAsync(ada));
	}

	[Fact]
	public async Task Reorder_saves_the_order_everyone_sees() {
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");
		var c = await AddAsync(ada, "C");

		var response = await ada.PutAsJsonAsync(Order, new ReorderRequest([a.Id, c.Id, b.Id]), Ct);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var saved = await response.Content.ReadFromJsonAsync<OwnWishlist>(Ct);
		Assert.Equal(["A", "C", "B"], saved!.Items.Select(x => x.Name));
		Assert.Equal(["A", "C", "B"], await MineAsync(ada));
		var seenByBob = await bob.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{await WishlistIdAsync(ada)}", Ct);
		Assert.Equal(["A", "C", "B"], seenByBob!.Items.Select(x => x.Name));
	}

	[Fact]
	public async Task The_last_reorder_wins() {
		var (_, ada) = await app.JoinAsync("Ada");
		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");

		await ada.PutAsJsonAsync(Order, new ReorderRequest([a.Id, b.Id]), Ct);
		await ada.PutAsJsonAsync(Order, new ReorderRequest([b.Id, a.Id]), Ct);

		Assert.Equal(["B", "A"], await MineAsync(ada));
	}

	[Fact]
	public async Task Reorders_saved_at_the_same_time_both_succeed() {
		var (_, ada) = await app.JoinAsync("Ada");
		var ids = new List<Guid>();
		for (var i = 0; i < 5; i++) ids.Add((await AddAsync(ada, $"Item {i}")).Id);

		var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(i =>
			ada.PutAsJsonAsync(Order, new ReorderRequest([..ids.Skip(i), ..ids.Take(i)]), Ct)));

		Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
		Assert.Equal(5, (await MineAsync(ada)).Count());
	}

	[Fact]
	public async Task Items_left_out_of_a_reorder_stay_on_top_in_their_order() {
		// Say the owner added C on another phone while this one was reordering A and B.
		var (_, ada) = await app.JoinAsync("Ada");
		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");
		await AddAsync(ada, "C");
		await AddAsync(ada, "D");

		await ada.PutAsJsonAsync(Order, new ReorderRequest([a.Id, b.Id, a.Id, Guid.NewGuid()]), Ct);

		Assert.Equal(["D", "C", "A", "B"], await MineAsync(ada));
	}

	[Fact]
	public async Task A_removed_item_a_gift_giver_bought_keeps_its_spot() {
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var adaList = await WishlistIdAsync(ada);
		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");
		var c = await AddAsync(ada, "C");
		await bob.PutAsync($"/api/wishlists/{adaList}/items/{b.Id}/bought", null, Ct);
		await ada.DeleteAsync($"/api/wishlists/mine/items/{b.Id}", Ct);

		var response = await ada.PutAsJsonAsync(Order, new ReorderRequest([a.Id, c.Id]), Ct);

		var body = await response.Content.ReadAsStringAsync(Ct);
		Assert.DoesNotContain(b.Id.ToString(), body);
		Assert.DoesNotContain("bought", body, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(["A", "C"], await MineAsync(ada));
		var seenByBob = await bob.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{adaList}", Ct);
		Assert.Equal(["A", "B", "C"], seenByBob!.Items.Select(x => x.Name));
	}

	[Fact]
	public async Task Reorder_only_changes_your_own_wishlist() {
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");
		await AddAsync(bob, "Bob's");

		var response = await bob.PutAsJsonAsync(Order, new ReorderRequest([a.Id, b.Id]), Ct);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(["B", "A"], await MineAsync(ada));
		Assert.Equal(["Bob's"], await MineAsync(bob));
	}

	[Fact]
	public async Task A_reorder_has_at_most_1000_items() {
		var (_, ada) = await app.JoinAsync("Ada");

		var response = await ada.PutAsJsonAsync(
			Order, new ReorderRequest(Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToList()), Ct);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Contains("itemIds", await response.Content.ReadAsStringAsync(Ct));
	}

	[Fact]
	public async Task Reorder_requires_sign_in() {
		var response = await app.Anonymous().PutAsJsonAsync(Order, new ReorderRequest([]), Ct);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Items_stored_before_the_order_existed_keep_oldest_first() {
		var (_, ada) = await app.JoinAsync("Ada");
		var adaList = await WishlistIdAsync(ada);
		var store = app.Services.GetRequiredService<IDocumentStore>();

		// Stored exactly as releases before the order wrote them: no position field.
		await using (var session = store.LightweightSession()) {
			session.Events.Append(adaList, StoredItemAdded("Old first"), StoredItemAdded("Old second"));
			await session.SaveChangesAsync(Ct);
		}

		await AddAsync(ada, "New");

		Assert.Equal(["New", "Old first", "Old second"], await MineAsync(ada));
	}

	[Fact]
	public async Task Replaying_the_events_gives_the_same_order() {
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var adaList = await WishlistIdAsync(ada);
		var store = app.Services.GetRequiredService<IDocumentStore>();
		await using (var session = store.LightweightSession()) {
			session.Events.Append(adaList, StoredItemAdded("Old"));
			await session.SaveChangesAsync(Ct);
		}

		var a = await AddAsync(ada, "A");
		var b = await AddAsync(ada, "B");
		await ada.PostAsJsonAsync("/api/wishlists/mine/items/import", new ImportRequest([Item("C"), Item("D")]), Ct);
		await bob.PutAsync($"/api/wishlists/{adaList}/items/{b.Id}/bought", null, Ct);
		await ada.DeleteAsync($"/api/wishlists/mine/items/{b.Id}", Ct);
		await ada.PutAsJsonAsync(Order, new ReorderRequest([a.Id]), Ct);
		await AddAsync(ada, "E");

		await using var query = store.QuerySession();
		var stored = await query.LoadAsync<WishlistView>(adaList, Ct);
		var replayed = await query.Events.AggregateStreamAsync<WishlistView>(adaList, token: Ct);
		Assert.Equal(["E", "C", "D", "B", "Old", "A"], stored!.Items.Select(x => x.Name));
		Assert.Equal(stored.Items.Select(x => x.Name), replayed!.Items.Select(x => x.Name));
	}

	/// <summary>An <see cref="ItemAdded" /> as an older release stored it, read back through Marten's serializer.</summary>
	private ItemAdded StoredItemAdded(string name) {
		var json = $$"""{"Link":null,"Name":"{{name}}","Notes":null,"Price":null,"ItemId":"{{Guid.CreateVersion7()}}","ReallyWant":false}""";
		var serializer = app.Services.GetRequiredService<IDocumentStore>().Options.Serializer();
		return serializer.FromJson<ItemAdded>(new MemoryStream(Encoding.UTF8.GetBytes(json)));
	}
}
