using Domain.Interfaces;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace Infra.Message;

public sealed class PublisherService : IMessagePublisher, IDisposable
{
    private static readonly Action<ILogger, Guid, Exception?> PublishFailed =
        LoggerMessage.Define<Guid>(
            logLevel: LogLevel.Error,
            new EventId(1, nameof(PublishFailed)),
            "Failed to publish message {MessageId}");

    private readonly IRabbitConnectionFactory _factory;
    private readonly RabbitOptions _options;
    private readonly ILogger<PublisherService> _logger;
    private readonly Lazy<Task<IChannel>> _channelLazy;
    private IChannel? _channel;

    public PublisherService(IRabbitConnectionFactory factory, IOptions<RabbitOptions> options, ILogger<PublisherService> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        RabbitOptions.Validate(options.Value);

        _factory = factory;
        _options = options.Value;
        _logger = logger;

        _channelLazy = new Lazy<Task<IChannel>>(() => _factory.CreateChannelAsync());
    }

    public async Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);

        _channel ??= await _channelLazy.Value.ConfigureAwait(false);

        var body = Encoding.UTF8.GetBytes(payload);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = messageType,
            MessageId = Guid.NewGuid().ToString("N")
        };

        try
        {
            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: _options.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PublishFailed(_logger, Guid.Parse(properties.MessageId), ex);
            throw;
        }
    }

    public void Dispose()
    {
        if (_channel is not null)
        {
            _channel.Dispose();
        }

        _factory.Dispose();
    }
}
