# AWS Logging Patterns - .NET 8 Clean Architecture

## Overview

This document provides comprehensive guidance for implementing logging in .NET 8 applications with environment-specific configurations: local debugging for development and AWS CloudWatch for production deployments.

## Environment-Based Logging Strategy

### Configuration Strategy

```json
// appsettings.json (Base configuration)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  },
  "AWS": {
    "Region": "us-east-1",
    "Profile": "default"
  }
}

// appsettings.Development.json (Local development)
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        }
      },
      {
        "Name": "Debug"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
        }
      }
    ]
  }
}

// appsettings.Production.json (AWS deployment)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      },
      {
        "Name": "AWSCloudWatch",
        "Args": {
          "logGroup": "/aws/ecs/clean-architecture-app",
          "logStreamPrefix": "clean-architecture-",
          "region": "us-east-1",
          "batchSizeLimit": 100,
          "period": "00:00:05",
          "createLogGroup": true,
          "logFormat": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AWS": {
    "CloudWatch": {
      "LogGroup": "/aws/ecs/clean-architecture-app",
      "LogStreamPrefix": "clean-architecture-",
      "Region": "us-east-1"
    },
    "XRay": {
      "Enabled": true,
      "SamplingRate": 0.1
    }
  }
}
```

## Serilog Configuration

### Program.cs Setup

```csharp
// Program.cs
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using AWS.Logger.SeriLog;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog early in the pipeline
Log.Logger = CreateSerilogLogger(builder.Configuration, builder.Environment);
builder.Host.UseSerilog(Log.Logger);

try
{
    Log.Information("Starting Clean Architecture application");
    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    
    // Configure services
    ConfigureServices(builder);
    
    var app = builder.Build();
    
    // Configure pipeline
    ConfigurePipeline(app);
    
    Log.Information("Application configured successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

static ILogger CreateSerilogLogger(IConfiguration configuration, IWebHostEnvironment environment)
{
    var loggerConfiguration = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CleanArchitectureApp")
        .Enrich.WithProperty("Environment", environment.EnvironmentName)
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .ReadFrom.Configuration(configuration);

    if (environment.IsDevelopment())
    {
        // Local development logging
        loggerConfiguration
            .MinimumLevel.Debug()
            .WriteTo.Console(
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}")
            .WriteTo.Debug()
            .WriteTo.File(
                path: "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}");
    }
    else if (environment.IsProduction())
    {
        // AWS CloudWatch logging
        var awsConfiguration = new AWSLoggerConfig
        {
            LogGroup = configuration["AWS:CloudWatch:LogGroup"] ?? "/aws/ecs/clean-architecture-app",
            LogStreamNamePrefix = configuration["AWS:CloudWatch:LogStreamPrefix"] ?? "clean-architecture-",
            Region = configuration["AWS:Region"] ?? "us-east-1",
            BatchSizeLimit = 100,
            BatchPushInterval = TimeSpan.FromSeconds(5),
            CreateLogGroup = true,
            LogFormat = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        };

        loggerConfiguration
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.AWSSeriLog(awsConfiguration);
    }
    else
    {
        // Staging/Testing environments
        loggerConfiguration
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.File(
                path: "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                formatter: new JsonFormatter());
    }

    return loggerConfiguration.CreateLogger();
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    // Add AWS services
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        builder.Services.AddAWSService<Amazon.CloudWatchLogs.IAmazonCloudWatchLogs>();
        
        // Add X-Ray tracing
        if (builder.Configuration.GetValue<bool>("AWS:XRay:Enabled"))
        {
            builder.Services.AddXRay(builder.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();
        }
    }
    
    // Add correlation ID middleware
    builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
    
    // Other service configurations...
    builder.Services.AddControllers();
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
}

static void ConfigurePipeline(WebApplication app)
{
    // Add request logging middleware
    app.UseMiddleware<RequestLoggingMiddleware>();
    
    // Add correlation ID middleware
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // Add X-Ray tracing middleware (AWS only)
    if (!app.Environment.IsDevelopment())
    {
        app.UseXRay("CleanArchitectureApp");
    }
    
    // Other middleware configurations...
    app.UseRouting();
    app.MapControllers();
}
```

### NuGet Packages Required

