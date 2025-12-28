using MyNewLittleBank.Tests.Integration.Infrastructure;

namespace MyNewLittleBank.Tests.Integration.Database;

/// <summary>
/// Collection definition for AuditInterceptor tests to prevent parallel execution.
/// Tests in this collection share the same test collection and run sequentially.
/// </summary>
[CollectionDefinition("AuditInterceptor", DisableParallelization = true)]
public sealed class AuditInterceptorTestCollection : ICollectionFixture<IntegrationInfrastructureFixture>
{
}
