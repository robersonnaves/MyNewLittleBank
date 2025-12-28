namespace Infra.Database.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime OccurredOn { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}
