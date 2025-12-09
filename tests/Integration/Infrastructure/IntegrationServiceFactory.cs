using System;
using System.Collections.Generic;
using System.Globalization;
using Domain.Interfaces;
using Infra.Database;
using Infra.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MyNewLittleBank.Tests.Integration.Infrastructure;

public static class IntegrationServiceFactory
{
    public static ServiceProvider Create(string postgresConnectionString, string? rabbitConnectionString = null)
    {
        var rabbitUri = ParseRabbitUri(rabbitConnectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgresConnectionString,
                ["RabbitMQ:HostName"] = rabbitUri.Host,
                ["RabbitMQ:Port"] = rabbitUri.Port.ToString(CultureInfo.InvariantCulture),
                ["RabbitMQ:UserName"] = rabbitUri.UserName,
                ["RabbitMQ:Password"] = rabbitUri.Password,
                ["RabbitMQ:Exchange"] = "svc.transactions",
                ["RabbitMQ:RoutingKey"] = "transactions",
                ["RabbitMQ:Queue"] = "queue.transactions"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddDatabaseInfrastructure(configuration);
        services.AddRabbitMessaging(configuration);
        services.AddSingleton<TestPublisher>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<TestPublisher>());
        services.AddSingleton(Options.Create(new OutboxOptions()));
        services.AddScoped<Func<MyNewLittleBankContext>>(sp => () => sp.GetRequiredService<MyNewLittleBankContext>());

        return services.BuildServiceProvider();
    }

    private static (string Host, int Port, string UserName, string Password) ParseRabbitUri(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ("localhost", 5672, "guest", "guest");
        }

        var uri = new Uri(connectionString);
        var userInfo = (uri.UserInfo ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries);
        var user = userInfo.Length > 0 ? userInfo[0] : "guest";
        var password = userInfo.Length > 1 ? userInfo[1] : "guest";

        return (uri.Host, uri.Port > 0 ? uri.Port : 5672, user, password);
    }
}
