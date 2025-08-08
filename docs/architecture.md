# Architecture Overview - Clean Architecture .NET 8

## Introduction

This document provides a high-level architectural overview of the Clean Architecture .NET 8 implementation. For detailed patterns and implementation guides, refer to the comprehensive patterns documentation in the `/docs/patterns/` directory.

**📚 Related Documentation:**
- [Clean Architecture Patterns](patterns/clean-architecture.md) - Complete implementation guide with code examples
- [Domain-Driven Design](patterns/domain-driven-design.md) - DDD patterns in .NET
- [CQRS Patterns](patterns/cqrs-patterns.md) - Command/Query implementation with MediatR
- [Implementation Roadmap](patterns/implementation-roadmap.md) - Step-by-step development plan
- [Coding Standards](patterns/coding-standards.md) - .NET coding conventions and best practices

## Architecture Overview

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│              Presentation               │
│           (WebApi Layer)                │
├─────────────────────────────────────────┤
│             Application                 │
│     (Use Cases & Orchestration)         │
├─────────────────────────────────────────┤
│            Infrastructure               │
│     (Data Access & External APIs)       │
├─────────────────────────────────────────┤
│               Domain                    │
│          (Business Logic)               │
└─────────────────────────────────────────┘
```

### Dependency Flow

- **Outer layers** depend on **inner layers**
- **Inner layers** are **independent** of outer layers
- **SharedKernel** has no dependencies (shared foundation)
- **Domain** depends only on **SharedKernel**
- **Application** depends on **Domain** and **SharedKernel**
- **Infrastructure** depends on **Domain**, **Application**, and **SharedKernel**
- **WebApi** depends on **Application** (and transitively on **Domain** and **SharedKernel**)

## Layer Responsibilities

### Domain Layer (`src/Domain/`)

**Purpose**: Contains enterprise-wide business rules and entities.

**Responsibilities**:
- Core business entities (`Customer`, `Order`)
- Value objects (`Money`, `Email`, `Address`)
- Domain events (`CustomerCreated`, `OrderCreated`)
- Repository interfaces (`ICustomerRepository`, `IOrderRepository`)
- Domain exceptions (`BusinessRuleViolationException`)
- Business rule validation

**Dependencies**: None (pure business logic)

**Key Files**:
```
Domain/
├── Entities/
│   ├── Customer.cs          # Customer aggregate root
│   └── Order.cs             # Order aggregate root
├── ValueObjects/
│   ├── Money.cs             # Currency and amount with operations
│   ├── Email.cs             # Email with validation
│   ├── Address.cs           # Physical address
│   ├── CustomerId.cs        # Strongly-typed customer ID
│   └── OrderId.cs           # Strongly-typed order ID
├── Events/
│   ├── CustomerCreated.cs   # Domain event for customer creation
│   └── OrderCreated.cs      # Domain event for order creation
├── Repositories/
│   ├── ICustomerRepository.cs
│   └── IOrderRepository.cs
└── Exceptions/
    ├── DomainException.cs
    └── BusinessRuleViolationException.cs
```

### Application Layer (`src/Application/`)

**Purpose**: Contains application-specific business rules and use cases.

**Responsibilities**:
- Command and query handlers (CQRS)
- Application services and orchestration
- DTOs and mapping logic
- Validation logic
- Transaction management
- Application-specific exceptions

**Dependencies**: Domain layer only

**Key Patterns**:
- **CQRS**: Separate command and query models
- **MediatR**: Mediator pattern for decoupling
- **Result Pattern**: Functional error handling

**Key Files**:
```
Application/
├── Commands/
│   ├── CreateCustomer/
│   │   ├── CreateCustomerCommand.cs
│   │   └── CreateCustomerCommandHandler.cs
│   └── CreateOrder/
│       ├── CreateOrderCommand.cs
│       └── CreateOrderCommandHandler.cs
├── Queries/
│   └── GetCustomerOrders/
│       ├── GetCustomerOrdersQuery.cs
│       └── GetCustomerOrdersQueryHandler.cs
└── Common/
    ├── Result.cs            # Result pattern implementation
    └── IApplicationDbContext.cs
