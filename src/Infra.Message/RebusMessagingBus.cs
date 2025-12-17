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

    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return PublishAsync(envelope, new Dictionary<string, string>(), cancellationToken);
    }

    public async Task PublishAsync(MessageEnvelope envelope, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.RoutingKey);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Headers de tracing são adicionados automaticamente pelo TracingHeadersStep no pipeline
            // Passar headers fornecidos - o middleware adicionará os headers de tracing
            await _bus.Advanced.Topics.Publish(envelope.RoutingKey, envelope, headers).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Message publish cancelled for type {MessageType}, routing key {RoutingKey}", envelope.MessageType, envelope.RoutingKey);
            throw;
        }
#pragma warning disable CA1031 // Rebus can throw various exception types and we need to log and rethrow for proper error handling
        catch (Exception ex)
        {
            // Catching all exceptions intentionally - Rebus can throw various exception types and we need to log and rethrow for proper error handling
            PublishFailed(_logger, envelope.MessageType, envelope.RoutingKey, envelope.RoutingKey, ex);
            throw;
        }
#pragma warning restore CA1031
    }
}
