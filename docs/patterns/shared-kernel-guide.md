# Shared Kernel Guide - .NET 8 Clean Architecture

## Overview

The Shared Kernel is a foundational part of Domain-Driven Design that contains common code and concepts shared across multiple bounded contexts. In our Clean Architecture implementation, the shared kernel provides base classes, common value objects, and shared abstractions.

## Base Classes

### Entity Base Class

```csharp
// Domain/Common/Entity.cs
namespace Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    
    protected Entity() { }
    
    protected Entity(TId id)
    {
        Id = id;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }
    
    public bool Equals(Entity<TId>? other)
    {
        return Equals((object?)other);
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
```

### Value Object Base Class

```csharp
// Domain/Common/ValueObject.cs
namespace Domain.Common;

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

### Aggregate Root Base Class

```csharp
// Domain/Common/AggregateRoot.cs
namespace Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    protected AggregateRoot() { }
    
    protected AggregateRoot(TId id) : base(id) { }
    
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

### Domain Event Base Class

```csharp
// Domain/Common/DomainEvent.cs
namespace Domain.Common;

public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}

// Domain/Common/IHasDomainEvents.cs
namespace Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

## Strongly-Typed IDs

### Base ID Structure

```csharp
// Domain/Common/TypedId.cs
namespace Domain.Common;

public abstract class TypedId<T> : ValueObject where T : TypedId<T>
{
    public Guid Value { get; }
    
    protected TypedId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{typeof(T).Name} cannot be empty", nameof(value));
        
        Value = value;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(TypedId<T> id) => id.Value;
}
```

### Concrete ID Implementations

```csharp
// Domain/ValueObjects/CustomerId.cs
namespace Domain.ValueObjects;

public sealed class CustomerId : TypedId<CustomerId>
{
    public CustomerId(Guid value) : base(value) { }
    
    public static CustomerId Create() => new(Guid.NewGuid());
    
    public static CustomerId From(Guid value) => new(value);
    
    public static CustomerId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid Customer ID format: {value}", nameof(value));
        
        return new CustomerId(guid);
    }
    
    public static explicit operator CustomerId(Guid value) => new(value);
}

// Domain/ValueObjects/OrderId.cs
namespace Domain.ValueObjects;

public sealed class OrderId : TypedId<OrderId>
{
    public OrderId(Guid value) : base(value) { }
    
    public static OrderId Create() => new(Guid.NewGuid());
    public static OrderId From(Guid value) => new(value);
    public static explicit operator OrderId(Guid value) => new(value);
}

// Domain/ValueObjects/ProductId.cs
namespace Domain.ValueObjects;

public sealed class ProductId : TypedId<ProductId>
{
    public ProductId(Guid value) : base(value) { }
    
    public static ProductId Create() => new(Guid.NewGuid());
    public static ProductId From(Guid value) => new(value);
    public static explicit operator ProductId(Guid value) => new(value);
}
```

## Common Value Objects

### Email Value Object

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

### Money Value Object

```csharp
// Domain/ValueObjects/Money.cs
namespace Domain.ValueObjects;

public sealed class Money : ValueObject
{
    private static readonly HashSet<string> SupportedCurrencies = new()
    {
        "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NOK", "DKK"
    };
    
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        
        var normalizedCurrency = currency.ToUpperInvariant();
        if (!SupportedCurrencies.Contains(normalizedCurrency))
            throw new ArgumentException($"Unsupported currency: {currency}", nameof(currency));
        
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = normalizedCurrency;
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
    
    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");
        
        return new Money(Amount / divisor, Currency);
    }
    
    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot compare different currencies: {Currency} and {other.Currency}");
        
        return Amount > other.Amount;
    }
    
    public bool IsLessThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot compare different currencies: {Currency} and {other.Currency}");
        
        return Amount < other.Amount;
    }
    
    public static Money Zero(string currency) => new(0, currency);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public override string ToString() => $"{Amount:F2} {Currency}";
    
    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);
    public static Money operator /(Money money, decimal divisor) => money.Divide(divisor);
    
    public static bool operator >(Money left, Money right) => left.IsGreaterThan(right);
    public static bool operator <(Money left, Money right) => left.IsLessThan(right);
    public static bool operator >=(Money left, Money right) => left.IsGreaterThan(right) || left.Equals(right);
    public static bool operator <=(Money left, Money right) => left.IsLessThan(right) || left.Equals(right);
}
```

### Address Value Object

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
    
    public Address(string street, string city, string state, string postalCode, string country)
    {
        Street = !string.IsNullOrWhiteSpace(street) ? street.Trim() 
            : throw new ArgumentException("Street cannot be empty", nameof(street));
        City = !string.IsNullOrWhiteSpace(city) ? city.Trim() 
            : throw new ArgumentException("City cannot be empty", nameof(city));
        State = !string.IsNullOrWhiteSpace(state) ? state.Trim() 
            : throw new ArgumentException("State cannot be empty", nameof(state));
        PostalCode = !string.IsNullOrWhiteSpace(postalCode) ? postalCode.Trim() 
            : throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));
        Country = !string.IsNullOrWhiteSpace(country) ? country.Trim() 
            : throw new ArgumentException("Country cannot be empty", nameof(country));
    }
    
    public string FullAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street.ToUpperInvariant();
        yield return City.ToUpperInvariant();
        yield return State.ToUpperInvariant();
        yield return PostalCode.ToUpperInvariant();
        yield return Country.ToUpperInvariant();
    }
    
    public override string ToString() => FullAddress;
}
```

### PhoneNumber Value Object

