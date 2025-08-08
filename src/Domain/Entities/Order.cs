using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;
using SharedKernel.Domain.BaseTypes;
using SharedKernel.Domain.Exceptions;

namespace Domain.Entities
{
    /// <summary>
    /// Order aggregate root with enhanced value objects and domain events.
    /// </summary>
    public sealed class Order : AggregateRoot
    {
        private Order() : base() { }

        private Order(
            OrderId orderId,
            CustomerId customerId,
            Money totalAmount,
            OrderStatus status = OrderStatus.Pending,
            Dictionary<string, object>? details = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
            : base(orderId.Value)
        {
            OrderId = orderId;
            CustomerId = customerId;
            TotalAmount = totalAmount;
            Status = status;
            Details = details ?? new Dictionary<string, object>();
            CreatedAt = createdAt ?? DateTime.UtcNow;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;

            ValidateBusinessRules();
        }

        public OrderId OrderId { get; private set; } = null!;
        public CustomerId CustomerId { get; private set; } = null!;
        public Money TotalAmount { get; private set; } = null!;
        public OrderStatus Status { get; private set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> Details { get; private set; } = new();

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Factory method to create a new order with domain event.
        /// </summary>
        public static Order Create(
            OrderId orderId,
            CustomerId customerId,
            Money totalAmount,
            Dictionary<string, object>? details = null)
        {
            var order = new Order(orderId, customerId, totalAmount, OrderStatus.Pending, details);

            order.AddDomainEvent(new OrderCreated(orderId, customerId, totalAmount));

            return order;
        }

        /// <summary>
        /// Check if order can be cancelled based on business rules.
        /// </summary>
        public bool CanBeCancelled()
        {
            return Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;
        }

        /// <summary>
        /// Cancel the order and raise domain event.
        /// </summary>
        public void Cancel(string reason = "Customer request")
        {
            if (!CanBeCancelled())
                throw new BusinessRuleViolationException(
                    $"Order in {Status} status cannot be cancelled",
                    "OrderCancellationRule");

            var previousStatus = Status;
            Status = OrderStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new OrderCancelled(OrderId, CustomerId, previousStatus, reason));
        }

        /// <summary>
        /// Confirm a pending order.
        /// </summary>
        public void Confirm()
        {
            if (Status != OrderStatus.Pending)
                throw new BusinessRuleViolationException(
                    $"Only pending orders can be confirmed. Current status: {Status}",
                    "OrderConfirmationRule");

            ChangeStatus(OrderStatus.Confirmed);
        }

        /// <summary>
        /// Ship a confirmed order.
        /// </summary>
        public void Ship()
        {
            if (Status != OrderStatus.Confirmed)
                throw new BusinessRuleViolationException(
                    $"Only confirmed orders can be shipped. Current status: {Status}",
                    "OrderShippingRule");

            ChangeStatus(OrderStatus.Shipped);
        }

        /// <summary>
        /// Mark a shipped order as delivered.
        /// </summary>
        public void Deliver()
        {
            if (Status != OrderStatus.Shipped)
                throw new BusinessRuleViolationException(
                    $"Only shipped orders can be delivered. Current status: {Status}",
                    "OrderDeliveryRule");

            ChangeStatus(OrderStatus.Delivered);
        }

        private void ChangeStatus(OrderStatus newStatus)
        {
            var oldStatus = Status;
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new OrderStatusChanged(OrderId, CustomerId, oldStatus, newStatus));
        }

        private void ValidateBusinessRules()
        {
            if (TotalAmount.Amount <= 0)
                throw new BusinessRuleViolationException(
                    "Order total amount must be greater than zero",
                    "MinimumOrderAmount");
        }
    }
}