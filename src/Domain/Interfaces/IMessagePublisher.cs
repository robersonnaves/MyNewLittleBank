namespace Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}
