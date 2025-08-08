# Clean Architecture - .NET 8 Implementation

## Overview
This document details the architecture patterns and design decisions for the Clean Architecture .NET 8 implementation, emphasizing Domain-Driven Design (DDD) and Clean Architecture principles with Entity Framework Core and flexible storage options.

## Core Architecture Principles

### 1. Domain-Driven Design (DDD)
The domain model captures the essential business concepts and rules, independent of technical implementation details. All business logic resides in the domain layer.

### 2. Clean Architecture
Dependencies flow inward: WebApi → Application → Domain. The domain layer remains pure with no external dependencies.

### 3. Repository Pattern
Abstracts data persistence behind interfaces, allowing infrastructure flexibility and testability.

### 4. Dependency Injection
Loose coupling through interface-based dependencies resolved at runtime using .NET's built-in DI container.

### 5. CQRS Pattern
Command Query Responsibility Segregation for clear separation of read and write operations.

## Domain Model Design

### Core Aggregates

#### Customer Aggregate (Aggregate Root)
```csharp
// Domain/Entities/Customer.cs
namespace Domain.Entities;

public class Customer : AggregateRoot<CustomerId>
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    public Customer(
        CustomerId id,
        string name,
        Email email,
        Address? address = null,
        Dictionary<string, object>? preferences = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Address = address;
        Preferences = preferences ?? new Dictionary<string, object>();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CustomerCreated(Id, Name, Email, CreatedAt));
    }
    
    public CustomerId Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public Address? Address { get; private set; }
    public bool IsActive { get; private set; }
    public Dictionary<string, object> Preferences { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void UpdateEmail(Email newEmail)
    {
        if (newEmail == null)
            throw new ArgumentNullException(nameof(newEmail));
            
        var oldEmail = Email;
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CustomerEmailChanged(Id, oldEmail, newEmail, UpdatedAt));
    }
    
    public void UpdateAddress(Address newAddress)
    {
        Address = newAddress ?? throw new ArgumentNullException(nameof(newAddress));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CustomerAddressChanged(Id, newAddress, UpdatedAt));
    }
    
    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolationException("Customer is already deactivated");
            
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CustomerDeactivated(Id, UpdatedAt));
    }
    
    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

#### Order Aggregate (Aggregate Root)
```csharp
// Domain/Entities/Order.cs
namespace Domain.Entities;

public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLineItem> _lineItems = new();
    private readonly List<DomainEvent> _domainEvents = new();
    
    public Order(
        OrderId id,
        CustomerId customerId,
        ShippingAddress shippingAddress,
        Dictionary<string, object>? details = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        Details = details ?? new Dictionary<string, object>();
        Status = OrderStatus.Pending;
        OrderDate = DateTime.UtcNow;
        
        AddDomainEvent(new OrderCreated(Id, CustomerId, OrderDate));
    }
    
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? ShippedDate { get; private set; }
    public DateTime? DeliveredDate { get; private set; }
    public Dictionary<string, object> Details { get; private set; }
    
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AddLineItem(
        ProductId productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleViolationException("Cannot add items to non-pending order");
            
        var lineItem = new OrderLineItem(
            OrderLineItemId.Create(),
            productId,
            productName,
            unitPrice,
            quantity
        );
        
        _lineItems.Add(lineItem);
        RecalculateTotalAmount();
        
        AddDomainEvent(new OrderLineItemAdded(Id, lineItem.Id, productId, quantity));
    }
    
    public void RemoveLineItem(OrderLineItemId lineItemId)
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleViolationException("Cannot remove items from non-pending order");
            
        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem == null)
            throw new ResourceNotFoundException($"Line item {lineItemId} not found");
            
        _lineItems.Remove(lineItem);
        RecalculateTotalAmount();
        
        AddDomainEvent(new OrderLineItemRemoved(Id, lineItemId));
    }
    
    public void ConfirmOrder()
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessRuleViolationException("Only pending orders can be confirmed");
            
        if (!_lineItems.Any())
            throw new BusinessRuleViolationException("Cannot confirm order without items");
            
        Status = OrderStatus.Confirmed;
        
        AddDomainEvent(new OrderConfirmed(Id, DateTime.UtcNow));
    }
    
    public void MarkAsShipped(DateTime shippedDate)
    {
        if (Status != OrderStatus.Confirmed)
            throw new BusinessRuleViolationException("Only confirmed orders can be shipped");
            
        Status = OrderStatus.Shipped;
        ShippedDate = shippedDate;
        
        AddDomainEvent(new OrderShipped(Id, shippedDate));
    }
    
    public void MarkAsDelivered(DateTime deliveredDate)
    {
        if (Status != OrderStatus.Shipped)
            throw new BusinessRuleViolationException("Only shipped orders can be delivered");
            
        Status = OrderStatus.Delivered;
        DeliveredDate = deliveredDate;
        
        AddDomainEvent(new OrderDelivered(Id, deliveredDate));
    }
    
    private void RecalculateTotalAmount()
    {
        var total = _lineItems
            .Select(li => li.GetTotalPrice())
            .Aggregate(Money.Zero("USD"), (acc, price) => acc.Add(price));
            
        TotalAmount = total;
    }
    
    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

