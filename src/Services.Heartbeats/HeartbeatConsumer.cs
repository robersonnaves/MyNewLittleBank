using Domain.Interfaces;
using Domain.Messaging;
using Infra.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Heartbeats;

public sealed class HeartbeatConsumer : IMessageConsumer
{
    private static readonly Action<ILogger, string, DateTime, Exception?> HeartbeatReceived =
        LoggerMessage.Define<string, DateTime>(
            LogLevel.Information,
            new EventId(1, nameof(HeartbeatReceived)),
            "Heartbeat received from {Service} at {Timestamp}");

    private readonly ILogger<HeartbeatConsumer> _logger;
    private readonly string _expectedRoutingKey;

    public HeartbeatConsumer(
        IOptions<RabbitOptions> options,
        ILogger<HeartbeatConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _expectedRoutingKey = options.Value.RoutingKey;
        _logger = logger;
    }

    public bool CanHandle(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return string.Equals(envelope.RoutingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase);
    }

    public Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!CanHandle(envelope))
        {
            _logger.LogDebug("Ignoring heartbeat message for routing key {RoutingKey}", envelope.RoutingKey);
            return Task.CompletedTask;
        }

        var message = System.Text.Json.JsonSerializer.Deserialize(envelope.Payload, HeartbeatJsonContext.Default.HeartbeatDto);
        if (message is null)
        {
            _logger.LogWarning("Heartbeat payload could not be deserialized for routing key {RoutingKey}", envelope.RoutingKey);
            return Task.CompletedTask;
        }

        HeartbeatReceived(_logger, message.ServiceName, message.TimestampUtc, null);
        return Task.CompletedTask;
    }
}
