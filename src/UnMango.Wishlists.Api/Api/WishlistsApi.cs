using System.Security.Claims;
using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using UnMango.Wishlists.Api.Auth;
using UnMango.Wishlists.Api.Domain;

namespace UnMango.Wishlists.Api.Api;

internal static class WishlistsApi
{
	private const int MaxNameLength = 200;
	private const int MaxNotesLength = 2000;
	private const int MaxLinkLength = 2000;

	extension(IEndpointRouteBuilder endpoints)
	{
		public void MapWishlistsApi() {
			endpoints.MapGet("/me", async Task<Results<Ok<Me>, UnauthorizedHttpResult>> (
				ClaimsPrincipal user,
				[FromServices] IQuerySession session,
				CancellationToken cancellationToken
			) => {
				var memberId = user.MemberId;
				var member = await session.LoadAsync<MemberView>(memberId, cancellationToken);
				var wishlist = await OwnWishlistAsync(session, memberId, cancellationToken);
				if (member is null || wishlist is null) return TypedResults.Unauthorized();

				return TypedResults.Ok(new Me(member.Id, member.Email, member.DisplayName, member.AvatarUrl, wishlist.Id));
			});

			endpoints.MapGet("/members", async Task<Ok<IReadOnlyList<Member>>> (
				[FromServices] IQuerySession session,
				CancellationToken cancellationToken
			) => {
				var members = await session.Query<MemberView>().ToListAsync(cancellationToken);
				var wishlists = await session.Query<WishlistView>().ToListAsync(cancellationToken);
				var wishlistByOwner = wishlists.ToDictionary(x => x.OwnerId);

				IReadOnlyList<Member> result = members
					.Where(x => wishlistByOwner.ContainsKey(x.Id))
					.OrderBy(x => x.DisplayName)
					.Select(x => {
						var wishlist = wishlistByOwner[x.Id];
						var items = wishlist.CurrentItems.ToList();
						return new Member(x.Id, x.DisplayName, x.AvatarUrl, wishlist.Id, items.Count, items.Count(i => i.ReallyWant));
					})
					.ToList();

				return TypedResults.Ok(result);
			});

			endpoints.MapGet("/purchases/mine", async Task<Ok<IReadOnlyList<MyPurchase>>> (
				ClaimsPrincipal user,
				[FromServices] IQuerySession session,
				CancellationToken cancellationToken
			) => {
				var memberId = user.MemberId;
				// A household has a handful of lists, so filtering in memory is simpler than querying dictionary values.
				var purchases = await session.Query<PurchasesView>().ToListAsync(cancellationToken);
				var mine = purchases
					.Select(p => (p.WishlistId, ItemIds: p.Buyers.Where(b => b.Value == memberId).Select(b => b.Key).ToHashSet()))
					.Where(x => x.ItemIds.Count > 0)
					.ToList();

				if (mine.Count == 0) return TypedResults.Ok<IReadOnlyList<MyPurchase>>([]);

				var wishlists = await session.LoadManyAsync<WishlistView>(cancellationToken, mine.Select(x => x.WishlistId));
				var members = await session.Query<MemberView>().ToListAsync(cancellationToken);
				var names = members.ToDictionary(x => x.Id, x => x.DisplayName);
				var itemIdsByWishlist = mine.ToDictionary(x => x.WishlistId, x => x.ItemIds);

				IReadOnlyList<MyPurchase> result = wishlists
					.Where(w => w.OwnerId != memberId)
					.OrderBy(w => names.GetValueOrDefault(w.OwnerId, ""))
					.SelectMany(w => w.Items
						.Where(i => itemIdsByWishlist[w.Id].Contains(i.Id))
						.Select(i => new MyPurchase(
							w.Id, w.OwnerId, names.GetValueOrDefault(w.OwnerId, ""), i.Id, i.Name, i.Link, i.Price, i.Removed)))
					.ToList();

				return TypedResults.Ok(result);
			});

			var wishlists = endpoints.MapGroup("/wishlists");
			var mine = wishlists.MapGroup("/mine");

			mine.MapGet("/", async Task<Results<Ok<OwnWishlist>, NotFound>> (
				ClaimsPrincipal user,
				[FromServices] IQuerySession session,
				CancellationToken cancellationToken
			) => await OwnWishlistAsync(session, user.MemberId, cancellationToken) is { } wishlist
				? TypedResults.Ok(ToOwn(wishlist))
				: TypedResults.NotFound());

			mine.MapPut("/", async Task<Results<Ok<OwnWishlist>, NotFound, ValidationProblem>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromBody] RenameWishlist req,
				CancellationToken cancellationToken
			) => {
				var name = req.Name?.Trim() ?? "";
				if (name.Length is 0 or > MaxNameLength) {
					return TypedResults.ValidationProblem(new Dictionary<string, string[]> {
						["name"] = [$"Name is required and must be at most {MaxNameLength} characters."],
					});
				}

				if (await OwnWishlistAsync(session, user.MemberId, cancellationToken) is not { } current) {
					return TypedResults.NotFound();
				}

				var stream = await session.Events.FetchForWriting<WishlistView>(current.Id, cancellationToken);
				var renamed = new WishlistRenamed(name);
				stream.AppendOne(renamed);
				await session.SaveChangesAsync(cancellationToken);

				return TypedResults.Ok(ToOwn(stream.Aggregate!.Apply(renamed)));
			});

