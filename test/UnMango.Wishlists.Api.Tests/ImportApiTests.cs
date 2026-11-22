using System.Net;
using System.Net.Http.Json;
using UnMango.Wishlists.Api.Api;

namespace UnMango.Wishlists.Api.Tests;

public sealed class ImportApiTests(AppFixture app)
{
	private static readonly CancellationToken Ct = CancellationToken.None;
	private const string Import = "/api/wishlists/mine/items/import";

	private static ItemRequest Item(string name, string? link = null) =>
		new(name, link, 12.50m, "Wool, size L", ReallyWant: true);

	private static async Task<OwnWishlist> MineAsync(HttpClient client) =>
		(await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct))!;

	[Fact]
	public async Task Import_adds_every_item_in_one_request() {
		var (_, ada) = await app.JoinAsync("Ada");

		var response = await ada.PostAsJsonAsync(Import, new ImportRequest([
			Item("Socks", "https://example.com/socks"),
			Item("Hat"),
		]), Ct);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var result = await response.Content.ReadFromJsonAsync<ImportResult>(Ct);
		Assert.Equal(["Socks", "Hat"], result!.Added.Select(x => x.Name));
		Assert.Equal(0, result.Skipped);

		var mine = await MineAsync(ada);
		Assert.Equal(["Socks", "Hat"], mine.Items.Select(x => x.Name));
		var socks = mine.Items[0];
		Assert.Equal("https://example.com/socks", socks.Link);
		Assert.Equal(12.50m, socks.Price);
		Assert.Equal("Wool, size L", socks.Notes);
		Assert.True(socks.ReallyWant);
	}

	[Fact]
	public async Task Importing_the_same_items_twice_adds_nothing_the_second_time() {
		var (_, ada) = await app.JoinAsync("Ada");
		var items = new ImportRequest([Item("Socks", "https://example.com/socks"), Item("Hat")]);
		await ada.PostAsJsonAsync(Import, items, Ct);

		var response = await ada.PostAsJsonAsync(Import, items, Ct);

		var result = await response.Content.ReadFromJsonAsync<ImportResult>(Ct);
		Assert.Empty(result!.Added);
		Assert.Equal(2, result.Skipped);
		Assert.Equal(2, (await MineAsync(ada)).Items.Count);
	}

	[Fact]
	public async Task Duplicates_match_by_link_or_by_name_when_neither_has_a_link() {
		var (_, ada) = await app.JoinAsync("Ada");
		await ada.PostAsJsonAsync("/api/wishlists/mine/items", Item("Socks", "https://example.com/socks"), Ct);
		await ada.PostAsJsonAsync("/api/wishlists/mine/items", Item("Hat"), Ct);

		var response = await ada.PostAsJsonAsync(Import, new ImportRequest([
			Item("Other name, same link", " https://example.com/socks "),
			Item(" hat "),
			Item("Socks"), // Same name, but the existing Socks has a link.
			Item("Hat", "https://example.com/hat"), // Same name, but this one has a link.
			Item("Scarf"),
			Item("scarf"), // A duplicate inside the file.
		]), Ct);

		var result = await response.Content.ReadFromJsonAsync<ImportResult>(Ct);
		Assert.Equal(["Socks", "Hat", "Scarf"], result!.Added.Select(x => x.Name));
		Assert.Equal(3, result.Skipped);
	}

	[Fact]
	public async Task Removed_items_are_not_duplicates() {
		var (_, ada) = await app.JoinAsync("Ada");
		var hat = await (await ada.PostAsJsonAsync("/api/wishlists/mine/items", Item("Hat"), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);
		await ada.DeleteAsync($"/api/wishlists/mine/items/{hat!.Id}", Ct);

		var response = await ada.PostAsJsonAsync(Import, new ImportRequest([Item("Hat")]), Ct);

		var result = await response.Content.ReadFromJsonAsync<ImportResult>(Ct);
		Assert.Single(result!.Added);
	}

	[Fact]
	public async Task One_invalid_item_rejects_the_whole_import_and_adds_nothing() {
		var (_, ada) = await app.JoinAsync("Ada");

		var response = await ada.PostAsJsonAsync(Import, new ImportRequest([
			Item("Socks"),
			Item("Hat", "javascript:alert(1)"),
			Item(" "),
		]), Ct);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var body = await response.Content.ReadAsStringAsync(Ct);
		Assert.Contains("items[1].link", body);
		Assert.Contains("items[2].name", body);
		Assert.Empty((await MineAsync(ada)).Items);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(201)]
	public async Task An_import_has_between_one_and_200_items(int count) {
		var (_, ada) = await app.JoinAsync("Ada");

		var response = await ada.PostAsJsonAsync(
			Import, new ImportRequest(Enumerable.Range(0, count).Select(i => Item($"Item {i}")).ToList()), Ct);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Contains("items", await response.Content.ReadAsStringAsync(Ct));
		Assert.Empty((await MineAsync(ada)).Items);
	}

	[Fact]
	public async Task An_import_of_200_items_is_allowed() {
		var (_, ada) = await app.JoinAsync("Ada");

		var response = await ada.PostAsJsonAsync(
			Import, new ImportRequest(Enumerable.Range(0, 200).Select(i => Item($"Item {i}")).ToList()), Ct);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(200, (await MineAsync(ada)).Items.Count);
	}

	[Fact]
	public async Task Import_never_touches_marks_and_never_shows_them_to_the_owner() {
		var (_, ada) = await app.JoinAsync("Ada");
		var (bobId, bob) = await app.JoinAsync("Bob");
		var adaList = (await ada.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;
		var socks = await (await ada.PostAsJsonAsync("/api/wishlists/mine/items", Item("Socks"), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);
		await bob.PutAsync($"/api/wishlists/{adaList}/items/{socks!.Id}/bought", null, Ct);

		var response = await ada.PostAsJsonAsync(Import, new ImportRequest([Item("Socks"), Item("Hat")]), Ct);

		var body = await response.Content.ReadAsStringAsync(Ct);
		Assert.DoesNotContain("bought", body, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(bobId.ToString(), body);
		var seenByBob = await bob.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{adaList}", Ct);
		Assert.Equal(bobId, seenByBob!.Items.Single(x => x.Id == socks.Id).BoughtBy);
		Assert.Equal(2, seenByBob.Items.Count);
	}

	[Fact]
	public async Task Import_requires_sign_in() {
		var response = await app.Anonymous().PostAsJsonAsync(Import, new ImportRequest([Item("Socks")]), Ct);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
