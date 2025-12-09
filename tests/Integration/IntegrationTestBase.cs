using MyNewLittleBank.Tests.Integration.Infrastructure;

namespace MyNewLittleBank.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase
{
    protected IntegrationTestBase(IntegrationInfrastructureFixture fixture)
    {
        Fixture = fixture;
    }

    protected IntegrationInfrastructureFixture Fixture { get; }
}
