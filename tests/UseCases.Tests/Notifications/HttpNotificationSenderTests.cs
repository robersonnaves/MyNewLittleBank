using System.Net;
using System.Net.Http;
using AwesomeAssertions;
using System.Diagnostics.CodeAnalysis;
using Domain.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UseCases.Notifications;

namespace UseCases.Tests.Notifications;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class HttpNotificationSenderTests
{
    [Fact]
    public async Task NotifyInsufficientFunds_Should_Skip_When_Disabled()
    {
        using var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new NotificationOptions
        {
            EnableInsufficientFundsNotifications = false
        });
        var sender = new HttpNotificationSender(httpClient, options, NullLogger<HttpNotificationSender>.Instance);

        var notification = BuildNotification();

        var result = await sender.NotifyInsufficientFundsAsync(notification, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifyInsufficientFunds_Should_Send_When_Enabled()
    {
        using var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new NotificationOptions
        {
            EnableInsufficientFundsNotifications = true,
            BaseUrl = new Uri("http://notifications"),
            TimeoutSeconds = 2
        });
        var sender = new HttpNotificationSender(httpClient, options, NullLogger<HttpNotificationSender>.Instance);

        var notification = BuildNotification(traceId: "trace-123");

        var result = await sender.NotifyInsufficientFundsAsync(notification, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().HaveCount(1);
        handler.Requests.Single().RequestUri.Should().Be(new Uri("http://notifications/alerts/insufficient-funds"));
        handler.Requests.Single().Headers.TryGetValues("X-Correlation-Id", out var headers).Should().BeTrue();
        headers!.Single().Should().Be("trace-123");
    }

    private static InsufficientFundsNotification BuildNotification(string traceId = "trace")
    {
        return new InsufficientFundsNotification(
            "39053344705",
            "123456",
            Guid.NewGuid(),
            50m,
            10m,
            DateTime.UtcNow,
            traceId);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
