namespace Domain.DTOs;

public sealed record CardTransactionDto(
    Guid TransactionId,
    Guid ClientId,
    string AccountNumber,
    decimal Amount,
    string CardNumber,
    TransactionStatus Status,
    DateTime OccurredOn);
