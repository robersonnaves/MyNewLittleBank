namespace Domain.Common;

/// <summary>
/// Domain-level exception with a stable code to aid consumers and tests.
/// </summary>
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException() : base("Domain exception.")
    {
        Code = "domain_exception";
    }

    public DomainException(string message) : base(message)
    {
        Code = "domain_exception";
    }

    public DomainException(string message, Exception? innerException) : base(message, innerException)
    {
        Code = "domain_exception";
    }

    private DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    public static DomainException InvalidCpf(string cpf) =>
        new("invalid_cpf", $"CPF '{cpf}' é inválido.");

    public static DomainException InvalidAccountNumber(string accountNumber) =>
        new("invalid_account_number", $"Account number '{accountNumber}' é inválido.");

    public static DomainException InvalidMoney(string message) =>
        new("invalid_money", message);

    public static DomainException InsufficientFunds(decimal balance, decimal requested) =>
        new("insufficient_funds", $"Saldo insuficiente para debitar {requested} (saldo atual {balance}).");

    public static DomainException InvalidTransaction(string message) =>
        new("invalid_transaction", message);
}
