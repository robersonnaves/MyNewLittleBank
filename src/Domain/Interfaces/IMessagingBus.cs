using Domain.Messaging;

namespace Domain.Interfaces;

public interface IMessagingBus
{
    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
    Task PublishAsync(MessageEnvelope envelope, IDictionary<string, string> headers, CancellationToken cancellationToken = default);
}
