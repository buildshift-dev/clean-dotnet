using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;
using FluentAssertions;
using SharedKernel.Domain.Exceptions;
using Xunit;

namespace Domain.UnitTests.Entities
{
    public class OrderTests
    {
        private readonly OrderId _orderId = OrderId.New();
        private readonly CustomerId _customerId = CustomerId.New();
        private readonly Money _validAmount = new Money(100.00m, "USD");

        [Fact]
        public void Create_Should_CreateNewOrder_When_ValidDataProvided()
        {
            // Arrange
            var details = new Dictionary<string, object> { { "item", "Test Product" } };

            // Act
            var order = Order.Create(_orderId, _customerId, _validAmount, details);

            // Assert
            order.Should().NotBeNull();
            order.OrderId.Should().Be(_orderId);
            order.CustomerId.Should().Be(_customerId);
            order.TotalAmount.Should().Be(_validAmount);
            order.Status.Should().Be(OrderStatus.Pending);
            order.Details.Should().ContainKey("item");
            order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Create_Should_RaiseOrderCreatedEvent_When_OrderCreated()
        {
            // Act
            var order = Order.Create(_orderId, _customerId, _validAmount);

            // Assert
            order.DomainEvents.Should().HaveCount(1);
            var domainEvent = order.DomainEvents.First();
            domainEvent.Should().BeOfType<OrderCreated>();

            var orderCreatedEvent = domainEvent as OrderCreated;
            orderCreatedEvent!.OrderId.Should().Be(_orderId);
            orderCreatedEvent.CustomerId.Should().Be(_customerId);
            orderCreatedEvent.TotalAmount.Should().Be(_validAmount);
        }

        [Fact]
        public void Create_Should_ThrowException_When_TotalAmountIsZero()
        {
            // Arrange
            var zeroAmount = new Money(0m, "USD");

            // Act & Assert
            var action = () => Order.Create(_orderId, _customerId, zeroAmount);

            action.Should().Throw<BusinessRuleViolationException>()
                .WithMessage("Order total amount must be greater than zero")
                .And.RuleName.Should().Be("MinimumOrderAmount");
        }

        [Fact]
        public void Create_Should_ThrowException_When_TotalAmountIsNegative()
        {
            // Act & Assert
            // Money constructor itself throws for negative amounts, so we test that scenario
            var action = () => new Money(-50m, "USD");

            action.Should().Throw<ArgumentException>()
                .WithMessage("Amount cannot be negative*");
        }

        [Theory]
        [InlineData(OrderStatus.Pending, true)]
        [InlineData(OrderStatus.Confirmed, true)]
        [InlineData(OrderStatus.Shipped, false)]
        [InlineData(OrderStatus.Delivered, false)]
        [InlineData(OrderStatus.Cancelled, false)]
        public void CanBeCancelled_Should_ReturnCorrectValue_BasedOnStatus(OrderStatus status, bool expectedResult)
        {
            // Arrange
            var order = CreateOrderWithStatus(status);

            // Act
            var canBeCancelled = order.CanBeCancelled();

            // Assert
            canBeCancelled.Should().Be(expectedResult);
        }

        [Fact]
        public void Cancel_Should_ChangeStatusToCancelled_When_OrderIsPending()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            var reason = "Customer changed mind";

            // Act
            order.Cancel(reason);

            // Assert
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Cancel_Should_RaiseOrderCancelledEvent_When_OrderCancelled()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.ClearDomainEvents(); // Clear the creation event
            var reason = "Out of stock";

            // Act
            order.Cancel(reason);

            // Assert
            order.DomainEvents.Should().HaveCount(1);
            var domainEvent = order.DomainEvents.First();
            domainEvent.Should().BeOfType<OrderCancelled>();

            var cancelledEvent = domainEvent as OrderCancelled;
            cancelledEvent!.OrderId.Should().Be(_orderId);
            cancelledEvent.CustomerId.Should().Be(_customerId);
            cancelledEvent.PreviousStatus.Should().Be(OrderStatus.Pending);
            cancelledEvent.Reason.Should().Be(reason);
        }

        [Fact]
        public void Cancel_Should_UseDefaultReason_When_NoReasonProvided()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.ClearDomainEvents();

            // Act
            order.Cancel();

            // Assert
            var cancelledEvent = order.DomainEvents.First() as OrderCancelled;
            cancelledEvent!.Reason.Should().Be("Customer request");
        }

        [Theory]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public void Cancel_Should_ThrowException_When_OrderCannotBeCancelled(OrderStatus status)
        {
            // Arrange
            var order = CreateOrderWithStatus(status);

            // Act & Assert
            var action = () => order.Cancel();

            action.Should().Throw<BusinessRuleViolationException>()
                .WithMessage($"Order in {status} status cannot be cancelled")
                .And.RuleName.Should().Be("OrderCancellationRule");
        }

        [Fact]
        public void Confirm_Should_ChangeStatusToConfirmed_When_OrderIsPending()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.ClearDomainEvents();

            // Act
            order.Confirm();

            // Assert
            order.Status.Should().Be(OrderStatus.Confirmed);
            order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Confirm_Should_RaiseOrderStatusChangedEvent_When_StatusChanged()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.ClearDomainEvents();

            // Act
            order.Confirm();

