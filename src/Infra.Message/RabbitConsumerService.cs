using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Infra.Message.Interfaces;

namespace Infra.Message;

public abstract class RabbitConsumerService<TMessage> : BackgroundService
{
    private readonly IRabbitConnectionFactory _factory;
    private readonly RabbitOptions _options;
    private readonly ILogger _logger;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected RabbitConsumerService(IRabbitConnectionFactory factory, IOptions<RabbitOptions> options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        RabbitOptions.Validate(options.Value);

        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        _channel = await _factory.CreateChannelAsync(stoppingToken).ConfigureAwait(false);
        await _channel.BasicQosAsync(0, _options.PrefetchCount, global: false, cancellationToken: stoppingToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(queue: _options.Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken).ConfigureAwait(false);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            if (!TryDeserialize(args.Body, out var message) || message is null)
            {
                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
                return;
            }

            await ProcessMessageAsync(message, args.BasicProperties, _stoppingToken).ConfigureAwait(false);
            await _channel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var retryCount = GetRetryCount(args.BasicProperties);
            if (retryCount >= _options.MaxRetries)
            {
                var dlqProperties = CreatePropertiesFromReadOnly(args.BasicProperties);
                await _channel.BasicPublishAsync(
                    exchange: _options.DeadLetterExchange,
                    routingKey: _options.RoutingKey,
                    mandatory: false,
                    basicProperties: dlqProperties,
                    body: args.Body,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
                _logger.LogError(ex, "Message moved to DLQ after {RetryCount} attempts", retryCount);
                return;
            }

            var retryProperties = CreatePropertiesFromReadOnly(args.BasicProperties);
            retryProperties.Headers ??= new Dictionary<string, object>();
            retryProperties.Headers["x-retry-count"] = retryCount + 1;

            await _channel.BasicPublishAsync(
                exchange: _options.DelayExchange,
                routingKey: _options.RoutingKey,
                mandatory: false,
                basicProperties: retryProperties,
                body: args.Body,
                cancellationToken: _stoppingToken).ConfigureAwait(false);

            await _channel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
            _logger.LogWarning(ex, "Message requeued for retry attempt {RetryCount}", retryCount + 1);
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
        {
            return 0;
        }

        if (!properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return 0;
        }

        return value switch
        {
            byte b => b,
            int i => i,
            long l => (int)l,
            _ => 0
        };
    }

    private static BasicProperties CreatePropertiesFromReadOnly(IReadOnlyBasicProperties source)
    {
        var properties = new BasicProperties
        {
            ContentType = source.ContentType,
            ContentEncoding = source.ContentEncoding,
            DeliveryMode = source.DeliveryMode,
            Priority = source.Priority,
            CorrelationId = source.CorrelationId,
            ReplyTo = source.ReplyTo,
            Expiration = source.Expiration,
            MessageId = source.MessageId,
            Timestamp = source.Timestamp,
            Type = source.Type,
            UserId = source.UserId,
            AppId = source.AppId,
            ClusterId = source.ClusterId
        };

        if (source.Headers is not null)
        {
            properties.Headers = new Dictionary<string, object>(source.Headers);
        }

        return properties;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract bool TryDeserialize(ReadOnlyMemory<byte> body, out TMessage? message);

    protected abstract ValueTask ProcessMessageAsync(TMessage message, IReadOnlyBasicProperties properties, CancellationToken cancellationToken);
}
