using System.Diagnostics.CodeAnalysis;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class PixTransactionConfiguration : IEntityTypeConfiguration<PixTransaction>
{
    public void Configure(EntityTypeBuilder<PixTransaction> builder)
    {
        builder.Property(transaction => transaction.OriginPixKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(transaction => transaction.DestinationPixKey)
            .IsRequired()
            .HasMaxLength(200);
    }
}