#### OrderLineItem Entity
```csharp
// Domain/Entities/OrderLineItem.cs
namespace Domain.Entities;

public class OrderLineItem : Entity<OrderLineItemId>
{
    public OrderLineItem(
        OrderLineItemId id,
        ProductId productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        
        if (quantity <= 0)
            throw new BusinessRuleViolationException("Quantity must be greater than zero");
            
        Quantity = quantity;
    }
    
    public OrderLineItemId Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    
    public Money GetTotalPrice()
    {
        return UnitPrice.Multiply(Quantity);
    }
    
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new BusinessRuleViolationException("Quantity must be greater than zero");
            
        Quantity = newQuantity;
    }
}
```

### Value Objects

#### Email Value Object
```csharp
// Domain/ValueObjects/Email.cs
namespace Domain.ValueObjects;

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
    
    public string Domain => Value.Split('@')[1];
    
    public string LocalPart => Value.Split('@')[0];
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(Email email) => email.Value;
    
    public static explicit operator Email(string value) => new(value);
}
```

#### Money Value Object
```csharp
// Domain/ValueObjects/Money.cs
namespace Domain.ValueObjects;

public sealed class Money : ValueObject
{
    private static readonly HashSet<string> SupportedCurrencies = new()
    {
        "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY"
    };
    
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));
            
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
            
        if (!SupportedCurrencies.Contains(currency.ToUpperInvariant()))
            throw new ArgumentException($"Unsupported currency: {currency}", nameof(currency));
            
        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }
    
    public Money Add(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
            
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add money in different currencies: {Currency} and {other.Currency}");
            
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Subtract(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
            
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract money in different currencies: {Currency} and {other.Currency}");
            
        return new Money(Amount - other.Amount, Currency);
    }
    
    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }
    
    public Money Multiply(int factor)
    {
        return new Money(Amount * factor, Currency);
    }
    
    public static Money Zero(string currency) => new(0, currency);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public override string ToString() => $"{Amount:F2} {Currency}";
}
```

#### Address Value Object
```csharp
// Domain/ValueObjects/Address.cs
namespace Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }
    
    public Address(
        string street,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        State = state ?? throw new ArgumentNullException(nameof(state));
        PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }
    
    public override string ToString()
    {
        return $"{Street}, {City}, {State} {PostalCode}, {Country}";
    }
}
```

#### Strongly-Typed IDs
```csharp
// Domain/ValueObjects/CustomerId.cs
namespace Domain.ValueObjects;

public sealed class CustomerId : ValueObject
{
    public Guid Value { get; }
    
    public CustomerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(value));
            
        Value = value;
    }
    
    public static CustomerId Create() => new(Guid.NewGuid());
    
    public static CustomerId From(Guid value) => new(value);
    
    public static CustomerId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid Customer ID format: {value}", nameof(value));
            
        return new CustomerId(guid);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(CustomerId id) => id.Value;
    
    public static explicit operator CustomerId(Guid value) => new(value);
}

// Similar implementations for OrderId, ProductId, OrderLineItemId, etc.
```

### Repository Interfaces

