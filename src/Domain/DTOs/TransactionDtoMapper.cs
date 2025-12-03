using Domain.Entities;

namespace Domain.DTOs;

public static class TransactionDtoMapper
{
    public static PixTransactionDto ToDto(PixTransaction entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new PixTransactionDto(entity.Id.Value, entity.ClientId.Value, entity.BankAccountId.Value, entity.Amount.Value, entity.OriginPixKey, entity.DestinationPixKey, entity.Status, entity.OccurredOn);
    }

    public static MoneyTransactionDto ToDto(MoneyTransaction entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new MoneyTransactionDto(entity.Id.Value, entity.ClientId.Value, entity.BankAccountId.Value, entity.Amount.Value, entity.Status, entity.OccurredOn);
    }

    public static CardTransactionDto ToDto(CardTransaction entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new CardTransactionDto(entity.Id.Value, entity.ClientId.Value, entity.BankAccountId.Value, entity.Amount.Value, entity.CardNumber, entity.Status, entity.OccurredOn);
    }

    public static Result<PixTransaction> ToEntity(PixTransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var common = BuildCommon(dto.TransactionId, dto.ClientId, dto.AccountNumber, dto.Amount);
        if (common.IsFailure) return Result<PixTransaction>.Failure(common.Error!);

        var (id, clientId, accountNumber, money) = common.Value;
        return PixTransaction.Create(id, clientId, accountNumber, money, dto.OriginPixKey, dto.DestinationPixKey, dto.Status, dto.OccurredOn);
    }

    public static Result<MoneyTransaction> ToEntity(MoneyTransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var common = BuildCommon(dto.TransactionId, dto.ClientId, dto.AccountNumber, dto.Amount);
        if (common.IsFailure) return Result<MoneyTransaction>.Failure(common.Error!);

        var (id, clientId, accountNumber, money) = common.Value;
        return MoneyTransaction.Create(id, clientId, accountNumber, money, dto.Status, dto.OccurredOn);
    }

    public static Result<CardTransaction> ToEntity(CardTransactionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var common = BuildCommon(dto.TransactionId, dto.ClientId, dto.AccountNumber, dto.Amount);
        if (common.IsFailure) return Result<CardTransaction>.Failure(common.Error!);

        var (id, clientId, accountNumber, money) = common.Value;
        return CardTransaction.Create(id, clientId, accountNumber, money, dto.CardNumber, dto.Status, dto.OccurredOn);
    }

    private static Result<(TransactionId Id, ClientId ClientId, AccountNumber AccountNumber, Money Money)> BuildCommon(
        Guid transactionId,
        Guid clientId,
        string accountNumber,
        decimal amount)
    {
        var idResult = TransactionId.TryCreate(transactionId);
        if (idResult.IsFailure) return Result<(TransactionId, ClientId, AccountNumber, Money)>.Failure(idResult.Error!);

        var clientIdResult = ClientId.TryCreate(clientId);
        if (clientIdResult.IsFailure) return Result<(TransactionId, ClientId, AccountNumber, Money)>.Failure(clientIdResult.Error!);

        var accountResult = AccountNumber.TryCreate(accountNumber);
        if (accountResult.IsFailure) return Result<(TransactionId, ClientId, AccountNumber, Money)>.Failure(accountResult.Error!);

        var moneyResult = Money.TryCreate(amount);
        if (moneyResult.IsFailure) return Result<(TransactionId, ClientId, AccountNumber, Money)>.Failure(moneyResult.Error!);

        return Result<(TransactionId, ClientId, AccountNumber, Money)>.Success(
            (idResult.Value, clientIdResult.Value, accountResult.Value, moneyResult.Value));
    }
}
