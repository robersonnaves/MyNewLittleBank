using Domain.Common;

namespace Infra.Database.Entities;

public sealed class InboxMessage
{
    private InboxMessage(Guid messageId, string consumer, DateTime processedOnUtc)
    {
        MessageId = messageId;
        Consumer = consumer;
        ProcessedOnUtc = processedOnUtc;
    }

    public Guid MessageId { get; }
    public string Consumer { get; }
    public DateTime ProcessedOnUtc { get; }

    public static Result<InboxMessage> Create(Guid messageId, string consumer, DateTime processedOnUtc)
    {
        if (messageId == Guid.Empty)
        {
            return Result<InboxMessage>.Failure("inbox_message_id_empty");
        }

        if (string.IsNullOrWhiteSpace(consumer))
        {
            return Result<InboxMessage>.Failure("inbox_consumer_empty");
        }

        if (processedOnUtc.Kind != DateTimeKind.Utc)
        {
            return Result<InboxMessage>.Failure("inbox_processed_on_not_utc");
        }

        return Result<InboxMessage>.Success(new InboxMessage(messageId, consumer.Trim(), processedOnUtc));
    }
}
