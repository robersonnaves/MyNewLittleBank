namespace Domain.DTOs;

public sealed record InsufficientFundsNotification(
    string Cpf,
    string AccountNumber,
    Guid TransactionId,
    decimal AttemptedAmount,
    decimal AvailableBalance,
    DateTime OccurredAt,
    string TraceId,
    string Reason = "insufficient_funds");
