using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Commands.CreateCustomer;
using Application.Queries.GetCustomer;
using Application.Queries.ListCustomers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Models.Requests;
using WebApi.Models.Responses;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for customer operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(IMediator mediator, ILogger<CustomersController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a new customer.
        /// </summary>
        /// <param name="request">Customer creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created customer</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer(
            [FromBody] CreateCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new CreateCustomerCommand
            {
                Name = request.Name,
                Email = request.Email,
                Preferences = request.Preferences
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetCustomer),
                    new { customerId = result.Value.CustomerId.Value },
                    CustomerResponse.FromDomain(result.Value));
            }

            return BadRequest(result.Error);
        }

        /// <summary>
        /// Get a specific customer by ID.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer details</returns>
        [HttpGet("{customerId:guid}")]
        [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CustomerResponse>> GetCustomer(
            [FromRoute] Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCustomerQuery { CustomerId = customerId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(CustomerResponse.FromDomain(result.Value));
            }

            return NotFound(result.Error);
        }

        /// <summary>
        /// List all customers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of customers</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CustomerResponse>>> ListCustomers(
            CancellationToken cancellationToken = default)
        {
            var query = new ListCustomersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value.Select(CustomerResponse.FromDomain));
            }

            return BadRequest(result.Error);
        }

        /// <summary>
        /// Search customers with filters.
        /// </summary>
        /// <param name="nameContains">Filter by name containing this text</param>
        /// <param name="emailContains">Filter by email containing this text</param>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="offset">Number of results to skip</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Filtered list of customers</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<ActionResult<IEnumerable<CustomerResponse>>> SearchCustomers(
            [FromQuery] string? nameContains = null,
            [FromQuery] string? emailContains = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0,
            CancellationToken cancellationToken = default)
        {
            // This would typically use a SearchCustomersQuery
            // For now, returning NotImplemented as we haven't created that query yet
            return Task.FromResult<ActionResult<IEnumerable<CustomerResponse>>>(
                StatusCode(StatusCodes.Status501NotImplemented, "Search customers endpoint not implemented yet"));
        }
    }
}