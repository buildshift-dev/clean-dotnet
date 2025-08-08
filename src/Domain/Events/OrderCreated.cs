using System;
using Domain.ValueObjects;
using SharedKernel.Domain.Events;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when an order is created.
    /// </summary>
    public sealed class OrderCreated : DomainEvent
    {
        public OrderCreated(OrderId orderId, CustomerId customerId, Money totalAmount)
            : base()
        {
            OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            TotalAmount = totalAmount ?? throw new ArgumentNullException(nameof(totalAmount));
        }

        public OrderId OrderId { get; }
        public CustomerId CustomerId { get; }
        public Money TotalAmount { get; }
    }
}