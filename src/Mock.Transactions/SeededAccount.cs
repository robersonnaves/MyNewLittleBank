namespace Mock.Transactions;

public sealed record SeededAccount(Guid ClientId, string AccountNumber, IReadOnlyList<string> PixKeys);
