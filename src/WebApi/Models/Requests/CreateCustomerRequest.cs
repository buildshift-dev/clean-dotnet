using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
{
    /// <summary>
    /// Request model for creating a customer.
    /// </summary>
    public sealed class CreateCustomerRequest
    {
        /// <summary>
        /// Customer name.
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Customer email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(320)]
        public string Email { get; init; } = string.Empty;

        /// <summary>
        /// Customer preferences as key-value pairs.
        /// </summary>
        public Dictionary<string, object>? Preferences { get; init; }
    }
}