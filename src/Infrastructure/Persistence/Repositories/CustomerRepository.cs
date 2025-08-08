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
    /// Entity Framework implementation of ICustomerRepository.
    /// </summary>
    public sealed class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Customer?> FindByIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        }

        public async Task<Customer?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        }

        public async Task<Customer> SaveAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customer.Id, cancellationToken);

            if (existingCustomer == null)
            {
                _context.Customers.Add(customer);
            }
            else
            {
                _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public async Task<IReadOnlyList<Customer>> ListAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Customer>> SearchAsync(
            string? nameContains = null,
            string? emailContains = null,
            bool? isActive = null,
            int limit = 50,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                query = query.Where(c => c.Name.Contains(nameContains));
            }

            if (!string.IsNullOrWhiteSpace(emailContains))
            {
                query = query.Where(c => EF.Property<string>(c, "Email").Contains(emailContains));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await query
                .OrderBy(c => c.Name)
                .Skip(offset)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}