using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Events;
using Domain.ValueObjects;
using SharedKernel.Domain.BaseTypes;
using SharedKernel.Domain.Exceptions;

namespace Domain.Entities
{
    /// <summary>
    /// Customer aggregate root with enhanced value objects and domain events.
    /// </summary>
    public sealed class Customer : AggregateRoot
    {
        private Customer() : base() { }

        private Customer(
            CustomerId customerId,
            string name,
            Email email,
            Address? address = null,
            PhoneNumber? phoneNumber = null,
            bool isActive = true,
            Dictionary<string, object>? preferences = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
            : base(customerId.Value)
        {
            CustomerId = customerId;
            Name = name;
            Email = email;
            Address = address;
            PhoneNumber = phoneNumber;
            IsActive = isActive;
            Preferences = preferences ?? new Dictionary<string, object>();
            CreatedAt = createdAt ?? DateTime.UtcNow;
            UpdatedAt = updatedAt ?? DateTime.UtcNow;

            ValidateBusinessRules();
        }

        public CustomerId CustomerId { get; private set; } = null!;
        public string Name { get; private set; } = string.Empty;
        public Email Email { get; private set; } = null!;
        public Address? Address { get; private set; }
        public PhoneNumber? PhoneNumber { get; private set; }
        public bool IsActive { get; private set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> Preferences { get; private set; } = new();

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Factory method to create a new customer with domain event.
        /// </summary>
        public static Customer Create(
            CustomerId customerId,
            string name,
            Email email,
            Address? address = null,
            PhoneNumber? phoneNumber = null,
            Dictionary<string, object>? preferences = null)
        {
            var customer = new Customer(customerId, name, email, address, phoneNumber, true, preferences);

            customer.AddDomainEvent(new CustomerCreated(customerId, name, email.Value));

            return customer;
        }

        /// <summary>
        /// Deactivate the customer and raise domain event.
        /// </summary>
        public void Deactivate(string reason = "Manual deactivation")
        {
            if (!IsActive)
                throw new BusinessRuleViolationException("Customer is already deactivated");

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new CustomerDeactivated(CustomerId, reason));
        }

        /// <summary>
        /// Update customer address.
        /// </summary>
        public void UpdateAddress(Address address)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Update customer phone number.
        /// </summary>
        public void UpdatePhoneNumber(PhoneNumber phoneNumber)
        {
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Update customer preferences.
        /// </summary>
        public void UpdatePreferences(Dictionary<string, object> preferences)
        {
            Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateBusinessRules()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new BusinessRuleViolationException("Customer name cannot be empty", "CustomerNameRequired");
        }
    }
}