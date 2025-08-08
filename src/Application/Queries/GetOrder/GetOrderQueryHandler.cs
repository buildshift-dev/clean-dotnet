using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Queries.GetOrder
{
    /// <summary>
    /// Handler for GetOrderQuery.
    /// </summary>
    public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<Order>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetOrderQueryHandler> _logger;

        public GetOrderQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetOrderQueryHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new System.ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Order>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting order with ID: {OrderId}", request.OrderId);

            var orderId = (OrderId)request.OrderId;
            var order = await _orderRepository.FindByIdAsync(orderId, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Order not found with ID: {OrderId}", request.OrderId);
                return Result<Order>.Failure($"Order with ID {request.OrderId} not found");
            }

            _logger.LogInformation("Successfully retrieved order with ID: {OrderId}", request.OrderId);
            return Result<Order>.Success(order);
        }
    }
}