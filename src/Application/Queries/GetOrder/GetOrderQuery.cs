using System;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Queries.GetOrder
{
    /// <summary>
    /// Query to retrieve a specific order by ID.
    /// </summary>
    public sealed class GetOrderQuery : IRequest<Result<Order>>
    {
        public Guid OrderId { get; init; }
    }
}