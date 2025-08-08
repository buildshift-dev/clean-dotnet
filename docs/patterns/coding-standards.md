# Coding Standards - .NET 8 Clean Architecture

## Overview

This document defines the coding standards and conventions for the Clean Architecture .NET 8 project. Following these standards ensures code consistency, readability, and maintainability across the entire codebase.

## General Principles

1. **Consistency**: Follow established patterns throughout the codebase
2. **Readability**: Code should be self-documenting and easy to understand
3. **Maintainability**: Write code that's easy to modify and extend
4. **Performance**: Consider performance implications of coding choices
5. **Security**: Follow secure coding practices

## C# Language Standards

### Naming Conventions

```csharp
// Classes, Methods, Properties - PascalCase
public class CustomerService
{
    public string CustomerName { get; set; }
    
    public void ProcessCustomerOrder() { }
}

// Local variables, parameters - camelCase
public void ProcessOrder(string customerName, int orderId)
{
    var orderService = new OrderService();
    bool isProcessed = false;
}

// Constants - PascalCase
public const string DefaultCurrency = "USD";
public static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

// Private fields - camelCase with underscore prefix
public class CustomerRepository
{
    private readonly ILogger<CustomerRepository> _logger;
    private readonly ApplicationDbContext _context;
    private static readonly string _connectionString = "...";
}

// Interfaces - PascalCase with "I" prefix
public interface ICustomerRepository
{
    Task<Customer> GetByIdAsync(CustomerId id);
}

// Enums - PascalCase for type and values
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

### File and Namespace Organization

```csharp
// One class per file, file name matches class name
// CustomerService.cs
namespace Domain.Services;

public class CustomerService
{
    // Implementation
}

// Namespace structure follows folder structure
// Domain/Entities/Customer.cs
namespace Domain.Entities;

// Application/Commands/CreateCustomer/CreateCustomerCommand.cs
namespace Application.Commands.CreateCustomer;
```

### Code Formatting

```csharp
// Brace placement - Opening braces on same line
public class Customer {
    public string Name { get; set; }
    
    public void UpdateName(string newName) {
        if (string.IsNullOrEmpty(newName))
            throw new ArgumentException("Name cannot be empty");
        
        Name = newName;
    }
}

// Method parameters - Break long parameter lists
public async Task<Result<OrderDto>> CreateOrderAsync(
    CustomerId customerId,
    List<OrderItemDto> items,
    ShippingAddress shippingAddress,
    string? specialInstructions,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// LINQ formatting - Each clause on new line for complex queries
var activeCustomers = await _context.Customers
    .Where(c => c.IsActive)
    .Where(c => c.CreatedAt >= startDate)
    .OrderBy(c => c.Name)
    .Select(c => new CustomerDto
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email.Value
    })
    .ToListAsync(cancellationToken);
```

### Documentation Standards

```csharp
/// <summary>
/// Represents a customer in the e-commerce system.
/// </summary>
/// <remarks>
/// Customers are the primary actors who place orders and maintain account information.
/// This is an aggregate root in the domain model.
/// </remarks>
public class Customer : AggregateRoot<CustomerId>
{
    /// <summary>
    /// Gets the customer's email address.
    /// </summary>
    /// <value>A valid email address as a value object.</value>
    public Email Email { get; private set; }
    
    /// <summary>
    /// Updates the customer's email address.
    /// </summary>
    /// <param name="newEmail">The new email address to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newEmail"/> is null.</exception>
    /// <exception cref="BusinessRuleViolationException">Thrown when the email is already in use.</exception>
    public void UpdateEmail(Email newEmail)
    {
        // Implementation
    }
}
```

## Architecture-Specific Standards

### Domain Layer

```csharp
// Entities - Rich behavior, encapsulation
public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLineItem> _lineItems = new();
    
    // Private setters for encapsulation
    public CustomerId CustomerId { get; private set; }
    public Money TotalAmount { get; private set; }
    
    // Business methods with proper validation
    public void AddLineItem(ProductId productId, string productName, Money unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new BusinessRuleViolationException("Quantity must be positive");
        
        var lineItem = new OrderLineItem(OrderLineItemId.Create(), productId, productName, unitPrice, quantity);
        _lineItems.Add(lineItem);
        RecalculateTotal();
        
        AddDomainEvent(new OrderLineItemAdded(Id, lineItem.Id));
    }
    
    // Private methods for internal logic
    private void RecalculateTotal()
    {
        TotalAmount = _lineItems
            .Select(li => li.GetTotalPrice())
            .Aggregate(Money.Zero("USD"), (sum, price) => sum.Add(price));
    }
}

