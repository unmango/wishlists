using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using UnMango.Wishlists.Api.Api;

namespace UnMango.Wishlists.Api;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Me))]
[JsonSerializable(typeof(IReadOnlyList<Member>))]
[JsonSerializable(typeof(ItemRequest))]
[JsonSerializable(typeof(RenameWishlist))]
[JsonSerializable(typeof(OwnItem))]
[JsonSerializable(typeof(OwnWishlist))]
[JsonSerializable(typeof(GiftWishlist))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppSerializationContext : JsonSerializerContext;
