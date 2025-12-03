namespace Domain.DTOs;

public sealed record MoneyTransactionDto(
    Guid TransactionId,
    Guid ClientId,
    string AccountNumber,
    decimal Amount,
    TransactionStatus Status,
    DateTime OccurredOn);
