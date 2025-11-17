using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UnMango.Wishlists.Api.Domain;

internal sealed record Wishlist
{
	public required Guid Id { get; init; }

	public required string Name { get; init; }

	public required User Owner { get; init; }

	public record Created(Guid WishlistId, string Name);
}

internal static class WishlistApi
{
	public record Create(string Name, User Owner);

	extension(IEndpointRouteBuilder endpoints)
	{
		public void MapWishlists() {
			endpoints.MapGet("/", async (
				[FromServices] AppDbContext context,
				CancellationToken cancellationToken
			) => TypedResults.Ok(await context.Wishlists.ToListAsync(cancellationToken)));

			endpoints.MapPost("/", async (
				[FromServices] AppDbContext context,
				[FromBody] Create req,
				CancellationToken cancellationToken
			) => {
				var wishlist = new Wishlist {
					Id = Guid.CreateVersion7(),
					Name = req.Name,
					Owner = req.Owner,
				};

				context.Wishlists.Add(wishlist);
				await context.SaveChangesAsync(cancellationToken);
				return TypedResults.Ok(wishlist);
			});
		}
	}
}
