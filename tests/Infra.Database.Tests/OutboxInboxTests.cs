#pragma warning disable CA2007
using AwesomeAssertions;
using Domain.Interfaces;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infra.Database.Tests;

public sealed class OutboxInboxTests
{
    [Fact]
    public async Task OutboxDispatcherShouldPublishAndMarkSent()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new MyNewLittleBankContext(options))
        {
            var messageResult = OutboxMessage.Create(Guid.NewGuid(), "pix.created", """{"id":1}""", DateTime.UtcNow);
            messageResult.IsSuccess.Should().BeTrue();
            await seedContext.OutboxMessages.AddAsync(messageResult.Value!);
            await seedContext.SaveChangesAsync();
        }

        var publisher = new FakePublisher();
        using var dispatcher = new OutboxDispatcher(
            () => new MyNewLittleBankContext(options),
            publisher,
            Options.Create(new OutboxOptions { BatchSize = 10, PollInterval = TimeSpan.FromMilliseconds(10) }),
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        publisher.PublishedMessages.Count.Should().Be(1);
        publisher.PublishedMessages.Single().MessageType.Should().Be("pix.created");

        await using var verifyContext = new MyNewLittleBankContext(options);
        var stored = await verifyContext.OutboxMessages.SingleAsync();
        stored.Status.Should().Be(OutboxMessageStatus.Sent);
        stored.SentOnUtc.Should().NotBeNull();
        stored.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task OutboxDispatcherShouldKeepFailedMessagesPendingRetry()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new MyNewLittleBankContext(options))
        {
            var messageResult = OutboxMessage.Create(Guid.NewGuid(), "pix.created", """{"id":2}""", DateTime.UtcNow);
            messageResult.IsSuccess.Should().BeTrue();
            await seedContext.OutboxMessages.AddAsync(messageResult.Value!);
            await seedContext.SaveChangesAsync();
        }

        var failingPublisher = new FailingPublisher();
        using var dispatcher = new OutboxDispatcher(
            () => new MyNewLittleBankContext(options),
            failingPublisher,
            Options.Create(new OutboxOptions { BatchSize = 10, PollInterval = TimeSpan.FromMilliseconds(10) }),
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await using var verifyContext = new MyNewLittleBankContext(options);
        var stored = await verifyContext.OutboxMessages.SingleAsync();
        stored.Status.Should().Be(OutboxMessageStatus.Failed);
        stored.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task InboxShouldPreventDuplicatesPerConsumer()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var store = new InboxMessageStore(() => new MyNewLittleBankContext(options));
        var messageId = Guid.NewGuid();
        const string consumer = "pix-consumer";

        var first = await store.TryMarkProcessedAsync(messageId, consumer, CancellationToken.None);
        var duplicate = await store.TryMarkProcessedAsync(messageId, consumer, CancellationToken.None);

        first.Should().BeTrue();
        duplicate.Should().BeFalse();

        await using var verifyContext = new MyNewLittleBankContext(options);
        var stored = await verifyContext.InboxMessages.ToListAsync();
        stored.Count.Should().Be(1);
        stored.Single().MessageId.Should().Be(messageId);
        stored.Single().Consumer.Should().Be(consumer);
    }

    private sealed class FakePublisher : IMessagePublisher
    {
        public List<(string MessageType, string Payload)> PublishedMessages { get; } = new();

        public Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublisher : IMessagePublisher
    {
        public Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("fail"));
    }
}
