# AWS JWT Security Patterns - .NET 8 Clean Architecture

## Overview

This document provides comprehensive guidance for implementing JWT-based authentication and authorization in .NET 8 applications deployed on AWS, covering AWS Cognito integration, API Gateway JWT authorizers, and secure token management patterns.

## AWS Cognito Integration

### Cognito User Pool Configuration

```json
// cloudformation/cognito.json
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "AWS Cognito User Pool for Clean Architecture App",
  
  "Parameters": {
    "Environment": {
      "Type": "String",
      "Default": "dev",
      "AllowedValues": ["dev", "staging", "prod"]
    },
    "AppName": {
      "Type": "String",
      "Default": "clean-architecture"
    }
  },
  
  "Resources": {
    "CognitoUserPool": {
      "Type": "AWS::Cognito::UserPool",
      "Properties": {
        "UserPoolName": {
          "Fn::Sub": "${AppName}-user-pool-${Environment}"
        },
        "Schema": [
          {
            "Name": "email",
            "AttributeDataType": "String",
            "Mutable": false,
            "Required": true
          },
          {
            "Name": "name",
            "AttributeDataType": "String",
            "Mutable": true,
            "Required": true
          },
          {
            "Name": "custom:role",
            "AttributeDataType": "String",
            "Mutable": true,
            "Required": false
          },
          {
            "Name": "custom:tenant_id",
            "AttributeDataType": "String",
            "Mutable": false,
            "Required": false
          }
        ],
        "Policies": {
          "PasswordPolicy": {
            "MinimumLength": 12,
            "RequireUppercase": true,
            "RequireLowercase": true,
            "RequireNumbers": true,
            "RequireSymbols": true,
            "TemporaryPasswordValidityDays": 7
          }
        },
        "MfaConfiguration": "OPTIONAL",
        "EnabledMfas": ["SMS_MFA", "SOFTWARE_TOKEN_MFA"],
        "AccountRecoverySetting": {
          "RecoveryMechanisms": [
            {
              "Name": "verified_email",
              "Priority": 1
            }
          ]
        },
        "AutoVerifiedAttributes": ["email"],
        "AliasAttributes": ["email"],
        "UsernameConfiguration": {
          "CaseSensitive": false
        },
        "UserPoolTags": {
          "Environment": { "Ref": "Environment" },
          "Application": { "Ref": "AppName" }
        }
      }
    },
    
    "CognitoUserPoolClient": {
      "Type": "AWS::Cognito::UserPoolClient",
      "Properties": {
        "ClientName": {
          "Fn::Sub": "${AppName}-client-${Environment}"
        },
        "UserPoolId": { "Ref": "CognitoUserPool" },
        "GenerateSecret": false,
        "RefreshTokenValidity": 30,
        "AccessTokenValidity": 60,
        "IdTokenValidity": 60,
        "TokenValidityUnits": {
          "RefreshToken": "days",
          "AccessToken": "minutes",
          "IdToken": "minutes"
        },
        "ExplicitAuthFlows": [
          "ADMIN_NO_SRP_AUTH",
          "USER_SRP_AUTH",
          "ALLOW_USER_PASSWORD_AUTH",
          "ALLOW_REFRESH_TOKEN_AUTH"
        ],
        "PreventUserExistenceErrors": "ENABLED",
        "SupportedIdentityProviders": ["COGNITO"]
      }
    },
    
    "CognitoIdentityPool": {
      "Type": "AWS::Cognito::IdentityPool",
      "Properties": {
        "IdentityPoolName": {
          "Fn::Sub": "${AppName}-identity-pool-${Environment}"
        },
        "AllowUnauthenticatedIdentities": false,
        "CognitoIdentityProviders": [
          {
            "ClientId": { "Ref": "CognitoUserPoolClient" },
            "ProviderName": { "Fn::GetAtt": "CognitoUserPool.ProviderName" },
            "ServerSideTokenCheck": true
          }
        ]
      }
    },
    
    "CognitoIdentityPoolRoleAttachment": {
      "Type": "AWS::Cognito::IdentityPoolRoleAttachment",
      "Properties": {
        "IdentityPoolId": { "Ref": "CognitoIdentityPool" },
        "Roles": {
          "authenticated": { "Fn::GetAtt": "CognitoAuthenticatedRole.Arn" }
        },
        "RoleMappings": {
          "UserPoolMapping": {
            "Type": "Rules",
            "AmbiguousRoleResolution": "AuthenticatedRole",
            "IdentityProvider": {
              "Fn::Sub": [
                "${UserPool}:${ClientId}",
                {
                  "UserPool": { "Fn::GetAtt": "CognitoUserPool.ProviderName" },
                  "ClientId": { "Ref": "CognitoUserPoolClient" }
                }
              ]
            },
            "RulesConfiguration": {
              "Rules": [
                {
                  "Claim": "custom:role",
                  "MatchType": "Equals",
                  "Value": "Admin",
                  "RoleArn": { "Fn::GetAtt": "CognitoAdminRole.Arn" }
                },
                {
                  "Claim": "custom:role",
                  "MatchType": "Equals",
                  "Value": "Manager",
                  "RoleArn": { "Fn::GetAtt": "CognitoManagerRole.Arn" }
                }
              ]
            }
          }
        }
      }
    }
  },
  
  "Outputs": {
    "UserPoolId": {
      "Description": "Cognito User Pool ID",
      "Value": { "Ref": "CognitoUserPool" },
      "Export": {
        "Name": { "Fn::Sub": "${AWS::StackName}-UserPoolId" }
      }
    },
    "UserPoolClientId": {
      "Description": "Cognito User Pool Client ID",
      "Value": { "Ref": "CognitoUserPoolClient" },
      "Export": {
        "Name": { "Fn::Sub": "${AWS::StackName}-UserPoolClientId" }
      }
    },
    "IdentityPoolId": {
      "Description": "Cognito Identity Pool ID",
      "Value": { "Ref": "CognitoIdentityPool" },
      "Export": {
        "Name": { "Fn::Sub": "${AWS::StackName}-IdentityPoolId" }
      }
    }
  }
}
```

### .NET Configuration for AWS Cognito

