# CQRS (Command Query Responsibility Segregation) Patterns in .NET

## Overview

CQRS is an architectural pattern that separates read and write operations into distinct models. This document outlines CQRS implementation patterns using MediatR in .NET 8, with practical examples and best practices.

## Core Concepts

### Command vs Query Separation

```csharp
// Commands: Change state, don't return data (or return only the result)
public record CreateCustomerCommand : IRequest<Result<Guid>>;

// Queries: Return data, don't change state
public record GetCustomerByIdQuery : IRequest<Result<CustomerDto>>;
```

## MediatR Implementation

### 1. Basic Setup

```csharp
// Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly);
    
    // Add pipeline behaviors
    cfg.AddBehavior<IPipelineBehavior<,>, ValidationBehavior<,>>();
    cfg.AddBehavior<IPipelineBehavior<,>, LoggingBehavior<,>>();
    cfg.AddBehavior<IPipelineBehavior<,>, PerformanceBehavior<,>>();
    cfg.AddBehavior<IPipelineBehavior<,>, TransactionBehavior<,>>();
});
```

### 2. Command Pattern

```csharp
// Command definition
public record CreateOrderCommand : IRequest<Result<OrderDto>>
{
    public Guid CustomerId { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public ShippingAddressDto ShippingAddress { get; init; } = null!;
    public string? SpecialInstructions { get; init; }
}

// Command handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    
    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IInventoryService inventoryService,
        IMapper mapper,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _inventoryService = inventoryService;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Validate customer exists
        var customerId = CustomerId.From(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        
        if (customer == null)
            return Result<OrderDto>.Failure($"Customer {request.CustomerId} not found");
        
        // Check inventory
        foreach (var item in request.Items)
        {
            var available = await _inventoryService.CheckAvailabilityAsync(
                item.ProductId,
                item.Quantity,
                cancellationToken);
                
            if (!available)
                return Result<OrderDto>.Failure($"Product {item.ProductId} is out of stock");
        }
        
        // Create order aggregate
        var order = new Order(
            OrderId.Create(),
            customerId,
            new ShippingAddress(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.State,
                request.ShippingAddress.PostalCode,
                request.ShippingAddress.Country
            )
        );
        
        // Add line items
        foreach (var item in request.Items)
        {
            order.AddLineItem(
                ProductId.From(item.ProductId),
                item.ProductName,
                new Money(item.UnitPrice, item.Currency),
                item.Quantity
            );
        }
        
        // Save order
        await _orderRepository.AddAsync(order, cancellationToken);
        
        // Reserve inventory
        await _inventoryService.ReserveItemsAsync(order.Id, request.Items, cancellationToken);
        
        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}",
            order.Id, customer.Id);
        
        // Map and return
        var orderDto = _mapper.Map<OrderDto>(order);
        return Result<OrderDto>.Success(orderDto);
    }
}

// Command validator
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");
        
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item")
            .Must(items => items.All(i => i.Quantity > 0))
            .WithMessage("All items must have positive quantity");
        
        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required")
            .SetValidator(new ShippingAddressValidator());
        
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}
```

### 3. Query Pattern

```csharp
// Query definition
public record GetOrdersByCustomerQuery : IRequest<Result<PagedResult<OrderSummaryDto>>>
{
    public Guid CustomerId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public OrderStatus? StatusFilter { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

// Query handler
public class GetOrdersByCustomerQueryHandler : 
    IRequestHandler<GetOrdersByCustomerQuery, Result<PagedResult<OrderSummaryDto>>>
{
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IMapper _mapper;
    
    public GetOrdersByCustomerQueryHandler(
        IOrderReadRepository orderReadRepository,
        IMapper mapper)
    {
        _orderReadRepository = orderReadRepository;
        _mapper = mapper;
    }
    
    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
        GetOrdersByCustomerQuery request,
        CancellationToken cancellationToken)
    {
        // Build specification
        var spec = new OrdersByCustomerSpecification(request.CustomerId);
        
        if (request.StatusFilter.HasValue)
            spec = spec.And(new OrdersByStatusSpecification(request.StatusFilter.Value));
        
        if (request.FromDate.HasValue)
            spec = spec.And(new OrdersAfterDateSpecification(request.FromDate.Value));
        
        if (request.ToDate.HasValue)
            spec = spec.And(new OrdersBeforeDateSpecification(request.ToDate.Value));
        
        // Get paginated results
        var orders = await _orderReadRepository.GetPagedAsync(
            spec,
            request.PageNumber,
            request.PageSize,
            request.SortBy ?? nameof(Order.OrderDate),
            request.SortDescending,
            cancellationToken);
        
        // Map to DTOs
        var orderDtos = _mapper.Map<List<OrderSummaryDto>>(orders.Items);
        
        var result = new PagedResult<OrderSummaryDto>(
            orderDtos,
            orders.TotalCount,
            request.PageNumber,
            request.PageSize
        );
        
        return Result<PagedResult<OrderSummaryDto>>.Success(result);
    }
}
```

