using System.Text.Json;
using Domain.DTOs;
using Domain.Interfaces;
using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UseCases.Transactions;

namespace Services.Pix;

public sealed class PixTransactionReceiver : RabbitConsumerService<PixTransactionDto>
{
    private readonly IProcessTransactionsHandler _handler;
    private readonly ILogger<PixTransactionReceiver> _logger;

    public PixTransactionReceiver(
        IRabbitConnectionFactory factory,
        IOptions<RabbitOptions> options,
        ILogger<PixTransactionReceiver> logger,
        IProcessTransactionsHandler handler)
        : base(factory, options, logger)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _logger = logger;
    }

    protected override bool TryDeserialize(ReadOnlyMemory<byte> body, out PixTransactionDto? message)
    {
        message = JsonSerializer.Deserialize(body.Span, PixTransactionJsonContext.Default.PixTransactionDto);
        return message is not null;
    }

    protected override async ValueTask ProcessMessageAsync(PixTransactionDto message, IReadOnlyBasicProperties properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogWarning("Pix transaction processing failed with error {Error}", result.Error);
            throw new InvalidOperationException(result.Error);
        }
    }
}
