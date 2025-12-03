using System.Diagnostics.CodeAnalysis;
using Infra.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => new { message.MessageId, message.Consumer });

        builder.Property(message => message.MessageId)
            .ValueGeneratedNever();

        builder.Property(message => message.Consumer)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(message => message.ProcessedOnUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(message => message.Consumer);
    }
}
