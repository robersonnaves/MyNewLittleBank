using System.Diagnostics;
using System.Text.Json;
using Domain.Common;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infra.Database.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = CreateAuditEntries(eventData.Context);
        
        foreach (var auditLog in auditEntries)
        {
            await eventData.Context.Set<AuditLog>().AddAsync(auditLog, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static List<AuditLog> CreateAuditEntries(DbContext context)
    {
        var auditEntries = new List<AuditLog>();
        var activity = Activity.Current;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable || 
                entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                Action = GetActionName(entry.State),
                OccurredOn = DateTime.UtcNow,
                TraceId = activity?.TraceId.ToString(),
                SpanId = activity?.SpanId.ToString()
            };

            if (entry.State == EntityState.Added)
            {
                auditLog.NewValues = SerializeProperties(entry.CurrentValues);
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditLog.OldValues = SerializeProperties(entry.OriginalValues);
            }
            else if (entry.State == EntityState.Modified)
            {
                auditLog.OldValues = SerializeProperties(entry.OriginalValues);
                auditLog.NewValues = SerializeProperties(entry.CurrentValues);
            }

            auditEntries.Add(auditLog);
        }

        return auditEntries;
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var keyValues = entry.Properties
            .Where(p => p.Metadata.IsKey())
            .Select(p => p.CurrentValue?.ToString() ?? "null");

        return string.Join("-", keyValues);
    }

    private static string GetActionName(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "Added",
            EntityState.Modified => "Modified",
            EntityState.Deleted => "Deleted",
            _ => "Unknown"
        };
    }

    private static string? SerializeProperties(PropertyValues propertyValues)
    {
        var properties = propertyValues.Properties
            .Where(p => !p.IsShadowProperty() && !p.IsForeignKey())
            .ToDictionary(
                p => p.Name,
                p => SerializePropertyValue(propertyValues[p]));

        return properties.Count == 0 ? null : JsonSerializer.Serialize(properties, JsonOptions);
    }

    private static object? SerializePropertyValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        // Handle value objects and complex types by getting their string representation
        var type = value.GetType();
        
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || 
            type == typeof(DateTime) || type == typeof(Guid))
        {
            return value;
        }

        // For value objects, try to get their underlying value
        var valueProperty = type.GetProperty("Value");
        if (valueProperty != null)
        {
            return valueProperty.GetValue(value);
        }

        // Fallback to string representation
        return value.ToString();
    }
}
