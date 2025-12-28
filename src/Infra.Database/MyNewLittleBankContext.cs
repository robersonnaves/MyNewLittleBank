using System.Linq;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infra.Database;

public sealed class MyNewLittleBankContext : DbContext
{
    public MyNewLittleBankContext(DbContextOptions<MyNewLittleBankContext> options)
        : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Entities.OutboxMessage> OutboxMessages => Set<Entities.OutboxMessage>();
    public DbSet<Entities.InboxMessage> InboxMessages => Set<Entities.InboxMessage>();
    public DbSet<Entities.AuditLog> AuditLogs => Set<Entities.AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime));

            foreach (var property in dateTimeProperties)
            {
                property.SetValueConverter(dateTimeConverter);
            }
        }

        var moneyConverter = new ValueConverter<Money, decimal>(
            money => money.Value,
            value => new Money(value));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var moneyProperties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(Money));

            foreach (var property in moneyProperties)
            {
                property.SetValueConverter(moneyConverter);
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyNewLittleBankContext).Assembly);
    }
}
