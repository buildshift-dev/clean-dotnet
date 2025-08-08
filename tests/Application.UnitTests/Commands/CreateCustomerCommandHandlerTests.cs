using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Commands.CreateCustomer;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests.Commands
{
    public class CreateCustomerCommandHandlerTests
    {
        private readonly Mock<ICustomerRepository> _customerRepositoryMock;
        private readonly Mock<ILogger<CreateCustomerCommandHandler>> _loggerMock;
        private readonly CreateCustomerCommandHandler _handler;

        public CreateCustomerCommandHandlerTests()
        {
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _loggerMock = new Mock<ILogger<CreateCustomerCommandHandler>>();
            _handler = new CreateCustomerCommandHandler(
                _customerRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_CreateCustomer_When_ValidDataProvided()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Preferences = new Dictionary<string, object> { { "newsletter", true } }
            };

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            _customerRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer c, CancellationToken ct) => c);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be("John Doe");
            result.Value.Email.Value.Should().Be("john.doe@example.com");
            result.Value.Preferences.Should().ContainKey("newsletter");

            _customerRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmailIsInvalid()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "invalid-email",
                Preferences = null
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Invalid email format");

            _customerRepositoryMock.Verify(
                x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _customerRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CustomerWithEmailAlreadyExists()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "existing@example.com",
                Preferences = null
            };

            var existingCustomer = Customer.Create(
                CustomerId.New(),
                "Existing Customer",
                new Email("existing@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCustomer);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Customer with email existing@example.com already exists");

            _customerRepositoryMock.Verify(
                x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _customerRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_LogInformation_When_CustomerCreatedSuccessfully()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "john@example.com"
            };

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            _customerRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer c, CancellationToken ct) => c);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created customer")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RepositoryThrowsException()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "john@example.com"
            };

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Error creating customer");
            result.Error.Should().Contain("Database connection failed");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating customer")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_PassCancellationToken_ToRepository()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "john@example.com"
            };

            var cancellationToken = new CancellationToken();

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), cancellationToken))
                .ReturnsAsync((Customer?)null);

            _customerRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Customer>(), cancellationToken))
                .ReturnsAsync((Customer c, CancellationToken ct) => c);

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _customerRepositoryMock.Verify(
                x => x.FindByEmailAsync(It.IsAny<Email>(), cancellationToken),
                Times.Once);
            _customerRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Customer>(), cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CreateCustomerWithoutPreferences_When_PreferencesAreNull()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "john@example.com",
                Preferences = null
            };

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            _customerRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer c, CancellationToken ct) => c);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Preferences.Should().NotBeNull();
            result.Value.Preferences.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_CustomerRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new CreateCustomerCommandHandler(
                null!,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("customerRepository");
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_LoggerIsNull()
        {
            // Act & Assert
            var action = () => new CreateCustomerCommandHandler(
                _customerRepositoryMock.Object,
                null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Handle_Should_ReturnFailure_When_EmailIsEmpty(string email)
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = email
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Email cannot be empty");
        }

        [Fact]
        public async Task Handle_Should_SaveCustomerWithCorrectEmail_When_EmailHasDifferentCase()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "John Doe",
                Email = "John.Doe@EXAMPLE.COM"
            };

            Customer? savedCustomer = null;

            _customerRepositoryMock
                .Setup(x => x.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            _customerRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .Callback<Customer, CancellationToken>((c, ct) => savedCustomer = c)
                .ReturnsAsync((Customer c, CancellationToken ct) => c);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            savedCustomer.Should().NotBeNull();
            savedCustomer!.Email.Value.Should().Be("john.doe@example.com");
        }
    }
}