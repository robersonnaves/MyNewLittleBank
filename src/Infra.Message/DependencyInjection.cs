using Domain.Interfaces;
using Infra.Message.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.Message;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitOptions>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<IRabbitConnectionFactory, RabbitConnectionFactory>();
        services.AddSingleton<IRabbitTopologyBootstrapper, RabbitTopologyBootstrapper>();
        services.AddSingleton<IMessagePublisher, PublisherService>();

        return services;
    }
}
