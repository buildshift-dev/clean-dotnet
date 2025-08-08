using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SharedKernel.Domain.BaseTypes;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Money value object with currency and amount.
    /// </summary>
    public sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be empty", nameof(currency));

            if (currency.Length != 3 || !IsValidCurrencyCode(currency))
                throw new ArgumentException($"Invalid currency code: {currency}", nameof(currency));

            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }

        public decimal Amount { get; }
        public string Currency { get; }

        private static bool IsValidCurrencyCode(string currency)
        {
            return currency.Length == 3 && currency.All(char.IsLetter);
        }

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

            var resultAmount = Amount - other.Amount;
            if (resultAmount < 0)
                throw new InvalidOperationException("Subtraction would result in negative amount");

            return new Money(resultAmount, Currency);
        }

        public Money Multiply(decimal factor)
        {
            if (factor < 0)
                throw new ArgumentException("Factor cannot be negative", nameof(factor));

            return new Money(Amount * factor, Currency);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString()
        {
            return $"{Amount:F2} {Currency}";
        }

        public static bool TryCreate(decimal amount, string currency, out Money money, out string error)
        {
            money = null!;
            error = string.Empty;

            try
            {
                money = new Money(amount, currency);
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