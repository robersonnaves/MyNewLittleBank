namespace Domain.Interfaces;

public interface IOutboxWriter
{
    Task AddAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}