```csharp
// Domain/ValueObjects/PhoneNumber.cs
namespace Domain.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$", // E.164 format
        RegexOptions.Compiled);
    
    public string Value { get; }
    
    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty", nameof(value));
        
        var normalized = NormalizePhoneNumber(value);
        
        if (!PhoneRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid phone number format: {value}", nameof(value));
        
        Value = normalized;
    }
    
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(".", "");
    }
    
    public string CountryCode
    {
        get
        {
            if (Value.StartsWith("+"))
                return Value.Substring(1, Math.Min(3, Value.Length - 1));
            return "";
        }
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static explicit operator PhoneNumber(string value) => new(value);
}
```

## Exception Hierarchy

### Base Domain Exception

```csharp
// Domain/Exceptions/DomainException.cs
namespace Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    
    protected DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

### Specific Domain Exceptions

```csharp
// Domain/Exceptions/BusinessRuleViolationException.cs
namespace Domain.Exceptions;

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
    
    public BusinessRuleViolationException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// Domain/Exceptions/ResourceNotFoundException.cs
namespace Domain.Exceptions;

public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string resourceName, object resourceId) 
        : base($"{resourceName} with ID {resourceId} was not found") { }
    
    public ResourceNotFoundException(string message) : base(message) { }
}

// Domain/Exceptions/InvalidOperationDomainException.cs
namespace Domain.Exceptions;

public class InvalidOperationDomainException : DomainException
{
    public InvalidOperationDomainException(string message) : base(message) { }
}
```

## Result Pattern

### Result Types

```csharp
// Application/Common/Result.cs
namespace Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string[]? ValidationErrors { get; }
    
    protected Result(bool isSuccess, string? error, string[]? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }
    
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result ValidationFailure(params string[] errors) => new(false, "Validation failed", errors);
    
    public static implicit operator Result(string error) => Failure(error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    
    private Result(bool isSuccess, T? value, string? error, string[]? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        Value = value;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
    public static new Result<T> ValidationFailure(params string[] errors) => new(false, default, "Validation failed", errors);
    
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);
    }
    
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);
}
```

## Domain Services Interface

```csharp
// Domain/Services/IDomainService.cs
namespace Domain.Services;

/// <summary>
/// Marker interface for domain services.
/// Domain services contain business logic that doesn't naturally fit within an entity or value object.
/// </summary>
public interface IDomainService
{
    // Marker interface - no methods
}
```

## Specification Pattern

```csharp
// Domain/Specifications/Specification.cs
namespace Domain.Specifications;

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
    
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
    {
        return specification.ToExpression();
    }
}

// Composite specifications
internal class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;
    
    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }
    
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();
        
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));
        
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

internal class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;
    
    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }
    
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();
        
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));
        
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

internal class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;
    
    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }
    
    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(expression, parameter));
        
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```

## Repository Base Interface

```csharp
// Domain/Repositories/IRepository.cs
namespace Domain.Repositories;

public interface IRepository<TEntity, TId> 
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> FindAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);
}
```

## Usage Examples

### Using Strongly-Typed IDs

```csharp
public class Customer : AggregateRoot<CustomerId>
{
    public Customer(CustomerId id, string name, Email email)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        
        AddDomainEvent(new CustomerCreated(Id, Name, Email, DateTime.UtcNow));
    }
    
    public string Name { get; private set; }
    public Email Email { get; private set; }
    
    public void UpdateEmail(Email newEmail)
    {
        var oldEmail = Email;
        Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
        
        AddDomainEvent(new CustomerEmailChanged(Id, oldEmail, newEmail, DateTime.UtcNow));
    }
}
```

### Using Value Objects

```csharp
public class Order : AggregateRoot<OrderId>
{
    public Order(OrderId id, CustomerId customerId, Address shippingAddress)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        TotalAmount = Money.Zero("USD");
        
        AddDomainEvent(new OrderCreated(Id, CustomerId, DateTime.UtcNow));
    }
    
    public CustomerId CustomerId { get; private set; }
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }
    
    public void AddLineItem(ProductId productId, string productName, Money unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new BusinessRuleViolationException("Quantity must be positive");
        
        var lineItemCost = unitPrice.Multiply(quantity);
        TotalAmount = TotalAmount.Add(lineItemCost);
        
        AddDomainEvent(new OrderLineItemAdded(Id, productId, quantity, lineItemCost));
    }
}
```

### Using Result Pattern

```csharp
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = new Email(request.Email);
            
            if (await _customerRepository.EmailExistsAsync(email, cancellationToken))
            {
                return Result<CustomerDto>.Failure($"Email {request.Email} is already in use");
            }
            
            var customer = new Customer(CustomerId.Create(), request.Name, email);
            await _customerRepository.AddAsync(customer, cancellationToken);
            
            var dto = _mapper.Map<CustomerDto>(customer);
            return Result<CustomerDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            return Result<CustomerDto>.Failure($"Validation error: {ex.Message}");
        }
    }
}
```

## Best Practices

1. **Keep the Shared Kernel Small**: Only include truly shared concepts
2. **Version Carefully**: Changes to shared kernel affect all bounded contexts
3. **Use Strongly-Typed IDs**: Prevent accidental ID mix-ups
4. **Immutable Value Objects**: All value objects should be immutable
5. **Rich Value Objects**: Include behavior, not just data
6. **Consistent Naming**: Follow established naming conventions
7. **Comprehensive Tests**: Test all shared kernel components thoroughly
8. **Documentation**: Document all public APIs in the shared kernel

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / C# 12*
*Status: Shared Kernel Implementation Guide*