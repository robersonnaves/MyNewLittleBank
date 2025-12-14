using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Domain.Interfaces;
using Infra.Message;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Heartbeats;

public sealed class HeartbeatPublisher : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> PublishFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(PublishFailed)),
            "Failed to publish heartbeat for {Service}");

    private readonly IMessagePublisher _publisher;
    private readonly HeartbeatOptions _options;
    private readonly RabbitOptions _rabbitOptions;
    private readonly ILogger<HeartbeatPublisher> _logger;

    public HeartbeatPublisher(
        IMessagePublisher publisher,
        IOptions<HeartbeatOptions> options,
        IOptions<RabbitOptions> rabbitOptions,
        ILogger<HeartbeatPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _publisher = publisher;
        _options = options.Value;
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var heartbeat = new HeartbeatDto(_options.ServiceName, "Alive", DateTime.UtcNow);
            var payload = JsonSerializer.Serialize(heartbeat, HeartbeatJsonContext.Default.HeartbeatDto);

            await SafePublishAsync(payload, stoppingToken).ConfigureAwait(false);

            await Task.Delay(_options.Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Heartbeat must continue running even if a publish fails; exception is logged for observability.")]
    private async Task SafePublishAsync(string payload, CancellationToken stoppingToken)
    {
        try
        {
            await _publisher.PublishAsync(_options.MessageType, payload, _rabbitOptions.RoutingKey, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PublishFailed(_logger, _options.ServiceName, ex);
        }
    }
}
