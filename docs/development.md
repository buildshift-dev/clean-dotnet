# Development Guide

## Clean Architecture .NET 8 - Development Documentation

This guide covers everything developers need to know to contribute to and extend the Clean Architecture .NET 8 project.

### 📚 Related Patterns Documentation

For comprehensive development patterns and best practices, see:
- **[Coding Standards](patterns/coding-standards.md)** - .NET 8 conventions and best practices
- **[Testing Strategy](patterns/testing-strategy.md)** - Comprehensive testing approach and examples
- **[Clean Architecture Patterns](patterns/clean-architecture.md)** - Complete implementation guide
- **[Domain-Driven Design](patterns/domain-driven-design.md)** - DDD patterns in .NET
- **[CQRS Patterns](patterns/cqrs-patterns.md)** - Command/Query implementation with MediatR

## Getting Started

### Quick Setup
```bash
# Project setup
make setup-dev             # Configure local development
make build                 # Build the solution
make test                  # Run tests

# Start developing
make run-watch             # Hot reload development server
```

### Prerequisites

**Required Software**:
- **.NET 8 SDK** (latest version)
- **Git** (version control)
- **Docker Desktop** (containerization)

**Recommended IDEs**:
- **Visual Studio 2022** (Windows/Mac)
- **Visual Studio Code** (Cross-platform)
- **JetBrains Rider** (Cross-platform)

**Optional Tools**:
- **AWS CLI** (cloud deployment)
- **Postman** (API testing)
- **SQL Server Management Studio** (database)

## Project Structure Deep Dive

### Solution Architecture
```
CleanArchitecture.sln                 # Solution file
├── src/                              # Source code
│   ├── Domain/                       # Business logic (depends on SharedKernel)
│   ├── Application/                  # Use cases (depends on Domain, SharedKernel)
│   ├── Infrastructure/               # Data access (depends on Domain, Application, SharedKernel)
│   ├── SharedKernel/                 # Shared base classes and utilities (no dependencies)
│   └── WebApi/                      # API endpoints (depends on Application)
├── tests/                           # Test projects
│   ├── Domain.UnitTests/            # Domain layer tests
│   └── Application.UnitTests/       # Application layer tests
├── cloudformation/                  # AWS infrastructure
├── scripts/                         # Deployment automation
└── docs/                           # Documentation
```

### Code Organization Principles

1. **Dependency Rule**: Inner layers don't depend on outer layers
2. **Single Responsibility**: Each class has one reason to change
3. **Interface Segregation**: Depend on abstractions, not concretions
4. **Dependency Inversion**: High-level modules don't depend on low-level modules

## Development Workflow

### Daily Development Commands
```bash
# Start development session
make run-watch              # Hot reload server

# Code quality checks
make format                 # Format code
make lint                   # Code analysis
make test                   # Run tests

# Build and deployment
make build                  # Build solution
make docker-build           # Build container (linux/amd64)
make deploy                 # Deploy to AWS
```

### Git Workflow
```bash
# Feature development
git checkout -b feature/customer-validation
# ... make changes ...
make pre-commit             # Quality checks
git add .
git commit -m "Add customer email validation"
git push origin feature/customer-validation
# ... create pull request ...
```

### Testing Workflow
```bash
# Run all tests
make test

# Run specific test project
dotnet test tests/Domain.UnitTests/

# Run specific test class
dotnet test --filter "CustomerTests"

# Run with coverage
make test-coverage

# Debug specific test
dotnet test --filter "Should_CreateCustomer_When_ValidData" --logger "console;verbosity=detailed"
```

## Development Guidelines

### 📚 Comprehensive Coding Standards

For complete coding standards including naming conventions, async patterns, nullable reference types, and architectural guidelines, see:
- **[Coding Standards](patterns/coding-standards.md)** - Complete .NET 8 coding conventions and best practices

### C# Coding Standards Summary

