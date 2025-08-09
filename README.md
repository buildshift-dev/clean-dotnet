# Clean Architecture .NET 8

A production-ready demonstration of Clean Architecture principles and Domain-Driven Design patterns implemented in .NET 8.

> **Note**: CloudFormation templates are designed for learning environments. Authentication is intentionally omitted to focus on architecture patterns.

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![C#](https://img.shields.io/badge/C%23-12.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Architecture Overview

This project demonstrates modern .NET development practices:

- **Clean Architecture** with strict layer separation
- **Domain-Driven Design (DDD)** with aggregates and value objects
- **CQRS & MediatR** for command/query separation
- **Entity Framework Core 8** with InMemory database
- **ASP.NET Core Web API** with Swagger/OpenAPI documentation
- **Docker containerization** with multi-stage builds
- **AWS ECS Fargate deployment** with CloudFormation
- **Comprehensive testing** with xUnit and FluentAssertions
- **CI/CD ready** with automated deployment scripts

### Project Structure

```
clean-dotnet/
├── src/
│   ├── Domain/              # Core business logic (entities, value objects)
│   │   ├── Entities/        # Customer, Order aggregates
│   │   ├── ValueObjects/    # Money, Email, Address, CustomerId
│   │   ├── Events/          # Domain events (CustomerCreated, OrderCreated)
│   │   ├── Repositories/    # Repository contracts
│   │   └── Exceptions/      # Domain-specific exceptions
│   ├── Application/         # Use cases and orchestration (CQRS)
│   │   ├── Commands/        # CreateCustomer, CreateOrder handlers
│   │   ├── Queries/         # GetCustomerOrders handler
│   │   └── Common/          # Result pattern, shared logic
│   ├── Infrastructure/      # Data access and external services
│   │   ├── Persistence/     # EF Core DbContext and repositories
│   │   └── Configurations/  # Entity configurations
│   ├── SharedKernel/        # Shared base classes and common utilities
│   │   ├── Common/          # Result pattern, PagedResult, IDateTime
│   │   └── Domain/          # Base entity, aggregate root, value object
│   └── WebApi/             # API controllers and presentation
│       ├── Controllers/     # RESTful API endpoints
│       └── Models/         # Request/Response DTOs
├── tests/
│   ├── Domain.UnitTests/    # Domain logic tests
│   └── Application.UnitTests/ # Application layer tests
├── cloudformation/         # AWS infrastructure as code
├── scripts/               # Deployment and setup automation
└── py/                   # Original Python reference (preserved)
```

## Quick Start

### Prerequisites

- **.NET 8 SDK** (latest version)
- **Docker Desktop** (for containerization)
- **Git** (for version control)
- **AWS CLI** (for cloud deployment - optional)

### One-Command Setup

```bash
# Build and test the project
make build
make test

# Start the API with hot reload
make run-watch
```

### Manual Setup

1. **Clone and navigate**
   ```bash
   git clone <repository-url>
   cd clean-dotnet
   ```

2. **Build and test**
   ```bash
   make restore    # Restore NuGet packages
   make build      # Build solution
   make test       # Run tests
   ```

3. **Run the API**
   ```bash
   make run        # Start API server
   # OR
   make run-watch  # Start with hot reload
   
   ```

4. **Access the application**
   - **API**: http://localhost:5000 (native) or http://localhost:8080 (Docker)
   - **Swagger UI**: http://localhost:5000/swagger or http://localhost:8080
   - **Health Check**: http://localhost:5000/health or http://localhost:8080/health

> **🍎 Mac Silicon Note**: For local development, use `make run` (native). For testing deployment containers, see the [Mac Silicon Docker section](#-mac-silicon-m1m2m3-local-docker) below.

## API Endpoints

- **Customers**: Create and manage customer records
- **Orders**: Create orders and retrieve customer order history  
- **Health**: Application health monitoring at `/health`

**Full API documentation**: Available at http://localhost:5000/swagger when running

## Development Commands

### Build & Test
```bash
make build                 # Build the entire solution
make test                  # Run all tests
make test-unit             # Run unit tests only
make test-coverage         # Run tests with coverage report
make clean                 # Clean build artifacts
```

### Run & Debug
```bash
make run                   # Start API server
make run-watch             # Start with hot reload
make run-https             # Start with HTTPS
```

### Code Quality
```bash
make format                # Format code with dotnet format
make lint                  # Run code analysis
```

### Docker Operations
```bash
make docker-build          # Build Docker image (linux/amd64)
make docker-run            # Run in Docker container (port 8080)
```

#### 🍎 Mac Silicon (M1/M2/M3) Local Docker
```bash
# Option 1: Run natively (fastest for local testing)
make run                   # Native .NET development server

# Option 2: Docker with platform emulation (slower but tests deployment)
docker run --rm -p 8080:8080 \
  --platform linux/amd64 \
  $(make docker-build-name 2>/dev/null || echo "clean-architecture-dotnet")

# Option 3: Build and test deployment-ready container
dotnet publish src/WebApi/WebApi.csproj -c Release -o ./publish
docker build --platform linux/amd64 -f Dockerfile.prebuild -t clean-dotnet-local .
docker run --rm -p 8080:8080 clean-dotnet-local

# Test the containerized API
curl http://localhost:8080/health        # Health check
open http://localhost:8080               # Swagger UI (macOS)
```

**Why different approaches?**
- **Native (`make run`)**: Fastest for development, runs on ARM64
- **Emulated Docker**: Tests the actual deployment container on your Mac  
- **Prebuild Docker**: Exactly what gets deployed to AWS (no emulation issues)

### AWS Deployment (Optional)
See [cloudformation/README.md](cloudformation/README.md) for complete deployment instructions for Mac Silicon and Cloud9/AMD64 platforms.

## Testing Strategy

### Test Coverage
- **Domain Layer**: Entities, value objects, business rules
- **Application Layer**: Command/query handlers, validation
- **API Layer**: Controller endpoints, request/response mapping

### Test Structure
```
tests/
├── Domain.UnitTests/
│   ├── Entities/           # Customer, Order behavior tests
│   ├── ValueObjects/       # Money, Email, Address tests
│   └── Shared/            # Base class tests
└── Application.UnitTests/
    ├── Commands/           # Command handler tests
    └── Queries/            # Query handler tests
```

### Testing Tools
- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertion library
- **Moq**: Mocking framework for dependencies
- **InMemory Database**: EF Core test double

## Key Features

- **Clean Architecture**: Independent layers, easy to test and maintain
- **Domain-Driven Design**: Aggregates, Value Objects, Domain Events
- **CQRS Pattern**: Separate command/query models with MediatR
- **Comprehensive Testing**: Domain, Application, and API layer tests
- **Docker Support**: Multi-stage builds optimized for production
- **AWS Ready**: ECS Fargate deployment with CloudFormation

## Example Usage

### Create Customer
```bash
curl -X POST http://localhost:5000/api/v1/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john.doe@example.com",
    "preferences": {
      "newsletter": true,
      "language": "en-US"
    }
  }'
```

### Create Order
```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "totalAmount": 149.99,
    "currency": "USD",
    "details": {
      "items": ["Laptop", "Mouse"],
      "shipping": "express"
    }
  }'
```

## Technology Stack

### Core Technologies
- **.NET 8**: Latest LTS version with performance improvements
- **C# 12**: Latest language features and syntax
- **ASP.NET Core**: High-performance web framework
- **Entity Framework Core 8**: Modern ORM with LINQ support

### Libraries & Frameworks
- **MediatR 12**: CQRS and mediator pattern
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation
- **xUnit**: Unit testing framework
- **FluentAssertions**: Fluent test assertions

### Infrastructure
- **Docker**: Multi-stage containerization
- **AWS ECS Fargate**: Serverless containers
- **CloudFormation**: Infrastructure as Code
- **Application Load Balancer**: Traffic distribution and SSL

## Migration from Python

This project was migrated from a Python FastAPI implementation, demonstrating equivalent clean architecture patterns in .NET:

| Python | .NET 8 |
|--------|--------|
| FastAPI | ASP.NET Core Web API |
| Pydantic | C# Records/Classes |
| SQLAlchemy | Entity Framework Core |
| Pytest | xUnit + FluentAssertions |
| Uvicorn | Kestrel Server |
| Type Hints | Strong Typing |

## Performance & Scalability

### Performance Features
- **Async/Await**: Non-blocking I/O throughout
- **EF Core**: Optimized queries and change tracking
- **JSON Serialization**: System.Text.Json for high performance
- **Minimal API Overhead**: Direct controller-to-handler mapping

### Scalability Considerations
- **Stateless Design**: Horizontally scalable
- **Database**: Ready for read replicas and sharding
- **Caching**: Structured for Redis integration
- **Load Balancing**: AWS ALB distributes traffic

## Contributing

### Development Workflow
1. **Fork** the repository
2. **Create** feature branch: `git checkout -b feature/amazing-feature`
3. **Run** quality checks: `make format && make lint && make test`
4. **Commit** changes: `git commit -m 'Add amazing feature'`
5. **Push** to branch: `git push origin feature/amazing-feature`
6. **Open** Pull Request

### Code Standards
- Follow .NET naming conventions (PascalCase, camelCase)
- Use async/await for all I/O operations
- Maintain 80%+ test coverage
- Add XML documentation for public APIs
- Follow Clean Architecture principles
- Use nullable reference types

## Troubleshooting

```bash
make troubleshoot          # Environment diagnostics
make help                  # Show all commands  
make clean && make build   # Fix build issues
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

