using API.Tests.Infrastructure;
using API.Contracts.Requests;
using API.Contracts.Responses;
using UseCases.Clients;
using Domain.Entities;
using Domain.ValueObjects;
using Shared;
using System.Net;
using System.Net.Http.Json;

namespace API.Tests.Endpoints;

public sealed class ClientEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClientEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks(); // Reset mocks to ensure test isolation
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_Clients_Should_Return_201_When_Data_Valid()
    {
        // Arrange
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var cpf = Cpf.TryCreate("52998224725").Value!;
        var client = Client.Create(clientId, cpf, "Maria Silva", "maria@test.com", "11999999999").Value!;
        
        var createClientHandlerMock = _factory.GetCreateClientHandlerMock();
        createClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Success(client));

        var request = new CreateClientRequest("52998224725", "Maria Silva", "maria@test.com", "11999999999");

        // Act
        var response = await _client.PostAsJsonAsync("/clients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var clientResponse = await response.Content.ReadFromJsonAsync<ClientResponse>();
        clientResponse.Should().NotBeNull();
        clientResponse!.Name.Should().Be("Maria Silva");
        clientResponse.Email.Should().Be("maria@test.com");
        clientResponse.Cpf.Should().Be("52998224725");
        clientResponse.MobileNumber.Should().Be("11999999999");

        createClientHandlerMock.Verify(
            h => h.HandleAsync(
                It.Is<CreateClientCommand>(cmd => 
                    cmd.Cpf == "52998224725" && 
                    cmd.Name == "Maria Silva" && 
                    cmd.Email == "maria@test.com" && 
                    cmd.MobileNumber == "11999999999"), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task POST_Clients_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var createClientHandlerMock = _factory.GetCreateClientHandlerMock();
        createClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("cpf_invalid_format"));

        var request = new CreateClientRequest("invalid-cpf", "Maria Silva", "maria@test.com", "11999999999");

        // Act
        var response = await _client.PostAsJsonAsync("/clients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("cpf_invalid_format");
    }

    [Fact]
    public async Task POST_Clients_Should_Return_409_When_Cpf_Already_Exists()
    {
        // Arrange
        var createClientHandlerMock = _factory.GetCreateClientHandlerMock();
        createClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateClientCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("client_cpf_already_exists"));

        var request = new CreateClientRequest("52998224725", "Maria Silva", "maria@test.com", "11999999999");

        // Act
        var response = await _client.PostAsJsonAsync("/clients", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_cpf_already_exists");
    }

    [Fact]
    public async Task GET_Clients_ById_Should_Return_200_When_Client_Found()
    {
        // Arrange
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var cpf = Cpf.TryCreate("52998224725").Value!;
        var client = Client.Create(clientId, cpf, "Maria Silva", "maria@test.com", "11999999999").Value!;
        
        var getClientHandlerMock = _factory.GetGetClientHandlerMock();
        getClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Success(client));

        // Act
        var response = await _client.GetAsync($"/clients/{clientId.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var clientResponse = await response.Content.ReadFromJsonAsync<ClientResponse>();
        clientResponse.Should().NotBeNull();
        clientResponse!.Id.Should().Be(clientId.Value);
        clientResponse.Name.Should().Be("Maria Silva");
        clientResponse.Email.Should().Be("maria@test.com");
        clientResponse.Cpf.Should().Be("52998224725");
        clientResponse.MobileNumber.Should().Be("11999999999");
    }

    [Fact]
    public async Task GET_Clients_ById_Should_Return_404_When_Client_Not_Found()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var getClientHandlerMock = _factory.GetGetClientHandlerMock();
        getClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("client_not_found"));

        // Act
        var response = await _client.GetAsync($"/clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_not_found");
    }

    [Fact]
    public async Task GET_Clients_ById_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var getClientHandlerMock = _factory.GetGetClientHandlerMock();
        getClientHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("client_id_invalid"));

        // Act
        var response = await _client.GetAsync($"/clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_id_invalid");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_200_When_Client_Found()
    {
        // Arrange
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var cpf = Cpf.TryCreate("52998224725").Value!;
        var client = Client.Create(clientId, cpf, "Maria Silva", "maria@test.com", "11999999999").Value!;
        
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Success(client));

        // Act
        var response = await _client.GetAsync("/clients/cpf/52998224725");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var clientResponse = await response.Content.ReadFromJsonAsync<ClientResponse>();
        clientResponse.Should().NotBeNull();
        clientResponse!.Id.Should().Be(clientId.Value);
        clientResponse.Name.Should().Be("Maria Silva");
        clientResponse.Cpf.Should().Be("52998224725");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_404_When_Client_Not_Found()
    {
        // Arrange
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("client_not_found"));

        // Act
        var response = await _client.GetAsync("/clients/cpf/52998224725");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_not_found");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_400_When_TaskCanceled()
    {
        // Arrange
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var response = await _client.GetAsync("/clients/cpf/52998224725");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_lookup_failed");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_400_When_InvalidOperationException()
    {
        // Arrange
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var response = await _client.GetAsync("/clients/cpf/52998224725");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_lookup_failed");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_400_When_General_Exception()
    {
        // Arrange
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var response = await _client.GetAsync("/clients/cpf/52998224725");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("client_lookup_failed");
    }

    [Fact]
    public async Task GET_Clients_ByCpf_Should_Return_400_When_Handler_Returns_Validation_Error()
    {
        // Arrange
        var getClientByCpfHandlerMock = _factory.GetGetClientByCpfHandlerMock();
        getClientByCpfHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetClientByCpfQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Client>.Failure("cpf_invalid_format"));

        // Act
        var response = await _client.GetAsync("/clients/cpf/invalid-cpf");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("cpf_invalid_format");
    }
}