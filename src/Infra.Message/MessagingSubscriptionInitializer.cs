using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Bus;

namespace Infra.Message;

public sealed class MessagingSubscriptionInitializer : IHostedService
{
    private readonly IBus _bus;
    private readonly RabbitOptions _options;
    private readonly ILogger<MessagingSubscriptionInitializer> _logger;
    private readonly IEnumerable<Domain.Interfaces.IMessageConsumer> _consumers;

    public MessagingSubscriptionInitializer(
        IBus bus,
        IOptions<RabbitOptions> options,
        IEnumerable<Domain.Interfaces.IMessageConsumer> consumers,
        ILogger<MessagingSubscriptionInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(consumers);
        ArgumentNullException.ThrowIfNull(logger);

        RabbitOptions.Validate(options.Value);

        _bus = bus;
        _options = options.Value;
        _consumers = consumers;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_consumers.Any())
        {
            _logger.LogInformation("No message consumers registered; skipping topic subscription for routing key {RoutingKey}", _options.RoutingKey);
            return;
        }

        await _bus.Advanced.Topics.Subscribe(_options.RoutingKey).ConfigureAwait(false);
        _logger.LogInformation("Subscribed to routing key/topic {RoutingKey}", _options.RoutingKey);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
