using System.Globalization;

namespace Domain.ValueObjects;

public readonly record struct Money(decimal Value) : IComparable<Money>
{
    public static Result<Money> TryCreate(decimal value)
    {
        if (value < 0)
        {
            return Result<Money>.Failure("money_negative");
        }

        return Result<Money>.Success(new Money(decimal.Round(value, 2, MidpointRounding.AwayFromZero)));
    }

    public static Money Zero => new(0m);

    public int CompareTo(Money other) => Value.CompareTo(other.Value);

    public static Money operator +(Money left, Money right) => new(left.Value + right.Value);
    public static Money operator -(Money left, Money right) => new(left.Value - right.Value);
    public static bool operator <(Money left, Money right) => left.Value < right.Value;
    public static bool operator >(Money left, Money right) => left.Value > right.Value;
    public static bool operator <=(Money left, Money right) => left.Value <= right.Value;
    public static bool operator >=(Money left, Money right) => left.Value >= right.Value;

    public Money Add(Money other) => this + other;

    public Money Subtract(Money other) => this - other;

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
}
