using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    /// <summary>
    /// Request model for creating an order.
    /// </summary>
    public sealed class CreateOrderRequest
    {
        /// <summary>
        /// ID of the customer placing the order.
        /// </summary>
        [Required]
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Total amount of the order.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero")]
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Currency code (ISO 4217).
        /// </summary>
        [Required]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter uppercase code")]
        public string Currency { get; init; } = "USD";

        /// <summary>
        /// Order details as key-value pairs.
        /// </summary>
        public Dictionary<string, object>? Details { get; init; }
    }
}