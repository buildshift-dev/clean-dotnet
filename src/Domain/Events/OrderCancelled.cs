using System;
using Domain.Enums;
using Domain.ValueObjects;
using SharedKernel.Domain.Events;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when an order is cancelled.
    /// </summary>
    public sealed class OrderCancelled : DomainEvent
    {
        public OrderCancelled(OrderId orderId, CustomerId customerId, OrderStatus previousStatus, string reason)
            : base()
        {
            OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            PreviousStatus = previousStatus;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        public OrderId OrderId { get; }
        public CustomerId CustomerId { get; }
        public OrderStatus PreviousStatus { get; }
        public string Reason { get; }
    }
}