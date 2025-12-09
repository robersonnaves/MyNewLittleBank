using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using RabbitMQ.Client;

namespace Shared.Health;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddHealthChecks();

        var postgresConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(postgresConnection))
        {
            builder.AddCheck("postgres", new NpgsqlHealthCheck(postgresConnection), tags: new[] { "ready" });
        }

        var rabbitSection = configuration.GetSection("RabbitMQ");
        if (rabbitSection.Exists())
        {
            builder.AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitSection), tags: new[] { "ready" });
        }

        builder.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        return builder;
    }

    private sealed class NpgsqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public NpgsqlHealthCheck(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }

    private sealed class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqHealthCheck(IConfiguration rabbitSection)
        {
            _factory = new ConnectionFactory
            {
                HostName = rabbitSection["HostName"] ?? "localhost",
                Port = rabbitSection.GetValue<int?>("Port") ?? AmqpTcpEndpoint.UseDefaultPort,
                VirtualHost = rabbitSection["VirtualHost"] ?? "/",
                UserName = rabbitSection["UserName"] ?? "guest",
                Password = rabbitSection["Password"] ?? "guest",
                Ssl =
                {
                    Enabled = rabbitSection.GetValue<bool?>("SslEnabled") ?? false,
                    ServerName = rabbitSection["SslServerName"] ?? string.Empty
                },
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false
            };
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = await _factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
                var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: false,
                    publisherConfirmationTrackingEnabled: false);
                await using var channel = await connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);
                _ = channel.IsOpen; // ensure channel negotiated
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
            }
        }
    }
}
