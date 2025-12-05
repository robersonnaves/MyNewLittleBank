using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infra.Message;

public sealed class RabbitConnectionFactory : IRabbitConnectionFactory
{
    private readonly RabbitOptions _options;
    private readonly ILogger<RabbitConnectionFactory> _logger;
    private IConnection? _connection;

    public RabbitConnectionFactory(IOptions<RabbitOptions> options, ILogger<RabbitConnectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        RabbitOptions.Validate(options.Value);

        _options = options.Value;
        _logger = logger;
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        if (_options.SslEnabled)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = _options.SslServerName ?? _options.HostName,
                AcceptablePolicyErrors = System.Net.Security.SslPolicyErrors.None
            };
        }

        _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("RabbitMQ connection opened to {Host}:{Port}", _options.HostName, _options.Port);
        return _connection;
    }

    public IConnection CreateConnection()
    {
        // Synchronous version for backward compatibility
        return CreateConnectionAsync().GetAwaiter().GetResult();
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        return await connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_connection is not null)
        {
            _connection.Dispose();
        }
    }
}
