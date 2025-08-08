using System;
using Domain.ValueObjects;
using SharedKernel.Domain.Events;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when a customer is deactivated.
    /// </summary>
    public sealed class CustomerDeactivated : DomainEvent
    {
        public CustomerDeactivated(CustomerId customerId, string reason)
            : base()
        {
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        public CustomerId CustomerId { get; }
        public string Reason { get; }
    }
}