## Advanced CQRS Patterns

### 1. Pipeline Behaviors

```csharp
// Validation behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Count != 0)
            throw new ValidationException(failures);
        
        return await next();
    }
}

// Logging behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestGuid = Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Handling {RequestName} ({RequestGuid}) {@Request}",
            requestName, requestGuid, request);
        
        var response = await next();
        
        _logger.LogInformation(
            "Handled {RequestName} ({RequestGuid})",
            requestName, requestGuid);
        
        return response;
    }
}

// Performance monitoring behavior
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;
    
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        
        var response = await next();
        
        _timer.Stop();
        
        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        
        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
                requestName, elapsedMilliseconds, request);
        }
        
        return response;
    }
}

// Transaction behavior
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    
    public TransactionBehavior(
        ApplicationDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if not a command
        if (request is not ICommand)
            return await next();
        
        var response = default(TResponse);
        var typeName = typeof(TRequest).Name;
        
        try
        {
            if (_dbContext.HasActiveTransaction)
            {
                return await next();
            }
            
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
                using (_logger.BeginScope("TransactionContext:{TransactionId}", transaction.TransactionId))
                {
                    _logger.LogInformation("Begin transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);
                    
                    response = await next();
                    
                    await _dbContext.CommitTransactionAsync(transaction, cancellationToken);
                    
                    _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}",
                        transaction.TransactionId, typeName);
                }
            });
            
            return response!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling transaction for {CommandName}", typeName);
            throw;
        }
    }
}
```

### 2. Read Model Projections

```csharp
// Separate read model for queries
public class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderLineItemReadModel> LineItems { get; set; } = new();
}

// Projection handler
public class OrderProjectionHandler :
    INotificationHandler<OrderCreated>,
    INotificationHandler<OrderConfirmed>,
    INotificationHandler<OrderShipped>
{
    private readonly IReadModelRepository _readModelRepository;
    
    public OrderProjectionHandler(IReadModelRepository readModelRepository)
    {
        _readModelRepository = readModelRepository;
    }
    
    public async Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        var readModel = new OrderReadModel
        {
            Id = notification.OrderId.Value,
            CustomerId = notification.CustomerId.Value,
            TotalAmount = notification.TotalAmount.Amount,
            Currency = notification.TotalAmount.Currency,
            Status = "Pending",
            OrderDate = notification.CreatedAt,
            ShippingAddress = notification.ShippingAddress.ToString()
        };
        
        await _readModelRepository.CreateOrderReadModelAsync(readModel, cancellationToken);
    }
    
    public async Task Handle(OrderConfirmed notification, CancellationToken cancellationToken)
    {
        await _readModelRepository.UpdateOrderStatusAsync(
            notification.OrderId.Value,
            "Confirmed",
            cancellationToken);
    }
    
    public async Task Handle(OrderShipped notification, CancellationToken cancellationToken)
    {
        await _readModelRepository.UpdateOrderStatusAsync(
            notification.OrderId.Value,
            "Shipped",
            cancellationToken);
    }
}
```

### 3. Command and Query Interfaces

```csharp
// Marker interfaces for commands and queries
public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

// Base command handler
public abstract class CommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
    protected abstract Task<Result> HandleCommand(TCommand command, CancellationToken cancellationToken);
    
    public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await HandleCommand(request, cancellationToken);
        }
        catch (BusinessRuleViolationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

// Base query handler
public abstract class QueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
    protected abstract Task<TResponse> HandleQuery(TQuery query, CancellationToken cancellationToken);
    
    public async Task<Result<TResponse>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HandleQuery(request, cancellationToken);
            return Result<TResponse>.Success(response);
        }
        catch (ResourceNotFoundException ex)
        {
            return Result<TResponse>.Failure(ex.Message);
        }
    }
}
```

### 4. Notification Pattern for Events

