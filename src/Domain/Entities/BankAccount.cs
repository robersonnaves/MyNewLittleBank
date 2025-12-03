namespace Domain.Entities;

public sealed class BankAccount : PersistenceBaseClass
{
    public Client? Client { get; init; }
    public Guid ClientId { get; init; }
    public string BankBranchCode { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string PixKey { get; init; } = string.Empty;
    public long Balance { get; private set; }

    public BankAccount() { }

    public static BankAccount Create(Client client, string bankBranchCode, string accountNumber, string pixKey, long initialBalance)
    {
        if (initialBalance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBalance), "O saldo inicial deve ser positivo.");
        }

        return new BankAccount
        {
            Id = Guid.NewGuid(),
            Client = client,
            BankBranchCode = bankBranchCode,
            AccountNumber = accountNumber,
            PixKey = pixKey,
            Balance = initialBalance
        };
    }

    public static BankAccount Create(Guid clientId, string bankBranchCode, string accountNumber, string pixKey, long initialBalance)
    {
        if (initialBalance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBalance), "O saldo inicial deve ser positivo.");
        }

        return new BankAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            BankBranchCode = bankBranchCode,
            AccountNumber = accountNumber,
            PixKey = pixKey,
            Balance = initialBalance
        };
    }

    public static BankAccount Create(Guid clientId, string bankBranchCode, string accountNumber, string pixKey)
    {
        return new BankAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            BankBranchCode = bankBranchCode,
            AccountNumber = accountNumber,
            PixKey = pixKey
        };
    }

    public long MakeDeposit(long amount)
    {
        Balance += amount;
        return Balance;
    }

    public long MakeWithdrawal(long amount)
    {
        Balance -= amount;
        return Balance;
    }
}
