using System.Text.RegularExpressions;

namespace Domain.ValueObjects;

public readonly record struct AccountNumber(string Value)
{
    private static readonly Regex ValidPattern = new("^[0-9]{5,20}$", RegexOptions.Compiled);

    public static Result<AccountNumber> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<AccountNumber>.Failure("account_number_empty");
        }

        if (!ValidPattern.IsMatch(value))
        {
            return Result<AccountNumber>.Failure("account_number_invalid_format");
        }

        return Result<AccountNumber>.Success(new AccountNumber(value));
    }

    public override string ToString() => Value;
}