```xml
<!-- Directory.Build.props or individual project files -->
<Project>
  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="7.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="4.1.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.Debug" Version="2.0.0" />
    <PackageReference Include="Serilog.Enrichers.Environment" Version="2.3.0" />
    <PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
    <PackageReference Include="Serilog.Formatting.Json" Version="1.0.0" />
    
    <!-- AWS-specific packages -->
    <PackageReference Include="AWS.Logger.SeriLog" Version="3.1.0" />
    <PackageReference Include="AWSSDK.CloudWatchLogs" Version="3.7.300" />
    <PackageReference Include="AWSXRayRecorder.Handlers.AspNetCore" Version="2.13.0" />
    <PackageReference Include="AWSXRayRecorder.Handlers.AwsSdk" Version="2.11.0" />
  </ItemGroup>
</Project>
```

## Correlation ID Implementation

### Correlation ID Provider

```csharp
// Application/Common/ICorrelationIdProvider.cs
namespace Application.Common;

public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}

public class CorrelationIdProvider : ICorrelationIdProvider
{
    private string _correlationId = Guid.NewGuid().ToString();
    
    public string CorrelationId => _correlationId;
    
    public void SetCorrelationId(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            _correlationId = correlationId;
        }
    }
}
```

### Correlation ID Middleware

```csharp
// Infrastructure/Middleware/CorrelationIdMiddleware.cs
namespace Infrastructure.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();
        
        correlationIdProvider.SetCorrelationId(correlationId);
        
        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
            return Task.CompletedTask;
        });
        
        // Add correlation ID to log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

## Request Logging Middleware

```csharp
// Infrastructure/Middleware/RequestLoggingMiddleware.cs
namespace Infrastructure.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        _logger.LogInformation("HTTP Request started: {Method} {Path} {QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP Request failed: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;
            
            _logger.Log(logLevel, 
                "HTTP Request completed: {Method} {Path} {StatusCode} in {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## Structured Logging in Application Layer

### Command Handler with Structured Logging

```csharp
// Application/Commands/CreateCustomer/CreateCustomerCommandHandler.cs
namespace Application.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    
    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IMapper mapper,
        ILogger<CreateCustomerCommandHandler> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }
    
    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = LogContext.PushProperty("Operation", "CreateCustomer");
        using var correlationActivity = LogContext.PushProperty("CorrelationId", _correlationIdProvider.CorrelationId);
        
        _logger.LogInformation("Creating customer with email {Email}",
            request.Email);
        
        try
        {
            // Validation
            var email = new Email(request.Email);
            
            _logger.LogDebug("Checking if email {Email} already exists", request.Email);
            
            if (await _customerRepository.EmailExistsAsync(email, cancellationToken))
            {
                _logger.LogWarning("Customer creation failed: Email {Email} already exists", request.Email);
                return Result<CustomerDto>.Failure($"Email {request.Email} is already in use");
            }
            
            // Create customer
            var customer = new Customer(
                CustomerId.Create(),
                request.Name,
                email,
                CreateAddress(request.Address),
                request.Preferences);
            
            _logger.LogDebug("Customer entity created with ID {CustomerId}", customer.Id);
            
            // Persist
            await _customerRepository.AddAsync(customer, cancellationToken);
            
            _logger.LogInformation("Customer created successfully with ID {CustomerId} and email {Email}",
                customer.Id, request.Email);
            
            // Map to DTO
            var dto = _mapper.Map<CustomerDto>(customer);
            
            return Result<CustomerDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Customer creation failed due to validation error for email {Email}",
                request.Email);
            return Result<CustomerDto>.Failure($"Validation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating customer with email {Email}",
                request.Email);
            return Result<CustomerDto>.Failure("An unexpected error occurred while creating the customer");
        }
    }
    
    private static Address? CreateAddress(AddressDto? addressDto)
    {
        if (addressDto == null) return null;
        
        return new Address(
            addressDto.Street,
            addressDto.City,
            addressDto.State,
            addressDto.PostalCode,
            addressDto.Country);
    }
}
```

### Repository with Logging

```csharp
// Infrastructure/Repositories/CustomerRepository.cs
namespace Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerRepository> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    
    public CustomerRepository(
        ApplicationDbContext context,
        ILogger<CustomerRepository> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _context = context;
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }
    
    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
    {
        using var activity = LogContext.PushProperty("RepositoryOperation", "GetCustomerById");
        using var correlationActivity = LogContext.PushProperty("CorrelationId", _correlationIdProvider.CorrelationId);
        
        _logger.LogDebug("Retrieving customer with ID {CustomerId}", id);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            
            stopwatch.Stop();
            
            if (customer != null)
            {
                _logger.LogDebug("Customer {CustomerId} retrieved successfully in {Duration}ms",
                    id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("Customer {CustomerId} not found in {Duration}ms",
                    id, stopwatch.ElapsedMilliseconds);
            }
            
            return customer;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error retrieving customer {CustomerId} after {Duration}ms",
                id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
    
    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        using var activity = LogContext.PushProperty("RepositoryOperation", "AddCustomer");
        using var correlationActivity = LogContext.PushProperty("CorrelationId", _correlationIdProvider.CorrelationId);
        
        _logger.LogInformation("Adding new customer with ID {CustomerId}", customer.Id);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Customer {CustomerId} added successfully in {Duration}ms",
                customer.Id, stopwatch.ElapsedMilliseconds);
            
            return customer;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error adding customer {CustomerId} after {Duration}ms",
                customer.Id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## AWS X-Ray Tracing Integration

### X-Ray Configuration

```csharp
// Infrastructure/Extensions/XRayExtensions.cs
namespace Infrastructure.Extensions;

public static class XRayExtensions
{
    public static IServiceCollection AddXRayTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var xrayConfig = configuration.GetSection("AWS:XRay");
        
        if (xrayConfig.GetValue<bool>("Enabled"))
        {
            services.AddXRay(options =>
            {
                options.SamplingRuleManifest = xrayConfig["SamplingRuleManifest"];
                options.PluginSetting = xrayConfig["PluginSetting"];
                options.DaemonAddress = xrayConfig["DaemonAddress"];
                options.TraceId = xrayConfig["TraceId"];
                options.SamplingRate = xrayConfig.GetValue<double>("SamplingRate", 0.1);
                options.UseRuntimeErrors = xrayConfig.GetValue<bool>("UseRuntimeErrors", true);
                options.CollectSqlQueries = xrayConfig.GetValue<bool>("CollectSqlQueries", false);
            });
            
            // Register X-Ray for AWS SDK calls
            AWSSDKHandler.RegisterXRayForAllServices();
        }
        
        return services;
    }
}
```

### Custom X-Ray Segments

```csharp
// Application/Common/XRayTracing.cs
namespace Application.Common;

public static class XRayTracing
{
    public static async Task<T> TraceAsync<T>(
        string segmentName,
        Func<Task<T>> operation,
        Dictionary<string, object>? metadata = null)
    {
        if (!AWSXRayRecorder.Instance.IsTracingDisabled())
        {
            return await AWSXRayRecorder.Instance.TraceAsync(segmentName, async () =>
            {
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        AWSXRayRecorder.Instance.AddMetadata(segmentName, kvp.Key, kvp.Value);
                    }
                }
                
                return await operation();
            });
        }
        
        return await operation();
    }
    
    public static async Task TraceAsync(
        string segmentName,
        Func<Task> operation,
        Dictionary<string, object>? metadata = null)
    {
        if (!AWSXRayRecorder.Instance.IsTracingDisabled())
        {
            await AWSXRayRecorder.Instance.TraceAsync(segmentName, async () =>
            {
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        AWSXRayRecorder.Instance.AddMetadata(segmentName, kvp.Key, kvp.Value);
                    }
                }
                
                await operation();
            });
        }
        else
        {
            await operation();
        }
    }
}

