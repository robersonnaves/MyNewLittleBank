using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Domain.Common;
using Domain.DTOs;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UseCases.Notifications;

public sealed class HttpNotificationSender : INotificationSender
{
    private const string AlertsPath = "/alerts/insufficient-funds";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly Action<ILogger, Guid, Exception?> NotificationSkipped =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(1, "NotificationSkipped"),
            "Insufficient funds notification skipped by configuration for transaction {TransactionId}");

    private static readonly Action<ILogger, Guid, HttpStatusCode, Exception?> NotificationFailedStatus =
        LoggerMessage.Define<Guid, HttpStatusCode>(
            LogLevel.Warning,
            new EventId(2, "NotificationFailedStatus"),
            "Failed to notify insufficient funds for transaction {TransactionId} (status: {StatusCode})");

    private static readonly Action<ILogger, Guid, Exception?> NotificationSent =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(3, "NotificationSent"),
            "Sent insufficient funds notification for transaction {TransactionId}");

    private static readonly Action<ILogger, Guid, Exception> NotificationError =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(4, "NotificationError"),
            "Error calling notification endpoint for transaction {TransactionId}");

    private readonly HttpClient _httpClient;
    private readonly NotificationOptions _options;
    private readonly ILogger<HttpNotificationSender> _logger;

    public HttpNotificationSender(
        HttpClient httpClient,
        IOptions<NotificationOptions> options,
        ILogger<HttpNotificationSender> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var resolvedOptions = options.Value ?? throw new InvalidOperationException("Notification options are required.");
        _logger = logger;

        if (resolvedOptions.EnableInsufficientFundsNotifications)
        {
            if (resolvedOptions.BaseUrl is null)
            {
                throw new InvalidOperationException("Notifications:BaseUrl must be configured when notifications are enabled.");
            }

            _httpClient = httpClient;
            _httpClient.BaseAddress ??= resolvedOptions.BaseUrl;
            _httpClient.Timeout = TimeSpan.FromSeconds(resolvedOptions.TimeoutSeconds > 0 ? resolvedOptions.TimeoutSeconds : 5);
        }
        else
        {
            _httpClient = httpClient;
        }

        _options = resolvedOptions;
    }

    public async Task<Result> NotifyInsufficientFundsAsync(
        InsufficientFundsNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!_options.EnableInsufficientFundsNotifications)
        {
            NotificationSkipped(_logger, notification.TransactionId, null);
            return Result.Success();
        }

        var payload = JsonSerializer.Serialize(notification, SerializerOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, AlertsPath)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(notification.TraceId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", notification.TraceId);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                NotificationFailedStatus(_logger, notification.TransactionId, response.StatusCode, null);
                return Result.Failure("notification_send_failed");
            }

            NotificationSent(_logger, notification.TransactionId, null);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            NotificationError(_logger, notification.TransactionId, ex);
            return Result.Failure("notification_send_failed");
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            NotificationError(_logger, notification.TransactionId, ex);
            return Result.Failure("notification_send_failed");
        }
        catch (JsonException ex)
        {
            NotificationError(_logger, notification.TransactionId, ex);
            return Result.Failure("notification_send_failed");
        }
#pragma warning disable CA1031 // Catching all exceptions intentionally - notification failures should not crash the transaction processing
        catch (Exception ex)
        {
            NotificationError(_logger, notification.TransactionId, ex);
            return Result.Failure("notification_send_failed");
        }
#pragma warning restore CA1031
    }
}
