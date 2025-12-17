using System;
using System.Threading.Tasks;
using Npgsql;
using Testcontainers;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace MyNewLittleBank.Tests.Integration.Infrastructure;

public sealed class IntegrationInfrastructureFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private readonly RabbitMqContainer _rabbitMq;

    public IntegrationInfrastructureFixture()
    {
        ApplyContainerRuntimeSettings();

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("integration_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        _rabbitMq = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();
    }

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync().ConfigureAwait(false);
        await _rabbitMq.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _rabbitMq.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<int> ExecuteScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ApplyContainerRuntimeSettings()
    {
        var engine = Environment.GetEnvironmentVariable("CONTAINER_ENGINE");
        if (string.Equals(engine, "podman", StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        }

        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            Environment.SetEnvironmentVariable("DOCKER_HOST", dockerHost);
        }
    }
}
