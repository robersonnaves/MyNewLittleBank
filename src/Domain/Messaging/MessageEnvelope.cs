namespace Domain.Messaging;

public sealed record MessageEnvelope(string MessageType, string Payload, string RoutingKey);
