using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Commands.CreateCustomer
{
    /// <summary>
    /// Handler for creating a new customer.
    /// </summary>
    public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Customer>>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CreateCustomerCommandHandler> _logger;

        public CreateCustomerCommandHandler(
            ICustomerRepository customerRepository,
            ILogger<CreateCustomerCommandHandler> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Customer>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate email format
                if (!Email.TryCreate(request.Email, out var email, out var emailError))
                {
                    return Result<Customer>.Failure(emailError);
                }

                // Check if customer with email already exists
                var existingCustomer = await _customerRepository.FindByEmailAsync(email, cancellationToken);
                if (existingCustomer != null)
                {
                    return Result<Customer>.Failure($"Customer with email {request.Email} already exists");
                }

                // Create new customer using factory method
                var customerId = CustomerId.New();
                var customer = Customer.Create(
                    customerId,
                    request.Name,
                    email,
                    preferences: request.Preferences);

                var savedCustomer = await _customerRepository.SaveAsync(customer, cancellationToken);

                _logger.LogInformation("Created customer {CustomerId} with email {Email}",
                    customerId, request.Email);

                return Result<Customer>.Success(savedCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer with email {Email}", request.Email);
                return Result<Customer>.Failure($"Error creating customer: {ex.Message}");
            }
        }
    }
}