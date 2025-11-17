using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Wishlist))]
[JsonSerializable(typeof(WishlistApi.Create))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppSerializationContext : JsonSerializerContext;
