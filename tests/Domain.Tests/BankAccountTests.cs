using FluentAssertions;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests;

public class BankAccountTests
{
    [Fact]
    public void Debit_ShouldThrow_WhenBalanceInsufficient()
    {
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value;
        var accountNumber = AccountNumber.TryCreate("123450").Value;
        var initialBalance = Money.TryCreate(100m).Value;
        var account = BankAccount.Open(clientId, accountNumber, initialBalance).Value;

        var action = () => account.Debit(Money.TryCreate(150m).Value, TransactionId.New().Value);

        action.Should().Throw<DomainException>()
            .Where(ex => ex.Code == "insufficient_funds");
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value;
        var accountNumber = AccountNumber.TryCreate("999991").Value;
        var account = BankAccount.Open(clientId, accountNumber, Money.Zero).Value;

        var result = account.Credit(Money.TryCreate(50m).Value, TransactionId.New().Value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(50m);
    }
}
