using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Money;

[JsonSerializable(typeof(MoneyTransactionDto))]
internal sealed partial class MoneyTransactionJsonContext : JsonSerializerContext
{
}
