using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Mock.Transactions;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CardTransactionDto))]
[JsonSerializable(typeof(PixTransactionDto))]
[JsonSerializable(typeof(MoneyTransactionDto))]
internal sealed partial class TransactionDtoJsonContext : JsonSerializerContext
{
}
