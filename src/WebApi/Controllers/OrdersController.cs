using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Commands.CreateOrder;
using Application.Queries.GetCustomerOrders;
using Application.Queries.GetOrder;
using Application.Queries.ListOrders;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Requests;
using WebApi.Models.Responses;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for order operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new order.
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created order</returns>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderResponse>> CreateOrder(
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new CreateOrderCommand
            {
                CustomerId = request.CustomerId,
                TotalAmount = request.TotalAmount,
                Currency = request.Currency,
                Details = request.Details
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetOrder),
                    new { orderId = result.Value.OrderId.Value },
                    OrderResponse.FromDomain(result.Value));
            }

            return BadRequest(result.Error);
        }

        /// <summary>
        /// Get a specific order by ID.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Order details</returns>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderResponse>> GetOrder(
            [FromRoute] Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetOrderQuery { OrderId = orderId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(OrderResponse.FromDomain(result.Value));
            }

            return NotFound(result.Error);
        }

        /// <summary>
        /// List all orders.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of orders</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> ListOrders(
            CancellationToken cancellationToken = default)
        {
            var query = new ListOrdersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value.Select(OrderResponse.FromDomain));
            }

            return BadRequest(result.Error);
        }

        /// <summary>
        /// Get all orders for a specific customer.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer's orders</returns>
        [HttpGet("customer/{customerId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetCustomerOrders(
            [FromRoute] Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCustomerOrdersQuery { CustomerId = customerId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value.Select(OrderResponse.FromDomain));
            }

            return BadRequest(result.Error);
        }
    }
}