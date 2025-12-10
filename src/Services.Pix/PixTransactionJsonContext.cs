using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.DTOs;

namespace Services.Pix;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PixTransactionDto))]
internal sealed partial class PixTransactionJsonContext : JsonSerializerContext
{
}
