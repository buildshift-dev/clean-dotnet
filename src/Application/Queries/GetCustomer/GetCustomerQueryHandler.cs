using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Queries.GetCustomer
{
    /// <summary>
    /// Handler for GetCustomerQuery.
    /// </summary>
    public sealed class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, Result<Customer>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<GetCustomerQueryHandler> _logger;

        public GetCustomerQueryHandler(
            ICustomerRepository customerRepository,
            ILogger<GetCustomerQueryHandler> logger)
        {
            _customerRepository = customerRepository ?? throw new System.ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Customer>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting customer with ID: {CustomerId}", request.CustomerId);

            var customerId = (CustomerId)request.CustomerId;
            var customer = await _customerRepository.FindByIdAsync(customerId, cancellationToken);

            if (customer is null)
            {
                _logger.LogWarning("Customer not found with ID: {CustomerId}", request.CustomerId);
                return Result<Customer>.Failure($"Customer with ID {request.CustomerId} not found");
            }

            _logger.LogInformation("Successfully retrieved customer with ID: {CustomerId}", request.CustomerId);
            return Result<Customer>.Success(customer);
        }
    }
}