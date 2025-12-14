using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Domain.Messaging;
using Infra.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UseCases.Transactions;

namespace Services.Pix;

public sealed class PixTransactionReceiver : IMessageConsumer
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<PixTransactionReceiver> _logger;
    private readonly string _expectedRoutingKey;
    private static readonly Action<ILogger, string, string, Exception?> IgnoredRoutingKey =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2, nameof(IgnoredRoutingKey)),
            "Ignoring message for Pix. RoutingKey={RoutingKey}, Expected={ExpectedRoutingKey}");
    private static readonly Action<ILogger, string, Exception?> ProcessingFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(ProcessingFailed)),
            "Pix transaction processing failed with error {Error}");

    public PixTransactionReceiver(
        IOptions<RabbitOptions> options,
        ILogger<PixTransactionReceiver> logger,
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

        var message = JsonSerializer.Deserialize(envelope.Payload, PixTransactionJsonContext.Default.PixTransactionDto);
        if (message is null)
        {
            _logger.LogWarning("Pix transaction payload could not be deserialized for routing key {RoutingKey}", envelope.RoutingKey);
            return;
        }

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProcessingFailed(_logger, result.Error!, null);
            throw new InvalidOperationException(result.Error);
        }
    }
}
