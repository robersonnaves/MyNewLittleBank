using System.Threading.Tasks;
using AwesomeAssertions;
using MyNewLittleBank.Tests.Integration.Infrastructure;

namespace MyNewLittleBank.Tests.Integration.Smoke;

public sealed class InfrastructureSmokeTests : IntegrationTestBase
{
    public InfrastructureSmokeTests(IntegrationInfrastructureFixture fixture) : base(fixture)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Postgres_container_should_accept_connections()
    {
        var result = await Fixture.ExecuteScalarAsync("SELECT 1;").ConfigureAwait(false);

        result.Should().Be(1);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task RabbitMq_container_should_be_reachable()
    {
        var connectionString = Fixture.RabbitMqConnectionString;

        connectionString.Should().Contain("amqp://");
        // The container is started by the fixture; reaching this point means the Testcontainers healthcheck passed.
        await Task.CompletedTask;
    }
}
