using System.Diagnostics;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Infra.Message;

/// <summary>
/// Pipeline step para processar headers de tracing na entrada (Consumer).
/// Extrai o contexto de trace (traceparent) e inicia/continua a Activity.
/// </summary>
public sealed class TracingIncomingStep : IIncomingStep
{
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var message = context.Load<Rebus.Messages.Message>();
        var headers = message.Headers;

        // Tentar extrair o parent context dos headers
        ActivityContext parentContext = default;
        
        if (headers.TryGetValue("traceparent", out var traceParent))
        {
            ActivityContext.TryParse(traceParent, null, out parentContext);
        }
        
        // Extrair tracestate se existir
        string? traceState = null;
        if (headers.TryGetValue("tracestate", out var ts))
        {
            traceState = ts;
        }

        // Criar Activity para o processamento da mensagem
        // Usamos o nome da fila ou tipo de mensagem para identificar a operação
        var label = headers.TryGetValue(Headers.Type, out var l) ? l : "Unknown";
        var activityName = $"Process {label}";        
        
        using var activity = new Activity(activityName);
        
        // Configurar Parent
        if (parentContext != default)
        {
            activity.SetParentId(parentContext.TraceId, parentContext.SpanId, parentContext.TraceFlags);
            activity.TraceStateString = traceState;
        }

        activity.Start();

        try
        {
            // Adicionar Tags padrão Ootel
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.operation", "process");
            
            if (headers.TryGetValue(Headers.MessageId, out var msgId))
                activity.SetTag("messaging.message_id", msgId);
                
            if (headers.TryGetValue(Headers.Type, out var msgType))
                activity.SetTag("messaging.message_type", msgType);

            await next().ConfigureAwait(false);
            
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        catch (TaskCanceledException) when (context.Load<Rebus.Messages.Message>().Headers.TryGetValue("cancellation-token", out _))
        {
            activity.SetStatus(ActivityStatusCode.Error, "Operation cancelled");
            activity.AddTag("exception.type", typeof(TaskCanceledException).FullName);
            throw;
        }
#pragma warning disable CA1031 // This is a tracing middleware that must capture any exception type for observability
        catch (Exception ex)
        {
            // Catching all exceptions intentionally - this is a tracing middleware that must capture any exception type for observability
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.AddTag("exception.type", ex.GetType().FullName);
            activity.AddTag("exception.message", ex.Message);
            activity.AddTag("exception.stacktrace", ex.StackTrace);
            throw;
        }
#pragma warning restore CA1031
        finally
        {
            activity.Stop();
        }
    }
}
