# Security Patterns - .NET 8 Clean Architecture

## Overview

This document outlines security patterns and best practices for the Clean Architecture .NET 8 implementation. Security is implemented as a cross-cutting concern while maintaining clean architecture principles.

## Authentication & Authorization

### JWT Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                var result = JsonSerializer.Serialize(new
                {
                    error = "Authentication failed",
                    message = context.Exception.Message
                });
                
                return context.Response.WriteAsync(result);
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                var result = JsonSerializer.Serialize(new
                {
                    error = "Unauthorized",
                    message = "You are not authorized to access this resource"
                });
                
                return context.Response.WriteAsync(result);
            }
        };
    });
```

### Authorization Policies

```csharp
// Program.cs - Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
    
    // Claim-based policies
    options.AddPolicy("CanManageOrders", policy =>
        policy.RequireClaim("permission", "orders:manage"));
    
    options.AddPolicy("CanViewReports", policy =>
        policy.RequireClaim("permission", "reports:view"));
    
    // Custom policies
    options.AddPolicy("ResourceOwner", policy =>
        policy.Requirements.Add(new ResourceOwnerRequirement()));
    
    options.AddPolicy("MinimumAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));
});

// Custom Authorization Requirements
public class ResourceOwnerRequirement : IAuthorizationRequirement
{
    // Empty marker interface
}

