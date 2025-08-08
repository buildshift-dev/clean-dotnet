using System;
using System.Collections.Generic;
using Domain.Entities;

namespace WebApi.Models.Responses
{
    /// <summary>
    /// Response model for order data.
    /// </summary>
    public sealed class OrderResponse
    {
        /// <summary>
        /// Order ID.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Order ID (strongly typed).
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Customer ID.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Total amount of the order.
        /// </summary>
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; init; } = string.Empty;

        /// <summary>
        /// Order status.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Order details.
        /// </summary>
        public Dictionary<string, object> Details { get; init; } = new();

        /// <summary>
        /// When the order was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// When the order was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; init; }

        /// <summary>
        /// Create a response from a domain entity.
        /// </summary>
        public static OrderResponse FromDomain(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                OrderId = order.OrderId.Value,
                CustomerId = order.CustomerId.Value,
                TotalAmount = order.TotalAmount.Amount,
                Currency = order.TotalAmount.Currency,
                Status = order.Status.ToString(),
                Details = order.Details,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}