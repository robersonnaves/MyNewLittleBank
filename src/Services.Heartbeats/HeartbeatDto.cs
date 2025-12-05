namespace Services.Heartbeats;

public sealed record HeartbeatDto(string ServiceName, string Status, DateTime TimestampUtc);
