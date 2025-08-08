using System;
using System.Collections.Generic;
using Domain.Entities;

namespace WebApi.Models.Responses
{
    /// <summary>
    /// Response model for customer data.
    /// </summary>
    public sealed class CustomerResponse
    {
        /// <summary>
        /// Customer ID.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Customer ID (strongly typed).
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Customer name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Customer email address.
        /// </summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// Whether the customer is active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Customer preferences.
        /// </summary>
        public Dictionary<string, object> Preferences { get; init; } = new();

        /// <summary>
        /// When the customer was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// When the customer was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; init; }

        /// <summary>
        /// Create a response from a domain entity.
        /// </summary>
        public static CustomerResponse FromDomain(Customer customer)
        {
            return new CustomerResponse
            {
                Id = customer.Id,
                CustomerId = customer.CustomerId.Value,
                Name = customer.Name,
                Email = customer.Email.Value,
                IsActive = customer.IsActive,
                Preferences = customer.Preferences,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt
            };
        }
    }
}