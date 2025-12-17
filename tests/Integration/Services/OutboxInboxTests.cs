using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Infra.Database;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyNewLittleBank.Tests.Integration.Infrastructure;

namespace MyNewLittleBank.Tests.Integration.Services;

public sealed class OutboxInboxTests : IntegrationTestBase
{
    public OutboxInboxTests(IntegrationInfrastructureFixture fixture) : base(fixture)
    {
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task OutboxDispatcher_should_mark_messages_sent_and_publish_payloads()
    {
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString, Fixture.RabbitMqConnectionString);
        
        Guid messageId;
        TestPublisher publisher;

        // Setup phase: Create database and seed outbox message
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var context = setupScope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
            await context.Database.EnsureCreatedAsync();

            var outboxMessage = OutboxMessage.Create(Guid.NewGuid(), "test.message", """{"hello":"world"}""", DateTime.UtcNow).Value!;
            messageId = outboxMessage.MessageId;
            await context.OutboxMessages.AddAsync(outboxMessage);
            await context.SaveChangesAsync();
        }

        // Dispatch phase: Execute OutboxDispatcher with isolated context
        await using (var dispatchScope = provider.CreateAsyncScope())
        {
            publisher = dispatchScope.ServiceProvider.GetRequiredService<TestPublisher>();
            using var dispatcher = new OutboxDispatcher(
                () => dispatchScope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>(),
                publisher,
                dispatchScope.ServiceProvider.GetRequiredService<IOptions<OutboxOptions>>(),
                dispatchScope.ServiceProvider.GetRequiredService<IOptions<Infra.Message.RabbitOptions>>(),
                NullLogger<OutboxDispatcher>.Instance);

            await dispatcher.DispatchPendingAsync(CancellationToken.None);
        }

        // Verify phase: Query results with fresh context instance
        await using (var verifyScope = provider.CreateAsyncScope())
        {
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
            var persisted = await verifyContext.OutboxMessages.SingleAsync(message => message.MessageId == messageId);
            persisted.Status.Should().Be(OutboxMessageStatus.Sent);
            persisted.SentOnUtc.Should().NotBeNull();
            publisher.PublishedMessages.Should().ContainSingle(tuple => tuple.MessageType == "test.message");
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task InboxMessageStore_should_reject_duplicate_processing()
    {
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString, Fixture.RabbitMqConnectionString);
        var messageId = Guid.NewGuid();

        // Setup phase: Create database
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var context = setupScope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Execute phase: Create store with root provider factory to avoid scope issues
        var store = new InboxMessageStore(() =>
        {
            var scope = provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        });

        var first = await store.TryMarkProcessedAsync(messageId, "integration-consumer", CancellationToken.None);
        var duplicate = await store.TryMarkProcessedAsync(messageId, "integration-consumer", CancellationToken.None);

        first.Should().BeTrue();
        duplicate.Should().BeFalse();
    }
}
