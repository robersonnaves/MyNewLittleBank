namespace Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(string messageType, string payload, string routingKey, CancellationToken cancellationToken = default);
}
