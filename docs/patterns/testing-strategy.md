# Testing Strategy - .NET 8 Clean Architecture

## Overview

This document outlines the comprehensive testing strategy for the Clean Architecture .NET 8 project. Our approach emphasizes testing business logic thoroughly while maintaining fast feedback cycles and high confidence in deployments.

## Testing Pyramid

```
        E2E Tests (Few)
       /               \
      /                 \
     /                   \
    Integration Tests (Some)
   /                       \
  /                         \
 Unit Tests (Many)          \
```

### Test Distribution
- **Unit Tests**: 70% - Fast, isolated, testing business logic
- **Integration Tests**: 20% - Testing component interactions
- **End-to-End Tests**: 10% - Testing complete user workflows

## Unit Testing

### Domain Layer Testing

#### Testing Entities

```csharp
// Tests/Domain.UnitTests/Entities/CustomerTests.cs
namespace Domain.UnitTests.Entities;

public class CustomerTests
{
    [Fact]
    public void Customer_Constructor_ValidData_ShouldCreateCustomerWithDomainEvent()
    {
        // Arrange
        var id = CustomerId.Create();
        var name = "John Doe";
        var email = new Email("john@example.com");
        
        // Act
        var customer = new Customer(id, name, email);
        
        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().Be(id);
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
        customer.IsActive.Should().BeTrue();
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreated>();
    }
    
    [Fact]
    public void UpdateEmail_ValidEmail_ShouldUpdateEmailAndRaiseEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var newEmail = new Email("newemail@example.com");
        var originalEventCount = customer.DomainEvents.Count;
        
        // Act
        customer.UpdateEmail(newEmail);
        
        // Assert
        customer.Email.Should().Be(newEmail);
        customer.DomainEvents.Should().HaveCount(originalEventCount + 1);
        customer.DomainEvents.Last().Should().BeOfType<CustomerEmailChanged>();
    }
    
    [Fact]
    public void UpdateEmail_NullEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var customer = CreateValidCustomer();
        
        // Act & Assert
        customer.Invoking(c => c.UpdateEmail(null!))
            .Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void Deactivate_ActiveCustomer_ShouldDeactivateAndRaiseEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();
        
        // Act
        customer.Deactivate();
        
        // Assert
        customer.IsActive.Should().BeFalse();
        customer.DomainEvents.Should().Contain(e => e is CustomerDeactivated);
    }
    
    [Fact]
    public void Deactivate_AlreadyInactiveCustomer_ShouldThrowBusinessRuleViolation()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.Deactivate();
        
        // Act & Assert
        customer.Invoking(c => c.Deactivate())
            .Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Customer is already deactivated");
    }
    
    private static Customer CreateValidCustomer()
    {
        return new Customer(
            CustomerId.Create(),
            "John Doe",
            new Email("john@example.com")
        );
    }
}
```

#### Testing Value Objects

```csharp
// Tests/Domain.UnitTests/ValueObjects/EmailTests.cs
namespace Domain.UnitTests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Email_Constructor_ValidEmail_ShouldCreateEmail()
    {
        // Arrange
        var emailValue = "test@example.com";
        
        // Act
        var email = new Email(emailValue);
        
        // Assert
        email.Value.Should().Be(emailValue.ToLowerInvariant());
        email.Domain.Should().Be("example.com");
        email.LocalPart.Should().Be("test");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user.example.com")]
    public void Email_Constructor_InvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        Action act = () => new Email(invalidEmail);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Email_Equality_SameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("TEST@EXAMPLE.COM"); // Different case
        
        // Act & Assert
        email1.Should().Be(email2);
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }
    
    [Fact]
    public void Email_ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var email = new Email("test@example.com");
        
        // Act & Assert
        email.ToString().Should().Be("test@example.com");
    }
}
```

#### Testing Money Value Object

