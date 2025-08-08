using System;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests.ValueObjects
{
    public class EmailTests
    {
        [Theory]
        [InlineData("user@example.com")]
        [InlineData("john.doe@company.org")]
        [InlineData("test123@test.co.uk")]
        [InlineData("user+tag@example.com")]
        [InlineData("firstname.lastname@example.com")]
        [InlineData("email@subdomain.example.com")]
        [InlineData("1234567890@example.com")]
        [InlineData("user%test@example.com")]
        [InlineData("_user@example.com")]
        [InlineData("user-name@example-domain.com")]
        public void Constructor_Should_CreateEmail_When_ValidFormat(string validEmail)
        {
            // Act
            var email = new Email(validEmail);

            // Assert
            email.Should().NotBeNull();
            email.Value.Should().Be(validEmail.ToLowerInvariant());
        }

        [Fact]
        public void Constructor_Should_ConvertToLowerCase()
        {
            // Arrange
            var upperCaseEmail = "User@EXAMPLE.COM";

            // Act
            var email = new Email(upperCaseEmail);

            // Assert
            email.Value.Should().Be("user@example.com");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_Should_ThrowException_When_EmailIsNullOrWhitespace(string invalidEmail)
        {
            // Act & Assert
            var action = () => new Email(invalidEmail);

            action.Should().Throw<ArgumentException>()
                .WithMessage("Email cannot be empty*")
                .WithParameterName("value");
        }

        [Theory]
        [InlineData("plaintext")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user.example.com")]
        [InlineData("user @example.com")]
        [InlineData("user@example")]
        [InlineData("user@.com")]
        [InlineData("user@example.")]
        [InlineData("user@@example.com")]
        [InlineData("user@example@com")]
        [InlineData("user@exam ple.com")]
        [InlineData("user@example.c")]
        public void Constructor_Should_ThrowException_When_EmailFormatIsInvalid(string invalidEmail)
        {
            // Act & Assert
            var action = () => new Email(invalidEmail);

            action.Should().Throw<ArgumentException>()
                .WithMessage($"Invalid email format: {invalidEmail}*")
                .WithParameterName("value");
        }

        [Fact]
        public void Domain_Should_ReturnCorrectDomainPart()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            var domain = email.Domain;

            // Assert
            domain.Should().Be("example.com");
        }

        [Fact]
        public void Domain_Should_HandleSubdomains()
        {
            // Arrange
            var email = new Email("user@mail.example.com");

            // Act
            var domain = email.Domain;

            // Assert
            domain.Should().Be("mail.example.com");
        }

        [Fact]
        public void LocalPart_Should_ReturnCorrectLocalPart()
        {
            // Arrange
            var email = new Email("john.doe@example.com");

            // Act
            var localPart = email.LocalPart;

            // Assert
            localPart.Should().Be("john.doe");
        }

        [Fact]
        public void LocalPart_Should_HandleSpecialCharacters()
        {
            // Arrange
            var email = new Email("user+tag@example.com");

            // Act
            var localPart = email.LocalPart;

            // Assert
            localPart.Should().Be("user+tag");
        }

        [Fact]
        public void Equality_Should_ReturnTrue_When_SameEmailAddress()
        {
            // Arrange
            var email1 = new Email("user@example.com");
            var email2 = new Email("user@example.com");

            // Act & Assert
            email1.Should().Be(email2);
            (email1 == email2).Should().BeTrue();
            email1.Equals(email2).Should().BeTrue();
            email1.GetHashCode().Should().Be(email2.GetHashCode());
        }

        [Fact]
        public void Equality_Should_ReturnTrue_When_DifferentCaseButSameEmail()
        {
            // Arrange
            var email1 = new Email("User@Example.COM");
            var email2 = new Email("user@example.com");

            // Act & Assert
            email1.Should().Be(email2);
            (email1 == email2).Should().BeTrue();
        }

        [Fact]
        public void Equality_Should_ReturnFalse_When_DifferentEmails()
        {
            // Arrange
            var email1 = new Email("user1@example.com");
            var email2 = new Email("user2@example.com");

            // Act & Assert
            email1.Should().NotBe(email2);
            (email1 == email2).Should().BeFalse();
            (email1 != email2).Should().BeTrue();
        }

        [Fact]
        public void Equality_Should_HandleNullComparison()
        {
            // Arrange
            var email = new Email("user@example.com");
            Email? nullEmail = null;

            // Act & Assert
            (email == nullEmail).Should().BeFalse();
            (nullEmail == email).Should().BeFalse();
            (nullEmail == null).Should().BeTrue();
        }

        [Fact]
        public void ToString_Should_ReturnEmailValue()
        {
            // Arrange
            var emailString = "user@example.com";
            var email = new Email(emailString);

            // Act
            var result = email.ToString();

            // Assert
            result.Should().Be(emailString);
        }

        [Fact]
        public void ImplicitStringConversion_Should_ReturnEmailValue()
        {
            // Arrange
            var email = new Email("user@example.com");

            // Act
            string emailString = email;

            // Assert
            emailString.Should().Be("user@example.com");
        }

        [Fact]
        public void TryCreate_Should_ReturnTrue_When_ValidEmail()
        {
            // Arrange
            var validEmail = "user@example.com";

            // Act
            var success = Email.TryCreate(validEmail, out var email, out var error);

            // Assert
            success.Should().BeTrue();
            email.Should().NotBeNull();
            email.Value.Should().Be(validEmail.ToLowerInvariant());
            error.Should().BeEmpty();
        }

        [Fact]
        public void TryCreate_Should_ReturnFalse_When_InvalidEmail()
        {
            // Arrange
            var invalidEmail = "not-an-email";

            // Act
            var success = Email.TryCreate(invalidEmail, out var email, out var error);

            // Assert
            success.Should().BeFalse();
            email.Should().BeNull();
            error.Should().Contain("Invalid email format");
        }

        [Fact]
        public void TryCreate_Should_ReturnFalse_When_EmptyEmail()
        {
            // Act
            var success = Email.TryCreate("", out var email, out var error);

            // Assert
            success.Should().BeFalse();
            email.Should().BeNull();
            error.Should().Contain("Email cannot be empty");
        }

        [Theory]
        [InlineData("a@example.com", "a", "example.com")]
        [InlineData("test.user@subdomain.example.org", "test.user", "subdomain.example.org")]
        [InlineData("123@test.co", "123", "test.co")]
        public void Email_Should_CorrectlyParseParts(string emailAddress, string expectedLocal, string expectedDomain)
        {
            // Arrange
            var email = new Email(emailAddress);

            // Act & Assert
            email.LocalPart.Should().Be(expectedLocal);
            email.Domain.Should().Be(expectedDomain);
        }

        [Fact]
        public void Email_Should_BeImmutable()
        {
            // Arrange
            var originalValue = "user@example.com";
            var email = new Email(originalValue);

            // Act - Try to access value
            var value = email.Value;

            // Assert - Value should remain unchanged
            email.Value.Should().Be(originalValue);
            value.Should().Be(originalValue);
        }

        [Theory]
        [InlineData("test@gmail.com")]
        [InlineData("user@outlook.com")]
        [InlineData("admin@company.io")]
        [InlineData("contact@website.tech")]
        [InlineData("support@service.cloud")]
        public void Constructor_Should_AcceptCommonEmailProviders(string emailAddress)
        {
            // Act
            var email = new Email(emailAddress);

            // Assert
            email.Value.Should().Be(emailAddress.ToLowerInvariant());
        }
    }
}