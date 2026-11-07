using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Tests.Thumbnails;

public sealed class WishlistViewThumbnailTests
{
	private static readonly Guid ItemId = Guid.NewGuid();
	private const string Link = "https://shop.example.com/socks";
	private const string Image = "https://cdn.example.com/socks.png";

	private static WishlistView WithItem(string? link = Link) =>
		WishlistView.Create(new WishlistCreated(Guid.NewGuid(), Guid.NewGuid(), "Ada's wishlist", Guid.NewGuid()))
			.Apply(new ItemAdded(ItemId, "Socks", link, 10m, null, false));

	private static WishlistItem Item(WishlistView view) => Assert.Single(view.Items);

	[Fact]
	public void A_found_thumbnail_is_recorded_when_the_link_still_matches() {
		var view = WithItem().Apply(new ItemThumbnailFound(ItemId, Link, Image));

		Assert.Equal(Image, Item(view).ThumbnailUrl);
	}

	[Fact]
	public void A_stale_thumbnail_for_an_old_link_is_ignored() {
		var view = WithItem()
			.Apply(new ItemEdited(ItemId, "Socks", "https://shop.example.com/other", 10m, null, false))
			.Apply(new ItemThumbnailFound(ItemId, Link, Image));

		Assert.Null(Item(view).ThumbnailUrl);
	}

	[Fact]
	public void Editing_other_fields_keeps_the_thumbnail() {
		var view = WithItem()
			.Apply(new ItemThumbnailFound(ItemId, Link, Image))
			.Apply(new ItemEdited(ItemId, "Wool socks", Link, 12m, "Size L", true));

		Assert.Equal(Image, Item(view).ThumbnailUrl);
	}

	[Theory]
	[InlineData("https://shop.example.com/other")]
	[InlineData(null)]
	public void Changing_or_clearing_the_link_clears_the_thumbnail(string? newLink) {
		var view = WithItem()
			.Apply(new ItemThumbnailFound(ItemId, Link, Image))
			.Apply(new ItemEdited(ItemId, "Socks", newLink, 10m, null, false));

		Assert.Null(Item(view).ThumbnailUrl);
	}
}