**Naming Conventions**:
```csharp
// PascalCase for public members
public class CustomerService { }
public void CreateCustomer() { }
public string CustomerName { get; set; }

// camelCase for private members and parameters
private readonly ICustomerRepository _customerRepository;
public void Handle(CreateCustomerCommand command) { }

// UPPER_CASE for constants
public const string DEFAULT_CURRENCY = "USD";
```

**Async/Await Patterns**:
```csharp
// Good: Async all the way down
public async Task<Result<Customer>> CreateCustomerAsync(CreateCustomerCommand command)
{
    var customer = await _customerRepository.FindByEmailAsync(command.Email);
    // ... business logic ...
    await _customerRepository.SaveAsync(customer);
    return Result<Customer>.Success(customer);
}

// Bad: Blocking async calls
public Customer CreateCustomer(CreateCustomerCommand command)
{
    var customer = _customerRepository.FindByEmailAsync(command.Email).Result; // DON'T DO THIS
    return customer;
}
```

**Nullable Reference Types**:
```csharp
// Good: Explicit nullability
public class Customer
{
    public string Name { get; private set; } = string.Empty;  // Not null
    public Address? Address { get; private set; }             // Nullable
}

// Bad: Implicit nullability
public class Customer
{
    public string Name { get; set; }    // Warning: possible null
}
```

### Domain Layer Guidelines

**Entity Design**:
```csharp
public class Customer : AggregateRoot
{
    // Private constructor for EF Core
    private Customer() { }
    
    // Factory method for creation
    public static Customer Create(CustomerId id, string name, Email email)
    {
        // Validation logic
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        
        var customer = new Customer { Id = Guid.NewGuid(), CustomerId = id, Name = name, Email = email };
        
        // Domain event
        customer.AddDomainEvent(new CustomerCreated(id, name, email.Value));
        
        return customer;
    }
    
    // Business behavior
    public void UpdateEmail(Email newEmail)
    {
        if (Email.Equals(newEmail))
            return; // No change
        
        var oldEmail = Email;
        Email = newEmail;
        AddDomainEvent(new CustomerEmailChanged(CustomerId, oldEmail.Value, newEmail.Value));
    }
}
```

