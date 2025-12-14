using Domain.Messaging;

namespace Domain.Interfaces;

public interface IMessagingBus
{
    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);
}
