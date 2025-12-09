using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Accounts;

public sealed record GetAccountBalanceQuery(string AccountNumber);

public interface IGetAccountBalanceHandler
{
    Task<Result<Money>> HandleAsync(GetAccountBalanceQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAccountBalanceHandler : IGetAccountBalanceHandler
{
    private readonly IReadRepository<BankAccount> _accountReader;

    public GetAccountBalanceHandler(IReadRepository<BankAccount> accountReader)
    {
        ArgumentNullException.ThrowIfNull(accountReader);
        _accountReader = accountReader;
    }

    public async Task<Result<Money>> HandleAsync(GetAccountBalanceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accountNumberResult = AccountNumber.TryCreate(query.AccountNumber);
        if (accountNumberResult.IsFailure)
        {
            return Result<Money>.Failure(accountNumberResult.Error!);
        }

        var accounts = await _accountReader
            .ListAsync(account => account.AccountNumber == accountNumberResult.Value, cancellationToken)
            .ConfigureAwait(false);

        if (accounts.Count == 0)
        {
            return Result<Money>.Failure("bank_account_not_found");
        }

        var account = accounts[0];
        return Result<Money>.Success(account.Balance);
    }
}
