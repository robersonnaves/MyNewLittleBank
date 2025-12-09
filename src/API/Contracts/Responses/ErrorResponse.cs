namespace API.Contracts.Responses;

public sealed record ErrorResponse(string Code, string? Detail = null);
