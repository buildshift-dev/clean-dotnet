using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Application.Queries.ListOrders
{
    /// <summary>
    /// Handler for ListOrdersQuery.
    /// </summary>
    public sealed class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, Result<IReadOnlyList<Order>>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<ListOrdersQueryHandler> _logger;

        public ListOrdersQueryHandler(
            IOrderRepository orderRepository,
            ILogger<ListOrdersQueryHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new System.ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IReadOnlyList<Order>>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving all orders");

            var orders = await _orderRepository.ListAllAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {OrderCount} orders", orders.Count);
            return Result<IReadOnlyList<Order>>.Success(orders);
        }
    }
}