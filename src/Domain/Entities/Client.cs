namespace Domain.Entities;

public class Client : PersistenceBaseClass
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string MobileNumber { get; private set; } = string.Empty;
    public ICollection<BankAccount> BankAccounts { get; } = new List<BankAccount>();

    public Client() { }

    public Client Create(string name, string email, string phone)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        MobileNumber = phone;

        return this;
    }

    public Client Update(string name, string email, string phone)
    {
        Name = name;
        Email = email;
        MobileNumber = phone;

        return this;
    }
}