public class ResourceOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ResourceOwnerHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Get user ID from claims
        var userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }
        
        // Get resource ID from route
        var resourceId = httpContext?.Request.RouteValues["id"]?.ToString();
        if (string.IsNullOrEmpty(resourceId))
        {
            return Task.CompletedTask;
        }
        
        // Check if user owns the resource (implement your logic)
        if (UserOwnsResource(userId, resourceId))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
    
    private bool UserOwnsResource(string userId, string resourceId)
    {
        // Implement your ownership logic here
        // This could involve database queries or other validation
        return true; // Placeholder
    }
}
```

### Secure Controllers

```csharp
// WebApi/Controllers/CustomersController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Global authorization requirement
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Gets all customers (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllCustomers(
        CancellationToken cancellationToken)
    {
        var query = new GetAllCustomersQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
    
    /// <summary>
    /// Gets current user's customer profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var query = new GetCustomerByUserIdQuery { UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
    
    /// <summary>
    /// Updates customer (Owner or Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "ResourceOwner")]
    public async Task<IActionResult> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand
        {
            CustomerId = id,
            Name = request.Name,
            Email = request.Email
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

## Input Validation & Sanitization

### Data Validation

```csharp
// Application/Commands/CreateCustomer/CreateCustomerCommandValidator.cs
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Name contains invalid characters")
            .Must(NotContainScriptTags).WithMessage("Name contains potentially malicious content");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .Length(5, 256).WithMessage("Email must be between 5 and 256 characters")
            .Must(NotContainScriptTags).WithMessage("Email contains potentially malicious content");
        
        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.Street)
                .NotEmpty().WithMessage("Street is required")
                .Length(5, 200).WithMessage("Street must be between 5 and 200 characters")
                .Must(NotContainScriptTags).WithMessage("Street contains potentially malicious content");
        });
    }
    
    private static bool NotContainScriptTags(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;
        
        var scriptPattern = @"<script[^>]*>.*?</script>";
        return !Regex.IsMatch(value, scriptPattern, RegexOptions.IgnoreCase);
    }
}
```

### Input Sanitization

```csharp
// Application/Common/InputSanitizer.cs
namespace Application.Common;

public static class InputSanitizer
{
    /// <summary>
    /// Sanitizes HTML input to prevent XSS attacks
    /// </summary>
    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        // Use HtmlSanitizer library
        var sanitizer = new HtmlSanitizer();
        
        // Configure allowed tags and attributes
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.Add("p");
        sanitizer.AllowedTags.Add("b");
        sanitizer.AllowedTags.Add("i");
        sanitizer.AllowedTags.Add("br");
        
        sanitizer.AllowedAttributes.Clear();
        
        return sanitizer.Sanitize(input);
    }
    
    /// <summary>
    /// Sanitizes SQL input to prevent SQL injection
    /// </summary>
    public static string SanitizeSqlInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        // Remove or escape dangerous SQL characters
        return input
            .Replace("'", "''")
            .Replace("--", "")
            .Replace("/*", "")
            .Replace("*/", "")
            .Replace("xp_", "")
            .Replace("sp_", "");
    }
    
    /// <summary>
    /// Removes potentially dangerous characters from file names
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;
        
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
        
        // Additional security checks
        sanitized = sanitized.Replace("..", "")
            .Replace("~", "")
            .Trim('.')
            .Trim();
        
        return sanitized;
    }
}
```

## Data Protection

### Sensitive Data Handling

```csharp
// Domain/ValueObjects/EncryptedValue.cs
namespace Domain.ValueObjects;

public sealed class EncryptedValue : ValueObject
{
    public string EncryptedData { get; }
    
    public EncryptedValue(string plainTextValue, IDataProtector protector)
    {
        if (string.IsNullOrEmpty(plainTextValue))
            throw new ArgumentException("Value cannot be empty", nameof(plainTextValue));
        
        EncryptedData = protector.Protect(plainTextValue);
    }
    
    private EncryptedValue(string encryptedData)
    {
        EncryptedData = encryptedData;
    }
    
    public string Decrypt(IDataProtector protector)
    {
        return protector.Unprotect(EncryptedData);
    }
    
    public static EncryptedValue FromEncrypted(string encryptedData)
    {
        return new EncryptedValue(encryptedData);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EncryptedData;
    }
    
    // Never expose the actual encrypted value in ToString
    public override string ToString() => "[ENCRYPTED]";
}

// Usage in Domain Entity
public class Customer : AggregateRoot<CustomerId>
{
    public EncryptedValue? SocialSecurityNumber { get; private set; }
    
    public void SetSocialSecurityNumber(string ssn, IDataProtector protector)
    {
        if (string.IsNullOrEmpty(ssn))
        {
            SocialSecurityNumber = null;
            return;
        }
        
        // Validate SSN format
        if (!Regex.IsMatch(ssn, @"^\d{3}-?\d{2}-?\d{4}$"))
            throw new ArgumentException("Invalid SSN format", nameof(ssn));
        
        SocialSecurityNumber = new EncryptedValue(ssn, protector);
    }
}
```

### Data Protection Configuration

```csharp
// Program.cs
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"c:\keys\")) // Production: use Azure Key Vault
    .SetApplicationName("CleanArchitectureApp")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
    .ProtectKeysWithDpapi(); // Windows only, use ProtectKeysWithAzureKeyVault for Azure

// Data Protector Service
public interface IDataProtectionService
{
    string Protect(string plainText, string purpose);
    string Unprotect(string encryptedText, string purpose);
}

public class DataProtectionService : IDataProtectionService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    
    public DataProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }
    
    public string Protect(string plainText, string purpose)
    {
        var protector = _dataProtectionProvider.CreateProtector(purpose);
        return protector.Protect(plainText);
    }
    
    public string Unprotect(string encryptedText, string purpose)
    {
        var protector = _dataProtectionProvider.CreateProtector(purpose);
        return protector.Unprotect(encryptedText);
    }
}
```

## Secure Logging

### Structured Logging Without Sensitive Data

```csharp
// Application/Common/SecureLogger.cs
namespace Application.Common;

public static class SecureLogger
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "key", "token", "ssn", "socialSecurityNumber",
        "creditCard", "creditCardNumber", "cvv", "pin", "bankAccount"
    };
    
    public static void LogSecurely<T>(ILogger logger, LogLevel level, string message, T data)
    {
        var sanitizedData = SanitizeObject(data);
        logger.Log(level, message, sanitizedData);
    }
    
    private static object SanitizeObject<T>(T obj)
    {
        if (obj == null)
            return null!;
        
        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sanitized = new Dictionary<string, object>();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            var propertyName = property.Name;
            
            if (SensitiveFields.Contains(propertyName))
            {
                sanitized[propertyName] = "[REDACTED]";
            }
            else if (value != null && property.PropertyType == typeof(string))
            {
                var stringValue = (string)value;
                sanitized[propertyName] = RedactSensitivePatterns(stringValue);
            }
            else
            {
                sanitized[propertyName] = value;
            }
        }
        
        return sanitized;
    }
    
    private static string RedactSensitivePatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Credit card pattern
        input = Regex.Replace(input, @"\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}", "[CREDIT_CARD_REDACTED]");
        
        // SSN pattern
        input = Regex.Replace(input, @"\d{3}-?\d{2}-?\d{4}", "[SSN_REDACTED]");
        
        // Email addresses (partially redact)
        input = Regex.Replace(input, @"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
            match =>
            {
                var localPart = match.Groups[1].Value;
                var domain = match.Groups[2].Value;
                var redactedLocal = localPart.Length > 2 
                    ? $"{localPart[0]}***{localPart[^1]}"
                    : "***";
                return $"{redactedLocal}@{domain}";
            });
        
        return input;
    }
}

// Usage in handlers
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    
    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        SecureLogger.LogSecurely(_logger, LogLevel.Information, 
            "Creating customer with data: {@CustomerData}", request);
        
        // Handler implementation
    }
}
```

## API Security

### Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    // Specific endpoint limits
    options.AddPolicy("LoginLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();
app.UseRateLimiter();

// Controller with rate limiting
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("LoginLimit")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Login implementation
    }
}
```

### CORS Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder
            .WithOrigins(
                "https://yourdomain.com",
                "https://www.yourdomain.com",
                "https://app.yourdomain.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization")
            .SetIsOriginAllowedToReturnTrue() // Be careful with this
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
    
    options.AddPolicy("Development", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}
```

### Security Headers Middleware

```csharp
// Infrastructure/Middleware/SecurityHeadersMiddleware.cs
namespace Infrastructure.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");
        
        // HSTS (HTTPS Strict Transport Security)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }
        
        // Remove server information
        context.Response.Headers.Remove("Server");
        
        await _next(context);
    }
}

// Program.cs
app.UseMiddleware<SecurityHeadersMiddleware>();
```

## Database Security

### Connection String Security

```csharp
// appsettings.Production.json (use Azure Key Vault or similar)
{
  "ConnectionStrings": {
    "DefaultConnection": "#{ConnectionString}#" // Token replaced by deployment
  }
}

// Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Validate connection string doesn't contain credentials in plain text
if (connectionString?.Contains("password=", StringComparison.OrdinalIgnoreCase) == true ||
    connectionString?.Contains("pwd=", StringComparison.OrdinalIgnoreCase) == true)
{
    throw new InvalidOperationException("Connection string should not contain plain text passwords");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});
```

### SQL Injection Prevention

```csharp
// Infrastructure/Repositories/CustomerRepository.cs
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    
    // Good: Parameterized queries through LINQ
    public async Task<IEnumerable<Customer>> SearchAsync(
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        // EF Core automatically parameterizes this
        return await _context.Customers
            .Where(c => c.Name.Contains(searchTerm) || c.Email.Value.Contains(searchTerm))
            .ToListAsync(cancellationToken);
    }
    
    // Good: Parameterized raw SQL when needed
    public async Task<IEnumerable<Customer>> GetCustomersByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FromSqlRaw("SELECT * FROM Customers WHERE Status = {0}", status)
            .ToListAsync(cancellationToken);
    }
    
    // BAD: Never do this - vulnerable to SQL injection
    // public async Task<IEnumerable<Customer>> BadSearchAsync(string searchTerm)
    // {
    //     var sql = $"SELECT * FROM Customers WHERE Name LIKE '%{searchTerm}%'";
    //     return await _context.Customers.FromSqlRaw(sql).ToListAsync();
    // }
}
```

## Secrets Management

### Azure Key Vault Integration

```csharp
// Program.cs
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}

// Secrets Service
public interface ISecretsService
{
    Task<string> GetSecretAsync(string secretName);
    Task SetSecretAsync(string secretName, string secretValue);
}

public class AzureKeyVaultSecretsService : ISecretsService
{
    private readonly SecretClient _secretClient;
    
    public AzureKeyVaultSecretsService(IConfiguration configuration)
    {
        var keyVaultUrl = configuration["KeyVaultUrl"];
        _secretClient = new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential());
    }
    
    public async Task<string> GetSecretAsync(string secretName)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new InvalidOperationException($"Secret '{secretName}' not found", ex);
        }
    }
    
    public async Task SetSecretAsync(string secretName, string secretValue)
    {
        await _secretClient.SetSecretAsync(secretName, secretValue);
    }
}
```

### Local Development Secrets

```bash
# Use .NET Secret Manager for local development
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-super-secret-key-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

## Security Testing

### Authentication Tests

```csharp
// Tests/WebApi.IntegrationTests/Security/AuthenticationTests.cs
namespace WebApi.IntegrationTests.Security;

public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GET_SecureEndpoint_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/customers");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GET_SecureEndpoint_WithValidToken_Returns200()
    {
        // Arrange
        var token = GenerateValidJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Act
        var response = await _client.GetAsync("/api/v1/customers/me");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task GET_AdminEndpoint_WithUserToken_Returns403()
    {
        // Arrange
        var userToken = GenerateJwtToken("user", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        
        // Act
        var response = await _client.GetAsync("/api/v1/customers");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    private string GenerateValidJwtToken()
    {
        return GenerateJwtToken("testuser", new[] { "Admin" });
    }
    
    private string GenerateJwtToken(string userId, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-that-is-long-enough-for-hmac-sha256"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("jti", Guid.NewGuid().ToString())
        };
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Input Validation Tests

```csharp
// Tests/Application.UnitTests/Security/InputValidationTests.cs
namespace Application.UnitTests.Security;

public class InputValidationTests
{
    private readonly CreateCustomerCommandValidator _validator;
    
    public InputValidationTests()
    {
        _validator = new CreateCustomerCommandValidator();
    }
    
    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    public async Task Validate_MaliciousInput_ShouldFail(string maliciousInput)
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = maliciousInput,
            Email = "test@example.com"
        };
        
        // Act
        var result = await _validator.ValidateAsync(command);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("potentially malicious content"));
    }
    
    [Theory]
    [InlineData("'; DROP TABLE Customers; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("admin'/*")]
    public async Task Validate_SqlInjectionAttempt_ShouldFail(string sqlInjectionAttempt)
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = sqlInjectionAttempt,
            Email = "test@example.com"
        };
        
        // Act
        var result = await _validator.ValidateAsync(command);
        
        // Assert
        result.IsValid.Should().BeFalse();
    }
}
```

## Security Monitoring

### Security Events Logging

```csharp
// Application/Common/SecurityEvents.cs
namespace Application.Common;

public static class SecurityEvents
{
    public static class EventIds
    {
        public static readonly EventId LoginAttempt = new(1001, "LoginAttempt");
        public static readonly EventId LoginSuccess = new(1002, "LoginSuccess");
        public static readonly EventId LoginFailure = new(1003, "LoginFailure");
        public static readonly EventId UnauthorizedAccess = new(1004, "UnauthorizedAccess");
        public static readonly EventId SuspiciousActivity = new(1005, "SuspiciousActivity");
        public static readonly EventId DataAccess = new(1006, "DataAccess");
        public static readonly EventId DataModification = new(1007, "DataModification");
    }
    
    public static void LogLoginAttempt(ILogger logger, string userId, string ipAddress)
    {
        logger.LogInformation(EventIds.LoginAttempt,
            "Login attempt for user {UserId} from IP {IpAddress}",
            userId, ipAddress);
    }
    
    public static void LogUnauthorizedAccess(ILogger logger, string userId, string resource, string ipAddress)
    {
        logger.LogWarning(EventIds.UnauthorizedAccess,
            "Unauthorized access attempt by user {UserId} to resource {Resource} from IP {IpAddress}",
            userId, resource, ipAddress);
    }
    
    public static void LogSuspiciousActivity(ILogger logger, string activity, string userId, string details)
    {
        logger.LogWarning(EventIds.SuspiciousActivity,
            "Suspicious activity detected: {Activity} by user {UserId}. Details: {Details}",
            activity, userId, details);
    }
}

// Usage in middleware
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            await _next(context);
        }
        finally
        {
            var userId = context.User.FindFirst("sub")?.Value ?? "Anonymous";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            var statusCode = context.Response.StatusCode;
            var duration = DateTime.UtcNow - startTime;
            
            if (statusCode == 401 || statusCode == 403)
            {
                SecurityEvents.LogUnauthorizedAccess(_logger, userId, endpoint, ipAddress!);
            }
            
            // Log all access to sensitive endpoints
            if (IsSensitiveEndpoint(context.Request.Path))
            {
                _logger.LogInformation(SecurityEvents.EventIds.DataAccess,
                    "Access to sensitive endpoint {Endpoint} by user {UserId} from IP {IpAddress}. Status: {StatusCode}, Duration: {Duration}ms",
                    endpoint, userId, ipAddress, statusCode, duration.TotalMilliseconds);
            }
        }
    }
    
    private static bool IsSensitiveEndpoint(PathString path)
    {
        var sensitivePatterns = new[]
        {
            "/api/v1/customers",
            "/api/v1/orders",
            "/api/v1/admin",
            "/api/v1/reports"
        };
        
        return sensitivePatterns.Any(pattern => 
            path.StartsWithSegments(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
```

## Security Checklist

### Development Checklist
- [ ] All API endpoints require authentication
- [ ] Authorization policies are properly configured
- [ ] Input validation is implemented for all user inputs
- [ ] SQL injection prevention through parameterized queries
- [ ] XSS prevention through input sanitization
- [ ] Sensitive data is encrypted at rest and in transit
- [ ] Secrets are stored in secure configuration (Key Vault)
- [ ] Security headers are configured
- [ ] CORS is properly configured for production
- [ ] Rate limiting is implemented
- [ ] Security events are logged
- [ ] Error messages don't leak sensitive information

### Production Checklist
- [ ] HTTPS is enforced
- [ ] SSL/TLS certificates are valid
- [ ] Database connections use encrypted connections
- [ ] Firewall rules are properly configured
- [ ] Security monitoring is in place
- [ ] Regular security updates are applied
- [ ] Penetration testing has been performed
- [ ] Compliance requirements are met (GDPR, PCI-DSS, etc.)

---
*Document Version: 1.0*
*Last Updated: 2025-08-08*
*Framework: .NET 8 / ASP.NET Core*
*Status: Security Implementation Guide*