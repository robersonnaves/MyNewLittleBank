using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Pix;

[JsonSerializable(typeof(PixTransactionDto))]
internal sealed partial class PixTransactionJsonContext : JsonSerializerContext
{
}
