using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.Domain.BaseTypes
{
    /// <summary>
    /// Base class for value objects.
    /// Provides value equality based on the object's components.
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// Gets the components used for equality comparison.
        /// </summary>
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public bool Equals(ValueObject? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj)
        {
            return obj is ValueObject other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Where(x => x != null)
                .Aggregate(1, (current, obj) => current * 23 + obj!.GetHashCode());
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !Equals(left, right);
        }
    }
}