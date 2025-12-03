using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Database;

public sealed class InboxMessageStore
{
    private readonly Func<MyNewLittleBankContext> _contextFactory;

    public InboxMessageStore(Func<MyNewLittleBankContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    public async Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        using var context = _contextFactory();

        var exists = await context.InboxMessages.AnyAsync(
            inbox => inbox.MessageId == messageId && inbox.Consumer == consumer,
            cancellationToken).ConfigureAwait(false);

        if (exists)
        {
            return false;
        }

        var inboxResult = InboxMessage.Create(messageId, consumer, DateTime.UtcNow);
        if (inboxResult.IsFailure)
        {
            throw new InvalidOperationException(inboxResult.Error);
        }

        await context.InboxMessages.AddAsync(inboxResult.Value!, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
