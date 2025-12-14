using System.Collections.Generic;
using Domain.Interfaces;
using Domain.Messaging;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using System.Diagnostics;

namespace Infra.Message;

public sealed class RebusMessagingBus : IMessagingBus
{
    private static readonly Action<ILogger, string, string, string, Exception?> PublishFailed =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(1, nameof(PublishFailed)),
            "Failed to publish message type {MessageType} with routing key {RoutingKey} to topic {Topic}");

    private readonly IBus _bus;
    private readonly ILogger<RebusMessagingBus> _logger;

    public RebusMessagingBus(IBus bus, ILogger<RebusMessagingBus> logger)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(logger);

        _bus = bus;
        _logger = logger;
    }

    public async Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.RoutingKey);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Headers de tracing são adicionados automaticamente pelo TracingHeadersStep no pipeline
            // Passar dicionário vazio - o middleware adicionará os headers de tracing
            await _bus.Advanced.Topics.Publish(envelope.RoutingKey, envelope, new Dictionary<string, string>()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PublishFailed(_logger, envelope.MessageType, envelope.RoutingKey, envelope.RoutingKey, ex);
            throw;
        }
    }
}