// Usage in command handler
public async Task<Result<CustomerDto>> Handle(
    CreateCustomerCommand request,
    CancellationToken cancellationToken)
{
    return await XRayTracing.TraceAsync("CreateCustomer", async () =>
    {
        // Your existing handler logic here
        return result;
    }, new Dictionary<string, object>
    {
        ["CustomerEmail"] = request.Email,
        ["Operation"] = "CreateCustomer"
    });
}
```

## AWS CloudWatch Custom Metrics

### Custom Metrics Service

```csharp
// Infrastructure/Services/CloudWatchMetricsService.cs
namespace Infrastructure.Services;

public interface IMetricsService
{
    Task RecordCounterAsync(string metricName, double value, Dictionary<string, string>? dimensions = null);
    Task RecordTimerAsync(string metricName, TimeSpan duration, Dictionary<string, string>? dimensions = null);
    Task RecordGaugeAsync(string metricName, double value, Dictionary<string, string>? dimensions = null);
}

public class CloudWatchMetricsService : IMetricsService
{
    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly ILogger<CloudWatchMetricsService> _logger;
    private readonly string _namespace;
    
    public CloudWatchMetricsService(
        IAmazonCloudWatch cloudWatch,
        ILogger<CloudWatchMetricsService> logger,
        IConfiguration configuration)
    {
        _cloudWatch = cloudWatch;
        _logger = logger;
        _namespace = configuration["AWS:CloudWatch:MetricsNamespace"] ?? "CleanArchitecture";
    }
    