```json
// appsettings.Production.json
{
  "AWS": {
    "Region": "us-east-1",
    "Cognito": {
      "UserPoolId": "us-east-1_XXXXXXXXX",
      "ClientId": "your-client-id",
      "Authority": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_XXXXXXXXX",
      "MetadataAddress": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_XXXXXXXXX/.well-known/openid_configuration",
      "RequireHttpsMetadata": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "ValidateLifetime": true,
      "ValidateIssuerSigningKey": true,
      "ClockSkew": "00:05:00"
    }
  },
  "JwtSettings": {
    "ValidIssuers": [
      "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_XXXXXXXXX"
    ],
    "ValidAudiences": [
      "your-client-id"
    ],
    "RequireExpirationTime": true,
    "RequireSignedTokens": true,
    "SaveSigninToken": false,
    "ValidateActor": false,
    "ValidateTokenReplay": false
  }
}
```

### JWT Authentication Setup

```csharp
// Program.cs - AWS Cognito JWT Configuration
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Amazon.CognitoIdentityProvider;

var builder = WebApplication.CreateBuilder(args);

// Configure AWS Cognito JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var cognitoConfig = builder.Configuration.GetSection("AWS:Cognito");
        
        options.Authority = cognitoConfig["Authority"];
        options.MetadataAddress = cognitoConfig["MetadataAddress"];
        options.RequireHttpsMetadata = cognitoConfig.GetValue<bool>("RequireHttpsMetadata", true);
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = cognitoConfig.GetValue<bool>("ValidateIssuer", true),
            ValidIssuers = builder.Configuration.GetSection("JwtSettings:ValidIssuers").Get<string[]>(),
            
            ValidateAudience = cognitoConfig.GetValue<bool>("ValidateAudience", true),
            ValidAudiences = builder.Configuration.GetSection("JwtSettings:ValidAudiences").Get<string[]>(),
            
            ValidateLifetime = cognitoConfig.GetValue<bool>("ValidateLifetime", true),
            ValidateIssuerSigningKey = cognitoConfig.GetValue<bool>("ValidateIssuerSigningKey", true),
            
            RequireExpirationTime = builder.Configuration.GetValue<bool>("JwtSettings:RequireExpirationTime", true),
            RequireSignedTokens = builder.Configuration.GetValue<bool>("JwtSettings:RequireSignedTokens", true),
            
            ClockSkew = TimeSpan.Parse(cognitoConfig["ClockSkew"] ?? "00:05:00"),
            
            // Custom validation for additional claims
            IssuerValidator = (issuer, token, parameters) =>
            {
                if (parameters.ValidIssuers?.Contains(issuer) == true)
                    return issuer;
                throw new SecurityTokenInvalidIssuerException($"Invalid issuer: {issuer}");
            },
            
            AudienceValidator = (audiences, token, parameters) =>
            {
                if (audiences?.Any(aud => parameters.ValidAudiences?.Contains(aud) == true) == true)
                    return true;
                throw new SecurityTokenInvalidAudienceException("Invalid audience");
            }
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogWarning("JWT Authentication failed: {Error} for token {Token}",
                    context.Exception.Message,
                    context.Request.Headers.Authorization.ToString().Substring(0, Math.Min(50, context.Request.Headers.Authorization.ToString().Length)));
                
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                
                return Task.CompletedTask;
            },
            
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                var userId = context.Principal?.FindFirst("sub")?.Value;
                var email = context.Principal?.FindFirst("email")?.Value;
                
                logger.LogInformation("JWT token validated for user {UserId} with email {Email}",
                    userId, email);
                
                // Add custom claims or perform additional validation
                return Task.CompletedTask;
            },
            
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogWarning("JWT Challenge triggered for path {Path}",
                    context.Request.Path);
                
                return Task.CompletedTask;
            }
        };
    });

// Configure AWS services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

## Custom JWT Claims Processing

### Claims Transformation Service

```csharp
// Infrastructure/Services/CognitoClaimsTransformation.cs
namespace Infrastructure.Services;

