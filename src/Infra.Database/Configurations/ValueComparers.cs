using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infra.Database.Configurations;

internal static class ValueComparers
{
    public static readonly ValueComparer<ClientId> ClientIdComparer =
        new(
            static (left, right) => left.Value == right.Value,
            static id => id.Value.GetHashCode(),
            static id => new ClientId(id.Value));

    public static readonly ValueComparer<AccountNumber> AccountNumberComparer =
        new(
            static (left, right) => left.Value == right.Value,
            static number => number.Value.GetHashCode(),
            static number => new AccountNumber(number.Value));

    public static readonly ValueComparer<TransactionId> TransactionIdComparer =
        new(
            static (left, right) => left.Value == right.Value,
            static id => id.Value.GetHashCode(),
            static id => new TransactionId(id.Value));

    public static readonly ValueComparer<Money> MoneyComparer =
        new(
            static (left, right) => left.Value == right.Value,
            static money => money.Value.GetHashCode(),
            static money => new Money(money.Value));

    public static readonly ValueComparer<Cpf> CpfComparer =
        new(
            static (left, right) => left.Value == right.Value,
            static cpf => cpf.Value.GetHashCode(StringComparison.Ordinal),
            static cpf => new Cpf(cpf.Value));
}