```

### Infrastructure Layer (`src/Infrastructure/`)

**Purpose**: Contains implementations of external concerns.

**Responsibilities**:
- Data access implementations
- External API integrations
- File system operations
- Email services
- Logging implementations
- Configuration management

**Dependencies**: Domain and Application layers

**Key Components**:
- **Entity Framework Core**: ORM for data persistence
- **Repository Pattern**: Concrete implementations
- **Database Context**: EF Core DbContext
- **External Services**: Third-party API integrations

**Key Files**:
```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs      # EF Core context
│   ├── Repositories/
│   │   ├── CustomerRepository.cs    # Customer repository implementation
│   │   └── OrderRepository.cs       # Order repository implementation
│   └── Configurations/
│       ├── CustomerConfiguration.cs # EF entity configuration
│       └── OrderConfiguration.cs    # EF entity configuration
└── DependencyInjection.cs         # Service registration
```

### SharedKernel Layer (`src/SharedKernel/`)

**Purpose**: Contains shared base classes and common utilities used across all layers.

**Responsibilities**:
- Base classes for entities, aggregates, and value objects
- Common interfaces and abstractions
- Shared result patterns and utilities
- Domain event infrastructure
- Repository and unit of work contracts

**Dependencies**: None (shared foundation)

**Key Components**:
- **Base Types**: AggregateRoot, Entity, ValueObject base classes
- **Result Pattern**: Functional error handling infrastructure  
- **Domain Events**: Event publishing and handling infrastructure
- **Common Utilities**: PagedResult, IDateTime, repository interfaces

**Key Files**:
```
SharedKernel/
├── Common/
│   ├── Result.cs                # Result pattern implementation
│   ├── PagedResult.cs          # Paged query results
│   └── IDateTime.cs            # DateTime abstraction
├── Domain/
│   ├── BaseTypes/
│   │   ├── AggregateRoot.cs    # Base aggregate root
│   │   ├── Entity.cs           # Base entity class
│   │   └── ValueObject.cs      # Base value object
│   ├── Events/
│   │   ├── DomainEvent.cs      # Base domain event
│   │   └── IDomainEvent.cs     # Domain event interface
│   ├── Exceptions/
│   │   ├── DomainException.cs  # Base domain exception
│   │   └── BusinessRuleViolationException.cs
│   └── Repositories/
│       ├── IRepository.cs      # Base repository interface
│       └── IUnitOfWork.cs      # Unit of work pattern
```

### Presentation Layer (`src/WebApi/`)

**Purpose**: Contains the user interface and external interfaces.

**Responsibilities**:
- HTTP API endpoints
- Request/response models
- Authentication and authorization
- API documentation
- Error handling middleware
- Dependency injection configuration

**Dependencies**: Application layer (and transitively Domain)

**Key Components**:
- **Controllers**: RESTful API endpoints
- **Models**: Request/Response DTOs
- **Middleware**: Cross-cutting concerns
- **Swagger**: API documentation

**Key Files**:
```
WebApi/
├── Controllers/
│   ├── CustomersController.cs
│   └── OrdersController.cs
├── Models/
│   ├── Requests/
│   │   ├── CreateCustomerRequest.cs
│   │   └── CreateOrderRequest.cs
│   └── Responses/
│       ├── CustomerResponse.cs
│       └── OrderResponse.cs
├── Program.cs              # Application startup
└── appsettings.json        # Configuration
```

## Design Patterns

### 1. Aggregate Pattern

**Purpose**: Maintain consistency boundaries and encapsulate business rules.

**Implementation**:
```csharp
public class Customer : AggregateRoot
{
    public CustomerId CustomerId { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    
    // Factory method ensures valid state
    public static Customer Create(CustomerId id, string name, Email email)
    {
        var customer = new Customer(id, name, email);
        customer.AddDomainEvent(new CustomerCreated(id, name, email.Value));
        return customer;
    }
}
```

**Benefits**:
- Encapsulation of business rules
- Consistency boundary enforcement
- Domain event publishing

### 2. Value Object Pattern

**Purpose**: Represent concepts that have no identity but have important characteristics.

**Implementation**:
```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(Amount + other.Amount, Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**Benefits**:
- Immutability by design
- Rich behavior instead of primitive obsession
- Validation encapsulation

### 3. Repository Pattern

**Purpose**: Abstract data access and provide a collection-like interface.

**Interface** (Domain):
```csharp
public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task SaveAsync(Customer customer, CancellationToken cancellationToken);
    Task<IEnumerable<Customer>> ListAllAsync(CancellationToken cancellationToken);
}
```

**Implementation** (Infrastructure):
```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id, cancellationToken);
    }
}
```

**Benefits**:
- Testability through interface contracts
- Database independence
- Centralized query logic

### 4. CQRS Pattern

**Purpose**: Separate read and write models for better performance and scalability.

**Command Example**:
```csharp
public record CreateCustomerCommand
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Dictionary<string, object>? Preferences { get; init; }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Customer>>
{
    public async Task<Result<Customer>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Validation and business logic
        // Return Result<Customer>
    }
}
```

**Query Example**:
```csharp
public record GetCustomerOrdersQuery
{
    public Guid CustomerId { get; init; }
}

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<IEnumerable<Order>>>
{
    // Query implementation
}
```

**Benefits**:
- Optimized read and write models
- Scalability through separation
- Clear intent and responsibility

### 5. Result Pattern

**Purpose**: Handle errors functionally without exceptions.

**Implementation**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

**Benefits**:
- Explicit error handling
- Functional programming approach
- Better composability

## Data Flow

### Command Flow (Write Operations)

```
1. Controller receives HTTP request
2. Maps request to Command
3. Sends Command via MediatR
4. CommandHandler processes business logic
5. Uses Repository to persist changes
6. Returns Result to Controller
7. Controller maps to HTTP response
```

### Query Flow (Read Operations)

```
1. Controller receives HTTP request
2. Maps request to Query
3. Sends Query via MediatR
4. QueryHandler retrieves data
5. Uses Repository for data access
6. Returns Result to Controller
7. Controller maps to HTTP response
```

### Domain Event Flow

```
1. Domain operation triggers event
2. Event added to AggregateRoot
3. Repository saves changes
4. Events published via MediatR
5. Event handlers process side effects
```

## Testing Strategy

### Unit Test Structure

**Domain Tests**:
- Entity behavior and business rules
- Value object validation and operations
- Domain event generation

**Application Tests**:
- Command and query handler logic
- Business rule enforcement
- Error handling scenarios

**Integration Tests**:
- API endpoint behavior
- Database integration
- End-to-end scenarios

### Testing Patterns

**Arrange-Act-Assert**:
```csharp
[Fact]
public void Customer_Create_Should_GenerateCustomerCreatedEvent()
{
    // Arrange
    var id = CustomerId.New();
    var email = Email.Create("test@example.com").Value;
    
    // Act
    var customer = Customer.Create(id, "John Doe", email);
    
    // Assert
    customer.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<CustomerCreated>();
}
```

## Deployment Architecture

### Local Development
- **Database**: EF Core InMemory
- **Logging**: Console with Serilog
- **HTTPS**: Development certificates

### Production (AWS ECS Fargate)
- **Database**: SQL Server or PostgreSQL RDS
- **Logging**: CloudWatch via Serilog
- **Load Balancing**: Application Load Balancer
- **Container Orchestration**: ECS Fargate
- **Infrastructure**: CloudFormation

## Performance Considerations

### Async/Await Pattern
```csharp
public async Task<Result<Customer>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
{
    // All I/O operations use async/await
    var existingCustomer = await _customerRepository.FindByEmailAsync(email, cancellationToken);
    
    if (existingCustomer != null)
        return Result<Customer>.Failure("Customer already exists");
        
    await _customerRepository.SaveAsync(customer, cancellationToken);
    return Result<Customer>.Success(customer);
}
```

### Entity Framework Optimization
- **Explicit Loading**: Avoid N+1 queries
- **Change Tracking**: Disable for read-only queries
- **Connection Pooling**: Configured in startup
- **Query Compilation**: EF Core compiled queries

### Caching Strategy
- **Application Level**: Memory cache for reference data
- **Distributed Cache**: Redis for shared data
- **HTTP Caching**: Response caching for stable endpoints

## Configuration Management

### Environment-Specific Settings
- **Development**: `appsettings.Development.json`
- **Production**: Environment variables and AWS Parameter Store
- **Staging**: `appsettings.Staging.json`

### Secret Management
- **Local**: User secrets for development
- **Production**: AWS Secrets Manager
- **CI/CD**: Environment variables in build pipeline

## Security Considerations

### Input Validation
- **Domain Level**: Value object validation
- **Application Level**: Command validation
- **API Level**: Model binding validation

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication
- **Role-Based**: Authorization policies
- **API Keys**: External service authentication

### Data Protection
- **Encryption**: Sensitive data at rest
- **HTTPS**: All communications encrypted
- **SQL Injection**: Parameterized queries via EF Core

## Advanced Topics

For detailed implementation guidance, explore our comprehensive patterns documentation:

### Core Architecture
- **[Clean Architecture Patterns](patterns/clean-architecture.md)** - Complete implementation with .NET 8 examples
- **[Domain-Driven Design](patterns/domain-driven-design.md)** - Rich domain models, aggregates, value objects
- **[Shared Kernel Guide](patterns/shared-kernel-guide.md)** - Common base classes and utilities

### Application Patterns
- **[CQRS Implementation](patterns/cqrs-patterns.md)** - MediatR-based command/query separation
- **[Testing Strategy](patterns/testing-strategy.md)** - Comprehensive testing approach
- **[Implementation Roadmap](patterns/implementation-roadmap.md)** - Step-by-step development plan

### Infrastructure & Security
- **[AWS Logging Patterns](patterns/aws-logging-patterns.md)** - CloudWatch integration and monitoring
- **[AWS JWT Security](patterns/aws-jwt-security.md)** - Cognito integration and security patterns
- **[Security Patterns](patterns/security-patterns.md)** - Authentication, authorization, and data protection

### Development Guidelines
- **[Coding Standards](patterns/coding-standards.md)** - .NET 8 conventions and best practices

## External Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://domainlanguage.com/ddd/)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://vaughnvernon.co/)
- [.NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)