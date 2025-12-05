using System.Text.Json;
using Domain.Interfaces;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.Heartbeats;

public sealed class HeartbeatPublisher : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMessagePublisher _publisher;
    private readonly HeartbeatOptions _options;
    private readonly ILogger<HeartbeatPublisher> _logger;

    public HeartbeatPublisher(
        IMessagePublisher publisher,
        IOptions<HeartbeatOptions> options,
        ILogger<HeartbeatPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var heartbeat = new HeartbeatDto(_options.ServiceName, "Alive", DateTime.UtcNow);
            var payload = JsonSerializer.Serialize(heartbeat, HeartbeatJsonContext.Default.HeartbeatDto);

            try
            {
                await _publisher.PublishAsync(_options.MessageType, payload, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish heartbeat for {Service}", _options.ServiceName);
            }

            await Task.Delay(_options.Interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