#### Customer Repository
```csharp
// Domain/Repositories/ICustomerRepository.cs
namespace Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(CustomerId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CustomerId id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
}
```

#### Order Repository
```csharp
// Domain/Repositories/IOrderRepository.cs
namespace Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}
```

### Domain Services

#### Order Pricing Service
```csharp
// Domain/Services/OrderPricingService.cs
namespace Domain.Services;

public class OrderPricingService : IDomainService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly ICustomerRepository _customerRepository;
    
    public OrderPricingService(
        IPromotionRepository promotionRepository,
        ICustomerRepository customerRepository)
    {
        _promotionRepository = promotionRepository;
        _customerRepository = customerRepository;
    }
    
    public async Task<Money> CalculateTotalWithDiscounts(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);
        if (customer == null)
            throw new ResourceNotFoundException($"Customer {order.CustomerId} not found");
        
        var baseTotal = order.TotalAmount;
        var discount = await CalculateDiscount(customer, baseTotal, cancellationToken);
        
        return baseTotal.Subtract(discount);
    }
    
    private async Task<Money> CalculateDiscount(
        Customer customer,
        Money orderTotal,
        CancellationToken cancellationToken)
    {
        // Business logic for calculating discounts based on customer tier,
        // active promotions, order amount, etc.
        var promotions = await _promotionRepository.GetActivePromotionsAsync(cancellationToken);
        
        decimal discountPercentage = 0;
        
        // VIP customers get 10% discount
        if (customer.IsVip())
            discountPercentage += 0.10m;
        
        // Check for applicable promotions
        foreach (var promotion in promotions)
        {
            if (promotion.IsApplicable(customer, orderTotal))
                discountPercentage += promotion.DiscountPercentage;
        }
        
        // Cap discount at 30%
        discountPercentage = Math.Min(discountPercentage, 0.30m);
        
        return orderTotal.Multiply(discountPercentage);
    }
}
```

## Infrastructure Layer

### Entity Framework Core Implementation

#### Database Context
```csharp
// Infrastructure/Data/ApplicationDbContext.cs
namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Global query filters
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => c.IsActive);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle domain events before saving
        var domainEntities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();
        
        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();
        
        // Clear domain events
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());
        
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Publish domain events after successful save
        await PublishDomainEvents(domainEvents);
        
        return result;
    }
    
    private async Task PublishDomainEvents(List<DomainEvent> domainEvents)
    {
        // Publish events via MediatR or event bus
        // Implementation depends on chosen event handling strategy
    }
}
```

#### Entity Configurations
```csharp
// Infrastructure/Data/Configurations/CustomerConfiguration.cs
namespace Infrastructure.Data.Configurations;

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
            
            email.HasIndex(e => e.Value)
                .IsUnique();
        });
        
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(50);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
        });
        
        builder.Property(c => c.Preferences)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) 
                     ?? new Dictionary<string, object>())
            .HasColumnType("jsonb"); // For PostgreSQL, use nvarchar(max) for SQL Server
        
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(c => c.CreatedAt)
            .IsRequired();
        
        builder.Property(c => c.UpdatedAt)
            .IsRequired();
        
        builder.Ignore(c => c.DomainEvents);
    }
}
```

#### Repository Implementations
```csharp
// Infrastructure/Repositories/CustomerRepository.cs
namespace Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerRepository> _logger;
    
    public CustomerRepository(
        ApplicationDbContext context,
        ILogger<CustomerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<Customer?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }
    
    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.ToLower();
        
        return await _context.Customers
            .Where(c => c.Name.ToLower().Contains(normalizedSearchTerm) ||
                       c.Email.Value.ToLower().Contains(normalizedSearchTerm))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding new customer with ID {CustomerId}", customer.Id);
        
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        
        return customer;
    }
    
    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer with ID {CustomerId}", customer.Id);
        
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task DeleteAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        var customer = await GetByIdAsync(id, cancellationToken);
        if (customer != null)
        {
            _logger.LogInformation("Deleting customer with ID {CustomerId}", id);
            
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    public async Task<bool> ExistsAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == id, cancellationToken);
    }
    
    public async Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AnyAsync(c => c.Email == email, cancellationToken);
    }
}
```

