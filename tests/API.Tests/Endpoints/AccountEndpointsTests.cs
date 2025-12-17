using API.Tests.Infrastructure;
using API.Contracts.Requests;
using API.Contracts.Responses;
using UseCases.Accounts;
using Domain.Entities;
using Domain.ValueObjects;
using Shared;
using System.Net;
using System.Net.Http.Json;

namespace API.Tests.Endpoints;

public sealed class AccountEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks(); // Reset mocks to ensure test isolation
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_Accounts_Should_Return_201_When_Data_Valid()
    {
        // Arrange
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var accountNumber = AccountNumber.TryCreate("123456").Value!;
        var initialBalance = Money.TryCreate(100.50m).Value!;
        var account = BankAccount.Open(clientId, accountNumber, initialBalance).Value!;
        
        var openAccountHandlerMock = _factory.GetOpenAccountHandlerMock();
        openAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<OpenAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Success(account));

        var request = new CreateAccountRequest(clientId.Value, "123456", 100.50m);

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        accountResponse.Should().NotBeNull();
        accountResponse!.AccountNumber.Should().Be("123456");
        accountResponse.Balance.Should().Be(100.50m);
        accountResponse.ClientId.Should().Be(clientId.Value);

        openAccountHandlerMock.Verify(
            h => h.HandleAsync(
                It.Is<OpenAccountCommand>(cmd => 
                    cmd.ClientId == clientId.Value && 
                    cmd.AccountNumber == "123456" && 
                    cmd.InitialBalance == 100.50m), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task POST_Accounts_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var openAccountHandlerMock = _factory.GetOpenAccountHandlerMock();
        openAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<OpenAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Failure("account_number_invalid"));

        var request = new CreateAccountRequest(Guid.NewGuid(), "invalid", 100.50m);

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("account_number_invalid");
    }

    [Fact]
    public async Task POST_Accounts_Should_Return_404_When_Client_Not_Found()
    {
        // Arrange
        var openAccountHandlerMock = _factory.GetOpenAccountHandlerMock();
        openAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<OpenAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Failure("client_not_found"));

        var request = new CreateAccountRequest(Guid.NewGuid(), "123456", 100.50m);

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_not_found");
    }

    [Fact]
    public async Task POST_Accounts_Should_Return_409_When_Account_Already_Exists()
    {
        // Arrange
        var openAccountHandlerMock = _factory.GetOpenAccountHandlerMock();
        openAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<OpenAccountCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Failure("bank_account_already_exists"));

        var request = new CreateAccountRequest(Guid.NewGuid(), "123456", 100.50m);

        // Act
        var response = await _client.PostAsJsonAsync("/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("bank_account_already_exists");
    }

    [Fact]
    public async Task GET_Accounts_ByAccountNumber_Should_Return_200_When_Account_Found()
    {
        // Arrange
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var accountNumber = AccountNumber.TryCreate("123456").Value!;
        var initialBalance = Money.TryCreate(100.50m).Value!;
        var account = BankAccount.Open(clientId, accountNumber, initialBalance).Value!;
        
        var getAccountHandlerMock = _factory.GetGetAccountHandlerMock();
        getAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Success(account));

        // Act
        var response = await _client.GetAsync("/accounts/123456");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var accountResponse = await response.Content.ReadFromJsonAsync<AccountResponse>();
        accountResponse.Should().NotBeNull();
        accountResponse!.AccountNumber.Should().Be("123456");
        accountResponse.Balance.Should().Be(100.50m);
        accountResponse.ClientId.Should().Be(clientId.Value);

        getAccountHandlerMock.Verify(
            h => h.HandleAsync(
                It.Is<GetAccountQuery>(q => q.AccountNumber == "123456"), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GET_Accounts_ByAccountNumber_Should_Return_404_When_Account_Not_Found()
    {
        // Arrange
        var getAccountHandlerMock = _factory.GetGetAccountHandlerMock();
        getAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Failure("bank_account_not_found"));

        // Act
        var response = await _client.GetAsync("/accounts/123456");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("bank_account_not_found");
    }

    [Fact]
    public async Task GET_Accounts_ByAccountNumber_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var getAccountHandlerMock = _factory.GetGetAccountHandlerMock();
        getAccountHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankAccount>.Failure("account_number_invalid"));

        // Act
        var response = await _client.GetAsync("/accounts/invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("account_number_invalid");
    }

    [Fact]
    public async Task GET_Accounts_Balance_Should_Return_200_When_Account_Found()
    {
        // Arrange
        var balance = Money.TryCreate(250.75m).Value!;
        
        var getAccountBalanceHandlerMock = _factory.GetGetAccountBalanceHandlerMock();
        getAccountBalanceHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountBalanceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Money>.Success(balance));

        // Act
        var response = await _client.GetAsync("/accounts/123456/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var balanceResponse = await response.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        balanceResponse.Should().NotBeNull();
        balanceResponse!.AccountNumber.Should().Be("123456");
        balanceResponse.Balance.Should().Be(250.75m);

        getAccountBalanceHandlerMock.Verify(
            h => h.HandleAsync(
                It.Is<GetAccountBalanceQuery>(q => q.AccountNumber == "123456"), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GET_Accounts_Balance_Should_Return_404_When_Account_Not_Found()
    {
        // Arrange
        var getAccountBalanceHandlerMock = _factory.GetGetAccountBalanceHandlerMock();
        getAccountBalanceHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountBalanceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Money>.Failure("bank_account_not_found"));

        // Act
        var response = await _client.GetAsync("/accounts/123456/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("bank_account_not_found");
    }

    [Fact]
    public async Task GET_Accounts_Balance_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var getAccountBalanceHandlerMock = _factory.GetGetAccountBalanceHandlerMock();
        getAccountBalanceHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountBalanceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Money>.Failure("account_number_invalid"));

        // Act
        var response = await _client.GetAsync("/accounts/invalid/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("account_number_invalid");
    }

    [Fact]
    public async Task GET_Accounts_Balance_Should_Return_Zero_Balance_When_Account_Has_No_Transactions()
    {
        // Arrange
        var zeroBalance = Money.Zero;
        
        var getAccountBalanceHandlerMock = _factory.GetGetAccountBalanceHandlerMock();
        getAccountBalanceHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetAccountBalanceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Money>.Success(zeroBalance));

        // Act
        var response = await _client.GetAsync("/accounts/123456/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var balanceResponse = await response.Content.ReadFromJsonAsync<AccountBalanceResponse>();
        balanceResponse.Should().NotBeNull();
        balanceResponse!.AccountNumber.Should().Be("123456");
        balanceResponse.Balance.Should().Be(0.0m);
    }
}