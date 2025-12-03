namespace Domain.Entities;

public sealed class MoneyTransaction : Transaction
{
    private MoneyTransaction(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        TransactionStatus status,
        DateTime occurredOn) : base(id, clientId, bankAccountId, amount, status, occurredOn)
    {
    }

    public override TransactionType Type => TransactionType.Money;

    public static Result<MoneyTransaction> Create(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        TransactionStatus status = TransactionStatus.Pending,
        DateTime? occurredOn = null)
    {
        if (amount < Money.Zero)
        {
            return Result<MoneyTransaction>.Failure("amount_negative");
        }

        return Result<MoneyTransaction>.Success(new MoneyTransaction(
            id,
            clientId,
            bankAccountId,
            amount,
            status,
            occurredOn ?? DateTime.UtcNow));
    }
}