**Value Object Design**:
```csharp
public sealed class Email : ValueObject
{
    public string Value { get; }
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure("Email is required");
        
        if (!IsValidEmail(email))
            return Result<Email>.Failure("Invalid email format");
        
        return Result<Email>.Success(new Email(email.ToLowerInvariant()));
    }
    
    public static bool TryCreate(string email, out Email? result, out string? error)
    {
        var createResult = Create(email);
        if (createResult.IsSuccess)
        {
            result = createResult.Value;
            error = null;
            return true;
        }
        
        result = null;
        error = createResult.Error;
        return false;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### Application Layer Guidelines

**Command Handler Pattern**:
```csharp
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Customer>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    
    public CreateCustomerCommandHandler(ICustomerRepository customerRepository, ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Result<Customer>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating customer with email {Email}", request.Email);
        
        // Validate email
        if (!Email.TryCreate(request.Email, out var email, out var emailError))
        {
            _logger.LogWarning("Invalid email format {Email}", request.Email);
            return Result<Customer>.Failure(emailError);
        }
        
        // Check for existing customer
        var existingCustomer = await _customerRepository.FindByEmailAsync(email, cancellationToken);
        if (existingCustomer != null)
        {
            _logger.LogWarning("Customer already exists with email {Email}", request.Email);
            return Result<Customer>.Failure("A customer with this email already exists");
        }
        
        // Create new customer
        var customerId = CustomerId.New();
        var customer = Customer.Create(customerId, request.Name, email, null, null, request.Preferences);
        
        await _customerRepository.SaveAsync(customer, cancellationToken);
        
        _logger.LogInformation("Customer created successfully {CustomerId}", customer.CustomerId.Value);
        return Result<Customer>.Success(customer);
    }
}
```

**Query Handler Pattern**:
```csharp
public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<IEnumerable<Order>>>
{
    private readonly IOrderRepository _orderRepository;
    
    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }
    
    public async Task<Result<IEnumerable<Order>>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(request.CustomerId);
        var orders = await _orderRepository.FindByCustomerAsync(customerId, cancellationToken);
        
        return Result<IEnumerable<Order>>.Success(orders);
    }
}
```

### Infrastructure Layer Guidelines

**Repository Implementation**:
```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerRepository> _logger;
    
    public CustomerRepository(ApplicationDbContext context, ILogger<CustomerRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding customer by ID {CustomerId}", id.Value);
        
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id, cancellationToken);
    }
    
    public async Task SaveAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving customer {CustomerId}", customer.CustomerId.Value);
        
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId, cancellationToken);
        
        if (existingCustomer == null)
        {
            _context.Customers.Add(customer);
        }
        else
        {
            _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Customer saved successfully {CustomerId}", customer.CustomerId.Value);
    }
}
```

**Entity Framework Configuration**:
```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        
        builder.HasKey(c => c.Id);
        
        // Value object conversions
        builder.Property(c => c.CustomerId)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .HasColumnName("customer_id");
        
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value) // Assumes valid data in DB
            .IsRequired()
            .HasMaxLength(320)
            .HasColumnName("email");
        
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("ix_customers_email");
        
        // Complex type for Address
        builder.OwnsOne(c => c.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasMaxLength(200).HasColumnName("address_street");
            addressBuilder.Property(a => a.City).HasMaxLength(100).HasColumnName("address_city");
            // ... other address properties
        });
        
        // JSON column for Preferences
        builder.Property(c => c.Preferences)
            .HasConversion(
                preferences => JsonSerializer.Serialize(preferences, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, object>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnName("preferences");
        
        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
```

### Web API Guidelines

**Controller Design**:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CustomersController> _logger;
    
    public CustomersController(IMediator mediator, ILogger<CustomersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Create a new customer.
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateCustomerCommand
        {
            Name = request.Name,
            Email = request.Email,
            Preferences = request.Preferences
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        return result.Match<ActionResult<CustomerResponse>>(
            customer => CreatedAtAction(
                nameof(GetCustomer),
                new { customerId = customer.CustomerId.Value },
                CustomerResponse.FromDomain(customer)),
            error => BadRequest(error));
    }
}
```

## Testing Guidelines

### 📚 Comprehensive Testing Strategy

For complete testing patterns including unit tests, integration tests, test organization, and best practices, see:
- **[Testing Strategy](patterns/testing-strategy.md)** - Complete testing implementation guide with examples

### Test Structure Overview
```
tests/
├── Domain.UnitTests/
│   ├── Entities/
│   │   ├── CustomerTests.cs          # Customer aggregate tests
│   │   └── OrderTests.cs             # Order aggregate tests
│   ├── ValueObjects/
│   │   ├── EmailTests.cs             # Email value object tests
│   │   ├── MoneyTests.cs             # Money value object tests
│   │   └── AddressTests.cs           # Address value object tests
│   └── Shared/
│       ├── EntityTests.cs            # Base entity tests
│       └── ValueObjectTests.cs       # Base value object tests
└── Application.UnitTests/
    ├── Commands/
    │   ├── CreateCustomerCommandHandlerTests.cs
    │   └── CreateOrderCommandHandlerTests.cs
    └── Queries/
        └── GetCustomerOrdersQueryHandlerTests.cs
```

### Unit Test Examples

**Domain Entity Tests**:
```csharp
public class CustomerTests
{
    [Fact]
    public void Create_Should_GenerateCustomerCreatedEvent_When_ValidData()
    {
        // Arrange
        var customerId = CustomerId.New();
        var name = "John Doe";
        var email = Email.Create("john.doe@example.com").Value;
        
        // Act
        var customer = Customer.Create(customerId, name, email);
        
        // Assert
        customer.Should().NotBeNull();
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreated>()
            .Which.CustomerId.Should().Be(customerId);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Should_ThrowArgumentException_When_NameIsInvalid(string invalidName)
    {
        // Arrange
        var customerId = CustomerId.New();
        var email = Email.Create("john.doe@example.com").Value;
        
        // Act & Assert
        var action = () => Customer.Create(customerId, invalidName, email);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name is required*")
            .And.ParamName.Should().Be("name");
    }
}
```

**Value Object Tests**:
```csharp
public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("valid.email@test-domain.com")]
    public void Create_Should_ReturnSuccess_When_EmailIsValid(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(validEmail.ToLowerInvariant());
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void Create_Should_ReturnFailure_When_EmailIsInvalid(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
    
    [Fact]
    public void Equals_Should_ReturnTrue_When_EmailsAreEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("TEST@EXAMPLE.COM").Value;
        
        // Act & Assert
        email1.Should().Be(email2);
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }
}
```

**Command Handler Tests**:
```csharp
public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ILogger<CreateCustomerCommandHandler>> _loggerMock;
    private readonly CreateCustomerCommandHandler _handler;
    
    public CreateCustomerCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _loggerMock = new Mock<ILogger<CreateCustomerCommandHandler>>();
        _handler = new CreateCustomerCommandHandler(_customerRepositoryMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task Handle_Should_ReturnSuccess_When_ValidCommand()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Preferences = new Dictionary<string, object> { { "newsletter", true } }
        };
        
        _customerRepositoryMock.Setup(r => r.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(command.Name);
        result.Value!.Email.Value.Should().Be(command.Email.ToLowerInvariant());
        
        _customerRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CustomerAlreadyExists()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };
        
        var existingCustomer = Customer.Create(
            CustomerId.New(),
            "Existing Customer",
            Email.Create(command.Email).Value);
        
        _customerRepositoryMock.Setup(r => r.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
        
        _customerRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

## Development Tools

### IDE Configuration

**Visual Studio Code** (`/.vscode/settings.json`):
```json
{
    "dotnet.defaultSolution": "CleanArchitecture.sln",
    "omnisharp.enableEditorConfigSupport": true,
    "omnisharp.enableRoslynAnalyzers": true,
    "files.exclude": {
        "**/bin": true,
        "**/obj": true
    },
    "dotnet-test-explorer.testProjectPath": "tests/**/*.csproj"
}
```

**Visual Studio Code** (`/.vscode/launch.json`):
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch WebApi",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/WebApi/bin/Debug/net8.0/WebApi.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/WebApi",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

### Development Commands

**Code Formatting**:
```bash
# Format all code
make format

# Check formatting
dotnet format --verify-no-changes

# Format specific files
dotnet format src/Domain/
```

**Code Analysis**:
```bash
# Run static analysis
make lint

# Run security analysis
make security

# Check for outdated packages
dotnet list package --outdated
```

**Database Operations**:
```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Add migration (if using real database)
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/WebApi

# Update database
dotnet ef database update --project src/Infrastructure --startup-project src/WebApi
```

**Package Management**:
```bash
# Check for outdated packages
dotnet outdated

# List all packages with versions
dotnet list package

# Check for vulnerable packages
dotnet list package --vulnerable
```

**Diagnostic Tools**:
```bash
# Monitor performance counters (while app is running)
dotnet counters monitor --process-id <pid>
dotnet counters monitor --name WebApi

# Collect memory dump
dotnet dump collect --process-id <pid>
dotnet dump analyze dump.dmp

# Collect performance trace
dotnet trace collect --process-id <pid>
dotnet trace collect --name WebApi --duration 00:00:30
```

**C# Scripting**:
```bash
# Run C# script files
dotnet script script.csx

# Interactive C# REPL
dotnet script
```

## Performance Considerations

### Async Best Practices
```csharp
// Good: Use ConfigureAwait(false) in libraries
public async Task<Customer> GetCustomerAsync(CustomerId id)
{
    return await _repository.FindByIdAsync(id).ConfigureAwait(false);
}

// Good: Use cancellation tokens
public async Task<Customer> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken)
{
    return await _repository.FindByIdAsync(id, cancellationToken);
}

// Bad: Blocking async code
public Customer GetCustomer(CustomerId id)
{
    return _repository.FindByIdAsync(id).Result;  // Don't do this!
}
```

### Memory Management
```csharp
// Good: Use using statements for disposable resources
public async Task<byte[]> GenerateReportAsync()
{
    using var stream = new MemoryStream();
    // ... generate report ...
    return stream.ToArray();
}

// Good: Avoid unnecessary allocations
private readonly StringBuilder _stringBuilder = new();

public string FormatCustomerName(string firstName, string lastName)
{
    _stringBuilder.Clear();
    _stringBuilder.Append(firstName);
    _stringBuilder.Append(' ');
    _stringBuilder.Append(lastName);
    return _stringBuilder.ToString();
}
```

### Database Performance
```csharp
// Good: Use compiled queries for frequently executed queries
private static readonly Func<ApplicationDbContext, CustomerId, Task<Customer?>> GetCustomerById =
    EF.CompileAsyncQuery((ApplicationDbContext context, CustomerId id) =>
        context.Customers.FirstOrDefault(c => c.CustomerId == id));

// Good: Use AsNoTracking for read-only queries
public async Task<IEnumerable<Customer>> GetCustomersAsync()
{
    return await _context.Customers
        .AsNoTracking()
        .ToListAsync();
}
```

## Security Guidelines

### Input Validation
```csharp
// Good: Validate at multiple layers
public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}

