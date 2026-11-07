using System.Net;
using System.Net.Http.Json;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using UnMango.Wishlists.Api.Api;
using UnMango.Wishlists.Api.Tests.Thumbnails;

namespace UnMango.Wishlists.Api.Tests;

public sealed class ThumbnailApiTests(AppFixture app)
{
	private static readonly CancellationToken Ct = CancellationToken.None;

	/// <summary>A shop page with its own og:image, unique to each test so tests don't share fake pages.</summary>
	private (string Page, string Image) Shop() {
		var id = Guid.NewGuid().ToString("N");
		var page = $"https://shop.example.com/{id}";
		var image = $"https://cdn.example.com/{id}.png";
		app.Web.ShopPage(page, image).Image(image, FakeWeb.Png, "image/png");
		return (page, image);
	}

	private static ItemRequest Item(string? link) => new("Socks", link, 12.50m, null, ReallyWant: false);

	private static async Task<OwnItem> AddAsync(HttpClient client, string? link) {
		var response = await client.PostAsJsonAsync("/api/wishlists/mine/items", Item(link), Ct);
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		return (await response.Content.ReadFromJsonAsync<OwnItem>(Ct))!;
	}

	private static async Task<OwnItem> MineAsync(HttpClient client, Guid itemId) =>
		(await client.GetFromJsonAsync<OwnWishlist>("/api/wishlists/mine", Ct))!.Items.Single(x => x.Id == itemId);

	/// <summary>Polls until the background lookup has landed.</summary>
	private static async Task<OwnItem> EventuallyAsync(HttpClient client, Guid itemId, Func<OwnItem, bool> done) {
		var deadline = DateTime.UtcNow.AddSeconds(15);
		while (true) {
			var item = await MineAsync(client, itemId);
			if (done(item)) return item;
			if (DateTime.UtcNow > deadline) Assert.Fail($"Timed out waiting on item {itemId}: {item}");
			await Task.Delay(100, Ct);
		}
	}

	/// <summary>Waits until the background lookup has seen every event so far.</summary>
	private async Task SettleAsync() {
		var store = app.Services.GetRequiredService<IDocumentStore>();
		await store.WaitForNonStaleProjectionDataAsync(TimeSpan.FromSeconds(15));
	}

	[Fact]
	public async Task Adding_an_item_with_a_shop_link_finds_its_thumbnail_on_every_screen() {
		var (page, _) = Shop();
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var adaList = (await ada.GetFromJsonAsync<Me>("/api/me", Ct))!.WishlistId;

		var added = await AddAsync(ada, page);
		Assert.Null(added.Thumbnail); // Saving never waits for the lookup.

		var mine = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null);
		Assert.StartsWith($"/api/items/{added.Id}/thumbnail", mine.Thumbnail);

		var theirs = await bob.GetFromJsonAsync<GiftWishlist>($"/api/wishlists/{adaList}", Ct);
		Assert.Equal(mine.Thumbnail, theirs!.Items.Single(x => x.Id == added.Id).Thumbnail);

		await bob.PutAsync($"/api/wishlists/{adaList}/items/{added.Id}/bought", null, Ct);
		var buying = await bob.GetFromJsonAsync<List<MyPurchase>>("/api/purchases/mine", Ct);
		Assert.Equal(mine.Thumbnail, buying!.Single(x => x.ItemId == added.Id).Thumbnail);
	}

	[Fact]
	public async Task The_thumbnail_is_proxied_as_a_private_cached_image() {
		var (page, _) = Shop();
		var (_, ada) = await app.JoinAsync("Ada");
		var (_, bob) = await app.JoinAsync("Bob");
		var added = await AddAsync(ada, page);
		var item = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null);

		var response = await bob.GetAsync(item.Thumbnail, Ct);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
		Assert.Equal(FakeWeb.Png, await response.Content.ReadAsByteArrayAsync(Ct));
		Assert.True(response.Headers.CacheControl?.Private);
		Assert.Equal(TimeSpan.FromDays(1), response.Headers.CacheControl?.MaxAge);
		Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
	}

	[Fact]
	public async Task The_thumbnail_endpoint_requires_sign_in() {
		var response = await app.Anonymous().GetAsync($"/api/items/{Guid.NewGuid()}/thumbnail", Ct);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task The_thumbnail_endpoint_refuses_an_image_that_turned_into_svg() {
		var (page, image) = Shop();
		var (_, ada) = await app.JoinAsync("Ada");
		var added = await AddAsync(ada, page);
		var item = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null);

		app.Web.Image(image, "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray(), "image/svg+xml");
		var response = await ada.GetAsync(item.Thumbnail, Ct);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Items_without_a_thumbnail_have_none() {
		var (_, ada) = await app.JoinAsync("Ada");
		var noImage = $"https://shop.example.com/{Guid.NewGuid():N}";
		app.Web.Html(noImage, "<html><head><title>No image</title></head></html>");

		var plain = await AddAsync(ada, noImage);
		var broken = await AddAsync(ada, $"https://shop.example.com/{Guid.NewGuid():N}"); // 404
		var unlinked = await AddAsync(ada, null);
		await SettleAsync();

		Assert.Null((await MineAsync(ada, plain.Id)).Thumbnail);
		Assert.Null((await MineAsync(ada, broken.Id)).Thumbnail);
		Assert.Null((await MineAsync(ada, unlinked.Id)).Thumbnail);
		var response = await ada.GetAsync($"/api/items/{plain.Id}/thumbnail", Ct);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Changing_the_link_replaces_the_thumbnail_and_clearing_it_removes_it() {
		var (first, _) = Shop();
		var (second, _) = Shop();
		var (_, ada) = await app.JoinAsync("Ada");
		var added = await AddAsync(ada, first);
		var before = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null);

		await ada.PutAsJsonAsync($"/api/wishlists/mine/items/{added.Id}", Item(second), Ct);
		var after = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null && x.Thumbnail != before.Thumbnail);
		Assert.NotEqual(before.Thumbnail, after.Thumbnail);

		await ada.PutAsJsonAsync($"/api/wishlists/mine/items/{added.Id}", Item(null), Ct);
		Assert.Null((await MineAsync(ada, added.Id)).Thumbnail);
	}

	[Fact]
	public async Task Editing_other_fields_keeps_the_thumbnail_without_looking_again() {
		var (page, _) = Shop();
		var (_, ada) = await app.JoinAsync("Ada");
		var added = await AddAsync(ada, page);
		var before = await EventuallyAsync(ada, added.Id, x => x.Thumbnail is not null);

		await ada.PutAsJsonAsync($"/api/wishlists/mine/items/{added.Id}", Item(page) with { Name = "Wool socks" }, Ct);
		await SettleAsync();

		var after = await MineAsync(ada, added.Id);
		Assert.Equal("Wool socks", after.Name);
		Assert.Equal(before.Thumbnail, after.Thumbnail);
		Assert.Single(app.Web.Requests, x => x == page);
	}
}
