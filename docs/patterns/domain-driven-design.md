# Domain-Driven Design (DDD) Patterns in .NET

## Overview

This document outlines the Domain-Driven Design patterns used in our Clean Architecture .NET implementation, with concrete examples from our codebase and guidance for future development.

## Core DDD Concepts

### 1. Entities

**Definition**: Objects with a unique identity that persists through state changes.

**Current Implementation**:
```csharp
public class Customer : Entity<CustomerId>
{
    public CustomerId Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    public Dictionary<string, object> Preferences { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Business methods encapsulate behavior
    public void UpdateEmail(Email newEmail) { /* ... */ }
    public void Deactivate() { /* ... */ }
}
```

**Characteristics**:
- Has unique identity (`Id`) - using strongly-typed IDs
- Identity remains constant through updates
- Equality based on identity, not attributes
- Encapsulates business behavior through methods
- Protected setters ensure invariants

### 2. Value Objects

**Definition**: Objects without identity, defined entirely by their attributes. Immutable and self-validating.

**Implementation Pattern**:

```csharp
// Base Value Object class
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;
            
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
    
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
    
    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }
    
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
```

**Example Value Objects**:

```csharp
// Email Value Object
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
    
    // Implicit conversion for convenience
    public static implicit operator string(Email email) => email.Value;
    
    // Explicit conversion for safety
    public static explicit operator Email(string value) => new(value);
}

// Money Value Object
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
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");
            
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public override string ToString() => $"{Amount:F2} {Currency}";
}

// Address Value Object
public sealed record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    public string FullAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

// PhoneNumber Value Object
public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$", // E.164 format
        RegexOptions.Compiled);
    
    public string Value { get; }
    
    public PhoneNumber(string value)
    {
        var normalized = value.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        if (!PhoneRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid phone number format: {value}", nameof(value));
            
        Value = normalized;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
}
```

**Value Object Guidelines**:
- Always immutable (use `sealed` class or `record`)
- Self-validating in constructor
- Rich behavior through methods
- No database IDs
- Equality based on all attributes
- Consider using C# 9+ records for simple value objects

### 3. Aggregates

**Definition**: A cluster of domain objects treated as a single unit with one aggregate root.

**Example Implementation**:

```csharp
// Order Aggregate Root
public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLineItem> _lineItems = new();
    private readonly List<DomainEvent> _domainEvents = new();
    
    // Identity
    public OrderId Id { get; private set; }
    
    // Reference to other aggregates by ID only
    public CustomerId CustomerId { get; private set; }
    
    // Owned entities within aggregate
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems.AsReadOnly();
    
    // Value objects
    public ShippingAddress ShippingAddress { get; private set; }
    public Money TotalAmount { get; private set; }
    
    // Aggregate behavior - all modifications go through the root
    public void AddLineItem(ProductId productId, string productName, Money unitPrice, int quantity)
    {
        // Business rule validation
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
        
        // Raise domain event
        AddDomainEvent(new OrderLineItemAdded(Id, lineItem.Id, productId, quantity));
    }
    
    public void RemoveLineItem(OrderLineItemId lineItemId)
    {
        // Only modify through aggregate root
        var lineItem = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (lineItem == null)
            throw new ResourceNotFoundException($"Line item {lineItemId} not found");
            
        _lineItems.Remove(lineItem);
        RecalculateTotalAmount();
    }
    
    private void RecalculateTotalAmount()
    {
        // Private method ensures invariants
        TotalAmount = _lineItems
            .Select(li => li.GetTotalPrice())
            .Aggregate(Money.Zero("USD"), (acc, price) => acc.Add(price));
    }
}

// Entity within the aggregate (not accessible outside)
public class OrderLineItem : Entity<OrderLineItemId>
{
    internal OrderLineItem(
        OrderLineItemId id,
        ProductId productId,
        string productName,
        Money unitPrice,
        int quantity)
    {
        // Internal constructor - can only be created by Order aggregate
        Id = id;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
    
    public OrderLineItemId Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    
    public Money GetTotalPrice() => UnitPrice.Multiply(Quantity);
}
```

**Aggregate Design Rules**:
- Only one aggregate root per aggregate
- External references by ID only
- Consistency boundary for transactions
- Child entities have local identity
- All modifications through aggregate root
- Keep aggregates small and focused

### 4. Domain Events

**Definition**: Something that happened in the domain that domain experts care about.

