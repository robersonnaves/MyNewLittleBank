using Domain.Interfaces;
using Infra.Database.Entities;

namespace Infra.Database;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly MyNewLittleBankContext _context;

    public OutboxWriter(MyNewLittleBankContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var messageResult = OutboxMessage.Create(Guid.NewGuid(), messageType, payload, DateTime.UtcNow);
        if (messageResult.IsFailure)
        {
            throw new InvalidOperationException($"Unable to create outbox message: {messageResult.Error}");
        }

        await _context.OutboxMessages.AddAsync(messageResult.Value, cancellationToken).ConfigureAwait(false);
    }
}
