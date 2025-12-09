using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Domain.Interfaces;

namespace MyNewLittleBank.Tests.Integration.Infrastructure;

public sealed class TestPublisher : IMessagePublisher
{
    public ConcurrentBag<(string MessageType, string Payload)> PublishedMessages { get; } = new();

    public Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        PublishedMessages.Add((messageType, payload));
        return Task.CompletedTask;
    }
}
