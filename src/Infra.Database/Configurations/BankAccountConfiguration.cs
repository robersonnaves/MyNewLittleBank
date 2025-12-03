using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

internal sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .ValueGeneratedNever();

        builder.Property(account => account.ClientId)
            .IsRequired()
            .HasConversion(
                static id => id.Value,
                static value => new ClientId(value))
            .Metadata.SetValueComparer(ValueComparers.ClientIdComparer);

        builder.Property(account => account.AccountNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(
                static number => number.Value,
                static value => new AccountNumber(value))
            .Metadata.SetValueComparer(ValueComparers.AccountNumberComparer);

        builder.Property(account => account.Balance)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasConversion(
                static money => money.Value,
                static value => new Money(value))
            .Metadata.SetValueComparer(ValueComparers.MoneyComparer);

        builder.Property(account => account.OpenedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(account => account.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasAlternateKey(account => account.AccountNumber);
        builder.HasIndex(account => account.AccountNumber).IsUnique();

        builder.Ignore(account => account.Transactions);
    }
}