```csharp
// Base domain event
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Specific domain events
public record CustomerCreated(
    CustomerId CustomerId,
    string Name,
    Email Email,
    DateTime CreatedAt) : DomainEvent;

public record OrderConfirmed(
    OrderId OrderId,
    DateTime ConfirmedAt) : DomainEvent;

public record PaymentProcessed(
    OrderId OrderId,
    Money Amount,
    string PaymentMethod,
    DateTime ProcessedAt) : DomainEvent;

// Aggregate root with event support
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### 5. Domain Services

**Definition**: When an operation doesn't naturally fit within an Entity or Value Object.

```csharp
// Domain service interface
public interface IOrderPricingService
{
    Task<Money> CalculateTotalWithDiscounts(Order order, Customer customer);
    Task<Money> CalculateShippingCost(ShippingAddress address, ShippingMethod method);
}

// Implementation
public class OrderPricingService : IOrderPricingService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IShippingRateRepository _shippingRateRepository;
    
    public OrderPricingService(
        IPromotionRepository promotionRepository,
        IShippingRateRepository shippingRateRepository)
    {
        _promotionRepository = promotionRepository;
        _shippingRateRepository = shippingRateRepository;
    }
    
    public async Task<Money> CalculateTotalWithDiscounts(Order order, Customer customer)
    {
        var baseTotal = order.TotalAmount;
        
        // Complex business logic that doesn't belong in Order or Customer
        var promotions = await _promotionRepository.GetActivePromotionsAsync();
        var applicablePromotions = promotions.Where(p => p.IsApplicableFor(customer, order));
        
        var discount = Money.Zero(baseTotal.Currency);
        foreach (var promotion in applicablePromotions)
        {
            discount = discount.Add(promotion.CalculateDiscount(baseTotal));
        }
        
        // Apply customer tier discount
        if (customer.Tier == CustomerTier.Gold)
            discount = discount.Add(baseTotal.Multiply(0.1m)); // 10% for gold
        else if (customer.Tier == CustomerTier.Silver)
            discount = discount.Add(baseTotal.Multiply(0.05m)); // 5% for silver
        
        return baseTotal.Subtract(discount);
    }
    
    public async Task<Money> CalculateShippingCost(ShippingAddress address, ShippingMethod method)
    {
        var rates = await _shippingRateRepository.GetRatesForZone(address.PostalCode);
        return rates.CalculateCost(method);
    }
}
```

### 6. Repositories

**Definition**: Abstractions for accessing aggregates, maintaining the illusion of an in-memory collection.

```csharp
// Repository interface (in Domain layer)
public interface ICustomerRepository
{
    // Get methods return aggregates
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    
    // Query methods for read models
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Persistence methods
    Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(CustomerId id, CancellationToken cancellationToken = default);
    
