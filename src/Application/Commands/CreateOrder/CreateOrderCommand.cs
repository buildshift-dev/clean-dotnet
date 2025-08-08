using System;
using System.Collections.Generic;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Commands.CreateOrder
{
    /// <summary>
    /// Command to create a new order.
    /// </summary>
    public sealed class CreateOrderCommand : IRequest<Result<Order>>
    {
        public Guid CustomerId { get; init; }
        public decimal TotalAmount { get; init; }
        public string Currency { get; init; } = "USD";
        public Dictionary<string, object>? Details { get; init; }
    }
}