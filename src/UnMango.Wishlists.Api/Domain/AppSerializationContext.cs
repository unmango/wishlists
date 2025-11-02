using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnMango.Wishlists.Api.Domain;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(User))]
internal partial class AppSerializationContext : JsonSerializerContext;
