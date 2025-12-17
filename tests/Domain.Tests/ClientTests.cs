using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests;

public class ClientTests
{
    [Fact]
    public void Create_Should_ReturnSuccess_When_All_Parameters_Are_Valid()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(clientId);
        result.Value.Cpf.Should().Be(cpf);
        result.Value.Name.Should().Be("João Silva");
        result.Value.Email.Should().Be("joao@test.com");
        result.Value.MobileNumber.Should().Be("11999999999");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_Name_Is_Empty()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "", "joao@test.com", "11999999999");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_name_empty");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_Name_Is_Null()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, null!, "joao@test.com", "11999999999");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_name_empty");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_Email_Is_Empty()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "João Silva", "", "11999999999");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_email_empty");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_Email_Is_Null()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "João Silva", null!, "11999999999");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_email_empty");
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_MobileNumber_Is_Empty()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_mobile_empty");
    }

    [Fact]
    public void Create_Should_Trim_Input_Values()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        // Act
        var result = Client.Create(clientId, cpf, "  João Silva  ", "  joao@test.com  ", "  11999999999  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("João Silva");
        result.Value.Email.Should().Be("joao@test.com");
        result.Value.MobileNumber.Should().Be("11999999999");
    }

    [Fact]
    public void Update_Should_Change_Client_Email_And_PhoneNumber()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        // Act
        var result = client.Update("João Santos", "joao.santos@test.com", "11888888888");

        // Assert
        result.IsSuccess.Should().BeTrue();
        client.Name.Should().Be("João Santos");
        client.Email.Should().Be("joao.santos@test.com");
        client.MobileNumber.Should().Be("11888888888");
    }

    [Fact]
    public void Update_Should_Not_Change_Cpf()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var originalCpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, originalCpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        // Act
        var result = client.Update("João Santos", "joao.santos@test.com", "11888888888");

        // Assert
        result.IsSuccess.Should().BeTrue();
        client.Cpf.Should().Be(originalCpf);
    }

    [Fact]
    public void Update_Should_ReturnFailure_When_Name_Is_Invalid()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        // Act
        var result = client.Update("", "joao.santos@test.com", "11888888888");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_update_invalid");
    }

    [Fact]
    public void AddAccount_Should_Add_An_Account_To_The_Client()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        // Act
        var result = client.AddAccount(account);

        // Assert
        result.IsSuccess.Should().BeTrue();
        client.BankAccounts.Should().Contain(account);
        client.BankAccounts.Count.Should().Be(1);
    }

    [Fact]
    public void AddAccount_Should_Fail_When_Account_Does_Not_Belong_To_Client()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var otherClientIdResult = ClientId.TryCreate(Guid.NewGuid());
        otherClientIdResult.IsSuccess.Should().BeTrue();
        var otherClientId = otherClientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(100m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(otherClientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        // Act
        var result = client.AddAccount(account);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("invalid_transaction");
    }

    [Fact]
    public void AddAccount_Should_Throw_When_Account_Is_Null()
    {
        // Arrange
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var createResult = Client.Create(clientId, cpf, "João Silva", "joao@test.com", "11999999999");
        createResult.IsSuccess.Should().BeTrue();
        var client = createResult.Value!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => client.AddAccount(null!));
    }
}