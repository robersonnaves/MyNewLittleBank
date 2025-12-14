using Domain.Messaging;

namespace Domain.Interfaces;

public interface IMessageConsumer
{
    bool CanHandle(MessageEnvelope envelope);
    Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken);
}
