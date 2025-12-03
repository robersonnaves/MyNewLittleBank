namespace Domain.Entities;

public sealed class BankAccount
{
    private readonly List<TransactionId> _transactions = new();

    private BankAccount(Guid id, ClientId clientId, AccountNumber accountNumber, Money balance)
    {
        Id = id;
        ClientId = clientId;
        AccountNumber = accountNumber;
        Balance = balance;
        OpenedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public ClientId ClientId { get; }
    public AccountNumber AccountNumber { get; }
    public Money Balance { get; private set; }
    public DateTime OpenedAt { get; }
    public IReadOnlyCollection<TransactionId> Transactions => _transactions.AsReadOnly();
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static Result<BankAccount> Open(ClientId clientId, AccountNumber accountNumber, Money initialBalance)
    {
        if (initialBalance < Money.Zero)
        {
            return Result<BankAccount>.Failure(DomainException.InvalidMoney("Saldo inicial não pode ser negativo.").Code);
        }

        return Result<BankAccount>.Success(new BankAccount(Guid.NewGuid(), clientId, accountNumber, initialBalance));
    }

    public Result<Money> Credit(Money amount, TransactionId? transactionId = null)
    {
        if (amount <= Money.Zero)
        {
            return Result<Money>.Failure(DomainException.InvalidMoney("Valor de crédito deve ser positivo.").Code);
        }

        Balance += amount;

        if (transactionId.HasValue)
        {
            RegisterTransaction(transactionId.Value);
        }

        return Result<Money>.Success(Balance);
    }

    public Result<Money> Debit(Money amount, TransactionId? transactionId = null)
    {
        if (amount <= Money.Zero)
        {
            return Result<Money>.Failure(DomainException.InvalidMoney("Valor de débito deve ser positivo.").Code);
        }

        if (amount > Balance)
        {
            throw DomainException.InsufficientFunds(Balance.Value, amount.Value);
        }

        Balance -= amount;

        if (transactionId.HasValue)
        {
            RegisterTransaction(transactionId.Value);
        }

        return Result<Money>.Success(Balance);
    }

    private void RegisterTransaction(TransactionId transactionId)
    {
        if (!_transactions.Contains(transactionId))
        {
            _transactions.Add(transactionId);
        }
    }
}
