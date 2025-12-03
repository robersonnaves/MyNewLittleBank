using System.Diagnostics.CodeAnalysis;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.MessageId);

        builder.Property(message => message.MessageType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(message => message.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(message => message.OccurredOnUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.SentOnUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.Status)
            .HasConversion<int>()
            .HasColumnName("status");

        builder.Property(message => message.Attempts)
            .HasDefaultValue(0);

        builder.HasIndex(message => message.Status);
    }
}
