using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests;

public class BankAccountTests
{
    [Fact]
    public void DebitShouldReturnFailureWhenBalanceInsufficient()
    {
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
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

        var result = account.Debit(amountResult.Value!, TransactionId.New().Value!);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("insufficient_funds");
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

    [Fact]
    public void Open_Should_ReturnFailure_When_InitialBalance_Is_Negative()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        // Money.TryCreate should fail for negative values
        var initialBalanceResult = Money.TryCreate(-100m);

        // Assert
        initialBalanceResult.IsSuccess.Should().BeFalse();
        initialBalanceResult.Error.Should().Be("money_negative");
    }

    [Fact]
    public void Open_Should_ReturnSuccess_With_Zero_Initial_Balance()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        // Act
        var result = BankAccount.Open(clientId, accountNumber, Money.Zero);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.Should().Be(Money.Zero);
        result.Value.ClientId.Should().Be(clientId);
        result.Value.AccountNumber.Should().Be(accountNumber);
        result.Value.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Open_Should_ReturnSuccess_With_Positive_Initial_Balance()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(500m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        // Act
        var result = BankAccount.Open(clientId, accountNumber, initialBalance);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.Should().Be(initialBalance);
        result.Value.ClientId.Should().Be(clientId);
        result.Value.AccountNumber.Should().Be(accountNumber);
        result.Value.OpenedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Credit_Should_ReturnFailure_When_Amount_Is_Zero()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, Money.Zero);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act
        var result = account.Credit(Money.Zero, transactionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("invalid_money");
    }

    [Fact]
    public void Credit_Should_Add_Transaction_To_Account()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, Money.Zero);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var creditAmountResult = Money.TryCreate(100m);
        creditAmountResult.IsSuccess.Should().BeTrue();
        var creditAmount = creditAmountResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act
        var result = account.Credit(creditAmount, transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        account.Transactions.Should().Contain(transactionId);
        account.Transactions.Count.Should().Be(1);
    }

    [Fact]
    public void Credit_Should_Work_Without_TransactionId()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, Money.Zero);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var creditAmountResult = Money.TryCreate(100m);
        creditAmountResult.IsSuccess.Should().BeTrue();
        var creditAmount = creditAmountResult.Value!;

        // Act
        var result = account.Credit(creditAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(100m);
        account.Transactions.Should().BeEmpty(); // No transaction registered
    }

    [Fact]
    public void Debit_Should_ReturnFailure_When_Amount_Is_Zero()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act
        var result = account.Debit(Money.Zero, transactionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("invalid_money");
    }

    [Fact]
    public void Debit_Should_Add_Transaction_To_Account_When_Successful()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var debitAmountResult = Money.TryCreate(50m);
        debitAmountResult.IsSuccess.Should().BeTrue();
        var debitAmount = debitAmountResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act
        var result = account.Debit(debitAmount, transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(50m);
        account.Transactions.Should().Contain(transactionId);
        account.Transactions.Count.Should().Be(1);
    }

    [Fact]
    public void Debit_Should_Not_Add_Transaction_When_Failed()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var debitAmountResult = Money.TryCreate(150m);
        debitAmountResult.IsSuccess.Should().BeTrue();
        var debitAmount = debitAmountResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act
        var result = account.Debit(debitAmount, transactionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("insufficient_funds");
        account.Transactions.Should().BeEmpty(); // Transaction should not be registered
        account.Balance.Should().Be(initialBalance); // Balance should remain unchanged
    }

    [Fact]
    public void BankAccount_Should_Have_Unique_Id()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumber1Result = AccountNumber.TryCreate("123456");
        accountNumber1Result.IsSuccess.Should().BeTrue();
        var accountNumber1 = accountNumber1Result.Value!;

        var accountNumber2Result = AccountNumber.TryCreate("654321");
        accountNumber2Result.IsSuccess.Should().BeTrue();
        var accountNumber2 = accountNumber2Result.Value!;

        // Act
        var account1Result = BankAccount.Open(clientId, accountNumber1, Money.Zero);
        account1Result.IsSuccess.Should().BeTrue();
        var account1 = account1Result.Value!;

        var account2Result = BankAccount.Open(clientId, accountNumber2, Money.Zero);
        account2Result.IsSuccess.Should().BeTrue();
        var account2 = account2Result.Value!;

        // Assert
        account1.Id.Should().NotBe(account2.Id);
    }

    [Fact]
    public void Multiple_Credits_And_Debits_Should_Update_Balance_Correctly()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(1000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var amount1Result = Money.TryCreate(200m);
        amount1Result.IsSuccess.Should().BeTrue();
        var amount1 = amount1Result.Value!;

        var amount2Result = Money.TryCreate(150m);
        amount2Result.IsSuccess.Should().BeTrue();
        var amount2 = amount2Result.Value!;

        var amount3Result = Money.TryCreate(300m);
        amount3Result.IsSuccess.Should().BeTrue();
        var amount3 = amount3Result.Value!;

        // Act
        var debitResult1 = account.Debit(amount1, TransactionId.New().Value!);
        var creditResult = account.Credit(amount2, TransactionId.New().Value!);
        var debitResult2 = account.Debit(amount3, TransactionId.New().Value!);

        // Assert
        debitResult1.IsSuccess.Should().BeTrue();
        creditResult.IsSuccess.Should().BeTrue();
        debitResult2.IsSuccess.Should().BeTrue();

        // Balance: 1000 - 200 + 150 - 300 = 650
        var expectedBalanceResult = Money.TryCreate(650m);
        expectedBalanceResult.IsSuccess.Should().BeTrue();
        var expectedBalance = expectedBalanceResult.Value!;

        account.Balance.Should().Be(expectedBalance);
        account.Transactions.Count.Should().Be(3);
    }
}