            // Assert
            order.DomainEvents.Should().HaveCount(1);
            var statusChangedEvent = order.DomainEvents.First() as OrderStatusChanged;
            statusChangedEvent!.OrderId.Should().Be(_orderId);
            statusChangedEvent.CustomerId.Should().Be(_customerId);
            statusChangedEvent.OldStatus.Should().Be(OrderStatus.Pending);
            statusChangedEvent.NewStatus.Should().Be(OrderStatus.Confirmed);
        }

        [Theory]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public void Confirm_Should_ThrowException_When_OrderIsNotPending(OrderStatus status)
        {
            // Arrange
            var order = CreateOrderWithStatus(status);

            // Act & Assert
            var action = () => order.Confirm();

            action.Should().Throw<BusinessRuleViolationException>()
                .WithMessage($"Only pending orders can be confirmed. Current status: {status}")
                .And.RuleName.Should().Be("OrderConfirmationRule");
        }

        [Fact]
        public void Ship_Should_ChangeStatusToShipped_When_OrderIsConfirmed()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.Confirm();
            order.ClearDomainEvents();

            // Act
            order.Ship();

            // Assert
            order.Status.Should().Be(OrderStatus.Shipped);
            order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Ship_Should_RaiseOrderStatusChangedEvent_When_Shipped()
        {
            // Arrange
            var order = CreateOrderWithStatus(OrderStatus.Confirmed);
            order.ClearDomainEvents();

            // Act
            order.Ship();

            // Assert
            var statusChangedEvent = order.DomainEvents.First() as OrderStatusChanged;
            statusChangedEvent!.OldStatus.Should().Be(OrderStatus.Confirmed);
            statusChangedEvent.NewStatus.Should().Be(OrderStatus.Shipped);
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public void Ship_Should_ThrowException_When_OrderIsNotConfirmed(OrderStatus status)
        {
            // Arrange
            var order = CreateOrderWithStatus(status);

            // Act & Assert
            var action = () => order.Ship();

            action.Should().Throw<BusinessRuleViolationException>()
                .WithMessage($"Only confirmed orders can be shipped. Current status: {status}")
                .And.RuleName.Should().Be("OrderShippingRule");
        }

        [Fact]
        public void Deliver_Should_ChangeStatusToDelivered_When_OrderIsShipped()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.Confirm();
            order.Ship();
            order.ClearDomainEvents();

            // Act
            order.Deliver();

            // Assert
            order.Status.Should().Be(OrderStatus.Delivered);
            order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deliver_Should_RaiseOrderStatusChangedEvent_When_Delivered()
        {
            // Arrange
            var order = CreateOrderWithStatus(OrderStatus.Shipped);
            order.ClearDomainEvents();

            // Act
            order.Deliver();

            // Assert
            var statusChangedEvent = order.DomainEvents.First() as OrderStatusChanged;
            statusChangedEvent!.OldStatus.Should().Be(OrderStatus.Shipped);
            statusChangedEvent.NewStatus.Should().Be(OrderStatus.Delivered);
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.Cancelled)]
        public void Deliver_Should_ThrowException_When_OrderIsNotShipped(OrderStatus status)
        {
            // Arrange
            var order = CreateOrderWithStatus(status);

            // Act & Assert
            var action = () => order.Deliver();

            action.Should().Throw<BusinessRuleViolationException>()
                .WithMessage($"Only shipped orders can be delivered. Current status: {status}")
                .And.RuleName.Should().Be("OrderDeliveryRule");
        }

        [Fact]
        public void OrderLifecycle_Should_TransitionCorrectly_ThroughAllStates()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);

            // Act & Assert - Complete lifecycle
            order.Status.Should().Be(OrderStatus.Pending);

            order.Confirm();
            order.Status.Should().Be(OrderStatus.Confirmed);

            order.Ship();
            order.Status.Should().Be(OrderStatus.Shipped);

            order.Deliver();
            order.Status.Should().Be(OrderStatus.Delivered);

            // Verify events were raised
            order.DomainEvents.Should().HaveCount(4); // Create + 3 status changes
        }

        [Fact]
        public void OrderCancellation_Should_WorkFromPendingState()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);

            // Act
            order.Cancel("Test cancellation");

            // Assert
            order.Status.Should().Be(OrderStatus.Cancelled);
            order.CanBeCancelled().Should().BeFalse();
        }

        [Fact]
        public void OrderCancellation_Should_WorkFromConfirmedState()
        {
            // Arrange
            var order = Order.Create(_orderId, _customerId, _validAmount);
            order.Confirm();

            // Act
            order.Cancel("Inventory issue");

            // Assert
            order.Status.Should().Be(OrderStatus.Cancelled);
        }

        private Order CreateOrderWithStatus(OrderStatus targetStatus)
        {
            var order = Order.Create(_orderId, _customerId, _validAmount);

            switch (targetStatus)
            {
                case OrderStatus.Confirmed:
                    order.Confirm();
                    break;
                case OrderStatus.Shipped:
                    order.Confirm();
                    order.Ship();
                    break;
                case OrderStatus.Delivered:
                    order.Confirm();
                    order.Ship();
                    order.Deliver();
                    break;
                case OrderStatus.Cancelled:
                    order.Cancel();
                    break;
                case OrderStatus.Pending:
                default:
                    // Already pending
                    break;
            }

            return order;
        }
    }
}