    public async Task RecordCounterAsync(
        string metricName, 
        double value, 
        Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum>
                {
                    new()
                    {
                        MetricName = metricName,
                        Value = value,
                        Unit = StandardUnit.Count,
                        Timestamp = DateTime.UtcNow,
                        Dimensions = ConvertDimensions(dimensions)
                    }
                }
            };
            
            await _cloudWatch.PutMetricDataAsync(request);
            
            _logger.LogDebug("Recorded metric {MetricName} with value {Value}", metricName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record metric {MetricName}", metricName);
        }
    }
    
    public async Task RecordTimerAsync(
        string metricName, 
        TimeSpan duration, 
        Dictionary<string, string>? dimensions = null)
    {
        await RecordGaugeAsync($"{metricName}.Duration", duration.TotalMilliseconds, dimensions);
    }
    
    public async Task RecordGaugeAsync(
        string metricName, 
        double value, 
        Dictionary<string, string>? dimensions = null)
    {
        try
        {
            var request = new PutMetricDataRequest
            {
                Namespace = _namespace,
                MetricData = new List<MetricDatum>
                {
                    new()
                    {
                        MetricName = metricName,
                        Value = value,
                        Unit = StandardUnit.None,
                        Timestamp = DateTime.UtcNow,
                        Dimensions = ConvertDimensions(dimensions)
                    }
                }
            };
            
            await _cloudWatch.PutMetricDataAsync(request);
            
            _logger.LogDebug("Recorded gauge metric {MetricName} with value {Value}", metricName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gauge metric {MetricName}", metricName);
        }
    }
    
    private static List<Dimension> ConvertDimensions(Dictionary<string, string>? dimensions)
    {
        if (dimensions == null) return new List<Dimension>();
        
        return dimensions.Select(d => new Dimension
        {
            Name = d.Key,
            Value = d.Value
        }).ToList();
    }
}

// No-op implementation for local development
public class NoOpMetricsService : IMetricsService
{
    public Task RecordCounterAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
    {
        return Task.CompletedTask;
    }
    
    public Task RecordTimerAsync(string metricName, TimeSpan duration, Dictionary<string, string>? dimensions = null)
    {
        return Task.CompletedTask;
    }
    
    public Task RecordGaugeAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
    {
        return Task.CompletedTask;
    }
}

// Registration in Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IMetricsService, NoOpMetricsService>();
}
else
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonCloudWatch>();
    builder.Services.AddSingleton<IMetricsService, CloudWatchMetricsService>();
}
```

### Metrics Pipeline Behavior

```csharp
// Application/Behaviors/MetricsBehavior.cs
namespace Application.Behaviors;