```csharp
// Tests/Domain.UnitTests/ValueObjects/MoneyTests.cs
namespace Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_Constructor_ValidValues_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");
        
        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Money_Constructor_NegativeAmount_ShouldThrowArgumentException(decimal negativeAmount)
    {
        // Act & Assert
        Action act = () => new Money(negativeAmount, "USD");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Money amount cannot be negative*");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    public void Money_Constructor_InvalidCurrency_ShouldThrowArgumentException(string invalidCurrency)
    {
        // Act & Assert
        Action act = () => new Money(100, invalidCurrency);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Money_Add_SameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");
        
        // Act
        var result = money1.Add(money2);
        
        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Money_Add_DifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");
        
        // Act & Assert
        money1.Invoking(m => m.Add(money2))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }
    
    [Fact]
    public void Money_Multiply_ShouldReturnProduct()
    {
        // Arrange
        var money = new Money(100, "USD");
        
        // Act
        var result = money.Multiply(1.5m);
        
        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Money_Zero_ShouldCreateZeroMoney()
    {
        // Act
        var zero = Money.Zero("USD");
        
        // Assert
        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("USD");
    }
}
```

### Application Layer Testing

#### Testing Command Handlers

```csharp
// Tests/Application.UnitTests/Commands/CreateCustomerCommandHandlerTests.cs
namespace Application.UnitTests.Commands;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CreateCustomerCommandHandler>> _loggerMock;
    private readonly CreateCustomerCommandHandler _handler;
    
    public CreateCustomerCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CreateCustomerCommandHandler>>();
        
        _handler = new CreateCustomerCommandHandler(
            _customerRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object
        );
    }
    
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateCustomerAndReturnDto()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        
        _customerRepositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);
        
        var expectedDto = new CustomerDto
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Email = command.Email
        };
        
        _mapperMock
            .Setup(x => x.Map<CustomerDto>(It.IsAny<Customer>()))
            .Returns(expectedDto);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(command.Name);
        result.Value!.Email.Should().Be(command.Email);
        
        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Customer>(c => c.Name == command.Name), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "existing@example.com"
        };
        
        _customerRepositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already in use");
        
        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    [Fact]
    public async Task Handle_InvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "invalid-email"
        };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
        
        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

#### Testing Query Handlers

```csharp
// Tests/Application.UnitTests/Queries/GetCustomerByIdQueryHandlerTests.cs
namespace Application.UnitTests.Queries;

public class GetCustomerByIdQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetCustomerByIdQueryHandler _handler;
    
    public GetCustomerByIdQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _mapperMock = new Mock<IMapper>();
        
        _handler = new GetCustomerByIdQueryHandler(
            _customerRepositoryMock.Object,
            _mapperMock.Object
        );
    }
    
    [Fact]
    public async Task Handle_ExistingCustomer_ShouldReturnCustomerDto()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var query = new GetCustomerByIdQuery { CustomerId = customerId.Value };
        
        var customer = new Customer(customerId, "John Doe", new Email("john@example.com"));
        var expectedDto = new CustomerDto
        {
            Id = customerId.Value,
            Name = "John Doe",
            Email = "john@example.com"
        };
        
        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        
        _mapperMock
            .Setup(x => x.Map<CustomerDto>(customer))
            .Returns(expectedDto);
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedDto);
    }
    
    [Fact]
    public async Task Handle_NonExistentCustomer_ShouldReturnFailure()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var query = new GetCustomerByIdQuery { CustomerId = customerId.Value };
        
        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
```

## Integration Testing

### Repository Integration Tests

```csharp
// Tests/Infrastructure.IntegrationTests/Repositories/CustomerRepositoryTests.cs
namespace Infrastructure.IntegrationTests.Repositories;

public class CustomerRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly CustomerRepository _repository;
    
    public CustomerRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new CustomerRepository(_fixture.Context, _fixture.Logger);
    }
    
    [Fact]
    public async Task AddAsync_ValidCustomer_ShouldPersistToDatabase()
    {
        // Arrange
        var customer = new Customer(
            CustomerId.Create(),
            "John Doe",
            new Email("john@example.com")
        );
        
        // Act
        var result = await _repository.AddAsync(customer);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(customer.Id);
        
        // Verify persistence
        var persisted = await _repository.GetByIdAsync(customer.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("John Doe");
    }
    
    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ShouldReturnCustomer()
    {
        // Arrange
        var email = new Email("test@example.com");
        var customer = new Customer(CustomerId.Create(), "Test User", email);
        await _repository.AddAsync(customer);
        
        // Act
        var result = await _repository.GetByEmailAsync(email);
        
        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }
    
    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ShouldReturnNull()
    {
        // Arrange
        var email = new Email("nonexistent@example.com");
        
        // Act
        var result = await _repository.GetByEmailAsync(email);
        
        // Assert
        result.Should().BeNull();
    }
}
```

### API Integration Tests

```csharp
// Tests/WebApi.IntegrationTests/Controllers/CustomersControllerTests.cs
namespace WebApi.IntegrationTests.Controllers;

