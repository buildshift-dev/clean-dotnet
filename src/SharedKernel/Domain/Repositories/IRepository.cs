using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using SharedKernel.Domain.BaseTypes;

namespace SharedKernel.Domain.Repositories
{
    /// <summary>
    /// Generic repository interface for aggregate roots.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TId">The aggregate ID type</typeparam>
    public interface IRepository<TAggregate, TId>
        where TAggregate : AggregateRoot
        where TId : notnull
    {
        Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TAggregate>> FindAsync(Expression<Func<TAggregate, bool>> predicate, CancellationToken cancellationToken = default);
        Task<TAggregate> AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
        Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
        Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    }
}