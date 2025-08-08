using System;
using System.Collections.Generic;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Strongly typed order identifier.
    /// </summary>
    public sealed class OrderId : ValueObject
    {
        public OrderId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty", nameof(value));

            Value = value;
        }

        public Guid Value { get; }

        public static OrderId New() => new(Guid.NewGuid());

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator Guid(OrderId orderId) => orderId.Value;
        public static explicit operator OrderId(Guid guid) => new(guid);
    }
}