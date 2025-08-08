# Deployment Guide

## Clean Architecture .NET 8 - Deployment Documentation

This guide covers the two main deployment scenarios for the Clean Architecture .NET 8 application.

## Deployment Scenarios

### 1. Local Development (macOS/Linux/Windows)
For local development on any operating system.

### 2. AWS ECS Fargate Deployment  
For production deployment to AWS cloud infrastructure.

---

## 🖥️ Local Development Deployment

### Prerequisites
- **.NET 8 SDK** - Latest version
- **Docker Desktop** (optional, for Docker testing)

### Quick Start
```bash
# Setup development environment
make setup-dev

# Start with hot reload
make run-watch

# Access application
open http://localhost:5000
```

### Manual Steps
```bash
# 1. Restore packages
dotnet restore

# 2. Build solution
dotnet build

# 3. Run tests
dotnet test

# 4. Start API
dotnet run --project src/WebApi

# 5. Access Swagger UI
open http://localhost:5000/swagger
```

### Local Configuration
- **Database**: Entity Framework InMemory
- **HTTPS Certificate**: Auto-generated development certificate
- **Hot Reload**: Automatic code reloading with `dotnet watch`

### Docker Testing (Local)
```bash
make docker-build          # Build Docker image
make docker-run             # Run container on port 8080
```

**Note**: Docker builds target linux/amd64 for AWS compatibility.

---

## ☁️ AWS ECS Fargate Deployment

### Prerequisites
- **AWS CLI** configured with credentials
- **Docker Desktop** running
- **Appropriate AWS permissions** (ECS, ECR, VPC, CloudFormation)

### Deployment Options

**📖 For complete AWS deployment instructions, choose:**

#### Option 1: Automated (Make Commands)
```bash
export AWS_REGION=us-east-1
export ECR_REPO=clean-architecture-dotnet
make deploy
```

#### Option 2: Manual Step-by-Step
See **[cloudformation/manual-deployment.md](../cloudformation/manual-deployment.md)** for detailed instructions.

**Recommended for:**
- Apple Silicon (M1/M2) Mac users experiencing Docker platform issues
- Learning the deployment process step-by-step  
- Troubleshooting deployment problems

### AWS Architecture Components
- **ECR**: Container image registry
- **VPC**: Network isolation with Application Load Balancer
- **ECS Fargate**: Serverless container hosting
- **CloudFormation**: Infrastructure as Code

### Platform Considerations

**Intel/AMD Systems**: Use automated make commands
**Apple Silicon Systems**: Use manual deployment guide for platform-specific Docker build instructions

---

## 📚 Additional Resources

### Detailed Deployment Guides
- **[cloudformation/README.md](../cloudformation/README.md)** - AWS deployment options overview
- **[cloudformation/manual-deployment.md](../cloudformation/manual-deployment.md)** - Detailed step-by-step AWS deployment

### Architecture Documentation  
- **[patterns/aws-logging-patterns.md](patterns/aws-logging-patterns.md)** - Environment-specific logging
- **[patterns/aws-jwt-security.md](patterns/aws-jwt-security.md)** - Security patterns

### Troubleshooting
```bash
make troubleshoot           # Environment diagnostic
curl -f http://localhost:5000/health  # Local health check
curl -f http://localhost:8080/health  # Docker health check
```

**Common Issues:**
- **Build failures**: Run `make clean && make restore`  
- **Docker platform issues**: See manual deployment guide
- **ECS task failures**: Check CloudWatch logs and health endpoint
- **Network issues**: Verify security groups and ALB configuration