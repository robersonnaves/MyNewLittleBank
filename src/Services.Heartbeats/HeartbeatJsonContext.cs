using System.Text.Json.Serialization;

namespace Services.Heartbeats;

[JsonSerializable(typeof(HeartbeatDto))]
internal sealed partial class HeartbeatJsonContext : JsonSerializerContext
{
}
