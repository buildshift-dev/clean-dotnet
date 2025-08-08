using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories
{
    /// <summary>
    /// Repository interface for Customer aggregate.
    /// </summary>
    public interface ICustomerRepository
    {
        Task<Customer?> FindByIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);

        Task<Customer?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default);

        Task<Customer> SaveAsync(Customer customer, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Customer>> ListAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Customer>> SearchAsync(
            string? nameContains = null,
            string? emailContains = null,
            bool? isActive = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default);
    }
}