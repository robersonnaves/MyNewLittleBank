using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UseCases.Transactions;

namespace Services.Card;

public sealed class CardTransactionReceiver : RabbitConsumerService<CardTransactionDto>
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<CardTransactionReceiver> _logger;

    public CardTransactionReceiver(
        IRabbitConnectionFactory factory,
        IOptions<RabbitOptions> options,
        ILogger<CardTransactionReceiver> logger,
        IProcessTransactionsHandler handler)
        : base(factory, options, logger)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _logger = logger;
    }

    protected override bool TryDeserialize(ReadOnlyMemory<byte> body, out CardTransactionDto? message)
    {
        message = JsonSerializer.Deserialize(body.Span, CardTransactionJsonContext.Default.CardTransactionDto);
        return message is not null;
    }

    protected override async ValueTask ProcessMessageAsync(CardTransactionDto message, IReadOnlyBasicProperties properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogWarning("Card transaction processing failed with error {Error}", result.Error);
            throw new InvalidOperationException(result.Error);
        }
    }
}