public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsBehavior<TRequest, TResponse>> _logger;
    
    public MetricsBehavior(
        IMetricsService metricsService,
        ILogger<MetricsBehavior<TRequest, TResponse>> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            // Record success metrics
            await _metricsService.RecordCounterAsync($"{requestName}.Success", 1,
                new Dictionary<string, string>
                {
                    ["RequestType"] = requestName,
                    ["Status"] = "Success"
                });
            
            await _metricsService.RecordTimerAsync($"{requestName}.Duration", stopwatch.Elapsed,
                new Dictionary<string, string>
                {
                    ["RequestType"] = requestName
                });
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record failure metrics
            await _metricsService.RecordCounterAsync($"{requestName}.Failure", 1,
                new Dictionary<string, string>
                {
                    ["RequestType"] = requestName,
                    ["Status"] = "Failure",
                    ["ExceptionType"] = ex.GetType().Name
                });
            
            _logger.LogError(ex, "Request {RequestName} failed after {Duration}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
```

## Docker Configuration for Local Development

### Docker Compose for Local Logging

```yaml
# docker-compose.override.yml
version: '3.8'

services:
  app:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
    ports:
      - "5000:80"
    volumes:
      - ./logs:/app/logs
    depends_on:
      - localstack
      
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=logs,cloudwatch
      - DEBUG=1
      - LAMBDA_EXECUTOR=docker
      - DOCKER_HOST=unix:///var/run/docker.sock
    volumes:
      - "/var/run/docker.sock:/var/run/docker.sock"
      - "./localstack:/tmp/localstack"

  # Optional: Log aggregation for local development
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      - ACCEPT_EULA=Y
    volumes:
      - seq-data:/data

volumes:
  seq-data:
```

### Dockerfile with Logging

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create logs directory
RUN mkdir -p /app/logs

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/WebApi/WebApi.csproj", "src/WebApi/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/WebApi/WebApi.csproj"
COPY . .
WORKDIR "/src/src/WebApi"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set permissions for logs directory
RUN chmod 755 /app/logs

ENTRYPOINT ["dotnet", "WebApi.dll"]
```

## Log Analysis and Monitoring

### CloudWatch Insights Queries

```sql
-- Query 1: Find errors in the last hour
fields @timestamp, @message, @logStream
| filter @message like /ERROR/
| filter @timestamp > now() - 1h
| sort @timestamp desc
| limit 100

-- Query 2: Performance analysis by endpoint
fields @timestamp, @message, @duration
| filter @message like /HTTP Request completed/
| parse @message /HTTP Request completed: (?<method>\w+) (?<path>\/\S*) (?<statusCode>\d+) in (?<duration>\d+)ms/
| filter statusCode >= 200 and statusCode < 300
| stats avg(duration), min(duration), max(duration), count() by path
| sort avg(duration) desc

-- Query 3: Find slow database queries
fields @timestamp, @message, @duration
| filter @message like /in \d+ms/
| parse @message /(?<operation>\w+) in (?<duration>\d+)ms/
| filter duration > 1000
| sort @timestamp desc

-- Query 4: Correlation ID tracking
fields @timestamp, @message, CorrelationId
| filter CorrelationId = "your-correlation-id-here"
| sort @timestamp asc

-- Query 5: Error rate by time window
fields @timestamp, @message
| filter @message like /ERROR/ or @message like /FATAL/
| bin(@timestamp, 5m) as time_window
| stats count(*) as error_count by time_window
| sort time_window desc
```

### CloudWatch Alarms Configuration

```json
// cloudformation/monitoring.json
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "CloudWatch Alarms for Clean Architecture App",
  
  "Resources": {
    "HighErrorRateAlarm": {
      "Type": "AWS::CloudWatch::Alarm",
      "Properties": {
        "AlarmName": "CleanArchitecture-HighErrorRate",
        "AlarmDescription": "High error rate detected",
        "MetricName": "Errors",
        "Namespace": "AWS/Logs",
        "Statistic": "Sum",
        "Period": 300,
        "EvaluationPeriods": 2,
        "Threshold": 10,
        "ComparisonOperator": "GreaterThanThreshold",
        "Dimensions": [
          {
            "Name": "LogGroupName",
            "Value": "/aws/ecs/clean-architecture-app"
          }
        ],
        "AlarmActions": [
          { "Ref": "SNSTopicArn" }
        ]
      }
    },
    
    "SlowResponseAlarm": {
      "Type": "AWS::CloudWatch::Alarm",
      "Properties": {
        "AlarmName": "CleanArchitecture-SlowResponse",
        "AlarmDescription": "Average response time is too high",
        "MetricName": "Duration",
        "Namespace": "CleanArchitecture",
        "Statistic": "Average",
        "Period": 300,
        "EvaluationPeriods": 2,
        "Threshold": 2000,
        "ComparisonOperator": "GreaterThanThreshold",
        "AlarmActions": [
          { "Ref": "SNSTopicArn" }
        ]
      }
    }
  }
}
```

## Best Practices Summary

### Development Environment
✅ **Console Logging**: Colored, human-readable format  
✅ **Debug Output**: For IDE integration  
✅ **File Logging**: Local file with rotation  
✅ **Detailed Logs**: Debug level for troubleshooting  

### Production Environment
✅ **CloudWatch Integration**: Structured JSON logs  
✅ **X-Ray Tracing**: Distributed tracing for performance  
✅ **Custom Metrics**: Business and performance metrics  
✅ **Security Logging**: Audit trails and security events  

### Cross-Cutting Concerns
✅ **Correlation IDs**: Track requests across services  
✅ **Structured Logging**: Consistent log format  
✅ **Performance Monitoring**: Request duration tracking  
✅ **Error Tracking**: Comprehensive error logging  

### Cost Optimization
✅ **Log Retention**: Configure appropriate retention periods  
✅ **Sampling**: Use sampling for high-volume trace data  
✅ **Filtering**: Filter out unnecessary debug logs in production  
✅ **Batching**: Batch log entries to reduce CloudWatch costs  

This comprehensive AWS logging setup provides environment-specific logging with local debugging for development and full AWS integration for production deployments.

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / Serilog / AWS CloudWatch*
*Status: AWS Logging Implementation Guide*