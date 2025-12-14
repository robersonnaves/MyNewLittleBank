using System.Diagnostics.CodeAnalysis;
using Domain.Interfaces;
using Infra.Database.Entities;
using Infra.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infra.Database;

public sealed class OutboxOptions
{
    public int BatchSize { get; init; } = 50;
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
}

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Dispatcher must capture and log failures to retry later without crashing the host.")]
public sealed class OutboxDispatcher : BackgroundService
{
    private static readonly Action<ILogger, Guid, Exception?> OutboxDispatchFailed =
        LoggerMessage.Define<Guid>(
            logLevel: LogLevel.Error,
            eventId: new EventId(1, nameof(OutboxDispatchFailed)),
            formatString: "Failed to dispatch outbox message {MessageId}");

    private readonly Func<MyNewLittleBankContext> _contextFactory;
    private readonly IMessagePublisher _publisher;
    private readonly OutboxOptions _options;
    private readonly RabbitOptions _rabbitOptions;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        Func<MyNewLittleBankContext> contextFactory,
        IMessagePublisher publisher,
        IOptions<OutboxOptions> options,
        IOptions<RabbitOptions> rabbitOptions,
        ILogger<OutboxDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rabbitOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _contextFactory = contextFactory;
        _publisher = publisher;
        _logger = logger;
        _options = options.Value;
        _rabbitOptions = rabbitOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchPendingAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var context = _contextFactory();

        var pending = await context.OutboxMessages
            .Where(message => message.Status != OutboxMessageStatus.Sent)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                await _publisher.PublishAsync(message.MessageType, message.Payload, _rabbitOptions.RoutingKey, cancellationToken).ConfigureAwait(false);
                message.MarkSent(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                OutboxDispatchFailed(_logger, message.MessageId, ex);
                message.MarkFailed();
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
