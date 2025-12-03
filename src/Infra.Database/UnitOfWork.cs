using Domain.Interfaces;

namespace Infra.Database;

public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly MyNewLittleBankContext _context;

    public UnitOfWork(MyNewLittleBankContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
