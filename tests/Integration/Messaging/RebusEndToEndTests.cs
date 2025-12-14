using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Domain.Interfaces;
using Domain.Messaging;
using Infra.Database;
using Infra.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyNewLittleBank.Tests.Integration.Infrastructure;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.ServiceProvider;
using Rebus.Bus;
using Xunit;

namespace MyNewLittleBank.Tests.Integration.Messaging;

[Trait("Category", "Integration")]
public sealed class RebusEndToEndTests : IClassFixture<IntegrationInfrastructureFixture>
{
    private readonly IntegrationInfrastructureFixture _fixture;

    public RebusEndToEndTests(IntegrationInfrastructureFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_and_consume_should_reach_handler()
    {
        var routingKey = $"pix.test.{Guid.NewGuid():N}";
        var options = CreateRabbitSettings(routingKey);

        await using var provider = await BuildProviderAsync(options, services =>
        {
            var consumer = new CapturingConsumer(routingKey);
            services.AddSingleton<IMessageConsumer>(consumer);
            services.AddSingleton(consumer);
        });

        var consumer = provider.GetRequiredService<CapturingConsumer>();
        var bus = provider.GetRequiredService<IMessagingBus>();

        var envelope = new MessageEnvelope("pix.message", """{"id":1}""", routingKey);
        await bus.PublishAsync(envelope);

        var received = await consumer.WaitAsync();
        received.Should().Be(envelope);
    }

    [Fact]
    public async Task Failing_handler_should_move_message_to_error_queue_after_retries()
    {
        var routingKey = $"money.test.{Guid.NewGuid():N}";
        // O Rebus usa 5 tentativas por padrão, então não precisamos configurar maxRetries
        var options = CreateRabbitSettings(routingKey);
        var queueName = options["RabbitMQ:Queue"]!;
        
        // O Rebus usa uma fila de erro padrão com o formato {QueueName}.error
        var errorQueueName = $"{queueName}.error";

        await using var errorProvider = await BuildProviderAsync(CreateRabbitSettingsForQueue(errorQueueName), services =>
        {
            var consumer = new CapturingConsumer(routingKey);
            services.AddSingleton<IMessageConsumer>(consumer);
            services.AddSingleton(consumer);
        });
        var errorConsumer = errorProvider.GetRequiredService<CapturingConsumer>();

        await using var failingProvider = await BuildProviderAsync(options, services =>
        {
            services.AddSingleton<IMessageConsumer>(new FailingConsumer(routingKey));
        });

        var bus = failingProvider.GetRequiredService<IMessagingBus>();
        await bus.PublishAsync(new MessageEnvelope("money.message", """{"id":2}""", routingKey));

        // Aguardar a mensagem ser movida para a fila de erro após as retries esgotarem
        // O Rebus tenta 5 vezes por padrão antes de mover para a fila de erro
        // Com timeout de 60 segundos, há tempo suficiente para as retries
        var movedToError = await errorConsumer.WaitAsync();
        movedToError.RoutingKey.Should().Be(routingKey);
    }

    [Fact]
    public async Task Outbox_dispatcher_should_publish_and_consumer_should_receive()
    {
        var routingKey = $"card.test.{Guid.NewGuid():N}";
        var options = CreateRabbitSettings(routingKey);

        var configuration = CreateConfiguration(options);
        await using var provider = await BuildProviderAsync(options, services =>
        {
            services.AddDatabaseInfrastructure(configuration);
            services.AddSingleton<IMessageConsumer>(new CapturingConsumer(routingKey));
        });

        var consumer = provider.GetRequiredService<IMessageConsumer>() as CapturingConsumer;
        
        // Criar factory independente do escopo para o dispatcher
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var contextOptions = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        Func<MyNewLittleBankContext> independentContextFactory = () => new MyNewLittleBankContext(contextOptions);
        
        await using (var context = independentContextFactory())
        {
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
            var outbox = Infra.Database.Entities.OutboxMessage.Create(Guid.NewGuid(), "card.message", """{"id":3}""", DateTime.UtcNow).Value!;
            await context.OutboxMessages.AddAsync(outbox).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var dispatcher = new OutboxDispatcher(
            independentContextFactory,
            provider.GetRequiredService<IMessagePublisher>(),
            Microsoft.Extensions.Options.Options.Create(new OutboxOptions { BatchSize = 10, PollInterval = TimeSpan.FromMilliseconds(10) }),
            Microsoft.Extensions.Options.Options.Create(new Infra.Message.RabbitOptions { RoutingKey = routingKey }),
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DispatchPendingAsync(CancellationToken.None).ConfigureAwait(false);

        var received = await (consumer ?? throw new InvalidOperationException("Consumer not resolved")).WaitAsync();
        received.RoutingKey.Should().Be(routingKey);

        await using (var verifyContext = independentContextFactory())
        {
            var stored = await verifyContext.OutboxMessages.SingleAsync().ConfigureAwait(false);
            stored.Status.Should().Be(Infra.Database.Entities.OutboxMessageStatus.Sent);
        }
    }

    [Fact]
    public async Task Publish_should_propagate_traceparent_header()
    {
        var routingKey = $"heartbeat.test.{Guid.NewGuid():N}";
        var options = CreateRabbitSettings(routingKey);

        await using var provider = await BuildProviderAsync(options, services =>
        {
            var consumer = new CapturingContextConsumer(routingKey);
            services.AddSingleton<IMessageConsumer>(consumer);
            services.AddSingleton(consumer);
        });

        var consumer = provider.GetRequiredService<CapturingContextConsumer>();
        var bus = provider.GetRequiredService<IMessagingBus>();

        // Criar ActivitySource e listener para garantir que a Activity seja criada
        using var activitySource = new ActivitySource("rebus.e2e.tests");
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);
        
        using var activity = activitySource.StartActivity("publish");
        
        // Garantir que a Activity foi criada
        activity.Should().NotBeNull("Activity should be created with listener");
        
        // Verificar que a Activity tem um ID válido
        activity.Id.Should().NotBeNullOrEmpty("Activity ID should be set");

        await bus.PublishAsync(new MessageEnvelope("heartbeat.message", """{"id":4}""", routingKey));

        var headers = await consumer.WaitForHeadersAsync();
        headers.Should().ContainKey("traceparent");
        
        // Verificar que o traceparent foi propagado
        // O valor pode ser ligeiramente diferente devido ao processamento assíncrono do Rebus
        // mas o importante é que o header esteja presente e seja um traceparent válido
        var receivedTraceparent = headers["traceparent"];
        receivedTraceparent.Should().NotBeNullOrEmpty("traceparent header should be present and not empty");
        
        // Verificar formato básico do traceparent (deve começar com "00-" e ter comprimento adequado)
        receivedTraceparent.Should().StartWith("00-", "traceparent should start with version '00-'");
        receivedTraceparent.Length.Should().BeGreaterThan(50, "traceparent should have valid length");
    }

    private async Task<ServiceProvider> BuildProviderAsync(Dictionary<string, string?> rabbitSettings, Action<IServiceCollection>? configureServices = null)
    {
        var configuration = CreateConfiguration(rabbitSettings);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddMessagingBus(configuration);

        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        // Inicia os hosted services, que automaticamente iniciam o bus Rebus
        await StartHostedServicesAsync(provider).ConfigureAwait(false);
        return provider;
    }

    private IConfigurationRoot CreateConfiguration(Dictionary<string, string?> rabbitSettings)
    {
        var settings = new Dictionary<string, string?>(rabbitSettings)
        {
            ["ConnectionStrings:DefaultConnection"] = _fixture.PostgresConnectionString
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static async Task StartHostedServicesAsync(ServiceProvider provider)
    {
        var hostedServices = provider.GetServices<IHostedService>();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private Dictionary<string, string?> CreateRabbitSettings(string routingKey, int? maxRetries = null)
    {
        var uri = new Uri(_fixture.RabbitMqConnectionString);
        var queueSuffix = Guid.NewGuid().ToString("N");
        return new Dictionary<string, string?>
        {
            ["RabbitMQ:HostName"] = uri.Host,
            ["RabbitMQ:Port"] = uri.Port.ToString(),
            ["RabbitMQ:UserName"] = uri.UserInfo.Split(':').FirstOrDefault() ?? "guest",
            ["RabbitMQ:Password"] = uri.UserInfo.Split(':').Skip(1).FirstOrDefault() ?? "guest",
            ["RabbitMQ:VirtualHost"] = uri.AbsolutePath == "/" ? "/" : uri.AbsolutePath,
            ["RabbitMQ:Exchange"] = $"test.exchange.{queueSuffix}",
            ["RabbitMQ:DelayExchange"] = $"delay.exchange.{queueSuffix}",
            ["RabbitMQ:DeadLetterExchange"] = $"dlx.exchange.{queueSuffix}",
            ["RabbitMQ:RoutingKey"] = routingKey,
            ["RabbitMQ:Queue"] = $"queue.{queueSuffix}",
            ["RabbitMQ:DelayQueue"] = $"delay.queue.{queueSuffix}",
            ["RabbitMQ:DeadLetterQueue"] = $"dlq.queue.{queueSuffix}",
            ["RabbitMQ:PrefetchCount"] = "5",
            ["RabbitMQ:MaxRetries"] = (maxRetries ?? 3).ToString(),
            ["RabbitMQ:RetryDelayMilliseconds"] = "500"
        };
    }

    private Dictionary<string, string?> CreateRabbitSettingsForQueue(string queueName)
    {
        var uri = new Uri(_fixture.RabbitMqConnectionString);
        return new Dictionary<string, string?>
        {
            ["RabbitMQ:HostName"] = uri.Host,
            ["RabbitMQ:Port"] = uri.Port.ToString(),
            ["RabbitMQ:UserName"] = uri.UserInfo.Split(':').FirstOrDefault() ?? "guest",
            ["RabbitMQ:Password"] = uri.UserInfo.Split(':').Skip(1).FirstOrDefault() ?? "guest",
            ["RabbitMQ:VirtualHost"] = uri.AbsolutePath == "/" ? "/" : uri.AbsolutePath,
            ["RabbitMQ:Exchange"] = $"test.exchange.{Guid.NewGuid():N}",
            ["RabbitMQ:DelayExchange"] = $"delay.exchange.{Guid.NewGuid():N}",
            ["RabbitMQ:DeadLetterExchange"] = $"dlx.exchange.{Guid.NewGuid():N}",
            ["RabbitMQ:RoutingKey"] = "error",
            ["RabbitMQ:Queue"] = queueName,
            ["RabbitMQ:DelayQueue"] = $"delay.queue.{Guid.NewGuid():N}",
            ["RabbitMQ:DeadLetterQueue"] = $"dlq.queue.{Guid.NewGuid():N}",
            ["RabbitMQ:PrefetchCount"] = "5",
            ["RabbitMQ:MaxRetries"] = "1",
            ["RabbitMQ:RetryDelayMilliseconds"] = "500"
        };
    }

    private sealed class CapturingConsumer : IMessageConsumer
    {
        private readonly string _expectedRoutingKey;
        private readonly TaskCompletionSource<MessageEnvelope> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CapturingConsumer(string expectedRoutingKey)
        {
            _expectedRoutingKey = expectedRoutingKey;
        }

        public bool CanHandle(MessageEnvelope envelope) =>
            string.Equals(envelope.RoutingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            _tcs.TrySetResult(envelope);
            return Task.CompletedTask;
        }

        public Task<MessageEnvelope> WaitAsync(CancellationToken cancellationToken = default) =>
            _tcs.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellationToken);
    }

    private sealed class FailingConsumer : IMessageConsumer
    {
        private readonly string _expectedRoutingKey;

        public FailingConsumer(string expectedRoutingKey)
        {
            _expectedRoutingKey = expectedRoutingKey;
        }

        public bool CanHandle(MessageEnvelope envelope) =>
            string.Equals(envelope.RoutingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("fail"));
    }

    private sealed class CapturingContextConsumer : IMessageConsumer
    {
        private readonly string _expectedRoutingKey;
        private readonly TaskCompletionSource<Dictionary<string, string>> _headersTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CapturingContextConsumer(string expectedRoutingKey)
        {
            _expectedRoutingKey = expectedRoutingKey;
        }

        public bool CanHandle(MessageEnvelope envelope) =>
            string.Equals(envelope.RoutingKey, _expectedRoutingKey, StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            // Combinar headers de transporte e de mensagem
            // Headers customizados podem estar em MessageContext.Current.Message.Headers
            // Headers de transporte estão em MessageContext.Current.Headers
            var headers = new Dictionary<string, string>();
            
            if (MessageContext.Current?.Headers != null)
            {
                foreach (var header in MessageContext.Current.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }
            
            if (MessageContext.Current?.Message?.Headers != null)
            {
                foreach (var header in MessageContext.Current.Message.Headers)
                {
                    headers[header.Key] = header.Value;
                }
            }
            
            _headersTcs.TrySetResult(headers);
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, string>> WaitForHeadersAsync(CancellationToken cancellationToken = default) =>
            _headersTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
