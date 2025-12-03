using System.Text.RegularExpressions;

namespace Domain.ValueObjects;

public readonly record struct Cpf(string Value)
{
    private static readonly Regex DigitsOnly = new("^[0-9]{11}$", RegexOptions.Compiled);

    public static Result<Cpf> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Cpf>.Failure("cpf_empty");
        }

        var digits = value
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        if (!DigitsOnly.IsMatch(digits))
        {
            return Result<Cpf>.Failure("cpf_invalid_format");
        }

        if (IsInvalidChecksum(digits))
        {
            return Result<Cpf>.Failure("cpf_invalid_checksum");
        }

        return Result<Cpf>.Success(new Cpf(digits));
    }

    private static bool IsInvalidChecksum(string digits)
    {
        if (digits.All(c => c == digits[0]))
        {
            return true;
        }

        var numbers = digits.Select(c => c - '0').ToArray();
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += numbers[i] * (10 - i);
        }

        var firstDigit = (sum * 10) % 11;
        if (firstDigit == 10) firstDigit = 0;
        if (numbers[9] != firstDigit)
        {
            return true;
        }

        sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += numbers[i] * (11 - i);
        }

        var secondDigit = (sum * 10) % 11;
        if (secondDigit == 10) secondDigit = 0;

        return numbers[10] != secondDigit;
    }

    public override string ToString() => Value;
}
