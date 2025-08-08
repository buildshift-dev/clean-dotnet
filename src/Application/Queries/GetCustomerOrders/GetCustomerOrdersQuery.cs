using System;
using System.Collections.Generic;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Queries.GetCustomerOrders
{
    /// <summary>
    /// Query to retrieve all orders for a specific customer.
    /// </summary>
    public sealed class GetCustomerOrdersQuery : IRequest<Result<IReadOnlyList<Order>>>
    {
        public Guid CustomerId { get; init; }
    }
}