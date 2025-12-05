namespace Services.Heartbeats;

public sealed class HeartbeatOptions
{
    public string ServiceName { get; init; } = "heartbeat-publisher";
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);
    public string MessageType { get; init; } = "heartbeat";
}
