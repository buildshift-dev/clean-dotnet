using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IOrderRepository.
    /// </summary>
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Order?> FindByIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
        }

        public async Task<Order> SaveAsync(Order order, CancellationToken cancellationToken = default)
        {
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

            if (existingOrder == null)
            {
                _context.Orders.Add(order);
            }
            else
            {
                _context.Entry(existingOrder).CurrentValues.SetValues(order);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return order;
        }

        public async Task<IReadOnlyList<Order>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Order>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}