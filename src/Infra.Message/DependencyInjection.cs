using Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Pipeline.Send;
using Rebus.Pipeline.Receive;
using Rebus.ServiceProvider;

namespace Infra.Message;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingBus(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitOptions>(configuration.GetSection("RabbitMQ"));
        services.AddRebus((configure, provider) =>
        {
            var options = provider.GetRequiredService<IOptions<RabbitOptions>>().Value;
            
            RabbitOptions.Validate(options);

            var connectionString = BuildRabbitConnectionString(options);
            configure.Transport(t => t.UseRabbitMq(connectionString, options.Queue)
                .InputQueueOptions(queueConfig =>
                {
                    // Configurar DLQ no RabbitMQ usando DeadLetterExchange
                    // Quando as retries do Rebus esgotarem, as mensagens serão movidas para a DLQ
                    if (!string.IsNullOrWhiteSpace(options.DeadLetterExchange))
                    {
                        queueConfig.AddArgument("x-dead-letter-exchange", options.DeadLetterExchange);
                        if (!string.IsNullOrWhiteSpace(options.DeadLetterQueue))
                        {
                            queueConfig.AddArgument("x-dead-letter-routing-key", options.DeadLetterQueue);
                        }
                    }
                }));
            
            configure.Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
                // O Rebus já tem retries configurados por padrão (5 tentativas)
                // Quando as retries esgotarem, a mensagem será movida para a fila de erro
                // A fila de erro padrão é {QueueName}.error, mas podemos configurar via DLQ no RabbitMQ
                
                // Adicionar middleware para propagar headers de tracing
                // OnSend deve funcionar tanto para Send quanto para Topics.Publish
                o.Decorate<IPipeline>(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var outgoingStep = new TracingHeadersStep();
                    var incomingStep = new TracingIncomingStep();
                    
                    return new PipelineStepInjector(pipeline)
                        .OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep))
                        .OnReceive(incomingStep, PipelineRelativePosition.Before, typeof(ActivateHandlersStep));
                });

            });
            
            return configure;
        });

        services.AutoRegisterHandlersFromAssemblyOf<MessageEnvelopeHandler>();
        services.AddScoped<IMessagingBus, RebusMessagingBus>();
        services.AddScoped<IMessagePublisher, RebusMessagePublisher>();
        services.AddHostedService<MessagingSubscriptionInitializer>();

        return services;
    }

    public static IServiceCollection AddRabbitMessaging(this IServiceCollection services, IConfiguration configuration) =>
        AddMessagingBus(services, configuration);

    private static string BuildRabbitConnectionString(RabbitOptions options)
    {
        var credentials = $"{Uri.EscapeDataString(options.UserName)}:{Uri.EscapeDataString(options.Password)}";
        return $"amqp://{credentials}@{options.HostName}:{options.Port}{options.VirtualHost}";
    }
}
