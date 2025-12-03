namespace Domain.Entities;

public sealed class PixTransaction : Transaction
{
    public string OriginPixKey { get; set; } = string.Empty;

    public string DestinationPixKey { get; set; } = string.Empty;

    public PixTransaction() { }

    public PixTransaction Create(Guid id, string originPixKey, string destinationPixKey, BankAccount bankAccount, MovementType movementType, int amount)
    {
        Id = id;
        OriginPixKey = originPixKey;
        DestinationPixKey = destinationPixKey;
        SetBankAccount(bankAccount);
        SetMovementType(movementType);
        SetAmount(amount);
        SetCreatedAt(DateTime.Now);
        return this;
    }
}
