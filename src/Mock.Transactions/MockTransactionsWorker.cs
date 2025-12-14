using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mock.Transactions;

public sealed class MockTransactionsWorker : BackgroundService
{

    private readonly TransactionDtoGeneratorFactory _factory;
    private readonly SeededAccountProvider _accountProvider;
    private readonly ApiSeedService _seedService;
    private readonly IMessagePublisher _publisher;
    private readonly IOptionsMonitor<MockTransactionsSettings> _settings;
    private readonly ILogger<MockTransactionsWorker> _logger;
    private readonly Action<ILogger, string, Exception?> _typeNotRegistered;
    private readonly Action<ILogger, string, string, Guid, Exception?> _publishedWithId;

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

        // EventId 3: Structured logging with transaction_id for observability
        // Uses LoggerMessage.Define for performance (avoids boxing with value types like Guid)
        _publishedWithId = LoggerMessage.Define<string, string, Guid>(
            LogLevel.Information,
            new EventId(3, nameof(_publishedWithId)),
            "Published mock transaction of type {Type} to {RoutingKey} with transaction_id {TransactionId}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settings.CurrentValue;
            var transactionType = settings.TransactionType;

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

            var effectiveType = transactionType;
            if (string.Equals(transactionType, "all", StringComparison.OrdinalIgnoreCase))
            {
                var types = _factory.AvailableTypes
                    .Where(type => !IsPix(type) || _accountProvider.HasPixKeys)
                    .ToList();

                if (types.Count != 0)
                {
                    effectiveType = types[Random.Shared.Next(types.Count)];
                }
            }

            if (IsPix(effectiveType) && !_accountProvider.HasPixKeys)
            {
                _logger.LogWarning("Pix publishing blocked until Pix keys are seeded or configured.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (!_factory.TryGet(effectiveType, out var generator) || generator is null)
            {
                _typeNotRegistered(_logger, effectiveType, null);
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
                _logger.LogWarning(ex, "Failed to generate transaction; generator reported invalid state.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
                continue;
            }
            
            // Extract TransactionId type-safely using pattern matching (no reflection, no JSON parsing)
            Guid transactionId = dto switch
            {
                PixTransactionDto pix => pix.TransactionId,
                CardTransactionDto card => card.TransactionId,
                MoneyTransactionDto money => money.TransactionId,
                _ => Guid.Empty
            };

            if (transactionId == Guid.Empty)
            {
                _logger.LogError("Generated transaction has empty TransactionId. Type: {Type}. Regenerating...", effectiveType);
                
                // Try regenerating the DTO once
                try
                {
                    dto = generator();
                    transactionId = dto switch
                    {
                        PixTransactionDto pix => pix.TransactionId,
                        CardTransactionDto card => card.TransactionId,
                        MoneyTransactionDto money => money.TransactionId,
                        _ => Guid.Empty
                    };
                    if (transactionId == Guid.Empty)
                    {
                        _logger.LogError("Regenerated transaction still has empty TransactionId. Skipping. Type: {Type}", effectiveType);
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to regenerate transaction. Type: {Type}", effectiveType);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                    continue;
                }
            }

            // Validate DTO-specific fields
            if (dto is PixTransactionDto pixDto)
            {
                if (string.IsNullOrWhiteSpace(pixDto.OriginPixKey) || string.IsNullOrWhiteSpace(pixDto.DestinationPixKey))
                {
                    _logger.LogError("Skipping Pix publish due to invalid Pix keys. Origin={Origin}, Destination={Destination}", pixDto.OriginPixKey, pixDto.DestinationPixKey);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                    continue;
                }
            }
            else if (dto is CardTransactionDto cardDto)
            {
                if (string.IsNullOrWhiteSpace(cardDto.CardNumber))
                {
                    _logger.LogError("Skipping Card publish due to missing card number.");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                    continue;
                }
            }

            // Defense-in-depth: Validate TransactionId in serialized JSON (single parse, no duplicate)
            var payload = SerializeDto(dto);
            var deserializedCheck = JsonSerializer.Deserialize<JsonElement>(payload);
            var propertyName = JsonNamingPolicy.CamelCase.ConvertName("TransactionId");
            if (!deserializedCheck.TryGetProperty(propertyName, out var idElement) || 
                idElement.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(idElement.GetString(), out var jsonTransactionId) ||
                jsonTransactionId != transactionId)
            {
                _logger.LogError("Serialized transaction TransactionId mismatch or missing. Type: {Type}, Expected: {ExpectedId}", effectiveType, transactionId);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                continue;
            }
            
            var messageType = $"mock.{effectiveType.ToUpperInvariant()}";
            var routingKey = string.Equals(settings.TransactionType, "all", StringComparison.OrdinalIgnoreCase) 
                ? $"{effectiveType.ToLowerInvariant()}.transactions" 
                : settings.RoutingKey;

            await _publisher.PublishAsync(messageType, payload, routingKey, stoppingToken).ConfigureAwait(false);
            _publishedWithId(_logger, effectiveType, routingKey, transactionId, null);

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

    private static bool IsPix(string type) => string.Equals(type, "pix", StringComparison.OrdinalIgnoreCase);

    private static string SerializeDto(object dto)
    {
        return dto switch
        {
            CardTransactionDto cardDto => JsonSerializer.Serialize(cardDto, TransactionDtoJsonContext.Default.CardTransactionDto),
            PixTransactionDto pixDto => JsonSerializer.Serialize(pixDto, TransactionDtoJsonContext.Default.PixTransactionDto),
            MoneyTransactionDto moneyDto => JsonSerializer.Serialize(moneyDto, TransactionDtoJsonContext.Default.MoneyTransactionDto),
            _ => throw new InvalidOperationException($"Unsupported DTO type: {dto.GetType().Name}")
        };
    }
}
