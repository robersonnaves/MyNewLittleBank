using System.Diagnostics;
using AwesomeAssertions;
using Domain.Entities;
using Domain.ValueObjects;
using Infra.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyNewLittleBank.Tests.Integration.Infrastructure;

namespace MyNewLittleBank.Tests.Integration.Database;

[Trait("Category", "Integration")]
[Collection("AuditInterceptor")] // Separate collection to prevent parallel execution
public sealed class AuditInterceptorIntegrationTests : IntegrationTestBase
{
    public AuditInterceptorIntegrationTests(IntegrationInfrastructureFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task SavingChanges_WhenTransactionRollback_ShouldNotPersistAuditLogs()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Test Client", "test@email.com", "11777777777");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act - Start transaction and rollback
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert - Verify nothing persisted
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().BeEmpty();

        var clients = await context.Clients.ToListAsync();
        clients.Should().BeEmpty();
    }

    [Fact]
    public async Task SavingChanges_WhenTransactionCommits_ShouldPersistAuditLogs()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Jane Doe", "jane@email.com", "11888888888");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act - Start transaction and commit
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert - Verify both entity and audit log persisted
        var clients = await context.Clients.ToListAsync();
        clients.Should().HaveCount(1);
        clients[0].Id.Should().Be(clientId);

        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.EntityName.Should().Be(nameof(Client));
        auditLog.EntityId.Should().Be(clientId.Value.ToString("D"));
        auditLog.Action.Should().Be("Added");
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.NewValues.Should().Contain("Jane Doe");
        auditLog.NewValues.Should().Contain("52998224725");
    }

    [Fact]
    public async Task SavingChanges_WithMultipleEntitiesInTransaction_ShouldCreateAtomicAuditLogs()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Create Client
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("11144477735");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "John Smith", "john@email.com", "11999999999");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Create BankAccount
        var accountNumberResult = AccountNumber.TryCreate("11111111111");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(5_000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        // Act - Add both entities in a single transaction
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.BankAccounts.AddAsync(account, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert - Verify both entities persisted
        var clients = await context.Clients.ToListAsync();
        clients.Should().HaveCount(1);

        var accounts = await context.BankAccounts.ToListAsync();
        accounts.Should().HaveCount(1);

        // Verify atomic audit logs creation
        var auditLogs = await context.AuditLogs.OrderBy(a => a.EntityName).ToListAsync();
        auditLogs.Should().HaveCount(2);

        var accountAudit = auditLogs.First(a => a.EntityName == nameof(BankAccount));
        accountAudit.Action.Should().Be("Added");
        accountAudit.NewValues.Should().Contain("5000");

        var clientAudit = auditLogs.First(a => a.EntityName == nameof(Client));
        clientAudit.Action.Should().Be("Added");
        clientAudit.NewValues.Should().Contain("John Smith");
    }

    [Fact]
    public async Task SavingChanges_WithMultipleEntitiesRollback_ShouldNotPersistAnyAuditLogs()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Create Client
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725"); // Valid CPF used in unit tests
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Bob Wilson", "bob@email.com", "11555555555");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Create BankAccount
        var accountNumberResult = AccountNumber.TryCreate("22222222222");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(3_000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        // Act - Add entities and rollback
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.BankAccounts.AddAsync(account, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert - Verify NOTHING persisted
        var clients = await context.Clients.ToListAsync();
        clients.Should().BeEmpty();

        var accounts = await context.BankAccounts.ToListAsync();
        accounts.Should().BeEmpty();

        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task AuditLog_JsonbColumns_ShouldSupportPostgresQueries()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // First create a client (needed due to FK constraint)
        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("72788740417");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Test User", "test@email.com", "11444444444");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.SaveChangesAsync();

        // Create and modify a BankAccount to generate audit logs with OldValues and NewValues
        var accountNumberResult = AccountNumber.TryCreate("33333333333");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(2_000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        await context.BankAccounts.AddAsync(account, CancellationToken.None);
        await context.SaveChangesAsync();

        // Clear initial insert audit log
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();

        // Modify account to create audit log with both OldValues and NewValues
        var creditAmount = Money.TryCreate(500m).Value!;
        account.Credit(creditAmount);
        await context.SaveChangesAsync();

        // Act & Assert - Query JSONB data using raw SQL with ADO.NET
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        // Query old balance from JSONB
        var oldBalanceQuery = @"
            SELECT old_values->>'Balance' 
            FROM audit_logs 
            WHERE entity_name = 'BankAccount' 
            AND action = 'Modified'
            LIMIT 1";

        string? oldBalance = null;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = oldBalanceQuery;
            oldBalance = (await command.ExecuteScalarAsync())?.ToString();
        }

        oldBalance.Should().NotBeNull();
        oldBalance.Should().Contain("2000");

        // Query new balance from JSONB
        var newBalanceQuery = @"
            SELECT new_values->>'Balance' 
            FROM audit_logs 
            WHERE entity_name = 'BankAccount' 
            AND action = 'Modified'
            LIMIT 1";

        string? newBalance = null;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = newBalanceQuery;
            newBalance = (await command.ExecuteScalarAsync())?.ToString();
        }

        newBalance.Should().NotBeNull();
        newBalance.Should().Contain("2500");

        // Verify we can query by JSON path
        var accountNumberQuery = @"
            SELECT new_values->>'AccountNumber' 
            FROM audit_logs 
            WHERE entity_name = 'BankAccount'
            LIMIT 1";

        string? queriedAccountNumber = null;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = accountNumberQuery;
            queriedAccountNumber = (await command.ExecuteScalarAsync())?.ToString();
        }

        queriedAccountNumber.Should().NotBeNull();
        queriedAccountNumber.Should().Contain("33333333333");

        await connection.CloseAsync();
    }

    [Fact]
    public async Task AuditLog_WithActiveActivity_ShouldCaptureTraceIdInTransaction()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        await context.Database.EnsureCreatedAsync();

        using var activitySource = new ActivitySource("IntegrationTestSource");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("TestTransactionActivity", ActivityKind.Internal);
        activity.Should().NotBeNull();

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("11144477735");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Alice Brown", "alice@email.com", "11666666666");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act - Save within transaction with active Activity
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert - Verify TraceId and SpanId captured
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.TraceId.Should().NotBeNull();
        auditLog.SpanId.Should().NotBeNull();
        auditLog.TraceId.Should().Be(activity!.TraceId.ToString());
        auditLog.SpanId.Should().Be(activity!.SpanId.ToString());
    }

    [Fact]
    public async Task AuditLog_Indexes_ShouldExistOnKeyColumns()
    {
        // Arrange
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        
        // Ensure clean database for this test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Act - Query PostgreSQL system catalog for indexes
        var indexQuery = @"
            SELECT indexname 
            FROM pg_indexes 
            WHERE tablename = 'audit_logs' 
            AND schemaname = 'public'
            ORDER BY indexname";

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var indexes = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = indexQuery;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(reader.GetString(0));
            }
        }

        await connection.CloseAsync();

        // Assert - Verify expected indexes exist
        indexes.Should().Contain(idx => idx.Contains("entity_id"), "entity_id index should exist");
        indexes.Should().Contain(idx => idx.Contains("trace_id"), "trace_id index should exist");
        indexes.Should().Contain(idx => idx.Contains("occurred_on"), "occurred_on index should exist");
        indexes.Should().Contain(idx => idx.Contains("pk_audit_logs"), "primary key index should exist");
    }
}