## Application Layer

### Use Cases with MediatR

#### Create Customer Command
```csharp
// Application/Commands/CreateCustomer/CreateCustomerCommand.cs
namespace Application.Commands.CreateCustomer;

public record CreateCustomerCommand : IRequest<Result<CustomerDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public AddressDto? Address { get; init; }
    public Dictionary<string, object>? Preferences { get; init; }
}

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
            // Validate email format
            Email email;
            try
            {
                email = new Email(request.Email);
            }
            catch (ArgumentException ex)
            {
                return Result<CustomerDto>.Failure($"Invalid email: {ex.Message}");
            }
            
            // Check if email already exists
            if (await _customerRepository.EmailExistsAsync(email, cancellationToken))
            {
                return Result<CustomerDto>.Failure($"Customer with email {request.Email} already exists");
            }
            
            // Create address value object if provided
            Address? address = null;
            if (request.Address != null)
            {
                address = new Address(
                    request.Address.Street,
                    request.Address.City,
                    request.Address.State,
                    request.Address.PostalCode,
                    request.Address.Country
                );
            }
            
            // Create customer entity
            var customer = new Customer(
                CustomerId.Create(),
                request.Name,
                email,
                address,
                request.Preferences
            );
            
            // Save to repository
            await _customerRepository.AddAsync(customer, cancellationToken);
            
            _logger.LogInformation("Created customer with ID {CustomerId}", customer.Id);
            
            // Map to DTO and return
            var dto = _mapper.Map<CustomerDto>(customer);
            return Result<CustomerDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Result<CustomerDto>.Failure("An error occurred while creating the customer");
        }
    }
}
```

#### Get Customer Orders Query
```csharp
// Application/Queries/GetCustomerOrders/GetCustomerOrdersQuery.cs
namespace Application.Queries.GetCustomerOrders;

public record GetCustomerOrdersQuery : IRequest<Result<IEnumerable<OrderDto>>>
{
    public Guid CustomerId { get; init; }
}

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<IEnumerable<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    
    public GetCustomerOrdersQueryHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _mapper = mapper;
    }
    
    public async Task<Result<IEnumerable<OrderDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var customerId = CustomerId.From(request.CustomerId);
        
        // Verify customer exists
        if (!await _customerRepository.ExistsAsync(customerId, cancellationToken))
        {
            return Result<IEnumerable<OrderDto>>.Failure($"Customer {request.CustomerId} not found");
        }
        
        // Get customer orders
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        
        // Map to DTOs
        var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
        
        return Result<IEnumerable<OrderDto>>.Success(orderDtos);
    }
}
```

### Result Pattern for Error Handling
```csharp
// Application/Common/Result.cs
namespace Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string[]? ValidationErrors { get; }
    
    protected Result(bool isSuccess, T? value, string? error, string[]? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ValidationErrors = validationErrors;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    
    public static Result<T> Failure(string error) => new(false, default, error);
    
    public static Result<T> ValidationFailure(params string[] errors) => new(false, default, "Validation failed", errors);
    
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess 
            ? Result<TNew>.Success(mapper(Value!)) 
            : Result<TNew>.Failure(Error!);
    }
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, null);
    
    public static Result Failure(string error) => new(false, error);
}
```

## Presentation Layer

### ASP.NET Core Web API

#### Controllers
```csharp
// WebApi/Controllers/CustomersController.cs
namespace WebApi.Controllers;

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
    /// Creates a new customer
    /// </summary>
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
    
    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerByIdQuery { CustomerId = id };
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result.IsSuccess)
            return Ok(result.Value);
            
        return NotFound();
    }
    
    /// <summary>
    /// Gets all orders for a customer
    /// </summary>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerOrders(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerOrdersQuery { CustomerId = id };
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result.IsSuccess)
            return Ok(result.Value);
            
        return NotFound();
    }
    
    /// <summary>
    /// Searches for customers
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var query = new SearchCustomersQuery { SearchTerm = searchTerm ?? string.Empty };
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result.Value);
    }
}
```

### Dependency Injection Setup