// Additional validation in domain
public static Result<Email> Create(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return Result<Email>.Failure("Email is required");
    
    if (!IsValidEmailFormat(email))
        return Result<Email>.Failure("Invalid email format");
    
    return Result<Email>.Success(new Email(email));
}
```

### Sensitive Data Handling
```csharp
// Good: Don't log sensitive data
_logger.LogInformation("Customer created with ID {CustomerId}", customer.CustomerId);

// Bad: Don't log sensitive information
_logger.LogInformation("Customer created: {@Customer}", customer); // Contains email, etc.

// Good: Use structured logging with safe data
_logger.LogInformation("Customer created {CustomerId} with email domain {EmailDomain}", 
    customer.CustomerId, 
    customer.Email.Value.Split('@')[1]);
```

## Common Pitfalls

### Async/Await Issues
```csharp
// Bad: Async void (except for event handlers)
public async void ProcessCustomer(Customer customer)
{
    await _repository.SaveAsync(customer);
}

// Good: Async Task
public async Task ProcessCustomerAsync(Customer customer)
{
    await _repository.SaveAsync(customer);
}

// Bad: Unnecessary async/await
public async Task<Customer> GetCustomerAsync(CustomerId id)
{
    return await _repository.FindByIdAsync(id);
}

// Good: Return Task directly when no processing needed
public Task<Customer> GetCustomerAsync(CustomerId id)
{
    return _repository.FindByIdAsync(id);
}
```

### Entity Framework Pitfalls
```csharp
// Bad: N+1 query problem
public async Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync()
{
    var customers = await _context.Customers.ToListAsync();
    foreach (var customer in customers)
    {
        customer.Orders = await _context.Orders.Where(o => o.CustomerId == customer.CustomerId).ToListAsync();
    }
    return customers;
}

// Good: Include related data
public async Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync()
{
    return await _context.Customers
        .Include(c => c.Orders)
        .ToListAsync();
}
```

## Learning Resources

### Books
- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** by Eric Evans
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **C# 12 and .NET 8** by Mark J. Price

### Online Resources
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)

### Courses
- [Clean Architecture with ASP.NET Core](https://www.pluralsight.com/courses/clean-architecture-asp-dot-net-core)
- [Domain-Driven Design in Practice](https://www.pluralsight.com/courses/domain-driven-design-in-practice)

---

Follow these guidelines to maintain high code quality and contribute effectively to the Clean Architecture .NET 8 project.