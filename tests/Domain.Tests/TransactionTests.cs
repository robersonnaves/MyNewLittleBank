using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void CardTransaction_Create_Should_ReturnSuccess_With_Valid_Data()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = CardTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount, 
            "4111111111111111");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(transactionId);
        result.Value.ClientId.Should().Be(clientId);
        result.Value.BankAccountId.Should().Be(accountNumber);
        result.Value.Amount.Should().Be(amount);
        result.Value.CardNumber.Should().Be("4111111111111111");
        result.Value.Status.Should().Be(TransactionStatus.Pending);
        result.Value.Type.Should().Be(TransactionType.Card);
    }

    [Fact]
    public void CardTransaction_Create_Should_ReturnFailure_When_CardNumber_Is_Empty()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = CardTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount, 
            "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("card_number_empty");
    }

    [Fact]
    public void CardTransaction_Create_Should_ReturnFailure_When_CardNumber_Is_Null()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = CardTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount, 
            null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("card_number_empty");
    }

    [Fact]
    public void CardTransaction_Create_Should_ReturnFailure_When_Amount_Is_Negative()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();

        var amountResult = Money.TryCreate(-100.50m);
        amountResult.IsSuccess.Should().BeFalse(); // This should fail at Money creation
    }

    [Fact]
    public void CardTransaction_Create_Should_Trim_CardNumber()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = CardTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount, 
            "  4111111111111111  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CardNumber.Should().Be("4111111111111111");
    }

    [Fact]
    public void MoneyTransaction_Create_Should_ReturnSuccess_With_Valid_Data()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(200.75m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = MoneyTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(transactionId);
        result.Value.ClientId.Should().Be(clientId);
        result.Value.BankAccountId.Should().Be(accountNumber);
        result.Value.Amount.Should().Be(amount);
        result.Value.Status.Should().Be(TransactionStatus.Pending);
        result.Value.Type.Should().Be(TransactionType.Money);
    }

    [Fact]
    public void PixTransaction_Create_Should_ReturnSuccess_With_Valid_Data()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(300.25m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = PixTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount,
            "origem@pix.com",
            "destino@pix.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(transactionId);
        result.Value.ClientId.Should().Be(clientId);
        result.Value.BankAccountId.Should().Be(accountNumber);
        result.Value.Amount.Should().Be(amount);
        result.Value.OriginPixKey.Should().Be("origem@pix.com");
        result.Value.DestinationPixKey.Should().Be("destino@pix.com");
        result.Value.Status.Should().Be(TransactionStatus.Pending);
        result.Value.Type.Should().Be(TransactionType.Pix);
    }

    [Fact]
    public void PixTransaction_Create_Should_ReturnFailure_When_OriginPixKey_Is_Invalid()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(300.25m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = PixTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount,
            "",
            "destino@pix.com");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("pix_keys_invalid");
    }

    [Fact]
    public void PixTransaction_Create_Should_ReturnFailure_When_DestinationPixKey_Is_Invalid()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(300.25m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = PixTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount,
            "origem@pix.com",
            null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("pix_keys_invalid");
    }

    [Fact]
    public void PixTransaction_Create_Should_Trim_PixKeys()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(300.25m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        // Act
        var result = PixTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount,
            "  origem@pix.com  ",
            "  destino@pix.com  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginPixKey.Should().Be("origem@pix.com");
        result.Value.DestinationPixKey.Should().Be("destino@pix.com");
    }

    [Fact]
    public void Transaction_ChangeStatus_Should_Update_Status()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        var transactionResult = CardTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount, 
            "4111111111111111");
        transactionResult.IsSuccess.Should().BeTrue();
        var transaction = transactionResult.Value!;

        // Act
        var updatedTransaction = transaction.ChangeStatus(TransactionStatus.Completed);

        // Assert
        updatedTransaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.Status.Should().Be(TransactionStatus.Completed); // Should be same instance
    }

    [Fact]
    public void Transaction_SetOccurredOn_Should_Update_OccurredOn()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        var transactionResult = MoneyTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount);
        transactionResult.IsSuccess.Should().BeTrue();
        var transaction = transactionResult.Value!;

        var newDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var updatedTransaction = transaction.SetOccurredOn(newDate);

        // Assert
        updatedTransaction.OccurredOn.Should().Be(newDate);
        transaction.OccurredOn.Should().Be(newDate); // Should be same instance
    }

    [Fact]
    public void Transaction_Create_With_Custom_Status_And_Date_Should_Use_Provided_Values()
    {
        // Arrange
        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var amountResult = Money.TryCreate(100.50m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        var customDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = PixTransaction.Create(
            transactionId, 
            clientId, 
            accountNumber, 
            amount,
            "origem@pix.com",
            "destino@pix.com",
            TransactionStatus.Completed,
            customDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(TransactionStatus.Completed);
        result.Value.OccurredOn.Should().Be(customDate);
    }
}