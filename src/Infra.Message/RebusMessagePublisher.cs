using Domain.Interfaces;
using Domain.Messaging;
using Rebus.Messages;

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
        var headers = BuildRebusHeaders(envelope);
        
        return _messagingBus.PublishAsync(envelope, headers, cancellationToken);
    }

    private static Dictionary<string, string> BuildRebusHeaders(MessageEnvelope envelope)
    {
        var headers = new Dictionary<string, string>
        {
            [Headers.ContentType] = "application/json",
            [Headers.Type] = typeof(MessageEnvelope).AssemblyQualifiedName ?? typeof(MessageEnvelope).FullName ?? typeof(MessageEnvelope).Name,
            ["message-type"] = envelope.MessageType,
            ["routing-key"] = envelope.RoutingKey
        };

        return headers;
    }
}
