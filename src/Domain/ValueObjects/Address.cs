using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Physical address value object.
    /// </summary>
    public sealed class Address : ValueObject
    {
        public Address(string street, string city, string state, string postalCode, string country, string? apartment = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty", nameof(street));

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty", nameof(city));

            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("State cannot be empty", nameof(state));

            if (string.IsNullOrWhiteSpace(postalCode))
                throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));

            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be empty", nameof(country));

            if (postalCode.Length < 3)
                throw new ArgumentException("Postal code too short", nameof(postalCode));

            Street = street.Trim();
            City = city.Trim();
            State = state.Trim();
            PostalCode = postalCode.Trim();
            Country = country.Trim();
            Apartment = apartment?.Trim();
        }

        public string Street { get; }
        public string City { get; }
        public string State { get; }
        public string PostalCode { get; }
        public string Country { get; }
        public string? Apartment { get; }

        public string FullAddress
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(Street);

                if (!string.IsNullOrEmpty(Apartment))
                    sb.AppendLine($"Apt {Apartment}");

                sb.AppendLine($"{City}, {State} {PostalCode}");
                sb.AppendLine(Country);

                return sb.ToString().TrimEnd();
            }
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return PostalCode;
            yield return Country;
            yield return Apartment;
        }

        public override string ToString()
        {
            return FullAddress;
        }
    }
}