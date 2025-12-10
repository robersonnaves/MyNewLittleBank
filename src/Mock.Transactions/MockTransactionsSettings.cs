namespace Mock.Transactions;

public sealed class MockTransactionsSettings
{
    public string TransactionType { get; init; } = "pix";
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
    public int Clients { get; init; } = 2;
    public int AccountsPerClient { get; init; } = 1;
    public decimal InitialBalance { get; init; } = 5000m;
}
