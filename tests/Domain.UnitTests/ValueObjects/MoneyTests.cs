using System;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests.ValueObjects
{
    public class MoneyTests
    {
        [Fact]
        public void Constructor_Should_CreateMoney_When_ValidAmountAndCurrency()
        {
            // Arrange & Act
            var money = new Money(100.50m, "USD");

            // Assert
            money.Amount.Should().Be(100.50m);
            money.Currency.Should().Be("USD");
        }

        [Fact]
        public void Constructor_Should_ConvertCurrencyToUpperCase_When_LowerCaseProvided()
        {
            // Arrange & Act
            var money = new Money(50m, "eur");

            // Assert
            money.Currency.Should().Be("EUR");
        }

        [Fact]
        public void Constructor_Should_AcceptZeroAmount()
        {
            // Arrange & Act
            var money = new Money(0m, "GBP");

            // Assert
            money.Amount.Should().Be(0m);
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_AmountIsNegative()
        {
            // Act & Assert
            var action = () => new Money(-10m, "USD");

            action.Should().Throw<ArgumentException>()
                .WithMessage("Amount cannot be negative*")
                .WithParameterName("amount");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_Should_ThrowException_When_CurrencyIsNullOrWhitespace(string currency)
        {
            // Act & Assert
            var action = () => new Money(100m, currency);

            action.Should().Throw<ArgumentException>()
                .WithMessage("Currency cannot be empty*")
                .WithParameterName("currency");
        }

        [Theory]
        [InlineData("US")]
        [InlineData("USDD")]
        [InlineData("1234")]
        [InlineData("AB")]
        public void Constructor_Should_ThrowException_When_CurrencyCodeIsInvalid(string currency)
        {
            // Act & Assert
            var action = () => new Money(100m, currency);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid currency code: {currency}*")
                .WithParameterName("currency");
        }

        [Fact]
        public void Add_Should_ReturnSumOfAmounts_When_SameCurrency()
        {
            // Arrange
            var money1 = new Money(100.50m, "USD");
            var money2 = new Money(50.25m, "USD");

            // Act
            var result = money1.Add(money2);

            // Assert
            result.Amount.Should().Be(150.75m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Add_Should_ThrowException_When_DifferentCurrencies()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(50m, "EUR");

            // Act & Assert
            var action = () => money1.Add(money2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot add different currencies: USD and EUR");
        }

        [Fact]
        public void Add_Should_HandleAddingZero()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(0m, "USD");

            // Act
            var result = money1.Add(money2);

            // Assert
            result.Amount.Should().Be(100m);
        }

        [Fact]
        public void Subtract_Should_ReturnDifference_When_SameCurrencyAndSufficientAmount()
        {
            // Arrange
            var money1 = new Money(100.75m, "USD");
            var money2 = new Money(30.25m, "USD");

            // Act
            var result = money1.Subtract(money2);

            // Assert
            result.Amount.Should().Be(70.50m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Subtract_Should_ReturnZero_When_SubtractingSameAmount()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(100m, "USD");

            // Act
            var result = money1.Subtract(money2);

            // Assert
            result.Amount.Should().Be(0m);
        }

        [Fact]
        public void Subtract_Should_ThrowException_When_DifferentCurrencies()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(50m, "GBP");

            // Act & Assert
            var action = () => money1.Subtract(money2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot subtract different currencies: USD and GBP");
        }

        [Fact]
        public void Subtract_Should_ThrowException_When_ResultWouldBeNegative()
        {
            // Arrange
            var money1 = new Money(50m, "USD");
            var money2 = new Money(100m, "USD");

            // Act & Assert
            var action = () => money1.Subtract(money2);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Subtraction would result in negative amount");
        }

        [Theory]
        [InlineData(100, 2, 200)]
        [InlineData(50.5, 3, 151.5)]
        [InlineData(100, 0.5, 50)]
        [InlineData(100, 0, 0)]
        [InlineData(33.33, 3, 99.99)]
        public void Multiply_Should_ReturnCorrectAmount_When_ValidFactor(decimal amount, decimal factor, decimal expected)
        {
            // Arrange
            var money = new Money(amount, "USD");

            // Act
            var result = money.Multiply(factor);

            // Assert
            result.Amount.Should().Be(expected);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Multiply_Should_ThrowException_When_FactorIsNegative()
        {
            // Arrange
            var money = new Money(100m, "USD");

            // Act & Assert
            var action = () => money.Multiply(-2);

            action.Should().Throw<ArgumentException>()
                .WithMessage("Factor cannot be negative*")
                .WithParameterName("factor");
        }

        [Fact]
        public void Equality_Should_ReturnTrue_When_SameAmountAndCurrency()
        {
            // Arrange
            var money1 = new Money(100.50m, "USD");
            var money2 = new Money(100.50m, "USD");

            // Act & Assert
            money1.Should().Be(money2);
            (money1 == money2).Should().BeTrue();
            money1.Equals(money2).Should().BeTrue();
            money1.GetHashCode().Should().Be(money2.GetHashCode());
        }

        [Fact]
        public void Equality_Should_ReturnFalse_When_DifferentAmount()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(200m, "USD");

            // Act & Assert
            money1.Should().NotBe(money2);
            (money1 == money2).Should().BeFalse();
            (money1 != money2).Should().BeTrue();
        }

        [Fact]
        public void Equality_Should_ReturnFalse_When_DifferentCurrency()
        {
            // Arrange
            var money1 = new Money(100m, "USD");
            var money2 = new Money(100m, "EUR");

            // Act & Assert
            money1.Should().NotBe(money2);
            (money1 == money2).Should().BeFalse();
        }

        [Fact]
        public void Equality_Should_HandleNullComparison()
        {
            // Arrange
            var money = new Money(100m, "USD");
            Money? nullMoney = null;

            // Act & Assert
            (money == nullMoney).Should().BeFalse();
            (nullMoney == money).Should().BeFalse();
            (nullMoney == null).Should().BeTrue();
        }

        [Fact]
        public void ToString_Should_ReturnFormattedString()
        {
            // Arrange
            var money = new Money(1234.567m, "USD");

            // Act
            var result = money.ToString();

            // Assert
            result.Should().Be("1234.57 USD"); // Two decimal places
        }

        [Fact]
        public void ToString_Should_FormatZeroCorrectly()
        {
            // Arrange
            var money = new Money(0m, "EUR");

            // Act
            var result = money.ToString();

            // Assert
            result.Should().Be("0.00 EUR");
        }

        [Fact]
        public void TryCreate_Should_ReturnTrue_When_ValidInput()
        {
            // Act
            var success = Money.TryCreate(100m, "USD", out var money, out var error);

            // Assert
            success.Should().BeTrue();
            money.Should().NotBeNull();
            money.Amount.Should().Be(100m);
            money.Currency.Should().Be("USD");
            error.Should().BeEmpty();
        }

        [Fact]
        public void TryCreate_Should_ReturnFalse_When_InvalidAmount()
        {
            // Act
            var success = Money.TryCreate(-50m, "USD", out var money, out var error);

            // Assert
            success.Should().BeFalse();
            money.Should().BeNull();
            error.Should().Contain("Amount cannot be negative");
        }

        [Fact]
        public void TryCreate_Should_ReturnFalse_When_InvalidCurrency()
        {
            // Act
            var success = Money.TryCreate(100m, "INVALID", out var money, out var error);

            // Assert
            success.Should().BeFalse();
            money.Should().BeNull();
            error.Should().Contain("Invalid currency code");
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        [InlineData("JPY")]
        [InlineData("CAD")]
        [InlineData("AUD")]
        [InlineData("CHF")]
        [InlineData("CNY")]
        public void Constructor_Should_AcceptCommonCurrencyCodes(string currency)
        {
            // Act
            var money = new Money(100m, currency);

            // Assert
            money.Currency.Should().Be(currency.ToUpperInvariant());
        }

        [Fact]
        public void Money_Should_BeImmutable()
        {
            // Arrange
            var original = new Money(100m, "USD");

            // Act
            var added = original.Add(new Money(50m, "USD"));
            var subtracted = original.Subtract(new Money(25m, "USD"));
            var multiplied = original.Multiply(2);

            // Assert
            original.Amount.Should().Be(100m); // Original unchanged
            added.Amount.Should().Be(150m);
            subtracted.Amount.Should().Be(75m);
            multiplied.Amount.Should().Be(200m);
        }
    }
}