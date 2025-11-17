using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace UnMango.Wishlists.Api.Domain;

internal sealed record Wishlist(Guid Id, string Name, Guid OwnerId)
{
	public record Created(Guid WishlistId, string Name, Guid OwnerId);

	public static Wishlist Create(Created created) =>
		new(created.WishlistId, created.Name, created.OwnerId);
}

internal static class WishlistApi
{
	public record Create(string Name, Guid OwnerId);

	extension(IEndpointRouteBuilder endpoints)
	{
		public void MapWishlists() {
			endpoints.MapGet("/", async Task<Ok<IEnumerable<Wishlist>>> (
				[FromServices] IDocumentSession session,
				CancellationToken cancellationToken
			) => {
				var wishlists = await session.Query<Wishlist>()
					.ToListAsync(cancellationToken);

				return TypedResults.Ok<IEnumerable<Wishlist>>(wishlists);
			});

			endpoints.MapGet("/{id:Guid}", async Task<Ok<Wishlist>> (
				[FromServices] IDocumentSession session,
				[FromRoute] Guid id,
				CancellationToken cancellationToken
			) => {
				var wishlist = await session.Events.AggregateStreamAsync<Wishlist>(id, token: cancellationToken);
				return TypedResults.Ok(wishlist);
			});

			endpoints.MapPost("/", async Task<Created> (
				[FromServices] IDocumentSession session,
				[FromBody] Create req,
				CancellationToken cancellationToken
			) => {
				var id = Guid.CreateVersion7();
				var created = new Wishlist.Created(id, req.Name, req.OwnerId);
				session.Events.StartStream<Wishlist>(id, created);
				await session.SaveChangesAsync(cancellationToken);
				return TypedResults.Created($"/{id}");
			});
		}
	}
}
