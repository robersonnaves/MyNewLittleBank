using Xunit;

namespace MyNewLittleBank.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationInfrastructureFixture>
{
    public const string Name = "integration";
}
