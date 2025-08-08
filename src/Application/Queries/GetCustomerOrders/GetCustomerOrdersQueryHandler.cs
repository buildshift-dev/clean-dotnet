using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Queries.GetCustomerOrders
{
    /// <summary>
    /// Handler for getting customer orders.
    /// </summary>
    public sealed class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<IReadOnlyList<Order>>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetCustomerOrdersQueryHandler> _logger;

        public GetCustomerOrdersQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetCustomerOrdersQueryHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IReadOnlyList<Order>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.CustomerId == Guid.Empty)
                {
                    return Result<IReadOnlyList<Order>>.Failure("Customer ID cannot be empty");
                }

                var customerId = new CustomerId(request.CustomerId);
                var orders = await _orderRepository.FindByCustomerAsync(customerId, cancellationToken);

                _logger.LogInformation("Retrieved {Count} orders for customer {CustomerId}",
                    orders.Count, customerId);

                return Result<IReadOnlyList<Order>>.Success(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for customer {CustomerId}", request.CustomerId);
                return Result<IReadOnlyList<Order>>.Failure($"Error retrieving customer orders: {ex.Message}");
            }
        }
    }
}