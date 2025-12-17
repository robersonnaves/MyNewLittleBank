using AwesomeAssertions;
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Tests;

public class ValueObjectsAdvancedTests
{
    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnSuccess_When_Value_Is_Valid()
    {
        // Act
        var result = AccountNumber.TryCreate("123456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("123456");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnSuccess_When_Value_Is_MinimumLength()
    {
        // Act
        var result = AccountNumber.TryCreate("12345");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("12345");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnSuccess_When_Value_Is_MaximumLength()
    {
        // Act
        var result = AccountNumber.TryCreate("12345678901234567890");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("12345678901234567890");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Is_Empty()
    {
        // Act
        var result = AccountNumber.TryCreate("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_empty");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Is_Null()
    {
        // Act
        var result = AccountNumber.TryCreate(null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_empty");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Is_Whitespace()
    {
        // Act
        var result = AccountNumber.TryCreate("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_empty");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Is_Too_Short()
    {
        // Act
        var result = AccountNumber.TryCreate("1234");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_invalid_format");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Is_Too_Long()
    {
        // Act
        var result = AccountNumber.TryCreate("123456789012345678901");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_invalid_format");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Contains_Letters()
    {
        // Act
        var result = AccountNumber.TryCreate("12345A");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_invalid_format");
    }

    [Fact]
    public void AccountNumber_TryCreate_Should_ReturnFailure_When_Value_Contains_Special_Characters()
    {
        // Act
        var result = AccountNumber.TryCreate("12345-");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("account_number_invalid_format");
    }

    [Fact]
    public void AccountNumber_ToString_Should_Return_Value()
    {
        // Arrange
        var result = AccountNumber.TryCreate("123456");
        result.IsSuccess.Should().BeTrue();
        var accountNumber = result.Value!;

        // Act
        var toString = accountNumber.ToString();

        // Assert
        toString.Should().Be("123456");
    }

    [Fact]
    public void ClientId_TryCreate_Should_ReturnSuccess_When_Guid_Is_Not_Empty()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = ClientId.TryCreate(guid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Fact]
    public void ClientId_TryCreate_Should_ReturnFailure_When_Guid_Is_Empty()
    {
        // Act
        var result = ClientId.TryCreate(Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_id_empty");
    }

    [Fact]
    public void ClientId_ToString_Should_Return_Guid_String()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var result = ClientId.TryCreate(guid);
        result.IsSuccess.Should().BeTrue();
        var clientId = result.Value!;

        // Act
        var toString = clientId.ToString();

        // Assert
        toString.Should().Be(guid.ToString());
    }

    [Fact]
    public void TransactionId_New_Should_Generate_Unique_Values()
    {
        // Act
        var result1 = TransactionId.New();
        var result2 = TransactionId.New();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value!.Value.Should().NotBe(result2.Value!.Value);
    }

    [Fact]
    public void TransactionId_New_Should_Generate_Non_Empty_Guid()
    {
        // Act
        var result = TransactionId.New();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TransactionId_TryCreate_Should_ReturnSuccess_When_Guid_Is_Not_Empty()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = TransactionId.TryCreate(guid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(guid);
    }

    [Fact]
    public void TransactionId_TryCreate_Should_ReturnFailure_When_Guid_Is_Empty()
    {
        // Act
        var result = TransactionId.TryCreate(Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("transaction_id_empty");
    }

    [Fact]
    public void TransactionId_ToString_Should_Return_Guid_String()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var result = TransactionId.TryCreate(guid);
        result.IsSuccess.Should().BeTrue();
        var transactionId = result.Value!;

        // Act
        var toString = transactionId.ToString();

        // Assert
        toString.Should().Be(guid.ToString());
    }

    [Fact]
    public void ValueObjects_Should_Be_Immutable()
    {
        // Arrange
        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var transactionIdResult = TransactionId.New();
        transactionIdResult.IsSuccess.Should().BeTrue();
        var transactionId = transactionIdResult.Value!;

        // Act & Assert - These should not have setters
        // This is a compile-time check but we can verify the types are record structs
        accountNumber.GetType().IsValueType.Should().BeTrue();
        clientId.GetType().IsValueType.Should().BeTrue();
        transactionId.GetType().IsValueType.Should().BeTrue();
    }

    [Fact]
    public void ValueObjects_Should_Support_Equality_Comparison()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        var accountNumber1Result = AccountNumber.TryCreate("123456");
        accountNumber1Result.IsSuccess.Should().BeTrue();
        var accountNumber1 = accountNumber1Result.Value!;

        var accountNumber2Result = AccountNumber.TryCreate("123456");
        accountNumber2Result.IsSuccess.Should().BeTrue();
        var accountNumber2 = accountNumber2Result.Value!;

        var accountNumber3Result = AccountNumber.TryCreate("654321");
        accountNumber3Result.IsSuccess.Should().BeTrue();
        var accountNumber3 = accountNumber3Result.Value!;

        var clientId1Result = ClientId.TryCreate(guid1);
        clientId1Result.IsSuccess.Should().BeTrue();
        var clientId1 = clientId1Result.Value!;

        var clientId2Result = ClientId.TryCreate(guid1);
        clientId2Result.IsSuccess.Should().BeTrue();
        var clientId2 = clientId2Result.Value!;

        var clientId3Result = ClientId.TryCreate(guid2);
        clientId3Result.IsSuccess.Should().BeTrue();
        var clientId3 = clientId3Result.Value!;

        // Act & Assert
        (accountNumber1 == accountNumber2).Should().BeTrue();
        (accountNumber1 != accountNumber3).Should().BeTrue();
        (clientId1 == clientId2).Should().BeTrue();
        (clientId1 != clientId3).Should().BeTrue();

        accountNumber1.Equals(accountNumber2).Should().BeTrue();
        accountNumber1.Equals(accountNumber3).Should().BeFalse();
        clientId1.Equals(clientId2).Should().BeTrue();
        clientId1.Equals(clientId3).Should().BeFalse();
    }

    [Fact]
    public void ValueObjects_Should_Have_Consistent_HashCodes()
    {
        // Arrange
        var guid = Guid.NewGuid();

        var accountNumber1Result = AccountNumber.TryCreate("123456");
        accountNumber1Result.IsSuccess.Should().BeTrue();
        var accountNumber1 = accountNumber1Result.Value!;

        var accountNumber2Result = AccountNumber.TryCreate("123456");
        accountNumber2Result.IsSuccess.Should().BeTrue();
        var accountNumber2 = accountNumber2Result.Value!;

        var clientId1Result = ClientId.TryCreate(guid);
        clientId1Result.IsSuccess.Should().BeTrue();
        var clientId1 = clientId1Result.Value!;

        var clientId2Result = ClientId.TryCreate(guid);
        clientId2Result.IsSuccess.Should().BeTrue();
        var clientId2 = clientId2Result.Value!;

        var transactionId1Result = TransactionId.TryCreate(guid);
        transactionId1Result.IsSuccess.Should().BeTrue();
        var transactionId1 = transactionId1Result.Value!;

        var transactionId2Result = TransactionId.TryCreate(guid);
        transactionId2Result.IsSuccess.Should().BeTrue();
        var transactionId2 = transactionId2Result.Value!;

        // Act & Assert
        accountNumber1.GetHashCode().Should().Be(accountNumber2.GetHashCode());
        clientId1.GetHashCode().Should().Be(clientId2.GetHashCode());
        transactionId1.GetHashCode().Should().Be(transactionId2.GetHashCode());
    }
}