// Value Objects - Immutable, self-validating
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);
    
    public string Value { get; }
    
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        
        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));
        
        Value = value.ToLowerInvariant();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
}

// Domain Services - Stateless, complex business logic
public class OrderPricingService : IDomainService
{
    public Money CalculateDiscount(Customer customer, Order order)
    {
        // Complex pricing logic that doesn't belong in entities
        return customer.Tier switch
        {
            CustomerTier.Gold => order.TotalAmount.Multiply(0.1m),
            CustomerTier.Silver => order.TotalAmount.Multiply(0.05m),
            _ => Money.Zero(order.TotalAmount.Currency)
        };
    }
}
```

### Application Layer

```csharp
// Commands - Immutable, data containers
public record CreateCustomerCommand : IRequest<Result<CustomerDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public AddressDto? Address { get; init; }
    public Dictionary<string, object>? Preferences { get; init; }
}

// Command Handlers - Single responsibility, async
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    
    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IMapper mapper,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validation
            var email = new Email(request.Email);
            
            if (await _customerRepository.EmailExistsAsync(email, cancellationToken))
            {
                return Result<CustomerDto>.Failure($"Email {request.Email} is already in use");
            }
            
            // Business logic
            var customer = new Customer(
                CustomerId.Create(),
                request.Name,
                email,
                CreateAddress(request.Address),
                request.Preferences);
            
            // Persistence
            await _customerRepository.AddAsync(customer, cancellationToken);
            
            _logger.LogInformation("Customer created with ID {CustomerId}", customer.Id);
            
            // Response
            var dto = _mapper.Map<CustomerDto>(customer);
            return Result<CustomerDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<CustomerDto>.Failure($"Validation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Result<CustomerDto>.Failure("An error occurred while creating the customer");
        }
    }
    
    private static Address? CreateAddress(AddressDto? addressDto)
    {
        return addressDto == null ? null : new Address(
            addressDto.Street,
            addressDto.City,
            addressDto.State,
            addressDto.PostalCode,
            addressDto.Country);
    }
}

// Validators - FluentValidation
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
        
        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.Street).NotEmpty().WithMessage("Street is required");
            RuleFor(x => x.Address!.City).NotEmpty().WithMessage("City is required");
        });
    }
}
```

### Infrastructure Layer

```csharp
// Repository Implementation
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerRepository> _logger;
    
    public CustomerRepository(
        ApplicationDbContext context,
        ILogger<CustomerRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting customer by ID {CustomerId}", id);
        
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        
        _logger.LogInformation("Adding new customer with ID {CustomerId}", customer.Id);
        
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        
        return customer;
    }
}

