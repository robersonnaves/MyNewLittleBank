using System.Diagnostics.CodeAnalysis;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever()
            .HasConversion(
                static id => id.Value,
                static value => new ClientId(value))
            .Metadata.SetValueComparer(ValueComparers.ClientIdComparer);

        builder.Property(c => c.Cpf)
            .IsRequired()
            .HasMaxLength(11)
            .HasConversion(
                static cpf => cpf.Value,
                static value => new Cpf(value))
            .Metadata.SetValueComparer(ValueComparers.CpfComparer);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(c => c.MobileNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(c => c.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(c => c.Cpf).IsUnique();

        builder.HasMany(c => c.BankAccounts)
            .WithOne()
            .HasForeignKey(account => account.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.BankAccounts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
