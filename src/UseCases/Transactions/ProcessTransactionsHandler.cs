using System.Diagnostics;
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
    private const string InsufficientFundsCode = "insufficient_funds";
    private const string ProcessedMessageType = "transaction.processed";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadRepository<BankAccount> _bankAccountReader;
    private readonly IWriteRepository<BankAccount> _bankAccountWriter;
    private readonly IReadRepository<Client> _clientReader;
    private readonly IWriteRepository<Transaction> _transactionWriter;
    private readonly INotificationSender _notificationSender;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessTransactionsHandler(
        IReadRepository<BankAccount> bankAccountReader,
        IWriteRepository<BankAccount> bankAccountWriter,
        IReadRepository<Client> clientReader,
        IWriteRepository<Transaction> transactionWriter,
        INotificationSender notificationSender,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(bankAccountReader);
        ArgumentNullException.ThrowIfNull(bankAccountWriter);
        ArgumentNullException.ThrowIfNull(clientReader);
        ArgumentNullException.ThrowIfNull(transactionWriter);
        ArgumentNullException.ThrowIfNull(notificationSender);
        ArgumentNullException.ThrowIfNull(outboxWriter);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _bankAccountReader = bankAccountReader;
        _bankAccountWriter = bankAccountWriter;
        _clientReader = clientReader;
        _transactionWriter = transactionWriter;
        _notificationSender = notificationSender;
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

        var transaction = transactionResult.Value ?? throw new InvalidOperationException("Transaction mapping returned null.");

        return await ProcessAsync(
            transaction,
            (account, mappedTransaction) => account.Debit(mappedTransaction.Amount, mappedTransaction.Id),
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

        var transaction = transactionResult.Value ?? throw new InvalidOperationException("Transaction mapping returned null.");

        return await ProcessAsync(
            transaction,
            (account, mappedTransaction) => account.Credit(mappedTransaction.Amount, mappedTransaction.Id),
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

        var transaction = transactionResult.Value ?? throw new InvalidOperationException("Transaction mapping returned null.");

        return await ProcessAsync(
            transaction,
            (account, mappedTransaction) => account.Debit(mappedTransaction.Amount, mappedTransaction.Id),
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

        var operationResult = operation(account, transaction);

        if (operationResult.IsFailure)
        {
            if (operationResult.Error == InsufficientFundsCode)
            {
                await NotifyInsufficientFundsAsync(transaction, account, cancellationToken).ConfigureAwait(false);
            }

            return Result.Failure(operationResult.Error!);
        }

        transaction.ChangeStatus(TransactionStatus.Completed);

        // Entity is already tracked, no need to call Update()
        // EF Core will automatically detect the Balance change

        await _transactionWriter.AddAsync(transaction, cancellationToken).ConfigureAwait(false);

        var payload = BuildOutboxPayload(transaction, operationResult.Value);
        await _outboxWriter.AddAsync(ProcessedMessageType, payload, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task NotifyInsufficientFundsAsync(Transaction transaction, BankAccount account, CancellationToken cancellationToken)
    {
        var client = await _clientReader
            .GetByIdAsync(new object[] { transaction.ClientId }, cancellationToken)
            .ConfigureAwait(false);

        var traceId = Activity.Current?.Id ?? ActivityTraceId.CreateRandom().ToString();
        var notification = new InsufficientFundsNotification(
            client?.Cpf.Value ?? string.Empty,
            account.AccountNumber.Value,
            transaction.Id.Value,
            transaction.Amount.Value,
            account.Balance.Value,
            transaction.OccurredOn,
            traceId);

        await _notificationSender
            .NotifyInsufficientFundsAsync(notification, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<BankAccount?> FindAccountAsync(AccountNumber accountNumber, CancellationToken cancellationToken)
    {
        return await _bankAccountReader
            .FirstOrDefaultAsync(account => account.AccountNumber == accountNumber, cancellationToken)
            .ConfigureAwait(false);
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