			mine.MapPost("/items", async Task<Results<Created<OwnItem>, NotFound, ValidationProblem>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromBody] ItemRequest req,
				CancellationToken cancellationToken
			) => {
				if (Validate(req) is { } errors) return TypedResults.ValidationProblem(errors);
				if (await OwnWishlistAsync(session, user.MemberId, cancellationToken) is not { } current) {
					return TypedResults.NotFound();
				}

				var stream = await session.Events.FetchForWriting<WishlistView>(current.Id, cancellationToken);
				var added = new ItemAdded(
					Guid.CreateVersion7(),
					req.Name.Trim(),
					Clean(req.Link),
					req.Price,
					Clean(req.Notes),
					req.ReallyWant);

				stream.AppendOne(added);
				await session.SaveChangesAsync(cancellationToken);

				var item = new OwnItem(added.ItemId, added.Name, added.Link, added.Price, added.Notes, added.ReallyWant);
				return TypedResults.Created($"/api/wishlists/mine/items/{item.Id}", item);
			});

			mine.MapPut("/items/{itemId:guid}", async Task<Results<Ok<OwnItem>, NotFound, ValidationProblem>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromRoute] Guid itemId,
				[FromBody] ItemRequest req,
				CancellationToken cancellationToken
			) => {
				if (Validate(req) is { } errors) return TypedResults.ValidationProblem(errors);
				if (await OwnWishlistAsync(session, user.MemberId, cancellationToken) is not { } current) {
					return TypedResults.NotFound();
				}

				var stream = await session.Events.FetchForWriting<WishlistView>(current.Id, cancellationToken);
				if (stream.Aggregate?.FindCurrent(itemId) is null) return TypedResults.NotFound();

				var edited = new ItemEdited(itemId, req.Name.Trim(), Clean(req.Link), req.Price, Clean(req.Notes), req.ReallyWant);
				stream.AppendOne(edited);
				await session.SaveChangesAsync(cancellationToken);

				return TypedResults.Ok(new OwnItem(itemId, edited.Name, edited.Link, edited.Price, edited.Notes, edited.ReallyWant));
			});

			mine.MapDelete("/items/{itemId:guid}", async Task<Results<NoContent, NotFound>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromRoute] Guid itemId,
				CancellationToken cancellationToken
			) => {
				if (await OwnWishlistAsync(session, user.MemberId, cancellationToken) is not { } current) {
					return TypedResults.NotFound();
				}

				var stream = await session.Events.FetchForWriting<WishlistView>(current.Id, cancellationToken);
				if (stream.Aggregate?.FindCurrent(itemId) is null) return TypedResults.NotFound();

				stream.AppendOne(new ItemRemoved(itemId));
				await session.SaveChangesAsync(cancellationToken);
				return TypedResults.NoContent();
			});

			wishlists.MapGet("/{id:guid}", async Task<Results<Ok<GiftWishlist>, RedirectHttpResult, NotFound>> (
				ClaimsPrincipal user,
				[FromServices] IQuerySession session,
				[FromRoute] Guid id,
				CancellationToken cancellationToken
			) => {
				var wishlist = await session.LoadAsync<WishlistView>(id, cancellationToken);
				if (wishlist is null) return TypedResults.NotFound();

				// The owner never sees bought marks, even when asking for their own list by id.
				if (wishlist.OwnerId == user.MemberId) return TypedResults.Redirect("/api/wishlists/mine");

				var purchases = await session.LoadAsync<PurchasesView>(wishlist.PurchasesId, cancellationToken);
				var members = await session.Query<MemberView>().ToListAsync(cancellationToken);
				var names = members.ToDictionary(x => x.Id, x => x.DisplayName);
				var buyers = purchases?.Buyers ?? new Dictionary<Guid, Guid>();

				var items = wishlist.Items
					.Where(x => !x.Removed || buyers.ContainsKey(x.Id))
					.Select(x => {
						Guid? buyer = buyers.TryGetValue(x.Id, out var b) ? b : null;
						return new GiftItem(
							x.Id, x.Name, x.Link, x.Price, x.Notes, x.ReallyWant, x.Removed,
							buyer, buyer is { } bid ? names.GetValueOrDefault(bid) : null);
					})
					.ToList();

				return TypedResults.Ok(new GiftWishlist(
					wishlist.Id, wishlist.Name, wishlist.OwnerId, names.GetValueOrDefault(wishlist.OwnerId, ""), items));
			});

			wishlists.MapPut("/{id:guid}/items/{itemId:guid}/bought", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromRoute] Guid id,
				[FromRoute] Guid itemId,
				CancellationToken cancellationToken
			) => {
				var memberId = user.MemberId;
				var wishlist = await session.LoadAsync<WishlistView>(id, cancellationToken);
				if (wishlist?.FindCurrent(itemId) is null) return TypedResults.NotFound();
				if (wishlist.OwnerId == memberId) return OwnerForbidden();

				var stream = await session.Events.FetchForWriting<PurchasesView>(wishlist.PurchasesId, cancellationToken);
				var buyer = stream.Aggregate?.BuyerOf(itemId);
				if (buyer == memberId) return TypedResults.NoContent();
				if (buyer is not null) {
					return TypedResults.Problem(
						statusCode: StatusCodes.Status409Conflict,
						title: "Someone else already bought this item.");
				}

				stream.AppendOne(new ItemMarkedBought(itemId, memberId));
				await session.SaveChangesAsync(cancellationToken);
				return TypedResults.NoContent();
			});

			wishlists.MapDelete("/{id:guid}/items/{itemId:guid}/bought", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
				ClaimsPrincipal user,
				[FromServices] IDocumentSession session,
				[FromRoute] Guid id,
				[FromRoute] Guid itemId,
				CancellationToken cancellationToken
			) => {
				var memberId = user.MemberId;
				var wishlist = await session.LoadAsync<WishlistView>(id, cancellationToken);
				if (wishlist is null || wishlist.Items.All(x => x.Id != itemId)) return TypedResults.NotFound();
				if (wishlist.OwnerId == memberId) return OwnerForbidden();

				var stream = await session.Events.FetchForWriting<PurchasesView>(wishlist.PurchasesId, cancellationToken);
				var buyer = stream.Aggregate?.BuyerOf(itemId);
				if (buyer is null) return TypedResults.NoContent();
				if (buyer != memberId) {
					return TypedResults.Problem(
						statusCode: StatusCodes.Status403Forbidden,
						title: "Only the person who bought this item can unmark it.");
				}

				stream.AppendOne(new ItemUnmarked(itemId, memberId));
				await session.SaveChangesAsync(cancellationToken);
				return TypedResults.NoContent();
			});
		}
	}

	private static ProblemHttpResult OwnerForbidden() => TypedResults.Problem(
		statusCode: StatusCodes.Status403Forbidden,
		title: "You can't see or change bought marks on your own wishlist.");

	private static Task<WishlistView?> OwnWishlistAsync(IQuerySession session, Guid memberId, CancellationToken cancellationToken) =>
		session.Query<WishlistView>().FirstOrDefaultAsync(x => x.OwnerId == memberId, cancellationToken);

	private static OwnWishlist ToOwn(WishlistView wishlist) => new(
		wishlist.Id,
		wishlist.Name,
		wishlist.CurrentItems
			.Select(x => new OwnItem(x.Id, x.Name, x.Link, x.Price, x.Notes, x.ReallyWant))
			.ToList());

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	internal static Dictionary<string, string[]>? Validate(ItemRequest req) {
		var errors = new Dictionary<string, string[]>();
		var name = req.Name?.Trim() ?? "";

		if (name.Length is 0 or > MaxNameLength) {
			errors["name"] = [$"Name is required and must be at most {MaxNameLength} characters."];
		}

		if (Clean(req.Link) is { } link
		    && (link.Length > MaxLinkLength
		        || !Uri.TryCreate(link, UriKind.Absolute, out var uri)
		        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))) {
			errors["link"] = ["Link must be an http or https URL."];
		}

		if (req.Price is < 0) {
			errors["price"] = ["Price can't be negative."];
		}

		if (req.Notes is { Length: > MaxNotesLength }) {
			errors["notes"] = [$"Notes must be at most {MaxNotesLength} characters."];
		}

		return errors.Count > 0 ? errors : null;
	}
}
