using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Commands.CreateOrder
{
    /// <summary>
    /// Handler for creating a new order.
    /// </summary>
    public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Order>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Order>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify customer exists
                var customerId = new CustomerId(request.CustomerId);
                var customer = await _customerRepository.FindByIdAsync(customerId, cancellationToken);

                if (customer == null)
                {
                    return Result<Order>.Failure($"Customer with ID {request.CustomerId} not found");
                }

                if (!customer.IsActive)
                {
                    return Result<Order>.Failure("Cannot create order for inactive customer");
                }

                // Create money value object
                if (!Money.TryCreate(request.TotalAmount, request.Currency, out var totalAmount, out var moneyError))
                {
                    return Result<Order>.Failure(moneyError);
                }

                // Create new order using factory method
                var orderId = OrderId.New();
                var order = Order.Create(
                    orderId,
                    customerId,
                    totalAmount,
                    request.Details);

                var savedOrder = await _orderRepository.SaveAsync(order, cancellationToken);

                _logger.LogInformation("Created order {OrderId} for customer {CustomerId}",
                    orderId, customerId);

                return Result<Order>.Success(savedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for customer {CustomerId}", request.CustomerId);
                return Result<Order>.Failure($"Error creating order: {ex.Message}");
            }
        }
    }
}