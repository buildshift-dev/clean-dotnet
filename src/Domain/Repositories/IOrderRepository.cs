using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories
{
    /// <summary>
    /// Repository interface for Order aggregate.
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order?> FindByIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

        Task<Order> SaveAsync(Order order, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> ListAllAsync(CancellationToken cancellationToken = default);
    }
}