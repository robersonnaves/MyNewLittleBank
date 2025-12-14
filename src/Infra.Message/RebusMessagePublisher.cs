using Domain.Interfaces;
using Domain.Messaging;

namespace Infra.Message;

public sealed class RebusMessagePublisher : IMessagePublisher
{
    private readonly IMessagingBus _messagingBus;

    public RebusMessagePublisher(IMessagingBus messagingBus)
    {
        ArgumentNullException.ThrowIfNull(messagingBus);
        _messagingBus = messagingBus;
    }

    public Task PublishAsync(string messageType, string payload, string routingKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        var envelope = new MessageEnvelope(messageType, payload, routingKey);
        return _messagingBus.PublishAsync(envelope, cancellationToken);
    }
}
