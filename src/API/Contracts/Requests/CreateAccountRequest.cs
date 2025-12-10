namespace API.Contracts.Requests;

public sealed record CreateAccountRequest(Guid ClientId, string AccountNumber, decimal InitialBalance);
