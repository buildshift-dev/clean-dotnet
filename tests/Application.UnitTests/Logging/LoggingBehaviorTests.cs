using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Application.Commands.CreateCustomer;
using Domain.ValueObjects;

namespace Application.UnitTests.Logging;

/// <summary>
/// Tests for logging behavior in the Application layer
/// </summary>
public class LoggingBehaviorTests
{
    private Serilog.ILogger CreateTestLogger()
    {
        // Create a unique in-memory sink for each test to avoid interference
        return new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();
    }

    [Fact]
    public void SerilogConfiguration_Should_LogInformationLevel_When_DefaultConfiguration()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        logger.Information("Test information message");

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        logEvents.Should().ContainSingle();
        logEvents.Single().Level.Should().Be(LogEventLevel.Information);
        logEvents.Single().MessageTemplate.Text.Should().Be("Test information message");
    }

    [Fact]
    public void StructuredLogging_Should_CaptureProperties_When_ObjectsProvided()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var customerId = CustomerId.New();
        var customerName = "John Doe";

        // Act
        logger.Information("Customer {CustomerId} created with name {CustomerName}",
            customerId.Value, customerName);

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Properties.Should().ContainKey("CustomerId");
        logEvent.Properties.Should().ContainKey("CustomerName");
        logEvent.Properties["CustomerName"].ToString().Should().Contain(customerName);
    }

    [Fact]
    public void ErrorLogging_Should_CaptureException_When_ExceptionProvided()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.Error(exception, "An error occurred");

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Level.Should().Be(LogEventLevel.Error);
        logEvent.Exception.Should().Be(exception);
        logEvent.MessageTemplate.Text.Should().Be("An error occurred");
    }

    [Fact]
    public void WarningLogging_Should_CaptureBusinessRuleViolations()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var customerId = CustomerId.New();
        var invalidEmail = "invalid-email";

        // Act
        logger.Warning("Invalid email {Email} provided for customer {CustomerId}",
            invalidEmail, customerId.Value);

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Level.Should().Be(LogEventLevel.Warning);
        logEvent.Properties.Should().ContainKey("Email");
        logEvent.Properties.Should().ContainKey("CustomerId");
        logEvent.Properties["Email"].ToString().Should().Contain(invalidEmail);
    }

    [Fact]
    public void LogLevelConfiguration_Should_FilterDebugMessages_When_InformationMinimumLevel()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        logger.Debug("Debug message - should be filtered");
        logger.Information("Information message - should appear");
        logger.Warning("Warning message - should appear");

        // Assert
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        logEvents.Should().HaveCount(2);
        logEvents.Should().NotContain(e => e.Level == LogEventLevel.Debug);
        logEvents.Should().Contain(e => e.Level == LogEventLevel.Information);
        logEvents.Should().Contain(e => e.Level == LogEventLevel.Warning);
    }

    [Fact]
    public void PerformanceLogging_Should_CaptureExecutionTime_When_OperationCompleted()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var operationName = "CreateCustomer";
        var executionTimeMs = 150;

        // Act
        logger.Information("Operation {OperationName} completed in {ExecutionTimeMs}ms",
            operationName, executionTimeMs);

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Properties.Should().ContainKey("OperationName");
        logEvent.Properties.Should().ContainKey("ExecutionTimeMs");
        logEvent.Properties["OperationName"].ToString().Should().Contain(operationName);
        logEvent.Properties["ExecutionTimeMs"].ToString().Should().Contain(executionTimeMs.ToString());
    }

    [Fact]
    public void SecurityLogging_Should_LogUnauthorizedAccess_When_SecurityViolationOccurs()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var userId = "user-123";
        var resource = "customer-data";
        var action = "DELETE";

        // Act
        logger.Warning("Unauthorized access attempt: User {UserId} tried to {Action} resource {Resource}",
            userId, action, resource);

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Level.Should().Be(LogEventLevel.Warning);
        logEvent.Properties.Should().ContainKey("UserId");
        logEvent.Properties.Should().ContainKey("Action");
        logEvent.Properties.Should().ContainKey("Resource");
        logEvent.Properties["Action"].ToString().Should().Contain(action);
    }

    [Fact]
    public void BusinessEventLogging_Should_StructureEventData_When_DomainEventOccurs()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var customerId = CustomerId.New();
        var eventType = "CustomerCreated";
        var customerName = "John Doe";

        // Act
        logger.Information("Domain event {EventType} occurred for customer {CustomerId} named {CustomerName}",
            eventType, customerId.Value, customerName);

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Properties.Should().ContainKey("EventType");
        logEvent.Properties.Should().ContainKey("CustomerId");
        logEvent.Properties.Should().ContainKey("CustomerName");
        logEvent.Properties["EventType"].ToString().Should().Contain(eventType);
    }

    [Fact]
    public void ValidationLogging_Should_CaptureMultipleErrors_When_ValidationFails()
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        var validationErrors = new[] { "Name is required", "Email format is invalid", "Phone number is missing" };
        var errorCount = validationErrors.Length;

        // Act
        logger.Warning("Validation failed with {ErrorCount} errors: {ValidationErrors}",
            errorCount, string.Join(", ", validationErrors));

        // Assert
        var logEvent = InMemorySink.Instance.LogEvents.Single();
        logEvent.Level.Should().Be(LogEventLevel.Warning);
        logEvent.Properties.Should().ContainKey("ErrorCount");
        logEvent.Properties.Should().ContainKey("ValidationErrors");
        logEvent.Properties["ErrorCount"].ToString().Should().Contain(errorCount.ToString());
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, true)]
    [InlineData(LogEventLevel.Information, true)]
    [InlineData(LogEventLevel.Warning, true)]
    [InlineData(LogEventLevel.Error, true)]
    [InlineData(LogEventLevel.Fatal, true)]
    public void LogLevel_Should_BeConfigurable_For_DifferentLevels(LogEventLevel level, bool shouldLog)
    {
        // Arrange
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        logger.Write(level, "Test message for level {Level}", level);

        // Assert
        if (shouldLog)
        {
            var logEvent = InMemorySink.Instance.LogEvents.Single();
            logEvent.Level.Should().Be(level);
        }
        else
        {
            InMemorySink.Instance.LogEvents.Should().BeEmpty();
        }
    }
}