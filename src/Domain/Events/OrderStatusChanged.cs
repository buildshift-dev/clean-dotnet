using System;
using Domain.Enums;
using Domain.ValueObjects;
using SharedKernel.Domain.Events;

namespace Domain.Events
{
    /// <summary>
    /// Domain event raised when order status changes.
    /// </summary>
    public sealed class OrderStatusChanged : DomainEvent
    {
        public OrderStatusChanged(OrderId orderId, CustomerId customerId, OrderStatus oldStatus, OrderStatus newStatus)
            : base()
        {
            OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }

        public OrderId OrderId { get; }
        public CustomerId CustomerId { get; }
        public OrderStatus OldStatus { get; }
        public OrderStatus NewStatus { get; }
    }
}