using System.Diagnostics.CodeAnalysis;

namespace Domain.Entities;

public sealed class Client
{
    private readonly List<BankAccount> _bankAccounts = new();

    private Client(ClientId id, Cpf cpf, string name, string email, string mobileNumber)
    {
        Id = id;
        Cpf = cpf;
        Name = name;
        Email = email;
        MobileNumber = mobileNumber;
    }

    public ClientId Id { get; }
    public Cpf Cpf { get; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string MobileNumber { get; private set; }
    public IReadOnlyCollection<BankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    public uint Xmin { get; private set; }

    public static Result<Client> Create(ClientId id, Cpf cpf, string name, string email, string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Client>.Failure("client_name_empty");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<Client>.Failure("client_email_empty");
        }

        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return Result<Client>.Failure("client_mobile_empty");
        }

        return Result<Client>.Success(new Client(id, cpf, name.Trim(), email.Trim(), mobileNumber.Trim()));
    }

    public Result<Client> Update(string name, string email, string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mobileNumber))
        {
            return Result<Client>.Failure("client_update_invalid");
        }

        Name = name.Trim();
        Email = email.Trim();
        MobileNumber = mobileNumber.Trim();

        return Result<Client>.Success(this);
    }

    public Result<Client> AddAccount(BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (account.ClientId != Id)
        {
            return Result<Client>.Failure(DomainException.InvalidTransaction("Conta não pertence ao cliente.").Code);
        }

        _bankAccounts.Add(account);
        return Result<Client>.Success(this);
    }
}
