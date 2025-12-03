using Domain.Entities;
using Domain.Interfaces;
using Infra.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infra.Database.Tests;

public sealed class MyNewLittleBankContextTests
{
    [Fact]
    public async Task SaveChangesAsync_PersistsEntitiesWithDiscriminator()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value;
        var cpf = Cpf.TryCreate("52998224725").Value;
        var accountNumber = AccountNumber.TryCreate("12345678901").Value;
        var transactionId = TransactionId.New().Value;
        var initialBalance = Money.TryCreate(1_000m).Value;
        var amount = Money.TryCreate(150m).Value;

        var client = Client.Create(clientId, cpf, "Client Name", "client@email.com", "11999999999").Value;
        var account = BankAccount.Open(clientId, accountNumber, initialBalance).Value;
        var transaction = PixTransaction.Create(
            transactionId,
            clientId,
            accountNumber,
            amount,
            "origin@pix",
            "destination@pix").Value;

        client.AddAccount(account);

        await context.Clients.AddAsync(client);
        await context.BankAccounts.AddAsync(account);
        await context.Transactions.AddAsync(transaction);

        await unitOfWork.SaveChangesAsync();

        var persisted = await context.Transactions.OfType<PixTransaction>().SingleAsync();
        persisted.Status.Should().Be(TransactionStatus.Pending);
        persisted.Amount.Value.Should().Be(amount.Value);
        persisted.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Model_ShouldExposeDiscriminatorAndConcurrencyTokens()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new MyNewLittleBankContext(options);
        var entityType = context.Model.FindEntityType(typeof(Transaction));

        entityType.Should().NotBeNull();
        var discriminatorProperty = entityType!.FindDiscriminatorProperty();
        discriminatorProperty.Should().NotBeNull();
        discriminatorProperty!.Name.Should().Be("transaction_type");
        entityType.FindProperty(nameof(Transaction.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_ShouldApplySpecifications()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        var repository = new EfRepository<BankAccount>(context);
        await using var unitOfWork = new UnitOfWork(context);

        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value;
        var firstAccount = BankAccount.Open(clientId, AccountNumber.TryCreate("12345678902").Value, Money.TryCreate(200m).Value).Value;
        var secondAccount = BankAccount.Open(clientId, AccountNumber.TryCreate("12345678903").Value, Money.TryCreate(50m).Value).Value;

        await repository.AddRangeAsync(new[] { firstAccount, secondAccount });
        await unitOfWork.SaveChangesAsync();

        var richAccounts = await repository.ListAsync(new BalanceAboveSpecification(100m));

        richAccounts.Should().HaveCount(1);
        richAccounts.Single().AccountNumber.Value.Should().Be("12345678902");
    }

    private sealed class BalanceAboveSpecification : Specification<BankAccount>
    {
        public BalanceAboveSpecification(decimal minimumBalance)
            : base(account => account.Balance.Value > minimumBalance)
        {
            ApplyOrderBy(account => account.OrderByDescending(x => x.OpenedAt));
        }
    }
}
