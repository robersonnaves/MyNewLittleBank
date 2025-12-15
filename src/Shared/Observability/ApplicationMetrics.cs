using System.Diagnostics.Metrics;

namespace Shared.Observability;

/// <summary>
/// Helper para criação padronizada de métricas de aplicação.
/// Encapsula a criação de Meters e Counters para garantir consistência.
/// </summary>
public static class ApplicationMetrics
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, Meter> _meters = new();

    /// <summary>
    /// Obtém ou cria um Meter para o escopo especificado.
    /// </summary>
    /// <param name="scopeName">Nome do escopo (geralmente o nome do serviço ou componente)</param>
    /// <param name="version">Versão do meter (opcional)</param>
    /// <returns>Instância de Meter</returns>
    public static Meter GetMeter(string scopeName, string version = "1.0.0")
    {
        lock (_lock)
        {
            if (!_meters.TryGetValue(scopeName, out var meter))
            {
                meter = new Meter(scopeName, version);
                _meters[scopeName] = meter;
            }
            return meter;
        }
    }

    /// <summary>
    /// Cria um contador padrão para métricas de negócio.
    /// </summary>
    public static Counter<T> CreateCounter<T>(string meterName, string counterName, string? unit = null, string? description = null) where T : struct
    {
        var meter = GetMeter(meterName);
        return meter.CreateCounter<T>(counterName, unit, description);
    }
    
    /// <summary>
    /// Cria um histograma padrão para métricas de latência/distribuição.
    /// </summary>
    public static Histogram<T> CreateHistogram<T>(string meterName, string histogramName, string? unit = null, string? description = null) where T : struct
    {
        var meter = GetMeter(meterName);
        return meter.CreateHistogram<T>(histogramName, unit, description);
    }
}
