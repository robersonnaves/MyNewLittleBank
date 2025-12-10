using Domain.Entities;
using Domain.Interfaces;
using Infra.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infra.Database.Tests;

#pragma warning disable CA2007 // ConfigureAwait not required in test context
public sealed class MyNewLittleBankContextTests
{
    [Fact]
    public async Task SaveChangesAsyncPersistsEntitiesWithDiscriminator()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        await using var unitOfWork = new UnitOfWork(context);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var cpfResult = Cpf.TryCreate("52998224725");
        cpfResult.IsSuccess.Should().BeTrue();
        var cpf = cpfResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("12345678901");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var transactionId = TransactionId.New().Value!;

        var initialBalanceResult = Money.TryCreate(1_000m);
        initialBalanceResult.IsSuccess.Should().BeTrue();
        var initialBalance = initialBalanceResult.Value!;

        var amountResult = Money.TryCreate(150m);
        amountResult.IsSuccess.Should().BeTrue();
        var amount = amountResult.Value!;

        var clientResult = Client.Create(clientId, cpf, "Client Name", "client@email.com", "11999999999");
        clientResult.IsSuccess.Should().BeTrue();
        var client = clientResult.Value!;

        var accountResult = BankAccount.Open(clientId, accountNumber, initialBalance);
        accountResult.IsSuccess.Should().BeTrue();
        var account = accountResult.Value!;

        var transactionResult = PixTransaction.Create(
            transactionId,
            clientId,
            accountNumber,
            amount,
            "origin@pix",
            "destination@pix");
        transactionResult.IsSuccess.Should().BeTrue();
        var transaction = transactionResult.Value!;

        client.AddAccount(account).IsSuccess.Should().BeTrue();

        await context.Clients.AddAsync(client, CancellationToken.None);
        await context.BankAccounts.AddAsync(account, CancellationToken.None);
        await context.Transactions.AddAsync(transaction, CancellationToken.None);

        await unitOfWork.SaveChangesAsync();

        var persisted = await context.Transactions.OfType<PixTransaction>().SingleAsync();
        persisted.Status.Should().Be(TransactionStatus.Pending);
        persisted.Amount.Value.Should().Be(amount.Value);
        // Note: InMemory database doesn't support xmin system column, so Xmin will be 0
        // In real PostgreSQL, xmin will contain the transaction ID
    }

    [Fact]
    public void ModelShouldExposeDiscriminatorAndConcurrencyTokens()
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
        entityType.FindProperty(nameof(Transaction.Xmin))!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public async Task RepositoryShouldApplySpecifications()
    {
        var options = new DbContextOptionsBuilder<MyNewLittleBankContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new MyNewLittleBankContext(options);
        var repository = new EfRepository<BankAccount>(context);
        await using var unitOfWork = new UnitOfWork(context);

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var firstAccountResult = BankAccount.Open(
            clientId,
            AccountNumber.TryCreate("12345678902").Value!,
            Money.TryCreate(200m).Value!);
        firstAccountResult.IsSuccess.Should().BeTrue();
        var firstAccount = firstAccountResult.Value!;

        var secondAccountResult = BankAccount.Open(
            clientId,
            AccountNumber.TryCreate("12345678903").Value!,
            Money.TryCreate(50m).Value!);
        secondAccountResult.IsSuccess.Should().BeTrue();
        var secondAccount = secondAccountResult.Value!;

        await repository.AddRangeAsync(new[] { firstAccount, secondAccount }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync();

        var richAccounts = await repository.ListAsync(new BalanceAboveSpecification(100m), CancellationToken.None);

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
