using System.Diagnostics;
using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UseCases.Transactions;

namespace Services.Money;

public sealed class MoneyTransactionReceiver : RabbitConsumerService<MoneyTransactionDto>
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<MoneyTransactionReceiver> _logger;
    private readonly string _expectedRoutingKey;
    private static readonly Action<ILogger, string, string, Exception?> IgnoredRoutingKey =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2, nameof(IgnoredRoutingKey)),
            "Ignoring message for Money. RoutingKey={RoutingKey}, Expected={ExpectedRoutingKey}");
    private static readonly Action<ILogger, string, Exception?> ProcessingFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(ProcessingFailed)),
            "Money transaction processing failed with error {Error}");

    public MoneyTransactionReceiver(
        IRabbitConnectionFactory factory,
        IOptions<RabbitOptions> options,
        ILogger<MoneyTransactionReceiver> logger,
        IProcessTransactionsHandler handler,
        ActivitySource activitySource)
        : base(factory, options, logger, activitySource)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _logger = logger;
        _expectedRoutingKey = options.Value.RoutingKey;
    }

    protected override bool TryDeserialize(ReadOnlyMemory<byte> body, out MoneyTransactionDto? message)
    {
        message = JsonSerializer.Deserialize(body.Span, MoneyTransactionJsonContext.Default.MoneyTransactionDto);
        return message is not null;
    }

    protected override async ValueTask ProcessMessageAsync(MoneyTransactionDto message, IReadOnlyBasicProperties properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ProcessingFailed(_logger, result.Error!, null);
            throw new InvalidOperationException(result.Error);
        }
    }

    protected override bool ShouldProcess(string routingKey, IReadOnlyBasicProperties properties)
    {
        if (string.Equals(routingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        IgnoredRoutingKey(_logger, routingKey, _expectedRoutingKey, null);
        return false;
    }
}
