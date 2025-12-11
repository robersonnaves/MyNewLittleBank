using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Domain.Common;
using Domain.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class ProcessTransactionsHandlerTests
{
    [Fact]
    public async Task HandleAsync_PixTransaction_Should_Debit_Account_And_Write_Outbox()
    {
        var account = CreateAccount(150m);
        var bankAccounts = new FakeBankAccountRepository(account);
        var transactions = new FakeTransactionRepository();
        var outbox = new FakeOutboxWriter();
        var uow = new FakeUnitOfWork();
        var handler = new ProcessTransactionsHandler(bankAccounts, transactions, outbox, uow);

        var dto = new PixTransactionDto(
            Guid.NewGuid(),
            account.ClientId.Value,
            account.AccountNumber.Value,
            50m,
            "payer@pix",
            "receiver@pix",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var result = await handler.HandleAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Value.Should().Be(100m);
        transactions.Items.Should().HaveCount(1);
        transactions.Items.Single().Type.Should().Be(TransactionType.Pix);
        outbox.Messages.Should().HaveCount(1);
        outbox.Messages.Single().MessageType.Should().Be("transaction.processed");
        ParseOutbox(outbox.Messages.Single().Payload).GetProperty("balanceAfterOperation").GetDecimal().Should().Be(100m);
        uow.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_CardTransaction_Should_Return_Failure_When_Insufficient_Funds()
    {
        var account = CreateAccount(10m);
        var bankAccounts = new FakeBankAccountRepository(account);
        var transactions = new FakeTransactionRepository();
        var outbox = new FakeOutboxWriter();
        var uow = new FakeUnitOfWork();
        var handler = new ProcessTransactionsHandler(bankAccounts, transactions, outbox, uow);

        var dto = new CardTransactionDto(
            Guid.NewGuid(),
            account.ClientId.Value,
            account.AccountNumber.Value,
            25m,
            "4111111111111111",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var result = await handler.HandleAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("insufficient_funds");
        transactions.Items.Should().BeEmpty();
        outbox.Messages.Should().BeEmpty();
        uow.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_MoneyTransaction_Should_Fail_When_Account_Not_Found()
    {
        var bankAccounts = new FakeBankAccountRepository();
        var transactions = new FakeTransactionRepository();
        var outbox = new FakeOutboxWriter();
        var uow = new FakeUnitOfWork();
        var handler = new ProcessTransactionsHandler(bankAccounts, transactions, outbox, uow);

        var dto = new MoneyTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "999999",
            10m,
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var result = await handler.HandleAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("bank_account_not_found");
        transactions.Items.Should().BeEmpty();
        outbox.Messages.Should().BeEmpty();
        uow.SaveChangesCalls.Should().Be(0);
    }

    private static JsonElement ParseOutbox(string payload) =>
        JsonDocument.Parse(payload).RootElement;

    private static BankAccount CreateAccount(decimal initialBalance)
    {
        var clientId = ClientId.TryCreate(Guid.NewGuid()).Value!;
        var accountNumber = AccountNumber.TryCreate("123456").Value!;
        var balance = Money.TryCreate(initialBalance).Value!;
        return BankAccount.Open(clientId, accountNumber, balance).Value!;
    }

    private sealed class FakeBankAccountRepository :
        IReadRepository<BankAccount>,
        IWriteRepository<BankAccount>
    {
        private readonly List<BankAccount> _items;

        public FakeBankAccountRepository(params BankAccount[] items)
        {
            _items = items.ToList();
        }

        public Task AddAsync(BankAccount entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<BankAccount> entities, CancellationToken cancellationToken = default)
        {
            _items.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<BankAccount, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().Any(predicate));

        public Task<BankAccount?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default)
        {
            var id = (Guid)keyValues[0];
            var account = _items.FirstOrDefault(x => x.Id == id);
            return Task.FromResult<BankAccount?>(account);
        }

        public Task<IReadOnlyList<BankAccount>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<BankAccount>)_items.ToList());

        public Task<IReadOnlyList<BankAccount>> ListAsync(System.Linq.Expressions.Expression<Func<BankAccount, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<BankAccount>)_items.AsQueryable().Where(predicate).ToList());

        public void Remove(BankAccount entity) => _items.Remove(entity);

        public void Update(BankAccount entity)
        {
            var index = _items.FindIndex(item => item.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }
        }
    }

    private sealed class FakeTransactionRepository : IWriteRepository<Transaction>
    {
        public List<Transaction> Items { get; } = new();

        public Task AddAsync(Transaction entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Transaction> entities, CancellationToken cancellationToken = default)
        {
            Items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Remove(Transaction entity) => Items.Remove(entity);

        public void Update(Transaction entity)
        {
        }
    }

    private sealed record OutboxMessage(string MessageType, string Payload);

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<OutboxMessage> Messages { get; } = new();

        public Task AddAsync(string messageType, string payload, CancellationToken cancellationToken = default)
        {
            Messages.Add(new OutboxMessage(messageType, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}
