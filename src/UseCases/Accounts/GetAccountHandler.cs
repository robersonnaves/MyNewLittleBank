using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Accounts;

public sealed record GetAccountQuery(string AccountNumber);

public interface IGetAccountHandler
{
    Task<Result<BankAccount>> HandleAsync(GetAccountQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAccountHandler : IGetAccountHandler
{
    private readonly IReadRepository<BankAccount> _accountReader;

    public GetAccountHandler(IReadRepository<BankAccount> accountReader)
    {
        ArgumentNullException.ThrowIfNull(accountReader);
        _accountReader = accountReader;
    }

    public async Task<Result<BankAccount>> HandleAsync(GetAccountQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountNumberResult = AccountNumber.TryCreate(query.AccountNumber);
        if (accountNumberResult.IsFailure)
        {
            return Result<BankAccount>.Failure(accountNumberResult.Error!);
        }

        var accounts = await _accountReader
            .ListAsync(account => account.AccountNumber == accountNumberResult.Value, cancellationToken)
            .ConfigureAwait(false);

        if (accounts.Count == 0)
        {
            return Result<BankAccount>.Failure("bank_account_not_found");
        }

        var account = accounts[0];
        return Result<BankAccount>.Success(account);
    }
}
