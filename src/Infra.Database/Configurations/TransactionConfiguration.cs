using System.Diagnostics.CodeAnalysis;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Database.Configurations;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via EF Core configuration discovery.")]
internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedNever()
            .HasConversion(
                static id => id.Value,
                static value => new TransactionId(value));

        builder.Property(transaction => transaction.ClientId)
            .IsRequired()
            .HasConversion(
                static id => id.Value,
                static value => new ClientId(value))
            .Metadata.SetValueComparer(ValueComparers.ClientIdComparer);

        builder.Property(transaction => transaction.BankAccountId)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(
                static number => number.Value,
                static value => new AccountNumber(value))
            .Metadata.SetValueComparer(ValueComparers.AccountNumberComparer);

        builder.Property(transaction => transaction.Amount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasConversion(
                static money => money.Value,
                static value => new Money(value))
            .Metadata.SetValueComparer(ValueComparers.MoneyComparer);

        builder.Property(transaction => transaction.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion(
                static status => status.ToString(),
                static value => Enum.Parse<TransactionStatus>(value, true));

        builder.Property(transaction => transaction.OccurredOn)
            .HasColumnType("timestamp with time zone");

        builder.Property(transaction => transaction.Xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        builder.HasDiscriminator<string>("transaction_type")
            .HasValue<PixTransaction>("pix")
            .HasValue<MoneyTransaction>("money")
            .HasValue<CardTransaction>("card");

        builder.HasIndex("transaction_type");
        builder.HasIndex(transaction => transaction.ClientId);
        builder.HasIndex(transaction => transaction.BankAccountId);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasPrincipalKey(account => account.AccountNumber)
            .HasForeignKey(transaction => transaction.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(transaction => transaction.Type);
    }
}
