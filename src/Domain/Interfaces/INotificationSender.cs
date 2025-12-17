using Domain.Common;
using Domain.DTOs;

namespace Domain.Interfaces;

public interface INotificationSender
{
    Task<Result> NotifyInsufficientFundsAsync(
        InsufficientFundsNotification notification,
        CancellationToken cancellationToken = default);
}
