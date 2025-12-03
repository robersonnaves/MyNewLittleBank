namespace Domain.Entities;

public sealed class MoneyTransaction : Transaction
{
    public MoneyTransaction() { }

    public MoneyTransaction Create(Guid id, BankAccount bankAccount, MovementType movementType, int amount)
    {
        Id = id;
        SetBankAccount(bankAccount);
        SetAmount(amount);
        SetMovementType(movementType);
        SetCreatedAt(DateTime.Now);
        return this;
    }
}
