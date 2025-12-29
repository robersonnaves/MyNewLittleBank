using System.Diagnostics.CodeAnalysis;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using UseCases.Accounts;
using UseCases.Clients;

namespace UseCases.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class CoreApiUseCasesTests
{
    [Fact]
    public async Task CreateClient_Should_Persist_When_Data_Valid()
    {
        var repository = new FakeClientRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateClientHandler(repository, repository, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateClientCommand("52998224725", "Maria", "maria@example.com", "11999999999"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Items.Should().HaveCount(1);
        repository.Items.Single().Name.Should().Be("Maria");
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateClient_Should_Return_Failure_When_Cpf_Already_Exists()
    {
        var existingClient = Client.Create(
            ClientId.TryCreate(Guid.NewGuid()).Value!,
            Cpf.TryCreate("52998224725").Value!,
            "Existing",
            "existing@example.com",
            "11999990000").Value!;

        var repository = new FakeClientRepository(existingClient);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateClientHandler(repository, repository, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateClientCommand("52998224725", "Maria", "maria@example.com", "11999999999"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("client_cpf_already_exists");
        repository.Items.Should().HaveCount(1);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetClient_Should_Return_NotFound_When_Client_Does_Not_Exist()
    {
        var repository = new FakeClientRepository();
        var handler = new GetClientHandler(repository);

        var result = await handler.HandleAsync(new GetClientQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("client_not_found");
    }

    [Fact]
    public async Task GetAccountBalance_Should_Return_Current_Balance_When_Account_Exists()
    {
        var accountNumber = AccountNumber.TryCreate("12345678").Value!;
        var account = BankAccount.Open(
            ClientId.TryCreate(Guid.NewGuid()).Value!,
            accountNumber,
            Money.TryCreate(250m).Value!).Value!;

        var repository = new FakeAccountRepository(account);
        var handler = new GetAccountBalanceHandler(repository);

        var result = await handler.HandleAsync(new GetAccountBalanceQuery(accountNumber.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(250m);
    }

    [Fact]
    public async Task GetAccount_Should_Return_NotFound_When_Account_Missing()
    {
        var repository = new FakeAccountRepository();
        var handler = new GetAccountHandler(repository);

        var result = await handler.HandleAsync(new GetAccountQuery("99999999"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("bank_account_not_found");
    }

    private sealed class FakeClientRepository : IReadRepository<Client>, IWriteRepository<Client>
    {
        private readonly List<Client> _items;

        public FakeClientRepository(params Client[] items)
        {
            _items = items.ToList();
        }

        public List<Client> Items => _items;

        public Task AddAsync(Client entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Client> entities, CancellationToken cancellationToken = default)
        {
            _items.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<Client, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().Any(predicate));

        public Task<Client?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default)
        {
            var id = (ClientId)keyValues[0];
            var client = _items.FirstOrDefault(c => c.Id == id);
            return Task.FromResult<Client?>(client);
        }

        public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Client>)_items.ToList());

        public Task<IReadOnlyList<Client>> ListAsync(System.Linq.Expressions.Expression<Func<Client, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<Client>)_items.AsQueryable().Where(predicate).ToList());

        public Task<Client?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<Client, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().Where(predicate).FirstOrDefault());

        public void Remove(Client entity) => _items.Remove(entity);

        public void Update(Client entity)
        {
            var index = _items.FindIndex(item => item.Id == entity.Id);
            if (index >= 0)
            {
                _items[index] = entity;
            }
        }
    }

    private sealed class FakeAccountRepository : IReadRepository<BankAccount>
    {
        private readonly List<BankAccount> _items;

        public FakeAccountRepository(params BankAccount[] items)
        {
            _items = items.ToList();
        }

        public Task<BankAccount?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default)
        {
            var id = (Guid)keyValues[0];
            var account = _items.FirstOrDefault(a => a.Id == id);
            return Task.FromResult<BankAccount?>(account);
        }

        public Task<IReadOnlyList<BankAccount>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<BankAccount>)_items.ToList());

        public Task<IReadOnlyList<BankAccount>> ListAsync(System.Linq.Expressions.Expression<Func<BankAccount, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<BankAccount>)_items.AsQueryable().Where(predicate).ToList());

        public Task<BankAccount?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<BankAccount, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().Where(predicate).FirstOrDefault());

        public Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<BankAccount, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.AsQueryable().Any(predicate));
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
