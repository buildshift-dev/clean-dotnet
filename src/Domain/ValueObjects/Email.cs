using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Email address value object with validation.
    /// </summary>
    public sealed class Email : ValueObject
    {
        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(value));

            if (!EmailRegex.IsMatch(value))
                throw new ArgumentException($"Invalid email format: {value}", nameof(value));

            Value = value.ToLowerInvariant();
        }

        public string Value { get; }

        public string Domain => Value.Split('@')[1];

        public string LocalPart => Value.Split('@')[0];

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(Email email) => email.Value;

        public static bool TryCreate(string value, out Email email, out string error)
        {
            email = null!;
            error = string.Empty;

            try
            {
                email = new Email(value);
                return true;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}