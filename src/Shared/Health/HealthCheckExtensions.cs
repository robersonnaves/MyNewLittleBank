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
    private static readonly string[] ReadyTags = { "ready" };
    private static readonly string[] LiveTags = { "live" };

    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddHealthChecks();

        var postgresConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(postgresConnection))
        {
            builder.AddCheck("postgres", new NpgsqlHealthCheck(postgresConnection), tags: ReadyTags);
        }

        var rabbitSection = configuration.GetSection("RabbitMQ");
        if (rabbitSection.Exists())
        {
            builder.AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitSection), tags: ReadyTags);
        }

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
#pragma warning disable CA2007 // await using handles ConfigureAwait automatically
                await using var connection = new NpgsqlConnection(_connectionString);
#pragma warning restore CA2007
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy();
            }
            catch (NpgsqlException ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy("Database health check cancelled");
            }
#pragma warning disable CA1031 // Health checks must catch all exceptions to return a result even for unexpected errors
            catch (Exception ex)
            {
                // Catching all exceptions intentionally - health checks must return a result even for unexpected errors
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
#pragma warning restore CA1031
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
#pragma warning disable CA2007 // await using handles ConfigureAwait automatically
                await using var connection = await _factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
                var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: false,
                    publisherConfirmationTrackingEnabled: false);
#pragma warning disable CA2007 // await using handles ConfigureAwait automatically
                await using var channel = await connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007
                _ = channel.IsOpen; // ensure channel negotiated
                return HealthCheckResult.Healthy();
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ broker unreachable", ex);
            }
            catch (RabbitMQ.Client.Exceptions.ConnectFailureException ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ health check cancelled");
            }
#pragma warning disable CA1031 // Health checks must catch all exceptions to return a result even for unexpected errors
            catch (Exception ex)
            {
                // Catching all exceptions intentionally - health checks must return a result even for unexpected errors
                return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
            }
#pragma warning restore CA1031
        }
    }
}
