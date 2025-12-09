using Domain.Entities;

namespace API.Contracts.Responses;

public sealed record AccountResponse(Guid Id, Guid ClientId, string AccountNumber, decimal Balance, DateTime OpenedAt)
{
    public static AccountResponse FromDomain(BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountResponse(
            account.Id,
            account.ClientId.Value,
            account.AccountNumber.Value,
            account.Balance.Value,
            account.OpenedAt);
    }
}
