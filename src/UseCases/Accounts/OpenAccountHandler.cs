using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Accounts;

public sealed record OpenAccountCommand(Guid ClientId, string AccountNumber, decimal InitialBalance);

public interface IOpenAccountHandler
{
    Task<Result<BankAccount>> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken = default);
}

public sealed class OpenAccountHandler : IOpenAccountHandler
{
    private readonly IReadRepository<Client> _clientReader;
    private readonly IReadRepository<BankAccount> _accountReader;
    private readonly IWriteRepository<BankAccount> _accountWriter;
    private readonly IUnitOfWork _unitOfWork;

    public OpenAccountHandler(
        IReadRepository<Client> clientReader,
        IReadRepository<BankAccount> accountReader,
        IWriteRepository<BankAccount> accountWriter,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(clientReader);
        ArgumentNullException.ThrowIfNull(accountReader);
        ArgumentNullException.ThrowIfNull(accountWriter);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _clientReader = clientReader;
        _accountReader = accountReader;
        _accountWriter = accountWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BankAccount>> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var clientIdResult = ClientId.TryCreate(command.ClientId);
        if (clientIdResult.IsFailure)
        {
            return Result<BankAccount>.Failure(clientIdResult.Error!);
        }

        var accountNumberResult = AccountNumber.TryCreate(command.AccountNumber);
        if (accountNumberResult.IsFailure)
        {
            return Result<BankAccount>.Failure(accountNumberResult.Error!);
        }

        var balanceResult = Money.TryCreate(command.InitialBalance);
        if (balanceResult.IsFailure)
        {
            return Result<BankAccount>.Failure(balanceResult.Error!);
        }

        var clientExists = await _clientReader
            .ExistsAsync(client => client.Id == clientIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!clientExists)
        {
            return Result<BankAccount>.Failure("client_not_found");
        }

        var accountExists = await _accountReader
            .ExistsAsync(account => account.AccountNumber == accountNumberResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (accountExists)
        {
            return Result<BankAccount>.Failure("bank_account_already_exists");
        }

        var createResult = BankAccount.Open(clientIdResult.Value, accountNumberResult.Value, balanceResult.Value);
        if (createResult.IsFailure)
        {
            return Result<BankAccount>.Failure(createResult.Error!);
        }

        var account = createResult.Value ?? throw new InvalidOperationException("Account creation returned null.");

        await _accountWriter.AddAsync(account, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BankAccount>.Success(account);
    }
}