// Entity Framework Configuration
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .ValueGeneratedNever();
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(256);
            
            email.HasIndex(e => e.Value).IsUnique();
        });
        
        builder.Ignore(c => c.DomainEvents);
    }
}
```

### Web API Layer

```csharp
// Controllers - Thin, delegation to MediatR
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CustomersController> _logger;
    
    public CustomersController(
        IMediator mediator,
        ILogger<CustomersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Customer creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer.</returns>
    /// <response code="201">Customer created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand
        {
            Name = request.Name,
            Email = request.Email,
            Address = request.Address,
            Preferences = request.Preferences
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = result.Value!.Id },
                result.Value);
        }
        
        return BadRequest(new ProblemDetails
        {
            Title = "Customer creation failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }
}
```

## Error Handling Standards

```csharp
// Domain Exceptions
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
    
    public BusinessRuleViolationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// Result Pattern
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// Global Exception Handling
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            BusinessRuleViolationException ex => new ProblemDetails
            {
                Title = "Business Rule Violation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            },
            ResourceNotFoundException ex => new ProblemDetails
            {
                Title = "Resource Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            },
            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            }
        };
        
        context.Response.StatusCode = problemDetails.Status ?? 500;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

## Testing Standards

```csharp
// Unit Test Naming: Method_Scenario_ExpectedResult
public class CustomerTests
{
    [Fact]
    public void UpdateEmail_ValidEmail_ShouldUpdateEmailAndRaiseEvent()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var newEmail = new Email("newemail@example.com");
        
        // Act
        customer.UpdateEmail(newEmail);
        
        // Assert
        customer.Email.Should().Be(newEmail);
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerEmailChanged>();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    public void Email_Constructor_InvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        Action act = () => new Email(invalidEmail);
        act.Should().Throw<ArgumentException>();
    }
    
    private static Customer CreateValidCustomer()
    {
        return new Customer(
            CustomerId.Create(),
            "John Doe",
            new Email("john@example.com"));
    }
}

// Integration Tests
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
    }
}
```

## Performance Standards

```csharp
// Async/Await - All I/O operations must be async
public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
{
    return await _context.Customers
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}

// ConfigureAwait(false) for library code
public async Task<Result> ProcessAsync()
{
    var data = await GetDataAsync().ConfigureAwait(false);
    return ProcessData(data);
}

// Memory optimization - Use Span<T> for temporary data
public ReadOnlySpan<char> ExtractDomain(ReadOnlySpan<char> email)
{
    var atIndex = email.IndexOf('@');
    return atIndex > 0 ? email.Slice(atIndex + 1) : ReadOnlySpan<char>.Empty;
}

// Database queries - Always use cancellation tokens
public async Task<List<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken)
{
    return await _context.Customers
        .Where(c => c.IsActive)
        .AsNoTracking() // For read-only queries
        .ToListAsync(cancellationToken);
}
```

## Security Standards

```csharp
// Input validation - Always validate user input
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 100)
            .Matches("^[a-zA-Z\\s]+$").WithMessage("Name can only contain letters and spaces");
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 256);
    }
}

// Sensitive data - Never log sensitive information
public async Task<Result> ProcessPaymentAsync(PaymentInfo payment)
{
    _logger.LogInformation("Processing payment for order {OrderId}", payment.OrderId);
    // Never log payment.CreditCardNumber or payment.CVV
    
    var result = await _paymentGateway.ProcessAsync(payment);
    
    if (result.IsSuccess)
        _logger.LogInformation("Payment processed successfully for order {OrderId}", payment.OrderId);
    else
        _logger.LogWarning("Payment failed for order {OrderId}: {Error}", payment.OrderId, result.Error);
    
    return result;
}

// SQL Injection prevention - Use parameterized queries
public async Task<List<Customer>> SearchCustomersAsync(string searchTerm)
{
    // EF Core automatically parameterizes this
    return await _context.Customers
        .Where(c => c.Name.Contains(searchTerm) || c.Email.Value.Contains(searchTerm))
        .ToListAsync();
}
```

## Code Review Checklist

### Domain Layer
- [ ] Entities have private setters and business methods
- [ ] Value objects are immutable and self-validating
- [ ] Domain events are raised for significant state changes
- [ ] Business rules are enforced within aggregates
- [ ] No dependencies on infrastructure concerns

### Application Layer
- [ ] Command/Query handlers have single responsibility
- [ ] All I/O operations are async with cancellation tokens
- [ ] Proper error handling with Result pattern
- [ ] Input validation using FluentValidation
- [ ] Logging at appropriate levels

### Infrastructure Layer
- [ ] Repository implementations follow interfaces
- [ ] Entity Framework configurations are complete
- [ ] Database queries are optimized
- [ ] Connection strings are externalized
- [ ] Proper disposal of resources

### Web API Layer
- [ ] Controllers are thin and delegate to MediatR
- [ ] Proper HTTP status codes
- [ ] API documentation with XML comments
- [ ] Input validation attributes
- [ ] Consistent error response format

### General
- [ ] Code follows naming conventions
- [ ] Methods are focused and not too long
- [ ] Proper separation of concerns
- [ ] Unit tests for business logic
- [ ] No hardcoded values

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / C# 12*
*Status: Coding Standards Guide*