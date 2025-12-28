using System.Diagnostics.CodeAnalysis;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.OldValues)
            .HasColumnType("jsonb");

        builder.Property(x => x.NewValues)
            .HasColumnType("jsonb");

        builder.Property(x => x.OccurredOn)
            .IsRequired();

        builder.Property(x => x.TraceId)
            .HasMaxLength(100);

        builder.Property(x => x.SpanId)
            .HasMaxLength(100);

        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.TraceId);
        builder.HasIndex(x => x.OccurredOn);
    }
}
