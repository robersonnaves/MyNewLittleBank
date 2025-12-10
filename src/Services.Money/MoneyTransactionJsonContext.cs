using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Money;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(MoneyTransactionDto))]
internal sealed partial class MoneyTransactionJsonContext : JsonSerializerContext
{
}
