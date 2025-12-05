using Microsoft.Extensions.Options;

namespace Infra.Message;

public sealed class RabbitOptions
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public bool SslEnabled { get; init; }
    public string? SslServerName { get; init; }

    public string Exchange { get; init; } = "svc.transactions";
    public string DelayExchange { get; init; } = "delay.transactions";
    public string DeadLetterExchange { get; init; } = "dlx.transactions";
    public string RoutingKey { get; init; } = "transactions";
    public string Queue { get; init; } = "queue.transactions";
    public string DelayQueue { get; init; } = "delay.transactions";
    public string DeadLetterQueue { get; init; } = "dlq.transactions";

    public ushort PrefetchCount { get; init; } = 10;
    public int MaxRetries { get; init; } = 3;
    public int RetryDelayMilliseconds { get; init; } = 5000;

    public static void Validate(RabbitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        const string hostNameRequired = "HostName is required.";
        const string exchangeRequired = "Exchange is required.";
        const string queueRequired = "Queue is required.";

        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new OptionsValidationException(nameof(RabbitOptions), typeof(RabbitOptions), new[] { hostNameRequired });
        }

        if (string.IsNullOrWhiteSpace(options.Exchange))
        {
            throw new OptionsValidationException(nameof(RabbitOptions), typeof(RabbitOptions), new[] { exchangeRequired });
        }

        if (string.IsNullOrWhiteSpace(options.Queue))
        {
            throw new OptionsValidationException(nameof(RabbitOptions), typeof(RabbitOptions), new[] { queueRequired });
        }
    }
}
