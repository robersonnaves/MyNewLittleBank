namespace Mock.Transactions;

public sealed class MockTransactionsSettings
{
    public string TransactionType { get; init; } = "pix";
    public string Exchange { get; init; } = "svc.transactions";
    public string RoutingKey { get; init; } = "pix.transactions";
    public double MessagesPerSecond { get; init; } = 5;
    public TimeSpan? Interval { get; init; }
}
