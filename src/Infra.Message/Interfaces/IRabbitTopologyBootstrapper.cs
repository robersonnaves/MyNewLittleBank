namespace Infra.Message.Interfaces;

public interface IRabbitTopologyBootstrapper
{
    Task EnsureTopologyAsync(CancellationToken cancellationToken = default);
}
