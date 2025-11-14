using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Wishlist))]
[JsonSerializable(typeof(Wishlist.Create))]
internal partial class AppSerializationContext : JsonSerializerContext;
