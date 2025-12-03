using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests;

public class BankAccountTests
{
    [Fact]
    public void DebitShouldThrowWhenBalanceInsufficient()
    {
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123450");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var amountResult = Money.TryCreate(150m);
        amountResult.IsSuccess.Should().BeTrue();

        var action = () => account.Debit(amountResult.Value!, TransactionId.New().Value!);

        action.Should().Throw<DomainException>()
            .Where(ex => ex.Code == "insufficient_funds");
    }

    [Fact]
    public void CreditShouldIncreaseBalance()
    {
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("999991");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, Money.Zero);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var creditAmountResult = Money.TryCreate(50m);
        creditAmountResult.IsSuccess.Should().BeTrue();

        var result = account.Credit(creditAmountResult.Value!, TransactionId.New().Value!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(50m);
    }
}
