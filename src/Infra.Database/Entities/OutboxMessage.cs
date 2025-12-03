using Domain.Common;
using Domain.ValueObjects;

namespace Infra.Database.Entities;

public enum OutboxMessageStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public sealed class OutboxMessage
{
    private OutboxMessage(Guid messageId, string messageType, string payload, DateTime occurredOnUtc)
    {
        MessageId = messageId;
        MessageType = messageType;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid MessageId { get; }
    public string MessageType { get; }
    public string Payload { get; }
    public DateTime OccurredOnUtc { get; }
    public DateTime? SentOnUtc { get; private set; }
    public int Attempts { get; private set; }
    public OutboxMessageStatus Status { get; private set; }

    public static Result<OutboxMessage> Create(Guid messageId, string messageType, string payload, DateTime occurredOnUtc)
    {
        if (messageId == Guid.Empty)
        {
            return Result<OutboxMessage>.Failure("outbox_message_id_empty");
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result<OutboxMessage>.Failure("outbox_message_type_empty");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return Result<OutboxMessage>.Failure("outbox_payload_empty");
        }

        if (occurredOnUtc.Kind != DateTimeKind.Utc)
        {
            return Result<OutboxMessage>.Failure("outbox_occurred_on_not_utc");
        }

        return Result<OutboxMessage>.Success(new OutboxMessage(messageId, messageType.Trim(), payload, occurredOnUtc));
    }

    public void MarkSent(DateTime sentOnUtc)
    {
        SentOnUtc = sentOnUtc;
        Status = OutboxMessageStatus.Sent;
        Attempts++;
    }

    public void MarkFailed()
    {
        Status = OutboxMessageStatus.Failed;
        Attempts++;
    }
}
