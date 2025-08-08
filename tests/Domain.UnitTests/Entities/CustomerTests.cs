using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Events;
using Domain.ValueObjects;
using FluentAssertions;
using SharedKernel.Domain.Exceptions;
using Xunit;

namespace Domain.UnitTests.Entities
{
    /// <summary>
    /// Unit tests for Customer entity.
    /// </summary>
    public sealed class CustomerTests
    {
        [Fact]
        public void Create_WithValidData_ShouldCreateCustomer()
        {
            // Arrange
            var customerId = CustomerId.New();
            var name = "John Doe";
            var email = new Email("john.doe@example.com");
            var preferences = new Dictionary<string, object> { { "theme", "dark" } };

            // Act
            var customer = Customer.Create(customerId, name, email, preferences: preferences);

            // Assert
            customer.Should().NotBeNull();
            customer.CustomerId.Should().Be(customerId);
            customer.Name.Should().Be(name);
            customer.Email.Should().Be(email);
            customer.IsActive.Should().BeTrue();
            customer.Preferences.Should().BeEquivalentTo(preferences);
            customer.DomainEvents.Should().HaveCount(1);
            customer.DomainEvents.Should().ContainSingle(e => e is CustomerCreated);
        }

        [Fact]
        public void Create_WithEmptyName_ShouldThrowException()
        {
            // Arrange
            var customerId = CustomerId.New();
            var name = "";
            var email = new Email("john.doe@example.com");

            // Act & Assert
            Assert.Throws<BusinessRuleViolationException>(() =>
                Customer.Create(customerId, name, email));
        }

        [Fact]
        public void Deactivate_WhenActive_ShouldDeactivateCustomer()
        {
            // Arrange
            var customer = CreateValidCustomer();
            var reason = "Test deactivation";

            // Act
            customer.Deactivate(reason);

            // Assert
            customer.IsActive.Should().BeFalse();
            customer.DomainEvents.Should().HaveCount(2); // Created + Deactivated
            customer.DomainEvents.Should().Contain(e => e is CustomerDeactivated);
        }

        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldThrowException()
        {
            // Arrange
            var customer = CreateValidCustomer();
            customer.Deactivate("First deactivation");

            // Act & Assert
            Assert.Throws<BusinessRuleViolationException>(() =>
                customer.Deactivate("Second deactivation"));
        }

        [Fact]
        public void UpdateAddress_WithValidAddress_ShouldUpdateAddress()
        {
            // Arrange
            var customer = CreateValidCustomer();
            var address = new Address("123 Main St", "Anytown", "State", "12345", "USA");

            // Act
            customer.UpdateAddress(address);

            // Assert
            customer.Address.Should().Be(address);
        }

        private static Customer CreateValidCustomer()
        {
            var customerId = CustomerId.New();
            var name = "John Doe";
            var email = new Email("john.doe@example.com");

            return Customer.Create(customerId, name, email);
        }
    }
}