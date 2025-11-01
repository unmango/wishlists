using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

internal sealed record User(Guid Id, string Name);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(User))]
internal partial class WishlistSerializationContext : JsonSerializerContext;
