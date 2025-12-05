using System.Diagnostics;
using Domain.Interfaces;
using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Heartbeats;

public sealed class HeartbeatConsumer : RabbitConsumerService<HeartbeatDto>
{
    private static readonly Action<ILogger, string, DateTime, Exception?> HeartbeatReceived =
        LoggerMessage.Define<string, DateTime>(
            LogLevel.Information,
            new EventId(1, nameof(HeartbeatReceived)),
            "Heartbeat received from {Service} at {Timestamp}");

    private readonly ILogger<HeartbeatConsumer> _logger;

    public HeartbeatConsumer(
        IRabbitConnectionFactory factory,
        IOptions<RabbitOptions> options,
        ILogger<HeartbeatConsumer> logger,
        ActivitySource activitySource)
        : base(factory, options, logger, activitySource)
    {
        _logger = logger;
    }

    protected override bool TryDeserialize(ReadOnlyMemory<byte> body, out HeartbeatDto? message)
    {
        message = System.Text.Json.JsonSerializer.Deserialize(body.Span, HeartbeatJsonContext.Default.HeartbeatDto);
        return message is not null;
    }

    protected override ValueTask ProcessMessageAsync(HeartbeatDto message, RabbitMQ.Client.IReadOnlyBasicProperties properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        HeartbeatReceived(_logger, message.ServiceName, message.TimestampUtc, null);
        return ValueTask.CompletedTask;
    }
}
