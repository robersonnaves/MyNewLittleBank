using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Infra.Database;
using UseCases.Clients;
using UseCases.Accounts;

namespace API.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ICreateClientHandler> CreateClientHandlerMock { get; } = new();
    public Mock<IGetClientHandler> GetClientHandlerMock { get; } = new();
    public Mock<IGetClientByCpfHandler> GetClientByCpfHandlerMock { get; } = new();
    public Mock<IOpenAccountHandler> OpenAccountHandlerMock { get; } = new();
    public Mock<IGetAccountHandler> GetAccountHandlerMock { get; } = new();
    public Mock<IGetAccountBalanceHandler> GetAccountBalanceHandlerMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["Swagger:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MyNewLittleBankContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<MyNewLittleBankContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.EnableSensitiveDataLogging();
            });

            // Register mock handlers as singletons to avoid scoped service issues
            services.AddSingleton<ICreateClientHandler>(CreateClientHandlerMock.Object);
            services.AddSingleton<IGetClientHandler>(GetClientHandlerMock.Object);
            services.AddSingleton<IGetClientByCpfHandler>(GetClientByCpfHandlerMock.Object);
            services.AddSingleton<IOpenAccountHandler>(OpenAccountHandlerMock.Object);
            services.AddSingleton<IGetAccountHandler>(GetAccountHandlerMock.Object);
            services.AddSingleton<IGetAccountBalanceHandler>(GetAccountBalanceHandlerMock.Object);

            // Replace health checks with test-friendly versions
            // Remove existing health check services
            services.RemoveAll<IHealthChecksBuilder>();
            var healthCheckServices = services.Where(x => x.ServiceType.FullName?.Contains("HealthCheck") == true).ToArray();
            foreach (var service in healthCheckServices)
            {
                services.Remove(service);
            }

            // Add simple test health checks
            services.AddHealthChecks()
                .AddCheck("postgres", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" })
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }

    public Mock<ICreateClientHandler> GetCreateClientHandlerMock() => CreateClientHandlerMock;
    public Mock<IGetClientHandler> GetGetClientHandlerMock() => GetClientHandlerMock;
    public Mock<IGetClientByCpfHandler> GetGetClientByCpfHandlerMock() => GetClientByCpfHandlerMock;
    public Mock<IOpenAccountHandler> GetOpenAccountHandlerMock() => OpenAccountHandlerMock;
    public Mock<IGetAccountHandler> GetGetAccountHandlerMock() => GetAccountHandlerMock;
    public Mock<IGetAccountBalanceHandler> GetGetAccountBalanceHandlerMock() => GetAccountBalanceHandlerMock;

    public void ResetAllMocks()
    {
        CreateClientHandlerMock.Reset();
        GetClientHandlerMock.Reset();
        GetClientByCpfHandlerMock.Reset();
        OpenAccountHandlerMock.Reset();
        GetAccountHandlerMock.Reset();
        GetAccountBalanceHandlerMock.Reset();
    }
}