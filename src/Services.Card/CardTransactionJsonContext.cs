using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Card;

[JsonSerializable(typeof(CardTransactionDto))]
internal sealed partial class CardTransactionJsonContext : JsonSerializerContext
{
}