public class CognitoClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<CognitoClaimsTransformation> _logger;
    
    public CognitoClaimsTransformation(ILogger<CognitoClaimsTransformation> logger)
    {
        _logger = logger;
    }
    
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);
        
        var claimsIdentity = (ClaimsIdentity)principal.Identity;
        
        // Transform Cognito-specific claims to application claims
        TransformCognitoClaims(claimsIdentity);
        
        // Add role-based claims
        AddRoleBasedClaims(claimsIdentity);
        
        // Add tenant-specific claims
        AddTenantClaims(claimsIdentity);
        
        _logger.LogDebug("Claims transformed for user {UserId}",
            principal.FindFirst("sub")?.Value);
        
        return Task.FromResult(principal);
    }
    
    private void TransformCognitoClaims(ClaimsIdentity identity)
    {
        // Map Cognito 'sub' to standard 'nameidentifier'
        var subClaim = identity.FindFirst("sub");
        if (subClaim != null && !identity.HasClaim(ClaimTypes.NameIdentifier, subClaim.Value))
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
        }
        
        // Map Cognito 'email' to standard claim
        var emailClaim = identity.FindFirst("email");
        if (emailClaim != null && !identity.HasClaim(ClaimTypes.Email, emailClaim.Value))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, emailClaim.Value));
        }
        
        // Map Cognito 'name' to standard claim
        var nameClaim = identity.FindFirst("name");
        if (nameClaim != null && !identity.HasClaim(ClaimTypes.Name, nameClaim.Value))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
        }
        
        // Transform custom role claim
        var customRoleClaim = identity.FindFirst("custom:role");
        if (customRoleClaim != null && !identity.HasClaim(ClaimTypes.Role, customRoleClaim.Value))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, customRoleClaim.Value));
        }
        
        // Transform custom tenant claim
        var tenantClaim = identity.FindFirst("custom:tenant_id");
        if (tenantClaim != null && !identity.HasClaim("tenant_id", tenantClaim.Value))
        {
            identity.AddClaim(new Claim("tenant_id", tenantClaim.Value));
        }
    }
    
    private void AddRoleBasedClaims(ClaimsIdentity identity)
    {
        var role = identity.FindFirst(ClaimTypes.Role)?.Value;
        
        switch (role?.ToUpperInvariant())
        {
            case "ADMIN":
                identity.AddClaim(new Claim("permission", "customers:manage"));
                identity.AddClaim(new Claim("permission", "orders:manage"));
                identity.AddClaim(new Claim("permission", "reports:view"));
                identity.AddClaim(new Claim("permission", "system:admin"));
                break;\n                \n            case "MANAGER":\n                identity.AddClaim(new Claim("permission", "customers:view"));\n                identity.AddClaim(new Claim("permission", "orders:view"));\n                identity.AddClaim(new Claim("permission", "orders:update"));\n                identity.AddClaim(new Claim("permission", "reports:view"));\n                break;\n                \n            case "USER":\n                identity.AddClaim(new Claim("permission", "profile:view"));\n                identity.AddClaim(new Claim("permission", "orders:view:own"));\n                break;\n        }\n    }\n    \n    private void AddTenantClaims(ClaimsIdentity identity)\n    {\n        var tenantId = identity.FindFirst("tenant_id")?.Value;\n        if (!string.IsNullOrEmpty(tenantId))\n        {\n            identity.AddClaim(new Claim("tenant_scope", $"tenant:{tenantId}"));\n        }\n    }\n}\n\n// Registration in Program.cs\nbuilder.Services.AddScoped<IClaimsTransformation, CognitoClaimsTransformation>();\n```\n\n## API Gateway JWT Authorizer\n\n### Lambda Authorizer for Custom Logic\n\n```csharp\n// src/Lambda/JwtAuthorizer/Function.cs\nusing Amazon.Lambda.APIGatewayEvents;\nusing Amazon.Lambda.Core;\nusing Microsoft.IdentityModel.Tokens;\nusing System.IdentityModel.Tokens.Jwt;\nusing System.Security.Claims;\n\n[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]\n\nnamespace JwtAuthorizer;\n\npublic class Function\n{\n    private readonly JwtSecurityTokenHandler _tokenHandler;\n    private readonly TokenValidationParameters _validationParameters;\n    private readonly ILogger<Function> _logger;\n    \n    public Function()\n    {\n        _tokenHandler = new JwtSecurityTokenHandler();\n        _logger = LoggerFactory.Create(builder => builder.AddConsole())\n            .CreateLogger<Function>();\n            \n        // Configure token validation parameters from environment variables\n        _validationParameters = new TokenValidationParameters\n        {\n            ValidateIssuer = true,\n            ValidIssuer = Environment.GetEnvironmentVariable(\"JWT_ISSUER\"),\n            \n            ValidateAudience = true,\n            ValidAudience = Environment.GetEnvironmentVariable(\"JWT_AUDIENCE\"),\n            \n            ValidateLifetime = true,\n            ValidateIssuerSigningKey = true,\n            \n            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>\n            {\n                // Fetch JWKS from Cognito\n                return GetSigningKeys(kid);\n            },\n            \n            ClockSkew = TimeSpan.FromMinutes(5)\n        };\n    }\n    \n    public async Task<APIGatewayCustomAuthorizerResponse> FunctionHandler(\n        APIGatewayCustomAuthorizerRequest request, \n        ILambdaContext context)\n    {\n        try\n        {\n            _logger.LogInformation(\"Processing authorization request for resource: {Resource}\",\n                request.Resource);\n            \n            var token = ExtractToken(request.AuthorizationToken);\n            if (string.IsNullOrEmpty(token))\n            {\n                _logger.LogWarning(\"No token provided in authorization header\");\n                throw new UnauthorizedAccessException(\"No token provided\");\n            }\n            \n            var principal = await ValidateTokenAsync(token);\n            var policy = await GeneratePolicyAsync(principal, request.Resource, request.HttpMethod);\n            \n            _logger.LogInformation(\"Authorization successful for user {UserId}\",\n                principal.FindFirst(\"sub\")?.Value);\n            \n            return policy;\n        }\n        catch (Exception ex)\n        {\n            _logger.LogError(ex, \"Authorization failed\");\n            \n            // Return deny policy\n            return new APIGatewayCustomAuthorizerResponse\n            {\n                PrincipalID = \"user\",\n                PolicyDocument = new APIGatewayCustomAuthorizerPolicy\n                {\n                    Version = \"2012-10-17\",\n                    Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>\n                    {\n                        new()\n                        {\n                            Action = new HashSet<string> { \"execute-api:Invoke\" },\n                            Effect = \"Deny\",\n                            Resource = new HashSet<string> { request.Resource ?? \"*\" }\n                        }\n                    }\n                }\n            };\n        }\n    }\n    \n    private string ExtractToken(string authorizationHeader)\n    {\n        if (string.IsNullOrEmpty(authorizationHeader))\n            return string.Empty;\n            \n        const string bearerPrefix = \"Bearer \";\n        if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))\n        {\n            return authorizationHeader.Substring(bearerPrefix.Length);\n        }\n        \n        return authorizationHeader;\n    }\n    \n    private async Task<ClaimsPrincipal> ValidateTokenAsync(string token)\n    {\n        try\n        {\n            var validatedToken = await _tokenHandler.ValidateTokenAsync(token, _validationParameters);\n            return new ClaimsPrincipal(validatedToken.ClaimsIdentity);\n        }\n        catch (SecurityTokenException ex)\n        {\n            _logger.LogWarning(ex, \"Token validation failed\");\n            throw new UnauthorizedAccessException(\"Invalid token\", ex);\n        }\n    }\n    \n    private async Task<APIGatewayCustomAuthorizerResponse> GeneratePolicyAsync(\n        ClaimsPrincipal principal, \n        string resource, \n        string httpMethod)\n    {\n        var userId = principal.FindFirst(\"sub\")?.Value ?? \"unknown\";\n        var role = principal.FindFirst(ClaimTypes.Role)?.Value;\n        var tenantId = principal.FindFirst(\"custom:tenant_id\")?.Value;\n        \n        var statements = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>();\n        \n        // Resource-based access control\n        var allowedResources = await DetermineAllowedResourcesAsync(principal, resource, httpMethod);\n        \n        if (allowedResources.Any())\n        {\n            statements.Add(new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement\n            {\n                Action = new HashSet<string> { \"execute-api:Invoke\" },\n                Effect = \"Allow\",\n                Resource = new HashSet<string>(allowedResources)\n            });\n        }\n        else\n        {\n            statements.Add(new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement\n            {\n                Action = new HashSet<string> { \"execute-api:Invoke\" },\n                Effect = \"Deny\",\n                Resource = new HashSet<string> { resource }\n            });\n        }\n        \n        // Create context for downstream services\n        var context = new Dictionary<string, object>\n        {\n            [\"userId\"] = userId,\n            [\"email\"] = principal.FindFirst(\"email\")?.Value ?? \"\",\n            [\"role\"] = role ?? \"User\",\n            [\"tenantId\"] = tenantId ?? \"\"\n        };\n        \n        return new APIGatewayCustomAuthorizerResponse\n        {\n            PrincipalID = userId,\n            PolicyDocument = new APIGatewayCustomAuthorizerPolicy\n            {\n                Version = \"2012-10-17\",\n                Statement = statements\n            },\n            Context = context\n        };\n    }\n    \n    private async Task<List<string>> DetermineAllowedResourcesAsync(\n        ClaimsPrincipal principal, \n        string requestedResource, \n        string httpMethod)\n    {\n        var role = principal.FindFirst(ClaimTypes.Role)?.Value;\n        var tenantId = principal.FindFirst(\"custom:tenant_id\")?.Value;\n        var allowedResources = new List<string>();\n        \n        switch (role?.ToUpperInvariant())\n        {\n            case \"ADMIN\":\n                // Admins have access to all resources\n                allowedResources.Add(\"*\");\n                break;\n                \n            case \"MANAGER\":\n                // Managers have access to most resources within their tenant\n                if (IsWithinTenant(requestedResource, tenantId))\n                {\n                    allowedResources.Add(requestedResource);\n                }\n                break;\n                \n            case \"USER\":\n                // Users have limited access to their own resources\n                if (IsUserOwnedResource(requestedResource, principal.FindFirst(\"sub\")?.Value) ||\n                    IsPublicResource(requestedResource))\n                {\n                    allowedResources.Add(requestedResource);\n                }\n                break;\n                \n            default:\n                // No access for unknown roles\n                break;\n        }\n        \n        return allowedResources;\n    }\n    \n    private bool IsWithinTenant(string resource, string tenantId)\n    {\n        // Implement tenant-based resource access logic\n        if (string.IsNullOrEmpty(tenantId))\n            return false;\n            \n        return resource.Contains($\"/tenants/{tenantId}/\") || \n               resource.Contains($\"tenantId={tenantId}\");\n    }\n    \n    private bool IsUserOwnedResource(string resource, string userId)\n    {\n        // Implement user-owned resource logic\n        if (string.IsNullOrEmpty(userId))\n            return false;\n            \n        return resource.Contains($\"/users/{userId}/\") ||\n               resource.Contains(\"/profile\") ||\n               resource.Contains($\"userId={userId}\");\n    }\n    \n    private bool IsPublicResource(string resource)\n    {\n        var publicPaths = new[] { \"/health\", \"/version\", \"/docs\" };\n        return publicPaths.Any(path => resource.Contains(path));\n    }\n    \n    private IEnumerable<SecurityKey> GetSigningKeys(string keyId)\n    {\n        // Implement JWKS fetching and caching logic\n        // This would typically fetch from:\n        // https://cognito-idp.{region}.amazonaws.com/{userPoolId}/.well-known/jwks.json\n        \n        // For demonstration, return empty collection\n        // In real implementation, cache and return appropriate keys\n        return Enumerable.Empty<SecurityKey>();\n    }\n}\n```\n\n## Multi-Tenant JWT Patterns\n\n### Tenant-Aware Authorization\n\n```csharp\n// Application/Common/TenantContext.cs\nnamespace Application.Common;\n\npublic interface ITenantContext\n{\n    string? TenantId { get; }\n    bool IsMultiTenant { get; }\n    void SetTenant(string tenantId);\n}\n\npublic class TenantContext : ITenantContext\n{\n    public string? TenantId { get; private set; }\n    public bool IsMultiTenant => !string.IsNullOrEmpty(TenantId);\n    \n    public void SetTenant(string tenantId)\n    {\n        TenantId = tenantId;\n    }\n}\n\n// Infrastructure/Middleware/TenantMiddleware.cs\nnamespace Infrastructure.Middleware;\n\npublic class TenantMiddleware\n{\n    private readonly RequestDelegate _next;\n    private readonly ILogger<TenantMiddleware> _logger;\n    \n    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)\n    {\n        _next = next;\n        _logger = logger;\n    }\n    \n    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)\n    {\n        var tenantId = ExtractTenantId(context);\n        \n        if (!string.IsNullOrEmpty(tenantId))\n        {\n            tenantContext.SetTenant(tenantId);\n            \n            _logger.LogDebug(\"Tenant context set to {TenantId} for request {RequestId}\",\n                tenantId, context.TraceIdentifier);\n        }\n        \n        await _next(context);\n    }\n    \n    private string? ExtractTenantId(HttpContext context)\n    {\n        // Method 1: Extract from JWT token\n        var tenantFromToken = context.User.FindFirst(\"custom:tenant_id\")?.Value;\n        if (!string.IsNullOrEmpty(tenantFromToken))\n        {\n            return tenantFromToken;\n        }\n        \n        // Method 2: Extract from custom header\n        if (context.Request.Headers.TryGetValue(\"X-Tenant-ID\", out var tenantHeader))\n        {\n            return tenantHeader.FirstOrDefault();\n        }\n        \n        // Method 3: Extract from subdomain\n        var host = context.Request.Host.Value;\n        if (host.Contains('.') && !host.StartsWith(\"www.\"))\n        {\n            var subdomain = host.Split('.')[0];\n            if (IsValidTenantSubdomain(subdomain))\n            {\n                return subdomain;\n            }\n        }\n        \n        // Method 4: Extract from route parameter\n        if (context.Request.RouteValues.TryGetValue(\"tenantId\", out var tenantRoute))\n        {\n            return tenantRoute?.ToString();\n        }\n        \n        return null;\n    }\n    \n    private static bool IsValidTenantSubdomain(string subdomain)\n    {\n        // Implement tenant subdomain validation logic\n        return !string.IsNullOrEmpty(subdomain) &&\n               subdomain.Length >= 3 &&\n               subdomain.All(c => char.IsLetterOrDigit(c) || c == '-');\n    }\n}\n```\n\n### Tenant-Aware Repository Pattern\n\n```csharp\n// Infrastructure/Repositories/TenantAwareRepository.cs\nnamespace Infrastructure.Repositories;\n\npublic abstract class TenantAwareRepository<TEntity, TId>\n    where TEntity : class\n    where TId : notnull\n{\n    protected readonly ApplicationDbContext Context;\n    protected readonly ITenantContext TenantContext;\n    protected readonly ILogger Logger;\n    \n    protected TenantAwareRepository(\n        ApplicationDbContext context,\n        ITenantContext tenantContext,\n        ILogger logger)\n    {\n        Context = context;\n        TenantContext = tenantContext;\n        Logger = logger;\n    }\n    \n    protected virtual IQueryable<TEntity> ApplyTenantFilter(IQueryable<TEntity> query)\n    {\n        if (TenantContext.IsMultiTenant)\n        {\n            // Apply tenant filter based on entity type\n            return ApplyTenantSpecificFilter(query, TenantContext.TenantId!);\n        }\n        \n        return query;\n    }\n    \n    protected abstract IQueryable<TEntity> ApplyTenantSpecificFilter(\n        IQueryable<TEntity> query, \n        string tenantId);\n    \n    protected virtual void ValidateTenantAccess(TEntity entity)\n    {\n        if (TenantContext.IsMultiTenant)\n        {\n            var entityTenantId = GetEntityTenantId(entity);\n            \n            if (entityTenantId != TenantContext.TenantId)\n            {\n                Logger.LogWarning(\n                    \"Attempted to access entity from tenant {EntityTenant} while in context of tenant {CurrentTenant}\",\n                    entityTenantId, TenantContext.TenantId);\n                \n                throw new UnauthorizedAccessException(\n                    $\"Entity belongs to tenant {entityTenantId} but current context is {TenantContext.TenantId}\");\n            }\n        }\n    }\n    \n    protected abstract string? GetEntityTenantId(TEntity entity);\n}\n\n// Specific implementation\npublic class CustomerRepository : TenantAwareRepository<Customer, CustomerId>, ICustomerRepository\n{\n    public CustomerRepository(\n        ApplicationDbContext context,\n        ITenantContext tenantContext,\n        ILogger<CustomerRepository> logger) \n        : base(context, tenantContext, logger)\n    {\n    }\n    \n    public async Task<Customer?> GetByIdAsync(\n        CustomerId id, \n        CancellationToken cancellationToken = default)\n    {\n        var query = Context.Customers.Where(c => c.Id == id);\n        query = ApplyTenantFilter(query);\n        \n        var customer = await query.FirstOrDefaultAsync(cancellationToken);\n        \n        if (customer != null)\n        {\n            ValidateTenantAccess(customer);\n        }\n        \n        return customer;\n    }\n    \n    public async Task<IEnumerable<Customer>> GetAllAsync(\n        CancellationToken cancellationToken = default)\n    {\n        var query = Context.Customers.AsQueryable();\n        query = ApplyTenantFilter(query);\n        \n        return await query.ToListAsync(cancellationToken);\n    }\n    \n    protected override IQueryable<Customer> ApplyTenantSpecificFilter(\n        IQueryable<Customer> query, \n        string tenantId)\n    {\n        return query.Where(c => c.TenantId == tenantId);\n    }\n    \n    protected override string? GetEntityTenantId(Customer entity)\n    {\n        return entity.TenantId;\n    }\n}\n```\n\n## JWT Token Refresh Patterns\n\n### Automatic Token Refresh Service\n\n```csharp\n// Application/Services/TokenRefreshService.cs\nnamespace Application.Services;\n\npublic interface ITokenRefreshService\n{\n    Task<TokenRefreshResult> RefreshTokenAsync(string refreshToken);\n    Task<bool> ValidateRefreshTokenAsync(string refreshToken);\n    Task RevokeTokenAsync(string refreshToken);\n}\n\npublic class CognitoTokenRefreshService : ITokenRefreshService\n{\n    private readonly IAmazonCognitoIdentityProvider _cognitoClient;\n    private readonly IConfiguration _configuration;\n    private readonly ILogger<CognitoTokenRefreshService> _logger;\n    \n    public CognitoTokenRefreshService(\n        IAmazonCognitoIdentityProvider cognitoClient,\n        IConfiguration configuration,\n        ILogger<CognitoTokenRefreshService> logger)\n    {\n        _cognitoClient = cognitoClient;\n        _configuration = configuration;\n        _logger = logger;\n    }\n    \n    public async Task<TokenRefreshResult> RefreshTokenAsync(string refreshToken)\n    {\n        try\n        {\n            var clientId = _configuration[\"AWS:Cognito:ClientId\"];\n            \n            var request = new InitiateAuthRequest\n            {\n                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,\n                ClientId = clientId,\n                AuthParameters = new Dictionary<string, string>\n                {\n                    [\"REFRESH_TOKEN\"] = refreshToken\n                }\n            };\n            \n            var response = await _cognitoClient.InitiateAuthAsync(request);\n            \n            if (response.AuthenticationResult != null)\n            {\n                _logger.LogInformation(\"Token refreshed successfully\");\n                \n                return new TokenRefreshResult\n                {\n                    IsSuccess = true,\n                    AccessToken = response.AuthenticationResult.AccessToken,\n                    IdToken = response.AuthenticationResult.IdToken,\n                    ExpiresIn = response.AuthenticationResult.ExpiresIn,\n                    TokenType = response.AuthenticationResult.TokenType,\n                    RefreshToken = response.AuthenticationResult.RefreshToken ?? refreshToken\n                };\n            }\n            \n            _logger.LogWarning(\"Token refresh failed: No authentication result\");\n            return TokenRefreshResult.Failed(\"No authentication result returned\");\n        }\n        catch (NotAuthorizedException ex)\n        {\n            _logger.LogWarning(ex, \"Token refresh failed: Not authorized\");\n            return TokenRefreshResult.Failed(\"Refresh token is invalid or expired\");\n        }\n        catch (Exception ex)\n        {\n            _logger.LogError(ex, \"Token refresh failed with unexpected error\");\n            return TokenRefreshResult.Failed(\"An unexpected error occurred during token refresh\");\n        }\n    }\n    \n    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)\n    {\n        try\n        {\n            // Cognito doesn't have a direct validation endpoint for refresh tokens\n            // We can attempt to use it for refresh to validate\n            var result = await RefreshTokenAsync(refreshToken);\n            return result.IsSuccess;\n        }\n        catch\n        {\n            return false;\n        }\n    }\n    \n    public async Task RevokeTokenAsync(string refreshToken)\n    {\n        try\n        {\n            var clientId = _configuration[\"AWS:Cognito:ClientId\"];\n            \n            var request = new RevokeTokenRequest\n            {\n                ClientId = clientId,\n                Token = refreshToken\n            };\n            \n            await _cognitoClient.RevokeTokenAsync(request);\n            \n            _logger.LogInformation(\"Token revoked successfully\");\n        }\n        catch (Exception ex)\n        {\n            _logger.LogError(ex, \"Failed to revoke token\");\n            throw;\n        }\n    }\n}\n\npublic class TokenRefreshResult\n{\n    public bool IsSuccess { get; init; }\n    public string? AccessToken { get; init; }\n    public string? IdToken { get; init; }\n    public string? RefreshToken { get; init; }\n    public int ExpiresIn { get; init; }\n    public string? TokenType { get; init; }\n    public string? ErrorMessage { get; init; }\n    \n    public static TokenRefreshResult Failed(string errorMessage) => new()\n    {\n        IsSuccess = false,\n        ErrorMessage = errorMessage\n    };\n}\n```\n\n### Token Refresh Middleware\n\n```csharp\n// Infrastructure/Middleware/TokenRefreshMiddleware.cs\nnamespace Infrastructure.Middleware;\n\npublic class TokenRefreshMiddleware\n{\n    private readonly RequestDelegate _next;\n    private readonly ITokenRefreshService _tokenRefreshService;\n    private readonly ILogger<TokenRefreshMiddleware> _logger;\n    \n    public TokenRefreshMiddleware(\n        RequestDelegate next,\n        ITokenRefreshService tokenRefreshService,\n        ILogger<TokenRefreshMiddleware> logger)\n    {\n        _next = next;\n        _tokenRefreshService = tokenRefreshService;\n        _logger = logger;\n    }\n    \n    public async Task InvokeAsync(HttpContext context)\n    {\n        // Check if token is about to expire\n        var expiryClaim = context.User.FindFirst(\"exp\");\n        if (expiryClaim != null && long.TryParse(expiryClaim.Value, out var expTimestamp))\n        {\n            var expTime = DateTimeOffset.FromUnixTimeSeconds(expTimestamp);\n            var timeUntilExpiry = expTime - DateTimeOffset.UtcNow;\n            \n            // If token expires in less than 5 minutes, suggest refresh\n            if (timeUntilExpiry.TotalMinutes < 5)\n            {\n                context.Response.Headers.Add(\"X-Token-Refresh-Suggested\", \"true\");\n                context.Response.Headers.Add(\"X-Token-Expires-In\", timeUntilExpiry.TotalSeconds.ToString());\n                \n                _logger.LogInformation(\"Token expires in {Minutes} minutes for user {UserId}\",\n                    timeUntilExpiry.TotalMinutes,\n                    context.User.FindFirst(\"sub\")?.Value);\n            }\n        }\n        \n        await _next(context);\n    }\n}\n```\n\n## Security Best Practices\n\n### JWT Security Headers\n\n```csharp\n// Infrastructure/Middleware/JwtSecurityMiddleware.cs\nnamespace Infrastructure.Middleware;\n\npublic class JwtSecurityMiddleware\n{\n    private readonly RequestDelegate _next;\n    private readonly ILogger<JwtSecurityMiddleware> _logger;\n    \n    public JwtSecurityMiddleware(\n        RequestDelegate next,\n        ILogger<JwtSecurityMiddleware> logger)\n    {\n        _next = next;\n        _logger = logger;\n    }\n    \n    public async Task InvokeAsync(HttpContext context)\n    {\n        // Validate JWT security headers\n        ValidateSecurityHeaders(context);\n        \n        // Add security response headers\n        AddSecurityResponseHeaders(context);\n        \n        // Log security events\n        LogSecurityEvents(context);\n        \n        await _next(context);\n    }\n    \n    private void ValidateSecurityHeaders(HttpContext context)\n    {\n        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();\n        \n        if (!string.IsNullOrEmpty(authHeader))\n        {\n            // Validate Bearer token format\n            if (!authHeader.StartsWith(\"Bearer \", StringComparison.OrdinalIgnoreCase))\n            {\n                _logger.LogWarning(\"Invalid authorization header format from IP {IpAddress}\",\n                    context.Connection.RemoteIpAddress);\n            }\n            \n            // Check for suspicious token patterns\n            var token = authHeader.Substring(\"Bearer \".Length);\n            if (token.Length < 100) // JWT tokens are typically much longer\n            {\n                _logger.LogWarning(\"Suspiciously short token from IP {IpAddress}\",\n                    context.Connection.RemoteIpAddress);\n            }\n        }\n        \n        // Validate required security headers\n        if (!context.Request.Headers.ContainsKey(\"User-Agent\"))\n        {\n            _logger.LogWarning(\"Request without User-Agent header from IP {IpAddress}\",\n                context.Connection.RemoteIpAddress);\n        }\n    }\n    \n    private static void AddSecurityResponseHeaders(HttpContext context)\n    {\n        // Prevent token from being cached\n        context.Response.Headers.Add(\"Cache-Control\", \"no-store, no-cache, must-revalidate\");\n        context.Response.Headers.Add(\"Pragma\", \"no-cache\");\n        context.Response.Headers.Add(\"Expires\", \"0\");\n        \n        // Security headers\n        context.Response.Headers.Add(\"X-Content-Type-Options\", \"nosniff\");\n        context.Response.Headers.Add(\"X-Frame-Options\", \"DENY\");\n        context.Response.Headers.Add(\"Referrer-Policy\", \"strict-origin-when-cross-origin\");\n    }\n    \n    private void LogSecurityEvents(HttpContext context)\n    {\n        var userId = context.User.FindFirst(\"sub\")?.Value;\n        var ipAddress = context.Connection.RemoteIpAddress?.ToString();\n        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();\n        var endpoint = $\"{context.Request.Method} {context.Request.Path}\";\n        \n        _logger.LogInformation(\n            \"JWT Security Check - User: {UserId}, IP: {IpAddress}, Endpoint: {Endpoint}, UserAgent: {UserAgent}\",\n            userId ?? \"Anonymous\", ipAddress, endpoint, userAgent);\n        \n        // Log suspicious activity\n        if (IsSuspiciousRequest(context))\n        {\n            _logger.LogWarning(\n                \"Suspicious JWT activity detected - User: {UserId}, IP: {IpAddress}, Endpoint: {Endpoint}\",\n                userId ?? \"Anonymous\", ipAddress, endpoint);\n        }\n    }\n    \n    private static bool IsSuspiciousRequest(HttpContext context)\n    {\n        // Implement suspicious activity detection logic\n        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();\n        \n        // Check for common attack patterns\n        var suspiciousPatterns = new[]\n        {\n            \"sqlmap\", \"nikto\", \"nmap\", \"burp\", \"scanner\",\n            \"<script\", \"javascript:\", \"data:text/html\"\n        };\n        \n        return suspiciousPatterns.Any(pattern => \n            userAgent?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true);\n    }\n}\n```\n\n### Rate Limiting for JWT Endpoints\n\n```csharp\n// Infrastructure/Extensions/JwtRateLimitingExtensions.cs\nnamespace Infrastructure.Extensions;\n\npublic static class JwtRateLimitingExtensions\n{\n    public static IServiceCollection AddJwtRateLimiting(\n        this IServiceCollection services,\n        IConfiguration configuration)\n    {\n        services.AddRateLimiter(options =>\n        {\n            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;\n            \n            // Rate limiting for login attempts\n            options.AddPolicy(\"LoginAttempts\", httpContext =>\n                RateLimitPartition.GetFixedWindowLimiter(\n                    partitionKey: GetClientIdentifier(httpContext),\n                    factory: partition => new FixedWindowRateLimiterOptions\n                    {\n                        AutoReplenishment = true,\n                        PermitLimit = 5, // 5 login attempts\n                        Window = TimeSpan.FromMinutes(15) // per 15 minutes\n                    }));\n            \n            // Rate limiting for token refresh\n            options.AddPolicy(\"TokenRefresh\", httpContext =>\n                RateLimitPartition.GetFixedWindowLimiter(\n                    partitionKey: GetUserIdentifier(httpContext),\n                    factory: partition => new FixedWindowRateLimiterOptions\n                    {\n                        AutoReplenishment = true,\n                        PermitLimit = 10, // 10 refresh attempts\n                        Window = TimeSpan.FromMinutes(5) // per 5 minutes\n                    }));\n            \n            // Global rate limiting for authenticated users\n            options.AddPolicy(\"AuthenticatedUser\", httpContext =>\n                RateLimitPartition.GetTokenBucketLimiter(\n                    partitionKey: GetUserIdentifier(httpContext),\n                    factory: partition => new TokenBucketRateLimiterOptions\n                    {\n                        TokenLimit = 100,\n                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,\n                        QueueLimit = 10,\n                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),\n                        TokensPerPeriod = 20,\n                        AutoReplenishment = true\n                    }));\n        });\n        \n        return services;\n    }\n    \n    private static string GetClientIdentifier(HttpContext httpContext)\n    {\n        // Use IP + User-Agent for client identification\n        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? \"unknown\";\n        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? \"unknown\";\n        return $\"{ip}:{userAgent.GetHashCode()}\";\n    }\n    \n    private static string GetUserIdentifier(HttpContext httpContext)\n    {\n        // Use user ID if authenticated, otherwise fall back to client identifier\n        var userId = httpContext.User.FindFirst(\"sub\")?.Value;\n        return userId ?? GetClientIdentifier(httpContext);\n    }\n}\n```\n\n## Monitoring and Alerting\n\n### JWT Security Metrics\n\n```csharp\n// Application/Services/JwtSecurityMetricsService.cs\nnamespace Application.Services;\n\npublic interface IJwtSecurityMetricsService\n{\n    Task RecordLoginAttemptAsync(bool successful, string? reason = null);\n    Task RecordTokenValidationAsync(bool successful, string? reason = null);\n    Task RecordTokenRefreshAsync(bool successful, string? reason = null);\n    Task RecordSuspiciousActivityAsync(string activityType, string details);\n}\n\npublic class CloudWatchJwtSecurityMetricsService : IJwtSecurityMetricsService\n{\n    private readonly IMetricsService _metricsService;\n    private readonly ILogger<CloudWatchJwtSecurityMetricsService> _logger;\n    \n    public CloudWatchJwtSecurityMetricsService(\n        IMetricsService metricsService,\n        ILogger<CloudWatchJwtSecurityMetricsService> logger)\n    {\n        _metricsService = metricsService;\n        _logger = logger;\n    }\n    \n    public async Task RecordLoginAttemptAsync(bool successful, string? reason = null)\n    {\n        var dimensions = new Dictionary<string, string>\n        {\n            [\"Status\"] = successful ? \"Success\" : \"Failed\"\n        };\n        \n        if (!successful && !string.IsNullOrEmpty(reason))\n        {\n            dimensions[\"FailureReason\"] = reason;\n        }\n        \n        await _metricsService.RecordCounterAsync(\"Login.Attempts\", 1, dimensions);\n        \n        _logger.LogInformation(\"Login attempt recorded: {Status} {Reason}\",\n            successful ? \"Success\" : \"Failed\", reason ?? \"\");\n    }\n    \n    public async Task RecordTokenValidationAsync(bool successful, string? reason = null)\n    {\n        var dimensions = new Dictionary<string, string>\n        {\n            [\"Status\"] = successful ? \"Valid\" : \"Invalid\"\n        };\n        \n        if (!successful && !string.IsNullOrEmpty(reason))\n        {\n            dimensions[\"ValidationError\"] = reason;\n        }\n        \n        await _metricsService.RecordCounterAsync(\"JWT.Validation\", 1, dimensions);\n    }\n    \n    public async Task RecordTokenRefreshAsync(bool successful, string? reason = null)\n    {\n        var dimensions = new Dictionary<string, string>\n        {\n            [\"Status\"] = successful ? \"Success\" : \"Failed\"\n        };\n        \n        if (!successful && !string.IsNullOrEmpty(reason))\n        {\n            dimensions[\"FailureReason\"] = reason;\n        }\n        \n        await _metricsService.RecordCounterAsync(\"JWT.Refresh\", 1, dimensions);\n    }\n    \n    public async Task RecordSuspiciousActivityAsync(string activityType, string details)\n    {\n        var dimensions = new Dictionary<string, string>\n        {\n            [\"ActivityType\"] = activityType\n        };\n        \n        await _metricsService.RecordCounterAsync(\"Security.SuspiciousActivity\", 1, dimensions);\n        \n        _logger.LogWarning(\"Suspicious activity recorded: {ActivityType} - {Details}\",\n            activityType, details);\n    }\n}\n```\n\n### CloudWatch Alarms for JWT Security\n\n```json\n// cloudformation/jwt-security-alarms.json\n{\n  \"AWSTemplateFormatVersion\": \"2010-09-09\",\n  \"Description\": \"CloudWatch Alarms for JWT Security Monitoring\",\n  \n  \"Resources\": {\n    \"HighFailedLoginRateAlarm\": {\n      \"Type\": \"AWS::CloudWatch::Alarm\",\n      \"Properties\": {\n        \"AlarmName\": \"JWT-HighFailedLoginRate\",\n        \"AlarmDescription\": \"High rate of failed login attempts detected\",\n        \"MetricName\": \"Login.Attempts\",\n        \"Namespace\": \"CleanArchitecture/Security\",\n        \"Statistic\": \"Sum\",\n        \"Period\": 300,\n        \"EvaluationPeriods\": 2,\n        \"Threshold\": 20,\n        \"ComparisonOperator\": \"GreaterThanThreshold\",\n        \"Dimensions\": [\n          {\n            \"Name\": \"Status\",\n            \"Value\": \"Failed\"\n          }\n        ],\n        \"TreatMissingData\": \"notBreaching\"\n      }\n    },\n    \n    \"InvalidTokenRateAlarm\": {\n      \"Type\": \"AWS::CloudWatch::Alarm\",\n      \"Properties\": {\n        \"AlarmName\": \"JWT-InvalidTokenRate\",\n        \"AlarmDescription\": \"High rate of invalid JWT tokens\",\n        \"MetricName\": \"JWT.Validation\",\n        \"Namespace\": \"CleanArchitecture/Security\",\n        \"Statistic\": \"Sum\",\n        \"Period\": 300,\n        \"EvaluationPeriods\": 2,\n        \"Threshold\": 50,\n        \"ComparisonOperator\": \"GreaterThanThreshold\",\n        \"Dimensions\": [\n          {\n            \"Name\": \"Status\",\n            \"Value\": \"Invalid\"\n          }\n        ]\n      }\n    },\n    \n    \"SuspiciousActivityAlarm\": {\n      \"Type\": \"AWS::CloudWatch::Alarm\",\n      \"Properties\": {\n        \"AlarmName\": \"JWT-SuspiciousActivity\",\n        \"AlarmDescription\": \"Suspicious JWT-related activity detected\",\n        \"MetricName\": \"Security.SuspiciousActivity\",\n        \"Namespace\": \"CleanArchitecture/Security\",\n        \"Statistic\": \"Sum\",\n        \"Period\": 300,\n        \"EvaluationPeriods\": 1,\n        \"Threshold\": 1,\n        \"ComparisonOperator\": \"GreaterThanOrEqualToThreshold\"\n      }\n    }\n  }\n}\n```\n\n## Implementation Checklist\n\n### AWS Cognito Setup\n- [ ] Create Cognito User Pool with appropriate policies\n- [ ] Configure User Pool Client with correct settings\n- [ ] Set up Identity Pool for role-based access\n- [ ] Configure custom attributes for roles and tenant ID\n- [ ] Set up MFA configuration\n- [ ] Configure password policies\n\n### .NET Application Configuration\n- [ ] Install required NuGet packages\n- [ ] Configure JWT authentication with Cognito\n- [ ] Implement claims transformation\n- [ ] Set up tenant-aware middleware\n- [ ] Configure rate limiting for authentication endpoints\n- [ ] Implement token refresh service\n\n### API Gateway Integration\n- [ ] Create Lambda authorizer function\n- [ ] Configure API Gateway with JWT authorizer\n- [ ] Set up CORS configuration\n- [ ] Configure request/response transformations\n- [ ] Set up throttling policies\n\n### Security Monitoring\n- [ ] Implement security metrics collection\n- [ ] Set up CloudWatch alarms\n- [ ] Configure log aggregation and analysis\n- [ ] Set up alerting for security events\n- [ ] Implement audit trail logging\n\n### Multi-Tenant Support\n- [ ] Implement tenant context middleware\n- [ ] Create tenant-aware repositories\n- [ ] Set up tenant isolation at database level\n- [ ] Configure tenant-based authorization\n- [ ] Implement tenant-specific rate limiting\n\n---\n*Document Version: 1.0*\n*Last Updated: 2025-08-08*\n*Framework: .NET 8 / AWS Cognito / API Gateway*\n*Status: AWS JWT Security Implementation Guide*