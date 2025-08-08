using System.Collections.Generic;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Queries.ListOrders
{
    /// <summary>
    /// Query to retrieve all orders.
    /// </summary>
    public sealed class ListOrdersQuery : IRequest<Result<IReadOnlyList<Order>>>
    {
    }
}