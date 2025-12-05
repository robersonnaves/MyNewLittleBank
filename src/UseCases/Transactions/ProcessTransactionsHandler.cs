using System.Linq;
using System.Text.Json;
using Domain.Common;
using Domain.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Transactions;

public interface IProcessTransactionsHandler
{
    Task<Result> HandleAsync(PixTransactionDto dto, CancellationToken cancellationToken = default);
    Task<Result> HandleAsync(MoneyTransactionDto dto, CancellationToken cancellationToken = default);
    Task<Result> HandleAsync(CardTransactionDto dto, CancellationToken cancellationToken = default);
}

public sealed class ProcessTransactionsHandler : IProcessTransactionsHandler
{
    private const string ProcessedMessageType = "transaction.processed";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadRepository<BankAccount> _bankAccountReader;
    private readonly IWriteRepository<BankAccount> _bankAccountWriter;
    private readonly IWriteRepository<Transaction> _transactionWriter;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessTransactionsHandler(
        IReadRepository<BankAccount> bankAccountReader,
        IWriteRepository<BankAccount> bankAccountWriter,
        IWriteRepository<Transaction> transactionWriter,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(bankAccountReader);
        ArgumentNullException.ThrowIfNull(bankAccountWriter);
        ArgumentNullException.ThrowIfNull(transactionWriter);
        ArgumentNullException.ThrowIfNull(outboxWriter);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _bankAccountReader = bankAccountReader;
        _bankAccountWriter = bankAccountWriter;
        _transactionWriter = transactionWriter;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(PixTransactionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var transactionResult = TransactionDtoMapper.ToEntity(dto);
        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error!);
        }

        return await ProcessAsync(
            transactionResult.Value,
            (account, transaction) => account.Debit(transaction.Amount, transaction.Id),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> HandleAsync(MoneyTransactionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var transactionResult = TransactionDtoMapper.ToEntity(dto);
        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error!);
        }

        return await ProcessAsync(
            transactionResult.Value,
            (account, transaction) => account.Credit(transaction.Amount, transaction.Id),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> HandleAsync(CardTransactionDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var transactionResult = TransactionDtoMapper.ToEntity(dto);
        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error!);
        }

        return await ProcessAsync(
            transactionResult.Value,
            (account, transaction) => account.Debit(transaction.Amount, transaction.Id),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> ProcessAsync(
        Transaction transaction,
        Func<BankAccount, Transaction, Result<Money>> operation,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(transaction.BankAccountId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Result.Failure("bank_account_not_found");
        }

        Result<Money> operationResult;
        try
        {
            operationResult = operation(account, transaction);
        }
        catch (DomainException domainException)
        {
            return Result.Failure(domainException.Code);
        }

        if (operationResult.IsFailure)
        {
            return Result.Failure(operationResult.Error!);
        }

        transaction.ChangeStatus(TransactionStatus.Completed);

        await _transactionWriter.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        _bankAccountWriter.Update(account);

        var payload = BuildOutboxPayload(transaction, operationResult.Value);
        await _outboxWriter.AddAsync(ProcessedMessageType, payload, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<BankAccount?> FindAccountAsync(AccountNumber accountNumber, CancellationToken cancellationToken)
    {
        var accounts = await _bankAccountReader
            .ListAsync(account => account.AccountNumber == accountNumber, cancellationToken)
            .ConfigureAwait(false);

        return accounts.FirstOrDefault();
    }

    private static string BuildOutboxPayload(Transaction transaction, Money balanceAfterOperation)
    {
        var message = new TransactionProcessedMessage(
            transaction.Id.Value,
            transaction.Type.ToString(),
            transaction.BankAccountId.Value,
            transaction.Amount.Value,
            balanceAfterOperation.Value,
            transaction.Status.ToString(),
            transaction.OccurredOn);

        return JsonSerializer.Serialize(message, SerializerOptions);
    }

    private sealed record TransactionProcessedMessage(
        Guid TransactionId,
        string TransactionType,
        string AccountNumber,
        decimal Amount,
        decimal BalanceAfterOperation,
        string Status,
        DateTime OccurredOn);
}
