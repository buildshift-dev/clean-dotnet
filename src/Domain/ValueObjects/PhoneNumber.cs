using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Phone number value object with validation.
    /// </summary>
    public sealed class PhoneNumber : ValueObject
    {
        private static readonly Regex DigitsOnlyRegex = new(@"\D", RegexOptions.Compiled);

        public PhoneNumber(string value, string countryCode = "+1")
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number cannot be empty", nameof(value));

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new ArgumentException("Country code cannot be empty", nameof(countryCode));

            if (!countryCode.StartsWith("+"))
                throw new ArgumentException("Country code must start with +", nameof(countryCode));

            var countryDigits = DigitsOnlyRegex.Replace(countryCode, "");
            if (string.IsNullOrEmpty(countryDigits))
                throw new ArgumentException("Country code must contain digits", nameof(countryCode));

            var digitsOnly = DigitsOnlyRegex.Replace(value, "");
            if (digitsOnly.Length < 10)
                throw new ArgumentException("Phone number must have at least 10 digits", nameof(value));

            if (digitsOnly.Length > 15)
                throw new ArgumentException("Phone number cannot have more than 15 digits", nameof(value));

            Value = value.Trim();
            CountryCode = countryCode.Trim();
        }

        public string Value { get; }
        public string CountryCode { get; }

        public string Formatted
        {
            get
            {
                var digitsOnly = DigitsOnlyRegex.Replace(Value, "");

                if (digitsOnly.Length == 10 && CountryCode == "+1")
                {
                    // US format: (xxx) xxx-xxxx
                    return $"({digitsOnly.Substring(0, 3)}) {digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6)}";
                }

                // International format with country code
                return $"{CountryCode} {Value}";
            }
        }

        public string DigitsOnly => DigitsOnlyRegex.Replace(Value, "");

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
            yield return CountryCode;
        }

        public override string ToString()
        {
            return Formatted;
        }
    }
}