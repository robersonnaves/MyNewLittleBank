namespace Domain.Entities;

public sealed class PixTransaction : Transaction
{
    private PixTransaction(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        string originPixKey,
        string destinationPixKey,
        TransactionStatus status,
        DateTime occurredOn) : base(id, clientId, bankAccountId, amount, status, occurredOn)
    {
        OriginPixKey = originPixKey;
        DestinationPixKey = destinationPixKey;
    }

    public string OriginPixKey { get; }
    public string DestinationPixKey { get; }

    public override TransactionType Type => TransactionType.Pix;

    public static Result<PixTransaction> Create(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        string originPixKey,
        string destinationPixKey,
        TransactionStatus status = TransactionStatus.Pending,
        DateTime? occurredOn = null)
    {
        if (string.IsNullOrWhiteSpace(originPixKey) || string.IsNullOrWhiteSpace(destinationPixKey))
        {
            return Result<PixTransaction>.Failure("pix_keys_invalid");
        }

        return Result<PixTransaction>.Success(new PixTransaction(
            id,
            clientId,
            bankAccountId,
            amount,
            originPixKey.Trim(),
            destinationPixKey.Trim(),
            status,
            occurredOn ?? DateTime.UtcNow));
    }
}
