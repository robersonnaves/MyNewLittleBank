using System.Diagnostics;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;

namespace Infra.Message;

/// <summary>
/// Pipeline step que adiciona headers de tracing (traceparent, tracestate) a todas as mensagens enviadas.
/// Garante que os headers sejam propagados corretamente através do pipeline do Rebus.
/// </summary>
public sealed class TracingHeadersStep : IOutgoingStep
{
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        
        var message = context.Load<Rebus.Messages.Message>();
        var headers = message.Headers;
        
        // Adicionar headers de tracing se Activity.Current estiver disponível
        var currentActivity = Activity.Current;
        if (currentActivity != null && !string.IsNullOrEmpty(currentActivity.Id))
        {
            headers["traceparent"] = currentActivity.Id;
            if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
            {
                headers["tracestate"] = currentActivity.TraceStateString;
            }
        }
        
        await next().ConfigureAwait(false);
    }
}
