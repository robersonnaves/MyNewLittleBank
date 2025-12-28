using System.Diagnostics;
using Domain.Entities;
using Infra.Database.Entities;
using Infra.Database.Interceptors;
using Infra.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infra.Database.Tests;

#pragma warning disable CA2007 // ConfigureAwait not required in test context
public sealed class AuditInterceptorTests
{
    [Fact]
    public async Task SavingChanges_WhenAddingClient_ShouldCreateAuditLogWithAddedAction()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "John Doe", "john@email.com", "11999999999");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act
        await context.Clients.AddAsync(client, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.EntityName.Should().Be(nameof(Client));
        auditLog.EntityId.Should().Be(clientId.Value.ToString());
        auditLog.Action.Should().Be("Added");
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.NewValues.Should().Contain("John Doe");
        auditLog.NewValues.Should().Contain("john@email.com");
        auditLog.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SavingChanges_WhenModifyingBankAccount_ShouldCreateAuditLogWithModifiedAction()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("12345678901");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var initialBalanceResult = Money.TryCreate(1_000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        await context.BankAccounts.AddAsync(account, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        // Clear audit logs from the initial insert
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await unitOfWork.SaveChangesAsync();

        // Act - Modify the account balance
        var creditAmount = Money.TryCreate(500m).Value!;
        account.Credit(creditAmount);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.EntityName.Should().Be(nameof(BankAccount));
        // BankAccount has both Id (PK) and AccountNumber (Alternate Key), so EntityId is composite
        var expectedEntityId = $"{account.Id:D}-{account.AccountNumber.Value}";
        auditLog.EntityId.Should().Be(expectedEntityId);
        auditLog.Action.Should().Be("Modified");
        auditLog.OldValues.Should().NotBeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.OldValues.Should().Contain("1000");
        auditLog.NewValues.Should().Contain("1500");
    }

    [Fact]
    public async Task SavingChanges_WhenDeletingTransaction_ShouldCreateAuditLogWithDeletedAction()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("12345678901");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var transactionId = TransactionId.New().Value!;
        var amountResult = Money.TryCreate(150m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        var transactionResult = PixTransaction.Create(
            transactionId,
            clientId,
            accountNumber,
            amount,
            "origin@pix",
            "destination@pix");
        transactionResult.IsSuccess.Should().BeTrue();
        var transaction = transactionResult.Value!;

        await context.Transactions.AddAsync(transaction, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        // Clear audit logs from the initial insert
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await unitOfWork.SaveChangesAsync();

        // Act - Delete the transaction
        context.Transactions.Remove(transaction);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.EntityName.Should().Be(nameof(PixTransaction));
        auditLog.EntityId.Should().Be(transactionId.Value.ToString());
        auditLog.Action.Should().Be("Deleted");
        auditLog.OldValues.Should().NotBeNull();
        auditLog.NewValues.Should().BeNull();
        auditLog.OldValues.Should().Contain("150");
    }

    [Fact]
    public async Task SavingChanges_WithActiveActivity_ShouldCaptureTraceIdAndSpanId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        using var activitySource = new ActivitySource("TestSource");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("TestActivity");

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Jane Doe", "jane@email.com", "11888888888");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act
        await context.Clients.AddAsync(client, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var auditLog = auditLogs[0];
        auditLog.TraceId.Should().NotBeNull();
        auditLog.SpanId.Should().NotBeNull();
        auditLog.TraceId.Should().Be(activity!.TraceId.ToString());
        auditLog.SpanId.Should().Be(activity!.SpanId.ToString());
    }

    [Fact(Skip = "InMemory database does not support transactions")]
    public async Task SavingChanges_WhenTransactionRollback_ShouldNotPersistAuditLogs()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Test Client", "test@email.com", "11777777777");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        // Act - Start a transaction and rollback
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Assert
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().BeEmpty();

        var clients = await context.Clients.ToListAsync();
        clients.Should().BeEmpty();
    }

    [Fact]
    public async Task SavingChanges_WhenEntityDoesNotImplementIAuditable_ShouldNotCreateAuditLog()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var outboxMessageResult = OutboxMessage.Create(
            Guid.NewGuid(),
            "test.event",
            "{\"data\": \"test\"}",
            DateTime.UtcNow);
        outboxMessageResult.IsSuccess.Should().BeTrue();
        var outboxMessage = outboxMessageResult.Value!;

        // Act
        await context.OutboxMessages.AddAsync(outboxMessage, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        // Assert - OutboxMessage doesn't implement IAuditable, so no audit log should be created
        var auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().BeEmpty();

        var messages = await context.OutboxMessages.ToListAsync();
        messages.Should().HaveCount(1);
    }
}
#pragma warning restore CA2007
