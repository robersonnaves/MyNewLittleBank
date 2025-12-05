using RabbitMQ.Client;

namespace Infra.Message.Interfaces;

public interface IRabbitConnectionFactory : IDisposable
{
    IConnection CreateConnection();
    Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
}
