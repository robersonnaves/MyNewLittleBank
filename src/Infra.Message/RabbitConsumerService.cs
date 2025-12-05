using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using OpenTelemetry.Context.Propagation;
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
    private readonly ActivitySource _activitySource;
    private readonly Action<ILogger, int, Exception?> _movedToDlq;
    private readonly Action<ILogger, int, Exception?> _requeuedForRetry;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected RabbitConsumerService(IRabbitConnectionFactory factory, IOptions<RabbitOptions> options, ILogger logger, ActivitySource activitySource)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(activitySource);

        RabbitOptions.Validate(options.Value);

        _factory = factory;
        _options = options.Value;
        _logger = logger;
        _activitySource = activitySource;
        _movedToDlq = LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(1, nameof(_movedToDlq)),
            "Message moved to DLQ after {RetryCount} attempts");
        _requeuedForRetry = LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(2, nameof(_requeuedForRetry)),
            "Message requeued for retry attempt {RetryCount}");
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Consumer must handle and route failures to retry/DLQ without crashing.")]
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

            var parentContext = Propagators.DefaultTextMapPropagator.Extract(default, args.BasicProperties, ExtractTraceContextFromBasicProperties);
            using var activity = _activitySource.StartActivity("rabbit.consume", ActivityKind.Consumer, parentContext.ActivityContext);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", _options.Queue);
            activity?.SetTag("messaging.rabbitmq.routing_key", _options.RoutingKey);

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
                _movedToDlq(_logger, retryCount, ex);
                return;
            }

            var retryProperties = CreatePropertiesFromReadOnly(args.BasicProperties);
            retryProperties.Headers ??= new Dictionary<string, object?>();
            retryProperties.Headers["x-retry-count"] = retryCount + 1;

            await _channel.BasicPublishAsync(
                exchange: _options.DelayExchange,
                routingKey: _options.RoutingKey,
                mandatory: false,
                basicProperties: retryProperties,
                body: args.Body,
                cancellationToken: _stoppingToken).ConfigureAwait(false);

            await _channel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
            _requeuedForRetry(_logger, retryCount + 1, ex);
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
            properties.Headers = new Dictionary<string, object?>(
                source.Headers.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        return properties;
    }

    private static IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties properties, string key)
    {
        if (properties.Headers is null)
        {
            return Enumerable.Empty<string>();
        }

        if (!properties.Headers.TryGetValue(key, out var value) || value is null)
        {
            return Enumerable.Empty<string>();
        }

        return value switch
        {
            byte[] bytes => new[] { Encoding.UTF8.GetString(bytes) },
            string text => new[] { text },
            _ => Enumerable.Empty<string>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract bool TryDeserialize(ReadOnlyMemory<byte> body, out TMessage? message);

    protected abstract ValueTask ProcessMessageAsync(TMessage message, IReadOnlyBasicProperties properties, CancellationToken cancellationToken);
}
