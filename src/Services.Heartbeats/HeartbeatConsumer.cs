using Domain.Interfaces;
using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Heartbeats;

public sealed class HeartbeatConsumer : RabbitConsumerService<HeartbeatDto>
{
    private readonly ILogger<HeartbeatConsumer> _logger;

    public HeartbeatConsumer(
        IRabbitConnectionFactory factory,
        IOptions<RabbitOptions> options,
        ILogger<HeartbeatConsumer> logger)
        : base(factory, options, logger)
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
        _logger.LogInformation("Heartbeat received from {Service} at {Timestamp}", message.ServiceName, message.TimestampUtc);
        return ValueTask.CompletedTask;
    }
}
