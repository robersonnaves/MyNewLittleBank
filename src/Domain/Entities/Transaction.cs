namespace Domain.Entities;

public abstract class Transaction
{
    protected Transaction(TransactionId id, ClientId clientId, AccountNumber bankAccountId, Money amount, TransactionStatus status, DateTime occurredOn)
    {
        Id = id;
        ClientId = clientId;
        BankAccountId = bankAccountId;
        Amount = amount;
        Status = status;
        OccurredOn = occurredOn;
    }

    public TransactionId Id { get; }
    public ClientId ClientId { get; }
    public AccountNumber BankAccountId { get; }
    public Money Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime OccurredOn { get; private set; }

    public Transaction ChangeStatus(TransactionStatus status)
    {
        Status = status;
        return this;
    }

    public Transaction SetOccurredOn(DateTime occurredOn)
    {
        OccurredOn = occurredOn;
        return this;
    }

    protected void SetAmount(Money amount)
    {
        Amount = amount;
    }

    public abstract TransactionType Type { get; }
}
