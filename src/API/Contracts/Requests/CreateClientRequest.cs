namespace API.Contracts.Requests;

public sealed record CreateClientRequest(string Cpf, string Name, string Email, string MobileNumber);
