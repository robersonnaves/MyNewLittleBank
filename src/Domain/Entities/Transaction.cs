using Domain.Interfaces;

namespace Domain.Entities;

public abstract class Transaction : PersistenceBaseClass, ITransaction
{
    public virtual Client? Client { get; private set; }
    public virtual Guid ClientId { get; private set; }
    public virtual BankAccount? BankAccount { get; private set; }
    public virtual Guid BankAccountId { get; private set; }
    public virtual MovementType MovementType { get; private set; }
    public virtual int Amount { get; private set; }
    public virtual bool IsCanceled { get; private set; }
    public virtual DateTime? CanceledAt { get; private set; }

    protected Transaction() { }

    public void SetClient(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Client = client;
        ClientId = client.Id;
    }

    public void SetBankAccount(BankAccount bankAccount)
    {
        ArgumentNullException.ThrowIfNull(bankAccount);

        BankAccount = bankAccount;
        BankAccountId = bankAccount.Id;
    }

    public void SetCreatedAt(DateTime createdAt)
    {
        CreatedAt = createdAt;
    }    

    public void SetAmount(int amount)
    {
        Amount = amount;
    }

    public void SetMovementType(MovementType movementType)
    {
        MovementType = movementType;
    }

    public Transaction Cancel()
    {
        IsCanceled = true;
        CanceledAt = DateTime.UtcNow;

        return this;
    }

    public decimal GetDecimalAmount()
    {
        return Amount / 100m;
    }
}
