using System.Text.Json;
using Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mock.Transactions;

public sealed class MockTransactionsWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly TransactionDtoGeneratorFactory _factory;
    private readonly SeededAccountProvider _accountProvider;
    private readonly ApiSeedService _seedService;
    private readonly IMessagePublisher _publisher;
    private readonly IOptionsMonitor<MockTransactionsSettings> _settings;
    private readonly ILogger<MockTransactionsWorker> _logger;
    private readonly Action<ILogger, string, Exception?> _typeNotRegistered;
    private readonly Action<ILogger, string, string, Exception?> _published;

    public MockTransactionsWorker(
        TransactionDtoGeneratorFactory factory,
        SeededAccountProvider accountProvider,
        ApiSeedService seedService,
        IMessagePublisher publisher,
        IOptionsMonitor<MockTransactionsSettings> settings,
        ILogger<MockTransactionsWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(accountProvider);
        ArgumentNullException.ThrowIfNull(seedService);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _factory = factory;
        _accountProvider = accountProvider;
        _seedService = seedService;
        _publisher = publisher;
        _settings = settings;
        _logger = logger;

        _typeNotRegistered = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(_typeNotRegistered)),
            "Transaction type {Type} is not registered; skipping publish.");

        _published = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(_published)),
            "Published mock transaction of type {Type} to {RoutingKey}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settings.CurrentValue;

            if (!_accountProvider.HasAccounts)
            {
                var seeded = settings.Seed.Enabled
                    ? await _seedService.TrySeedAsync(stoppingToken).ConfigureAwait(false)
                    : Array.Empty<SeededAccount>();

                if (seeded is null || seeded.Count == 0)
                {
                    _logger.LogWarning("No seeded accounts available; delaying publish until seed succeeds.");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _accountProvider.SetAccounts(seeded);
            }

            if (!_factory.TryGet(settings.TransactionType, out var generator) || generator is null)
            {
                _typeNotRegistered(_logger, settings.TransactionType, null);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                continue;
            }

            object dto;
            try
            {
                dto = generator();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to generate transaction because no accounts are available yet.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                continue;
            }
            
            // Validate DTO has valid transaction ID
            var transactionId = dto.GetType().GetProperty("TransactionId")?.GetValue(dto) as Guid?;
            if (transactionId == null || transactionId == Guid.Empty)
            {
                _logger.LogError("Generated transaction has empty or null TransactionId. Type: {Type}", settings.TransactionType);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                continue;
            }
            
            var payload = JsonSerializer.Serialize(dto, SerializerOptions);
            var messageType = $"mock.{settings.TransactionType.ToUpperInvariant()}";

            await _publisher.PublishAsync(messageType, payload, stoppingToken).ConfigureAwait(false);
            _published(_logger, settings.TransactionType, settings.RoutingKey, null);

            await Task.Delay(CalculateDelay(settings), stoppingToken).ConfigureAwait(false);
        }
    }

    private static TimeSpan CalculateDelay(MockTransactionsSettings settings)
    {
        if (settings.Interval.HasValue && settings.Interval.Value > TimeSpan.Zero)
        {
            return settings.Interval.Value;
        }

        if (settings.MessagesPerSecond <= 0)
        {
            return TimeSpan.FromSeconds(1);
        }

        var delaySeconds = 1.0 / settings.MessagesPerSecond;
        return TimeSpan.FromSeconds(delaySeconds);
    }
}
