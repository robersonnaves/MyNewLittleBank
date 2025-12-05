using System.Diagnostics.CodeAnalysis;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infra.Message;

[SuppressMessage("Usage", "CA1810:Initialize reference type static fields inline", Justification = "LoggerMessage pattern for performance.")]
public sealed class RabbitTopologyBootstrapper : IRabbitTopologyBootstrapper
{
    private static readonly Action<ILogger, Exception?> TopologyEnsured =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(TopologyEnsured)),
            "RabbitMQ topology ensured");

    private readonly IRabbitConnectionFactory _factory;
    private readonly RabbitOptions _options;
    private readonly ILogger<RabbitTopologyBootstrapper> _logger;

    public RabbitTopologyBootstrapper(IRabbitConnectionFactory factory, IOptions<RabbitOptions> options, ILogger<RabbitTopologyBootstrapper> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        RabbitOptions.Validate(options.Value);

        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureTopologyAsync(CancellationToken cancellationToken = default)
    {
        var channel = await _factory.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        try
        {

        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.ExchangeDeclareAsync(_options.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.ExchangeDeclareAsync(_options.DelayExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = _options.RoutingKey
        };

        await channel.QueueDeclareAsync(_options.Queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(_options.Queue, _options.Exchange, routingKey: _options.RoutingKey, cancellationToken: cancellationToken).ConfigureAwait(false);

        var delayArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.Exchange,
            ["x-dead-letter-routing-key"] = _options.RoutingKey,
            ["x-message-ttl"] = _options.RetryDelayMilliseconds
        };

        await channel.QueueDeclareAsync(_options.DelayQueue, durable: true, exclusive: false, autoDelete: false, arguments: delayArguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(_options.DelayQueue, _options.DelayExchange, routingKey: _options.RoutingKey, cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueDeclareAsync(_options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(_options.DeadLetterQueue, _options.DeadLetterExchange, routingKey: _options.RoutingKey, cancellationToken: cancellationToken).ConfigureAwait(false);

        TopologyEnsured(_logger, null);
        }
        finally
        {
            channel.Dispose();
        }
    }
}