```csharp
// Domain event notification
public record CustomerRegisteredNotification : INotification
{
    public Guid CustomerId { get; init; }
    public string Email { get; init; }
    public string Name { get; init; }
    public DateTime RegisteredAt { get; init; }
}

// Multiple handlers for the same notification
public class SendWelcomeEmailHandler : INotificationHandler<CustomerRegisteredNotification>
{
    private readonly IEmailService _emailService;
    
    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task Handle(
        CustomerRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            notification.Name,
            cancellationToken);
    }
}

public class CreateCustomerProfileHandler : INotificationHandler<CustomerRegisteredNotification>
{
    private readonly IProfileService _profileService;
    
    public CreateCustomerProfileHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }
    
    public async Task Handle(
        CustomerRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        await _profileService.CreateDefaultProfileAsync(
            notification.CustomerId,
            cancellationToken);
    }
}

public class NotifyMarketingTeamHandler : INotificationHandler<CustomerRegisteredNotification>
{
    private readonly IMarketingService _marketingService;
    
    public NotifyMarketingTeamHandler(IMarketingService marketingService)
    {
        _marketingService = marketingService;
    }
    
    public async Task Handle(
        CustomerRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        await _marketingService.AddToNewCustomerCampaignAsync(
            notification.CustomerId,
            notification.Email,
            cancellationToken);
    }
}
```

## Separate Read/Write Databases

### 1. Configuration

```csharp
// Separate DbContexts for read and write
public class WriteDbContext : DbContext
{
    public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options) { }
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply write model configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WriteDbContext).Assembly,
            t => t.Namespace?.Contains("WriteModel") ?? false);
    }
}

public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }
    
    public DbSet<OrderReadModel> OrderReadModels => Set<OrderReadModel>();
    public DbSet<CustomerSummary> CustomerSummaries => Set<CustomerSummary>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply read model configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReadDbContext).Assembly,
            t => t.Namespace?.Contains("ReadModel") ?? false);
        
        // Read-only configurations
        modelBuilder.Entity<OrderReadModel>()
            .HasNoKey()
            .ToView("OrderSummaryView");
    }
}

// Program.cs configuration
builder.Services.AddDbContext<WriteDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WriteDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());
});

builder.Services.AddDbContext<ReadDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ReadDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
```

### 2. Synchronization Strategies

```csharp
// Event-based synchronization
public class ReadModelSynchronizer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReadModelSynchronizer> _logger;
    
    public ReadModelSynchronizer(
        IServiceProvider serviceProvider,
        ILogger<ReadModelSynchronizer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
                var projectionManager = scope.ServiceProvider.GetRequiredService<IProjectionManager>();
                
                var events = await eventStore.GetUnprocessedEventsAsync(stoppingToken);
                
                foreach (var @event in events)
                {
                    await projectionManager.ProjectAsync(@event, stoppingToken);
                    await eventStore.MarkAsProcessedAsync(@event.Id, stoppingToken);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing read models");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}

// CDC (Change Data Capture) based synchronization with Debezium
public class DebeziumEventProcessor : IHostedService
{
    private readonly IKafkaConsumer _kafkaConsumer;
    private readonly IProjectionManager _projectionManager;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _kafkaConsumer.SubscribeAsync(new[] { "dbserver1.inventory.customers" });
        
        _kafkaConsumer.OnMessageReceived += async (message) =>
        {
            var changeEvent = JsonSerializer.Deserialize<ChangeDataEvent>(message);
            await _projectionManager.HandleChangeDataAsync(changeEvent);
        };
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _kafkaConsumer.UnsubscribeAsync();
    }
}
```

## Testing CQRS Components

### 1. Testing Commands

```csharp
public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly CreateOrderCommandHandler _handler;
    
    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<CreateOrderCommandHandler>>();
        
        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _inventoryServiceMock.Object,
            mapperMock.Object,
            loggerMock.Object);
    }
    
    [Fact]
    public async Task Handle_Should_Create_Order_When_Valid()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<OrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 10.00m }
            },
            ShippingAddress = new ShippingAddressDto
            {
                Street = "123 Main St",
                City = "City",
                State = "ST",
                PostalCode = "12345",
                Country = "USA"
            }
        };
        
        var customer = CreateValidCustomer(command.CustomerId);
        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        
        _inventoryServiceMock
            .Setup(x => x.CheckAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 2. Testing Queries

```csharp
public class GetOrdersByCustomerQueryHandlerTests
{
    private readonly Mock<IOrderReadRepository> _readRepositoryMock;
    private readonly GetOrdersByCustomerQueryHandler _handler;
    
