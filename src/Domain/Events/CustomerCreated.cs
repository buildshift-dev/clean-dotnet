using System;
using Domain.ValueObjects;
using SharedKernel.Domain.Events;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when a customer is created.
    /// </summary>
    public sealed class CustomerCreated : DomainEvent
    {
        public CustomerCreated(CustomerId customerId, string customerName, string customerEmail)
            : base()
        {
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            CustomerName = customerName ?? throw new ArgumentNullException(nameof(customerName));
            CustomerEmail = customerEmail ?? throw new ArgumentNullException(nameof(customerEmail));
        }

        public CustomerId CustomerId { get; }
        public string CustomerName { get; }
        public string CustomerEmail { get; }
    }
}