```csharp
// WebApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clean Architecture API",
        Version = "v1",
        Description = "Clean Architecture .NET 8 Implementation"
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("CleanArchitectureDb");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Register repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register domain services
builder.Services.AddScoped<OrderPricingService>();

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly);
    cfg.AddBehavior<IPipelineBehavior<,>, ValidationBehavior<,>>();
    cfg.AddBehavior<IPipelineBehavior<,>, LoggingBehavior<,>>();
});

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerCommandValidator>();

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed database
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DataSeeder.SeedAsync(dbContext);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

## Testing Strategy

### Domain Layer Tests
```csharp
// Tests/Domain.UnitTests/Entities/CustomerTests.cs
namespace Domain.UnitTests.Entities;

public class CustomerTests
{
    [Fact]
    public void Customer_Creation_Should_Succeed_With_Valid_Data()
    {
        // Arrange
        var id = CustomerId.Create();
        var name = "John Doe";
        var email = new Email("john.doe@example.com");
        
        // Act
        var customer = new Customer(id, name, email);
        
        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().Be(id);
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
        customer.IsActive.Should().BeTrue();
        customer.DomainEvents.Should().ContainSingle();
        customer.DomainEvents.First().Should().BeOfType<CustomerCreated>();
    }
    
    [Fact]
    public void UpdateEmail_Should_Add_Domain_Event()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var newEmail = new Email("newemail@example.com");
        
        // Act
        customer.UpdateEmail(newEmail);
        
        // Assert
        customer.Email.Should().Be(newEmail);
        customer.DomainEvents.Should().Contain(e => e is CustomerEmailChanged);
    }
    
    [Fact]
    public void Deactivate_Already_Inactive_Customer_Should_Throw()
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

### Application Layer Tests
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
    public async Task Handle_Should_Create_Customer_When_Email_Is_Unique()
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
        
        _mapperMock
            .Setup(x => x.Map<CustomerDto>(It.IsAny<Customer>()))
            .Returns(new CustomerDto { Name = command.Name, Email = command.Email });
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(command.Name);
        result.Value!.Email.Should().Be(command.Email);
        
        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_Should_Return_Failure_When_Email_Already_Exists()
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
        result.Error.Should().Contain("already exists");
        
        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

## Migration Strategy

### From Development to Production

1. **Database Migration Path**
   - Development: Entity Framework InMemory provider
   - Staging: PostgreSQL or SQL Server local instance
   - Production: Azure SQL Database or AWS RDS
   - Alternative: Cosmos DB for global distribution

2. **Code Organization Evolution**
   - Start with single project per layer
   - Split into feature modules as complexity grows
   - Consider vertical slice architecture for large teams
   - Maintain clean architecture boundaries

3. **Testing Evolution**
   - Development: Unit tests with mocks
   - Integration: Add database integration tests
   - End-to-end: API integration tests
   - Production: Performance and load tests

4. **Infrastructure Considerations**
   - Containerization with Docker
   - Orchestration with Kubernetes or Azure Container Apps
   - CI/CD with Azure DevOps or GitHub Actions
   - Monitoring with Application Insights or Datadog

## Key Benefits of This Architecture

1. **Testability**: Business logic isolated from infrastructure
2. **Flexibility**: Easy to change database or external services
3. **Maintainability**: Clear separation of concerns
4. **Type Safety**: Strong typing throughout with C# and .NET
5. **Domain Focus**: Business rules in pure C# without framework dependencies
6. **Performance**: Async/await patterns for scalability
7. **Developer Experience**: Rich tooling support in Visual Studio and VS Code

## Best Practices

1. **Keep Domain Pure**: No framework dependencies in Domain layer
2. **Use Value Objects**: For concepts without identity
3. **Implement Domain Events**: For decoupled communication
4. **Follow SOLID Principles**: Throughout all layers
5. **Use Result Pattern**: For explicit error handling
6. **Write Tests First**: TDD approach for critical business logic
7. **Document APIs**: With OpenAPI/Swagger
8. **Log Appropriately**: Structured logging with Serilog
9. **Handle Concurrency**: Use optimistic concurrency tokens
10. **Validate Early**: At API boundaries with FluentValidation

---
*Document Version: 2.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / C# 12*
*Status: Clean Architecture Implementation Guide*