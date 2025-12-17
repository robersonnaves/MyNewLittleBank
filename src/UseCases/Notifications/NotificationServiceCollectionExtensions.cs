using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace UseCases.Notifications;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationSender(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));

        services.AddHttpClient<INotificationSender, HttpNotificationSender>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationOptions>>().Value;
            if (options.EnableInsufficientFundsNotifications && options.BaseUrl is null)
            {
                throw new InvalidOperationException("Notifications:BaseUrl must be configured when notifications are enabled.");
            }

            if (options.BaseUrl is not null)
            {
                client.BaseAddress = options.BaseUrl;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 5);
        });

        return services;
    }
}
