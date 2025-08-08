using System;
using System.Collections.Generic;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Strongly typed customer identifier.
    /// </summary>
    public sealed class CustomerId : ValueObject
    {
        public CustomerId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Customer ID cannot be empty", nameof(value));

            Value = value;
        }

        public Guid Value { get; }

        public static CustomerId New() => new(Guid.NewGuid());

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Guid(CustomerId customerId) => customerId.Value;
        public static explicit operator CustomerId(Guid guid) => new(guid);
    }
}