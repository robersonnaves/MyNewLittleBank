namespace Domain.DTOs;

public sealed record PixTransactionDto(
    Guid TransactionId,
    Guid ClientId,
    string AccountNumber,
    decimal Amount,
    string OriginPixKey,
    string DestinationPixKey,
    TransactionStatus Status,
    DateTime OccurredOn);
