using System.Text.Json;
using System.Text.Json.Serialization;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace UnMango.Wishlists.Api;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppSerializationContext : JsonSerializerContext;

internal static class Http
{
	public record CreateWishlist(string Name);

	extension(IEndpointRouteBuilder app)
	{
		public void MapWishlists() => MapWishlistsCore(app.MapGroup("/wishlists"));
	}

	private static void MapWishlistsCore(RouteGroupBuilder builder) {
		builder.MapGet("/",
			async ([FromServices] IDocumentSession session, CancellationToken cancellationToken) =>
				TypedResults.Ok(await Wishlist.List(session, cancellationToken)));

		builder.MapPost("/",
			async ([FromServices] IDocumentSession session, CreateWishlist req, CancellationToken cancellationToken) =>
				TypedResults.Ok(await Wishlist.Create(session, req.Name, cancellationToken)));

		builder.MapGet("/{id:Guid}",
			async ([FromServices] IDocumentSession session, [FromRoute] Guid id, CancellationToken cancellationToken) =>
			TypedResults.Ok(await Wishlist.Get(session, id, cancellationToken)));
	}
}
