using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Card;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CardTransactionDto))]
internal sealed partial class CardTransactionJsonContext : JsonSerializerContext
{
}