    // Existence checks
    Task<bool> ExistsAsync(CustomerId id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
}

// Unit of Work pattern for transactional consistency
public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IProductRepository Products { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 7. Specifications

**Definition**: Encapsulate query logic in a reusable way.

```csharp
// Base specification
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }
    
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }
    
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }
    
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

// Concrete specifications
public class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.IsActive;
    }
}

public class PremiumCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Tier == CustomerTier.Gold || customer.Tier == CustomerTier.Platinum;
    }
}

public class HighValueOrderSpecification : Specification<Order>
{
    private readonly Money _threshold;
    
    public HighValueOrderSpecification(Money threshold)
    {
        _threshold = threshold;
    }
    
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.TotalAmount.Amount >= _threshold.Amount && 
                       order.TotalAmount.Currency == _threshold.Currency;
    }
}

// Usage in repository
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<IEnumerable<Customer>> FindAsync(
        Specification<Customer> specification,
        CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Where(specification.ToExpression())
            .ToListAsync(cancellationToken);
    }
}

// Usage in domain service
public class CustomerSegmentationService
{
    private readonly ICustomerRepository _repository;
    
    public async Task<IEnumerable<Customer>> GetVipCustomersAsync()
    {
        var spec = new ActiveCustomerSpecification()
            .And(new PremiumCustomerSpecification());
            
        return await _repository.FindAsync(spec);
    }
}
```

### 8. Factory Pattern

**Definition**: Encapsulate complex object creation logic.

```csharp
// Factory interface
public interface IOrderFactory
{
    Order CreateOrder(CustomerId customerId, ShippingAddress shippingAddress);
    Order CreateOrderFromCart(CustomerId customerId, ShoppingCart cart);
    Order CreateSubscriptionOrder(CustomerId customerId, SubscriptionPlan plan);
}

// Factory implementation
public class OrderFactory : IOrderFactory
{
    private readonly IProductRepository _productRepository;
    private readonly IPricingService _pricingService;
    
    public OrderFactory(
        IProductRepository productRepository,
        IPricingService pricingService)
    {
        _productRepository = productRepository;
        _pricingService = pricingService;
    }
    
    public Order CreateOrder(CustomerId customerId, ShippingAddress shippingAddress)
    {
        var orderId = OrderId.Create();
        return new Order(orderId, customerId, shippingAddress);
    }
    
    public Order CreateOrderFromCart(CustomerId customerId, ShoppingCart cart)
    {
        var order = CreateOrder(customerId, cart.ShippingAddress);
        
        foreach (var cartItem in cart.Items)
        {
            var product = _productRepository.GetByIdAsync(cartItem.ProductId).Result;
            var price = _pricingService.GetProductPrice(product);
            
            order.AddLineItem(
                cartItem.ProductId,
                product.Name,
                price,
                cartItem.Quantity
            );
        }
        
        return order;
    }
    
    public Order CreateSubscriptionOrder(CustomerId customerId, SubscriptionPlan plan)
    {
        var order = CreateOrder(customerId, plan.DefaultShippingAddress);
        
        foreach (var item in plan.Items)
        {
            order.AddLineItem(
                item.ProductId,
                item.ProductName,
                item.Price,
                item.Quantity
            );
        }
        
        order.ApplySubscriptionDiscount(plan.DiscountPercentage);
        
        return order;
    }
}
```

## .NET-Specific DDD Patterns

### 1. Strongly-Typed IDs with Structs

```csharp
// Using readonly struct for performance
public readonly struct CustomerId : IEquatable<CustomerId>
{
    private readonly Guid _value;
    
    public CustomerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(value));
        _value = value;
    }
    
    public static CustomerId Create() => new(Guid.NewGuid());
    
    public bool Equals(CustomerId other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is CustomerId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString();
    
    public static bool operator ==(CustomerId left, CustomerId right) => left.Equals(right);
    public static bool operator !=(CustomerId left, CustomerId right) => !left.Equals(right);
    
    // EF Core conversion
    public static implicit operator Guid(CustomerId id) => id._value;
    public static explicit operator CustomerId(Guid value) => new(value);
}
```

### 2. Domain Validation with FluentValidation

```csharp
// Domain validator
public class CreateOrderValidator : AbstractValidator<Order>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");
            
        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");
            
        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(items => items.All(i => i.Quantity > 0))
            .WithMessage("All items must have positive quantity");
            
        RuleFor(x => x.TotalAmount)
            .Must(amount => amount.Amount > 0)
            .WithMessage("Order total must be greater than zero");
    }
}
```

### 3. Domain Events with MediatR

```csharp
// Domain event handler
public class CustomerCreatedHandler : INotificationHandler<CustomerCreated>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<CustomerCreatedHandler> _logger;
    
    public CustomerCreatedHandler(
        IEmailService emailService,
        ILogger<CustomerCreatedHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task Handle(CustomerCreated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CustomerCreated event for {CustomerId}", notification.CustomerId);
        
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            notification.Name,
            cancellationToken);
    }
}
```

### 4. Entity Framework Core Configurations

```csharp
// Complex type configuration for value objects
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Configure strongly-typed ID
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value));
        
        // Configure owned value object
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256);
            email.HasIndex(e => e.Value).IsUnique();
        });
        
        // Configure complex value object
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street");
            address.Property(a => a.City).HasColumnName("City");
            address.Property(a => a.State).HasColumnName("State");
            address.Property(a => a.PostalCode).HasColumnName("PostalCode");
            address.Property(a => a.Country).HasColumnName("Country");
        });
        
        // Configure collection as JSON
        builder.Property(c => c.Preferences)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);
        
        // Ignore domain events (not persisted)
        builder.Ignore(c => c.DomainEvents);
    }
}
```

## Advanced DDD Patterns in .NET

### 1. Saga/Process Manager Pattern

```csharp
public class OrderFulfillmentSaga
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IShippingService _shippingService;
    
    public async Task HandleOrderPlaced(OrderPlaced @event)
    {
        var order = await _orderRepository.GetByIdAsync(@event.OrderId);
        
        try
        {
            // Step 1: Reserve inventory
            await _inventoryService.ReserveItemsAsync(order.LineItems);
            
            // Step 2: Process payment
            var paymentResult = await _paymentService.ProcessPaymentAsync(order);
            
            if (!paymentResult.IsSuccessful)
            {
                // Compensate: Release inventory
                await _inventoryService.ReleaseItemsAsync(order.LineItems);
                order.MarkAsFailed("Payment failed");
                return;
            }
            
            // Step 3: Arrange shipping
            var shippingLabel = await _shippingService.CreateShippingLabelAsync(order);
            order.MarkAsReadyToShip(shippingLabel);
            
            await _orderRepository.UpdateAsync(order);
        }
        catch (Exception ex)
        {
            // Compensate all successful steps
            await CompensateAsync(order);
            throw;
        }
    }
}
```

### 2. Event Sourcing Pattern

```csharp
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion);
    Task<List<DomainEvent>> GetEventsAsync(Guid aggregateId);
}

public abstract class EventSourcedAggregate
{
    private readonly List<DomainEvent> _changes = new();
    
    public Guid Id { get; protected set; }
    public int Version { get; private set; } = -1;
    
    public IEnumerable<DomainEvent> GetUncommittedChanges() => _changes;
    
    public void MarkChangesAsCommitted()
    {
        _changes.Clear();
    }
    
    public void LoadFromHistory(IEnumerable<DomainEvent> history)
    {
        foreach (var @event in history)
        {
            ApplyChange(@event, false);
        }
    }
    
    protected void ApplyChange(DomainEvent @event)
    {
        ApplyChange(@event, true);
    }
    
    private void ApplyChange(DomainEvent @event, bool isNew)
    {
        dynamic dynamicEvent = @event;
        Apply(dynamicEvent);
        
        if (isNew)
        {
            _changes.Add(@event);
        }
        
        Version++;
    }
    
    protected abstract void Apply(DomainEvent @event);
}
```

## Testing DDD Components

### 1. Testing Entities and Value Objects

```csharp
public class CustomerTests
{
    [Fact]
    public void Customer_Should_Raise_CustomerCreated_Event_When_Created()
    {
        // Arrange & Act
        var customer = new Customer(
            CustomerId.Create(),
            "John Doe",
            new Email("john@example.com")
        );
        
        // Assert
        customer.DomainEvents.Should().ContainSingle();
        customer.DomainEvents.First().Should().BeOfType<CustomerCreated>();
    }
    
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Email_Should_Reject_Invalid_Formats(string invalidEmail)
    {
        // Act & Assert
        Action act = () => new Email(invalidEmail);
        act.Should().Throw<ArgumentException>();
    }
}
```

### 2. Testing Domain Services

```csharp
public class OrderPricingServiceTests
{
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly OrderPricingService _service;
    
    public OrderPricingServiceTests()
    {
        _promotionRepositoryMock = new Mock<IPromotionRepository>();
        _service = new OrderPricingService(_promotionRepositoryMock.Object);
    }
    
    [Fact]
    public async Task CalculateTotalWithDiscounts_Should_Apply_Gold_Customer_Discount()
    {
        // Arrange
        var customer = CreateGoldCustomer();
        var order = CreateOrderWithTotal(new Money(100, "USD"));
        
        _promotionRepositoryMock
            .Setup(x => x.GetActivePromotionsAsync())
            .ReturnsAsync(new List<Promotion>());
        
        // Act
        var result = await _service.CalculateTotalWithDiscounts(order, customer);
        
        // Assert
        result.Amount.Should().Be(90); // 10% discount for gold
    }
}
```

## Migration Path from Anemic to Rich Domain Model

### Phase 1: Identify Anemic Models
- DTOs used as domain entities
- Business logic in services instead of entities
- Public setters on all properties

### Phase 2: Introduce Value Objects
- Replace primitive types with value objects
- Add validation in value object constructors
- Implement equality based on values

### Phase 3: Move Behavior to Entities
- Convert public setters to private
- Add business methods to entities
- Encapsulate invariants within aggregates

### Phase 4: Implement Domain Events
- Add event support to aggregate roots
- Raise events for significant state changes
- Handle events for side effects

### Phase 5: Define Aggregate Boundaries
- Group related entities into aggregates
- Establish consistency boundaries
- Reference other aggregates by ID only

## Best Practices

1. **Keep Aggregates Small**: Large aggregates hurt performance and concurrency
2. **Use Value Objects Liberally**: They make the model more expressive
3. **Model True Invariants**: Only enforce rules that must always be true
4. **Use Domain Events**: For loose coupling between aggregates
5. **Avoid Anemic Models**: Put behavior where the data is
6. **Test Domain Logic**: Focus tests on business rules
7. **Use Ubiquitous Language**: Terms from domain experts in code
8. **Prefer Composition**: Over inheritance for flexibility
9. **Make Implicit Explicit**: Turn hidden concepts into first-class objects
10. **Fail Fast**: Validate at construction time

---
*Document Version: 2.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / C# 12*
*Status: DDD Implementation Guide*