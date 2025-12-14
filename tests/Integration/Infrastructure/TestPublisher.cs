using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Domain.Interfaces;

namespace MyNewLittleBank.Tests.Integration.Infrastructure;

public sealed class TestPublisher : IMessagePublisher
{
    public ConcurrentBag<(string MessageType, string Payload, string RoutingKey)> PublishedMessages { get; } = new();

    public Task PublishAsync(string messageType, string payload, string routingKey, CancellationToken cancellationToken = default)
    {
        PublishedMessages.Add((messageType, payload, routingKey));
        return Task.CompletedTask;
    }
}
