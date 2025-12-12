using System.Linq;

namespace Mock.Transactions;

public sealed class SeededAccountProvider
{
    private readonly Random _random = new();
    private IReadOnlyList<SeededAccount> _accounts = Array.Empty<SeededAccount>();

    public bool HasAccounts => _accounts.Count > 0;
    public bool HasPixKeys => _accounts.Any(account => account.PixKeys.Any(IsValidPixKey));

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

    public (SeededAccount Account, string PixKey) NextPixKey(string? excludePixKey = null, bool allowFallback = true)
    {
        var candidates = BuildPixKeyCandidates(excludePixKey);

        if (candidates.Count == 0 && allowFallback && excludePixKey is not null)
        {
            candidates = BuildPixKeyCandidates();
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No Pix keys available for generation.");
        }

        var index = _random.Next(candidates.Count);
        return candidates[index];
    }

    private List<(SeededAccount Account, string PixKey)> BuildPixKeyCandidates(string? excludePixKey = null)
    {
        return _accounts
            .Select(account => (Account: account, PixKeys: account.PixKeys.Where(IsValidPixKey).ToList()))
            .Where(tuple => tuple.PixKeys.Count > 0)
            .SelectMany(tuple => tuple.PixKeys, (tuple, pixKey) => (tuple.Account, PixKey: pixKey))
            .Where(pair => excludePixKey is null || !string.Equals(pair.PixKey, excludePixKey, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsValidPixKey(string? value) => !string.IsNullOrWhiteSpace(value);
}
