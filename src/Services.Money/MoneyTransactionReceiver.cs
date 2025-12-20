using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Domain.Messaging;
using Infra.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UseCases.Transactions;

namespace Services.Money;

public sealed class MoneyTransactionReceiver : IMessageConsumer
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<MoneyTransactionReceiver> _logger;
    private readonly string _expectedRoutingKey;
    
    // Business errors that should not trigger message retry
    private static readonly HashSet<string> BusinessErrors = new()
    {
        "bank_account_not_found",
        "insufficient_funds",
        "client_not_found",
        "invalid_cpf",
        "invalid_account_number",
        "invalid_money",
        "invalid_transaction"
    };
    
    private static readonly Action<ILogger, string, string, Exception?> IgnoredRoutingKey =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2, nameof(IgnoredRoutingKey)),
            "Ignoring message for Money. RoutingKey={RoutingKey}, Expected={ExpectedRoutingKey}");
    private static readonly Action<ILogger, string, Exception?> BusinessErrorOccurred =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(BusinessErrorOccurred)),
            "Money transaction rejected due to business rule: {Error}");
    private static readonly Action<ILogger, string, Exception?> TechnicalErrorOccurred =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5, nameof(TechnicalErrorOccurred)),
            "Money transaction processing failed due to technical error: {Error}");
    private static readonly Action<ILogger, string, Exception?> IgnoredMessageType =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3, nameof(IgnoredMessageType)),
            "Ignoring non-transaction message type {MessageType} in Money receiver");
    private static readonly Action<ILogger, string, Exception?> DeserializationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4, nameof(DeserializationFailed)),
            "Money transaction payload could not be deserialized for routing key {RoutingKey}");

    public MoneyTransactionReceiver(
        IOptions<RabbitOptions> options,
        ILogger<MoneyTransactionReceiver> logger,
        IProcessTransactionsHandler handler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        _logger = logger;
        _expectedRoutingKey = options.Value.RoutingKey;
    }

    public bool CanHandle(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return string.Equals(envelope.RoutingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!CanHandle(envelope))
        {
            IgnoredRoutingKey(_logger, envelope.RoutingKey, _expectedRoutingKey, null);
            return;
        }

        if (!IsTransactionMessage(envelope.MessageType))
        {
            IgnoredMessageType(_logger, envelope.MessageType, null);
            return;
        }

        var message = JsonSerializer.Deserialize(envelope.Payload, MoneyTransactionJsonContext.Default.MoneyTransactionDto);
        if (message is null)
        {
            DeserializationFailed(_logger, envelope.RoutingKey, null);
            return;
        }

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            // Business errors should not cause retry - message already processed
            if (BusinessErrors.Contains(result.Error!))
            {
                BusinessErrorOccurred(_logger, result.Error!, null);
                return; // Acknowledge message without retry
            }
            
            // Technical errors should retry
            TechnicalErrorOccurred(_logger, result.Error!, null);
            throw new InvalidOperationException(result.Error);
        }
    }

    private static bool IsTransactionMessage(string messageType)
    {
        return messageType.StartsWith("mock.", StringComparison.OrdinalIgnoreCase);
    }
}
