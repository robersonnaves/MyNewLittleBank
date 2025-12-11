namespace Mock.Transactions;

public sealed class MockTransactionsSettings
{
    public string TransactionType { get; init; } = "all";
    public string Exchange { get; init; } = "svc.transactions";
    public string RoutingKey { get; init; } = "pix.transactions";
    public double MessagesPerSecond { get; init; } = 5;
    public TimeSpan? Interval { get; init; }
    public string ApiBaseUrl { get; init; } = "http://localhost:8080";
    public SeedSettings Seed { get; init; } = new();
}

public sealed class SeedSettings
{
    public bool Enabled { get; init; } = true;
    public bool ReuseExisting { get; init; } = true;
    public int Clients { get; init; } = 10;
    public int MinAccountsPerClient { get; init; } = 1;
    public int MaxAccountsPerClient { get; init; } = 3;
    public decimal InitialBalance { get; init; } = 5000m;
}
