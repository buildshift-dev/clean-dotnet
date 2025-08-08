using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Queries.ListCustomers
{
    /// <summary>
    /// Handler for ListCustomersQuery.
    /// </summary>
    public sealed class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, Result<IReadOnlyList<Customer>>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<ListCustomersQueryHandler> _logger;

        public ListCustomersQueryHandler(
            ICustomerRepository customerRepository,
            ILogger<ListCustomersQueryHandler> logger)
        {
            _customerRepository = customerRepository ?? throw new System.ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IReadOnlyList<Customer>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving all customers");

            var customers = await _customerRepository.ListAllAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {CustomerCount} customers", customers.Count);
            return Result<IReadOnlyList<Customer>>.Success(customers);
        }
    }
}