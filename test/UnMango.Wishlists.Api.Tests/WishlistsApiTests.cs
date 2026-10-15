using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UnMango.Wishlists.Api.Api;

namespace UnMango.Wishlists.Api.Tests;

public sealed class WishlistsApiTests(AppFixture app)
{
	private static readonly CancellationToken Ct = CancellationToken.None;

	private static ItemRequest Item(string name = "Socks") =>
		new(name, "https://example.com/socks", 12.50m, "Wool, size L", ReallyWant: true);

	[Fact]
	public async Task Api_requires_sign_in() {
		var response = await app.Anonymous().GetAsync("/api/me", Ct);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task First_sign_in_creates_a_named_wishlist() {
		var (memberId, client) = await app.JoinAsync("Ada");

		var me = await client.GetFromJsonAsync<Me>("/api/me", Ct);
		var mine = await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct);

		Assert.Equal(memberId, me!.Id);
		Assert.Equal(me.WishlistId, mine!.Id);
		Assert.Equal("Ada's wishlist", mine.Name);
		Assert.Empty(mine.Items);
	}

	[Fact]
	public async Task Members_lists_everyone_with_their_wishlist() {
		var (adaId, ada) = await app.JoinAsync("Ada");
		var (bobId, _) = await app.JoinAsync("Bob");

		var members = await ada.GetFromJsonAsync<List<Member>>("/api/members", Ct);

		Assert.Contains(members!, x => x.Id == adaId);
		Assert.Contains(members!, x => x.Id == bobId);
	}

	[Fact]
	public async Task Owner_can_add_edit_remove_and_rename() {
		var (_, client) = await app.JoinAsync("Ada");

		var created = await client.PostAsJsonAsync("/api/wishlists/mine/items", Item(), Ct);
		Assert.Equal(HttpStatusCode.Created, created.StatusCode);
		var item = await created.Content.ReadFromJsonAsync<OwnItem>(Ct);

		var edited = await client.PutAsJsonAsync($"/api/wishlists/mine/items/{item!.Id}", Item("Fancy socks"), Ct);
		Assert.Equal(HttpStatusCode.OK, edited.StatusCode);

		var renamed = await client.PutAsJsonAsync("/api/wishlists/mine", new RenameWishlist("Ada's Christmas"), Ct);
		Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

		var mine = await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct);
		Assert.Equal("Ada's Christmas", mine!.Name);
		Assert.Equal("Fancy socks", Assert.Single(mine.Items).Name);

		var removed = await client.DeleteAsync($"/api/wishlists/mine/items/{item.Id}", Ct);
		Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
		Assert.Empty((await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct))!.Items);
	}

	[Fact]
	public async Task Invalid_items_are_rejected() {
		var (_, client) = await app.JoinAsync("Ada");

		var response = await client.PostAsJsonAsync(
			"/api/wishlists/mine/items",
			new ItemRequest(" ", "javascript:alert(1)", -1, null, false),
			Ct);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var body = await response.Content.ReadAsStringAsync(Ct);
		Assert.Contains("name", body);
		Assert.Contains("link", body);
		Assert.Contains("price", body);
	}

	[Fact]
	public async Task Gift_givers_see_who_bought_but_the_owner_never_does() {
		var (_, owner) = await app.JoinAsync("Ada");
		var (bobId, bob) = await app.JoinAsync("Bob");
		var (_, cy) = await app.JoinAsync("Cy");
		var wishlistId = (await owner.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;
		var item = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item(), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);

		var marked = await bob.PutAsync($"/api/wishlists/{wishlistId}/items/{item!.Id}/bought", null, Ct);
		Assert.Equal(HttpStatusCode.NoContent, marked.StatusCode);

		var seenByCy = await cy.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{wishlistId}", Ct);
		var giftItem = Assert.Single(seenByCy!.Items);
		Assert.Equal(bobId, giftItem.BoughtBy);
		Assert.Equal("Bob Test", giftItem.BoughtByName);

		// The owner's view has no bought fields at all, by id or through /mine.
		var byId = await owner.GetAsync($"/api/wishlists/{wishlistId}", Ct);
		Assert.Equal(HttpStatusCode.Redirect, byId.StatusCode);
		Assert.Equal("/api/wishlists/mine", byId.Headers.Location!.OriginalString);

		var mine = await owner.GetStringAsync("/api/wishlists/mine", Ct);
		Assert.DoesNotContain("bought", mine, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain(bobId.ToString(), mine);
	}

	[Fact]
	public async Task Owner_cannot_mark_or_unmark_their_own_items() {
		var (_, owner) = await app.JoinAsync("Ada");
		var wishlistId = (await owner.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;
		var item = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item(), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);

		var mark = await owner.PutAsync($"/api/wishlists/{wishlistId}/items/{item!.Id}/bought", null, Ct);
		var unmark = await owner.DeleteAsync($"/api/wishlists/{wishlistId}/items/{item.Id}/bought", Ct);

		Assert.Equal(HttpStatusCode.Forbidden, mark.StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, unmark.StatusCode);
	}

	[Fact]
	public async Task Only_one_person_can_buy_an_item_and_only_they_can_unmark_it() {
		var (_, owner) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var (_, cy) = await app.JoinAsync("Cy");
		var wishlistId = (await owner.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;
		var item = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item(), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);
		var bought = $"/api/wishlists/{wishlistId}/items/{item!.Id}/bought";

		Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsync(bought, null, Ct)).StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, (await bob.PutAsync(bought, null, Ct)).StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, (await cy.PutAsync(bought, null, Ct)).StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, (await cy.DeleteAsync(bought, Ct)).StatusCode);

		Assert.Equal(HttpStatusCode.NoContent, (await bob.DeleteAsync(bought, Ct)).StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, (await cy.PutAsync(bought, null, Ct)).StatusCode);
	}

	[Fact]
	public async Task Items_removed_after_being_bought_stay_visible_to_gift_givers() {
		var (_, owner) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var wishlistId = (await owner.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;
		var bought = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item("Bought"), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);
		var notBought = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item("Not bought"), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);

		await bob.PutAsync($"/api/wishlists/{wishlistId}/items/{bought!.Id}/bought", null, Ct);
		await owner.DeleteAsync($"/api/wishlists/mine/items/{bought.Id}", Ct);
		await owner.DeleteAsync($"/api/wishlists/mine/items/{notBought!.Id}", Ct);

		var seen = await bob.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{wishlistId}", Ct);
		var item = Assert.Single(seen!.Items);
		Assert.Equal(bought.Id, item.Id);
		Assert.True(item.RemovedByOwner);
	}

	[Fact]
	public async Task Owner_cannot_edit_someone_elses_items() {
		var (_, owner) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var item = await (await owner.PostAsJsonAsync("/api/wishlists/mine/items", Item(), Ct))
			.Content.ReadFromJsonAsync<OwnItem>(Ct);

		var response = await bob.PutAsJsonAsync($"/api/wishlists/mine/items/{item!.Id}", Item("Hacked"), Ct);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}
}
