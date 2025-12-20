using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Domain.Messaging;
using Infra.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UseCases.Transactions;

namespace Services.Card;

public sealed class CardTransactionReceiver : IMessageConsumer
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<CardTransactionReceiver> _logger;
    private readonly string _expectedRoutingKey;
    private static readonly Action<ILogger, string, string, Exception?> IgnoredRoutingKey =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2, nameof(IgnoredRoutingKey)),
            "Ignoring message for Card. RoutingKey={RoutingKey}, Expected={ExpectedRoutingKey}");
    private static readonly Action<ILogger, string, Exception?> ProcessingFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(ProcessingFailed)),
            "Card transaction processing failed with error {Error}");
    private static readonly Action<ILogger, string, Exception?> IgnoredMessageType =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3, nameof(IgnoredMessageType)),
            "Ignoring non-transaction message type {MessageType} in Card receiver");
    private static readonly Action<ILogger, string, Exception?> DeserializationFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4, nameof(DeserializationFailed)),
            "Card transaction payload could not be deserialized for routing key {RoutingKey}");

    public CardTransactionReceiver(
        IOptions<RabbitOptions> options,
        ILogger<CardTransactionReceiver> logger,
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

        var message = JsonSerializer.Deserialize(envelope.Payload, CardTransactionJsonContext.Default.CardTransactionDto);
        if (message is null)
        {
            DeserializationFailed(_logger, envelope.RoutingKey, null);
            return;
        }

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProcessingFailed(_logger, result.Error!, null);
            
            // Business errors should not cause retry - message already processed
            if (result.Error == "bank_account_not_found" || 
                result.Error == "insufficient_funds")
            {
                return; // Acknowledge message without retry
            }
            
            // Technical errors should retry
            throw new InvalidOperationException(result.Error);
        }
    }

    private static bool IsTransactionMessage(string messageType)
    {
        return messageType.StartsWith("mock.", StringComparison.OrdinalIgnoreCase);
    }
}
