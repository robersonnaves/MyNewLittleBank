using Domain.Interfaces;
using Domain.Messaging;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Infra.Message;

public sealed class MessageEnvelopeHandler : IHandleMessages<MessageEnvelope>
{
    private readonly IEnumerable<IMessageConsumer> _consumers;
    private readonly ILogger<MessageEnvelopeHandler> _logger;

    public MessageEnvelopeHandler(IEnumerable<IMessageConsumer> consumers, ILogger<MessageEnvelopeHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(consumers);
        ArgumentNullException.ThrowIfNull(logger);

        _consumers = consumers;
        _logger = logger;
    }

    public async Task Handle(MessageEnvelope message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Garantir que MessageContext.Current está disponível para os consumidores
        // Os headers do Rebus são automaticamente disponibilizados via MessageContext.Current
        var consumer = _consumers.FirstOrDefault(c => c.CanHandle(message));
        if (consumer is null)
        {
            _logger.LogWarning("No consumer registered for routing key {RoutingKey} and message type {MessageType}", message.RoutingKey, message.MessageType);
            return;
        }

        await consumer.HandleAsync(message, CancellationToken.None).ConfigureAwait(false);
    }
}
