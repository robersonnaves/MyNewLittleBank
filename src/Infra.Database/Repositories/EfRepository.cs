using System.Linq.Expressions;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infra.Database.Repositories;

public sealed class EfRepository<TEntity> :
    IReadRepository<TEntity>,
    IWriteRepository<TEntity>,
    ISpecificationRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _set;

    public EfRepository(MyNewLittleBankContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _set = context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _set.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _set.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _set.Remove(entity);
    }

    public async Task<TEntity?> GetByIdAsync(object[] keyValues, CancellationToken cancellationToken = default) =>
        await _set.FindAsync(keyValues, cancellationToken).AsTask().ConfigureAwait(false);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _set.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false)).AsReadOnly();

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        (await _set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false)).AsReadOnly();

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _set.Where(predicate).FirstOrDefaultAsync(cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        (await ApplySpecification(specification).ToListAsync(cancellationToken).ConfigureAwait(false)).AsReadOnly();

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        ApplySpecification(specification).CountAsync(cancellationToken);

    private IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<TEntity> query = _set.AsNoTracking();

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.OrderBy is not null)
        {
            query = specification.OrderBy(query);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = specification.OrderByDescending(query);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
