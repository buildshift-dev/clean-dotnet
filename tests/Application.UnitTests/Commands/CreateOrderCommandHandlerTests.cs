using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Commands.CreateOrder;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests.Commands
{
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<ICustomerRepository> _customerRepositoryMock;
        private readonly Mock<ILogger<CreateOrderCommandHandler>> _loggerMock;
        private readonly CreateOrderCommandHandler _handler;

        public CreateOrderCommandHandlerTests()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();
            _handler = new CreateOrderCommandHandler(
                _orderRepositoryMock.Object,
                _customerRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_CreateOrder_When_ValidDataAndActiveCustomer()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 99.99m,
                Currency = "USD",
                Details = new Dictionary<string, object> { { "item", "Product A" } }
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _orderRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order o, CancellationToken ct) => o);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.CustomerId.Value.Should().Be(customerId);
            result.Value.TotalAmount.Amount.Should().Be(99.99m);
            result.Value.TotalAmount.Currency.Should().Be("USD");
            result.Value.Details.Should().ContainKey("item");

            _orderRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CustomerNotFound()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD"
            };

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be($"Customer with ID {customerId} not found");

            _orderRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CustomerIsInactive()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD"
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));
            customer.Deactivate();

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot create order for inactive customer");

            _orderRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TotalAmountIsZero()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 0m,
                Currency = "USD"
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Order total amount must be greater than zero");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TotalAmountIsNegative()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = -50m,
                Currency = "USD"
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Amount cannot be negative");

            _orderRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("INVALID")]
        [InlineData("US")]
        public async Task Handle_Should_ReturnFailure_When_CurrencyIsInvalid(string currency)
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = currency
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().ContainAny("Currency cannot be empty", "Invalid currency code");
        }

        [Fact]
        public async Task Handle_Should_LogInformation_When_OrderCreatedSuccessfully()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD"
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _orderRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order o, CancellationToken ct) => o);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created order")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RepositoryThrowsException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD"
            };

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Error creating order");
            result.Error.Should().Contain("Database connection failed");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating order")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_PassCancellationToken_ToRepositories()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD"
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            var cancellationToken = new CancellationToken();

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), cancellationToken))
                .ReturnsAsync(customer);

            _orderRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Order>(), cancellationToken))
                .ReturnsAsync((Order o, CancellationToken ct) => o);

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _customerRepositoryMock.Verify(
                x => x.FindByIdAsync(It.IsAny<CustomerId>(), cancellationToken),
                Times.Once);
            _orderRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<Order>(), cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CreateOrderWithoutDetails_When_DetailsAreNull()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = "USD",
                Details = null
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _orderRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order o, CancellationToken ct) => o);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Details.Should().NotBeNull();
            result.Value.Details.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_OrderRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new CreateOrderCommandHandler(
                null!,
                _customerRepositoryMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("orderRepository");
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_CustomerRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new CreateOrderCommandHandler(
                _orderRepositoryMock.Object,
                null!,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("customerRepository");
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_LoggerIsNull()
        {
            // Act & Assert
            var action = () => new CreateOrderCommandHandler(
                _orderRepositoryMock.Object,
                _customerRepositoryMock.Object,
                null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        public async Task Handle_Should_AcceptDifferentCurrencies(string currency)
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                TotalAmount = 100m,
                Currency = currency
            };

            var customer = Customer.Create(
                new CustomerId(customerId),
                "John Doe",
                new Email("john@example.com"));

            _customerRepositoryMock
                .Setup(x => x.FindByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            _orderRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order o, CancellationToken ct) => o);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalAmount.Currency.Should().Be(currency);
        }
    }
}