public class CustomersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public CustomersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task POST_Customers_ValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/customers", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Name.Should().Be(request.Name);
        customer.Email.Should().Be(request.Email);
        
        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(customer.Id.ToString());
    }
    
    [Fact]
    public async Task POST_Customers_InvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            Name = "John Doe",
            Email = "invalid-email"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/customers", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Detail.Should().Contain("Validation error");
    }
    
    [Fact]
    public async Task GET_Customers_ExistingId_Returns200OK()
    {
        // Arrange
        var createRequest = new CreateCustomerRequest
        {
            Name = "Jane Doe",
            Email = "jane@example.com"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();
        
        // Act
        var response = await _client.GetAsync($"/api/v1/customers/{createdCustomer!.Id}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        customer!.Id.Should().Be(createdCustomer.Id);
        customer.Name.Should().Be("Jane Doe");
    }
    
    [Fact]
    public async Task GET_Customers_NonExistentId_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/api/v1/customers/{nonExistentId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## End-to-End Testing

### Complete Workflow Tests

```csharp
// Tests/E2E.Tests/CustomerOrderWorkflowTests.cs
namespace E2E.Tests;

public class CustomerOrderWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public CustomerOrderWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task CompleteOrderWorkflow_ShouldCreateCustomerAndPlaceOrder()
    {
        // Step 1: Create Customer
        var createCustomerRequest = new CreateCustomerRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "City",
                State = "ST",
                PostalCode = "12345",
                Country = "USA"
            }
        };
        
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", createCustomerRequest);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();
        customer.Should().NotBeNull();
        
        // Step 2: Create Order
        var createOrderRequest = new CreateOrderRequest
        {
            CustomerId = customer!.Id,
            Items = new List<OrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    UnitPrice = 29.99m,
                    Currency = "USD",
                    Quantity = 2
                }
            },
            ShippingAddress = new ShippingAddressDto
            {
                Street = "456 Oak Ave",
                City = "Another City",
                State = "ST",
                PostalCode = "67890",
                Country = "USA"
            }
        };
        
        var orderResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.CustomerId.Should().Be(customer.Id);
        order.TotalAmount.Should().Be(59.98m); // 29.99 * 2
        
        // Step 3: Verify Customer Orders
        var customerOrdersResponse = await _client.GetAsync($"/api/v1/customers/{customer.Id}/orders");
        customerOrdersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customerOrders = await customerOrdersResponse.Content.ReadFromJsonAsync<List<OrderDto>>();
        customerOrders.Should().NotBeNull();
        customerOrders.Should().ContainSingle();
        customerOrders![0].Id.Should().Be(order.Id);
    }
}
```

## Test Infrastructure

### Database Test Fixture

```csharp
// Tests/Infrastructure.IntegrationTests/DatabaseFixture.cs
namespace Infrastructure.IntegrationTests;

public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; private set; }
    public ILogger<CustomerRepository> Logger { get; private set; }
    
    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        Logger = loggerFactory.CreateLogger<CustomerRepository>();
    }
    
    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
```

### Test Data Builders

```csharp
// Tests/Common/Builders/CustomerBuilder.cs
namespace Tests.Common.Builders;

public class CustomerBuilder
{
    private CustomerId _id = CustomerId.Create();
    private string _name = "John Doe";
    private Email _email = new("john@example.com");
    private Address? _address;
    private Dictionary<string, object>? _preferences;
    
    public CustomerBuilder WithId(CustomerId id)
    {
        _id = id;
        return this;
    }
    
    public CustomerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CustomerBuilder WithEmail(string email)
    {
        _email = new Email(email);
        return this;
    }
    
    public CustomerBuilder WithAddress(Address address)
    {
        _address = address;
        return this;
    }
    
    public CustomerBuilder WithPreferences(Dictionary<string, object> preferences)
    {
        _preferences = preferences;
        return this;
    }
    
    public Customer Build()
    {
        var customer = new Customer(_id, _name, _email, _address, _preferences);
        customer.ClearDomainEvents(); // Clear events for testing
        return customer;
    }
    
    public static implicit operator Customer(CustomerBuilder builder) => builder.Build();
}

// Usage:
// var customer = new CustomerBuilder()
//     .WithName("Jane Doe")
//     .WithEmail("jane@example.com")
//     .Build();
```

## Performance Testing

### Load Testing with NBomber

```csharp
// Tests/Performance.Tests/CustomerApiLoadTests.cs
namespace Performance.Tests;

public class CustomerApiLoadTests
{
    [Fact]
    public void Customer_Creation_Load_Test()
    {
        var scenario = Scenario.Create("create_customer", async context =>
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5001");
            
            var request = new CreateCustomerRequest
            {
                Name = $"User {context.ScenarioInfo.CurrentSequenceNumber}",
                Email = $"user{context.ScenarioInfo.CurrentSequenceNumber}@example.com"
            };
            
            var response = await client.PostAsJsonAsync("/api/v1/customers", request);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(1))
        );
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
```

## Test Configuration

### xUnit Configuration

```json
// xunit.runner.json
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 0,
  "methodDisplay": "method",
  "preEnumerateTheories": false
}
```

### Test Settings

```json
// runsettings.xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
  </RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="Code Coverage" uri="datacollector://Microsoft/CodeCoverage/2.0" assemblyQualifiedName="Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector">
        <Configuration>
          <CodeCoverage>
            <ModulePaths>
              <Include>
                <ModulePath>.*\.dll$</ModulePath>
              </Include>
              <Exclude>
                <ModulePath>.*Tests\.dll$</ModulePath>
              </Exclude>
            </ModulePaths>
          </CodeCoverage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/tests.yml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Unit Tests
      run: |
        dotnet test tests/Domain.UnitTests \
          --no-build \
          --verbosity normal \
          --collect:"XPlat Code Coverage" \
          --results-directory ./coverage
        
        dotnet test tests/Application.UnitTests \
          --no-build \
          --verbosity normal \
          --collect:"XPlat Code Coverage" \
          --results-directory ./coverage
    
    - name: Integration Tests
      run: |
        dotnet test tests/Infrastructure.IntegrationTests \
          --no-build \
          --verbosity normal
        
        dotnet test tests/WebApi.IntegrationTests \
          --no-build \
          --verbosity normal
    
    - name: E2E Tests
      run: |
        dotnet test tests/E2E.Tests \
          --no-build \
          --verbosity normal
    
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:"coverage/**/coverage.cobertura.xml" \
          -targetdir:"coverage/report" \
          -reporttypes:Html
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        directory: ./coverage
        flags: unittests
        name: codecov-umbrella
```

## Best Practices

### General Testing Principles

1. **AAA Pattern**: Arrange, Act, Assert
2. **Test Naming**: Method_Scenario_ExpectedResult
3. **Single Responsibility**: One concept per test
4. **Independent Tests**: Tests should not depend on each other
5. **Deterministic**: Tests should always produce the same result

### Domain Testing

1. **Focus on Behavior**: Test what the code does, not how
2. **Test Business Rules**: Ensure all business rules are covered
3. **Test Edge Cases**: Boundary conditions and error scenarios
4. **Test Invariants**: Verify that object state remains valid

### Integration Testing

1. **Test Real Interactions**: Use actual database, not mocks
2. **Isolated Environment**: Each test should have clean state
3. **Representative Data**: Use realistic test data
4. **Test Configurations**: Verify configuration is correct

### Performance Testing

1. **Baseline Metrics**: Establish performance baselines
2. **Realistic Load**: Test under expected production load
3. **Monitor Resources**: CPU, memory, database connections
4. **Automated Checks**: Include in CI/CD pipeline

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / xUnit / FluentAssertions*
*Status: Testing Strategy Guide*