    public GetOrdersByCustomerQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<IOrderReadRepository>();
        var mapperMock = new Mock<IMapper>();
        
        _handler = new GetOrdersByCustomerQueryHandler(
            _readRepositoryMock.Object,
            mapperMock.Object);
    }
    
    [Fact]
    public async Task Handle_Should_Return_Paginated_Orders()
    {
        // Arrange
        var query = new GetOrdersByCustomerQuery
        {
            CustomerId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = 10
        };
        
        var orders = CreateOrderList(15);
        var pagedResult = new PagedResult<Order>(
            orders.Take(10).ToList(),
            15,
            1,
            10);
        
        _readRepositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<Specification<Order>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(15);
        result.Value!.Items.Count.Should().Be(10);
    }
}
```

### 3. Integration Testing

```csharp
public class CqrsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public CqrsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateOrder_EndToEnd_Should_Work()
    {
        // Arrange
        var createCustomerCommand = new { Name = "Test Customer", Email = "test@example.com" };
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", createCustomerCommand);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();
        
        var createOrderCommand = new
        {
            CustomerId = customer!.Id,
            Items = new[]
            {
                new { ProductId = Guid.NewGuid(), ProductName = "Product", Quantity = 1, UnitPrice = 10.00 }
            },
            ShippingAddress = new
            {
                Street = "123 Main St",
                City = "City",
                State = "ST",
                PostalCode = "12345",
                Country = "USA"
            }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderCommand);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.CustomerId.Should().Be(customer.Id);
    }
}
```

## Performance Optimization

### 1. Query Optimization

```csharp
// Compiled queries for frequently used queries
public static class CompiledQueries
{
    public static readonly Func<ReadDbContext, Guid, Task<OrderReadModel?>> GetOrderById =
        EF.CompileAsyncQuery((ReadDbContext context, Guid orderId) =>
            context.OrderReadModels.FirstOrDefault(o => o.Id == orderId));
    
    public static readonly Func<ReadDbContext, Guid, IAsyncEnumerable<OrderReadModel>> GetOrdersByCustomer =
        EF.CompileAsyncQuery((ReadDbContext context, Guid customerId) =>
            context.OrderReadModels
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate));
}

// Usage
public class OptimizedOrderQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly ReadDbContext _context;
    
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await CompiledQueries.GetOrderById(_context, request.OrderId);
        return MapToDto(order);
    }
}
```

### 2. Caching Strategies

```csharp
// Memory cache for queries
public class CachedQueryHandler<TQuery, TResponse> : IPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, ICacheable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedQueryHandler<TQuery, TResponse>> _logger;
    
    public CachedQueryHandler(IMemoryCache cache, ILogger<CachedQueryHandler<TQuery, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TQuery request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogInformation("Cache hit for {QueryType} with key {CacheKey}",
                typeof(TQuery).Name, cacheKey);
            return cachedResponse!;
        }
        
        var response = await next();
        
        _cache.Set(cacheKey, response, request.CacheDuration);
        
        _logger.LogInformation("Cached response for {QueryType} with key {CacheKey}",
            typeof(TQuery).Name, cacheKey);
        
        return response;
    }
}

// Redis cache for distributed scenarios
public class DistributedCacheHandler<TQuery, TResponse> : IPipelineBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>, ICacheable
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheHandler<TQuery, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TQuery request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{typeof(TQuery).Name}:{request.CacheKey}";
        
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<TResponse>(cachedData)!;
        }
        
        var response = await next();
        
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = request.CacheDuration
        };
        
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            options,
            cancellationToken);
        
        return response;
    }
}
```

## Best Practices

1. **Keep Commands and Queries Simple**: They should be DTOs without business logic
2. **One Handler per Command/Query**: Single responsibility principle
3. **Use Pipeline Behaviors**: For cross-cutting concerns
4. **Validate Early**: Use FluentValidation in pipeline
5. **Return Results**: Use Result pattern for explicit error handling
6. **Separate Read/Write Models**: Optimize for each use case
7. **Use Projections**: For complex read models
8. **Cache Queries**: But never cache commands
9. **Log Everything**: Commands and queries are system boundaries
10. **Test Handlers**: Unit test business logic, integration test the pipeline

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / MediatR 12*
*Status: CQRS Implementation Guide*