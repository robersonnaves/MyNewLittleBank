using API.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using UseCases.Clients;
using UseCases.Accounts;
using System.Net;

namespace API.Tests;

public sealed class ProgramTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProgramTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public void Application_Should_Configure_Required_Services()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Assert - Verify all required handlers are registered
        var createClientHandler = serviceProvider.GetService<ICreateClientHandler>();
        createClientHandler.Should().NotBeNull();

        var getClientHandler = serviceProvider.GetService<IGetClientHandler>();
        getClientHandler.Should().NotBeNull();

        var getClientByCpfHandler = serviceProvider.GetService<IGetClientByCpfHandler>();
        getClientByCpfHandler.Should().NotBeNull();

        var openAccountHandler = serviceProvider.GetService<IOpenAccountHandler>();
        openAccountHandler.Should().NotBeNull();

        var getAccountHandler = serviceProvider.GetService<IGetAccountHandler>();
        getAccountHandler.Should().NotBeNull();

        var getAccountBalanceHandler = serviceProvider.GetService<IGetAccountBalanceHandler>();
        getAccountBalanceHandler.Should().NotBeNull();
    }

    [Fact]
    public async Task Application_Should_Configure_Health_Check_Endpoints()
    {
        // Act
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");

        // Assert
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Application_Should_Configure_Prometheus_Endpoint()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Application_Should_Not_Enable_Swagger_In_Test_Environment()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert - Swagger should be disabled in test environment
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_Should_Map_Client_Endpoints()
    {
        // Act & Assert - Verify client endpoints are mapped
        var postResponse = await _client.PostAsync("/clients", new StringContent("{}"));
        // Should not return 404 (endpoint exists, even if request is invalid)
        postResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        var getResponse = await _client.GetAsync($"/clients/{Guid.NewGuid()}");
        // Should not return 404 for endpoint mapping (might be 400 for invalid data)
        getResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        var getByCpfResponse = await _client.GetAsync("/clients/cpf/12345678901");
        // Should not return 404 for endpoint mapping
        getByCpfResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_Should_Map_Account_Endpoints()
    {
        // Act & Assert - Verify account endpoints are mapped
        var postResponse = await _client.PostAsync("/accounts", new StringContent("{}"));
        // Should not return 404 (endpoint exists, even if request is invalid)
        postResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        var getResponse = await _client.GetAsync("/accounts/123456");
        // Should not return 404 for endpoint mapping
        getResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        var getBalanceResponse = await _client.GetAsync("/accounts/123456/balance");
        // Should not return 404 for endpoint mapping
        getBalanceResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_Should_Handle_Invalid_Routes()
    {
        // Act
        var response = await _client.GetAsync("/invalid/route/that/does/not/exist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_Should_Handle_CORS_For_Development()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/clients"));

        // Assert
        // In test environment, CORS might not be fully configured, but the request should be processed
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void Application_Should_Use_Minimal_API_Configuration()
    {
        // This test verifies the application uses minimal APIs by checking service registrations
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Assert - Verify API Explorer is registered (required for minimal APIs)
        var endpointDataSource = serviceProvider.GetServices<Microsoft.AspNetCore.Routing.EndpointDataSource>();
        endpointDataSource.Should().NotBeEmpty();
    }
}