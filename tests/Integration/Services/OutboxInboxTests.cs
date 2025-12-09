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
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var outboxMessage = OutboxMessage.Create(Guid.NewGuid(), "test.message", """{"hello":"world"}""", DateTime.UtcNow).Value!;
        await context.OutboxMessages.AddAsync(outboxMessage).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var publisher = scope.ServiceProvider.GetRequiredService<TestPublisher>();
        var dispatcher = new OutboxDispatcher(
            () => scope.ServiceProvider.GetRequiredService<MyNewLittleBankContext>(),
            publisher,
            scope.ServiceProvider.GetRequiredService<IOptions<OutboxOptions>>(),
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingAsync(CancellationToken.None).ConfigureAwait(false);

        var persisted = await context.OutboxMessages.SingleAsync(message => message.MessageId == outboxMessage.MessageId).ConfigureAwait(false);
        persisted.Status.Should().Be(OutboxMessageStatus.Sent);
        persisted.SentOnUtc.Should().NotBeNull();
        publisher.PublishedMessages.Should().ContainSingle(tuple => tuple.MessageType == "test.message");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task InboxMessageStore_should_reject_duplicate_processing()
    {
        using var provider = IntegrationServiceFactory.Create(Fixture.PostgresConnectionString, Fixture.RabbitMqConnectionString);
        var context = provider.GetRequiredService<MyNewLittleBankContext>();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        var store = provider.GetRequiredService<InboxMessageStore>();
        var messageId = Guid.NewGuid();

        var first = await store.TryMarkProcessedAsync(messageId, "integration-consumer", CancellationToken.None).ConfigureAwait(false);
        var duplicate = await store.TryMarkProcessedAsync(messageId, "integration-consumer", CancellationToken.None).ConfigureAwait(false);

        first.Should().BeTrue();
        duplicate.Should().BeFalse();
    }
}
