namespace Mock.Transactions;

public sealed class SeededAccountProvider
{
    private readonly Random _random = new();
    private IReadOnlyList<SeededAccount> _accounts = Array.Empty<SeededAccount>();

    public bool HasAccounts => _accounts.Count > 0;

    public void SetAccounts(IReadOnlyList<SeededAccount> accounts)
    {
        _accounts = accounts ?? Array.Empty<SeededAccount>();
    }

    public SeededAccount? Next()
    {
        if (_accounts.Count == 0)
        {
            return null;
        }

        var index = _random.Next(_accounts.Count);
        return _accounts[index];
    }
}
