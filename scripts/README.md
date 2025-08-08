# Scripts Documentation

This directory contains deployment and setup scripts for the Clean Architecture .NET 8 project.

## 📋 Available Scripts

### 🔧 `setup-dotnet-environment.sh`
**Complete environment setup script for .NET 8 development**

**Supported Platforms:**
- Amazon Linux 2023
- Ubuntu/Debian
- CentOS/RHEL
- macOS (with Homebrew support)

**What it installs:**
- ✅ .NET 8 SDK (automatically detects and installs the right version)
- ✅ Git (if not already installed)  
- ✅ Docker (with proper user permissions)
- ✅ AWS CLI v2
- ✅ Development tools (build-essential, wget, curl, jq, etc.)
- ✅ Shell aliases and completions
- ✅ Project template structure

**Usage:**
```bash
# Make executable and run
chmod +x scripts/setup-dotnet-environment.sh
./scripts/setup-dotnet-environment.sh

# Note: setup-environment make command has been removed
# Use setup-dev for local development configuration
```

**Features:**
- 🎯 Auto-detects operating system and uses appropriate package manager
- 🛡️ Safe installation with error handling and rollback guidance
- 📋 Comprehensive verification and troubleshooting
- 🎨 Colored output with progress indicators
- 🔧 Sets up development aliases and shell completions

---

### 🚀 `deploy-aws.sh`
**Complete AWS deployment script for ECS Fargate**

**Prerequisites:**
- AWS CLI configured with appropriate permissions
- Docker installed and running
- .NET 8 SDK installed

**What it does:**
1. ✅ Builds and tests the .NET application
2. ✅ Deploys CloudFormation infrastructure (ECR, VPC, ALB)
3. ✅ Builds and pushes Docker image to ECR
4. ✅ Deploys ECS Fargate service
5. ✅ Waits for service health check
6. ✅ Provides endpoint URLs and management commands

**Usage:**
```bash
# Basic deployment
./scripts/deploy-aws.sh

# With custom options
./scripts/deploy-aws.sh --region us-west-2 --project-name my-api

# Via Makefile
export AWS_REGION=us-east-1
export ECR_REPO=my-clean-api
make deploy
```

**Command Line Options:**
- `--region REGION` - AWS region (default: us-east-1)
- `--project-name NAME` - Project name for AWS resources
- `--ecr-repo NAME` - ECR repository name
- `--help` - Show help message

**Environment Variables:**
- `AWS_REGION` - Overrides default region
- `ECR_REPO` - ECR repository name

---

### 🛠️ `dev-setup.sh`
**Local development environment setup and management**

**What it does:**
1. ✅ Verifies .NET 8 installation
2. ✅ Checks project structure
3. ✅ Restores NuGet packages
4. ✅ Builds and tests the solution
5. ✅ Sets up HTTPS development certificates
6. ✅ Creates development configuration files
7. ✅ Installs development tools (EF Core, ReportGenerator, dotnet-outdated, dotnet-script, diagnostic tools)
8. ✅ Creates IDE configuration files (.editorconfig, .gitignore)
9. ✅ Optionally starts the development server

**Usage:**
```bash
# Full setup with server start
./scripts/dev-setup.sh

# Setup without starting server
./scripts/dev-setup.sh --no-run

# Via Makefile
make setup-dev
```

**Created Files:**
- `src/WebApi/appsettings.Development.json`
- `src/WebApi/Properties/launchSettings.json`
- `.editorconfig` (C# formatting rules)
- `.gitignore` (.NET specific ignore patterns)

---

### ☁️ `setup-cloud9-dotnet.sh`
**AWS Cloud9 environment setup for .NET 8 development**

**Designed for:** A Cloud Guru learning environments and AWS Cloud9 instances

**What it installs:**
1. ✅ .NET 8 SDK with Entity Framework tools
2. ✅ Additional development tools (dotnet-outdated, dotnet-script)
3. ✅ Diagnostic tools (dotnet-counters, dotnet-dump, dotnet-trace)
4. ✅ ReportGenerator for code coverage
5. ✅ Docker for containerization
6. ✅ Git version control
7. ✅ Essential development utilities (jq, wget, tree, vim, etc.)
8. ✅ AWS CLI verification and configuration
9. ✅ Shell aliases and command completions
10. ✅ Common development aliases (dr=dotnet restore, db=dotnet build, etc.)

**Usage:**
```bash
# Make executable and run
chmod +x scripts/setup-cloud9-dotnet.sh
./scripts/setup-cloud9-dotnet.sh

# Via Makefile (if available)
make setup-cloud9
```

**Features:**
- 🎯 Optimized for Amazon Linux 2023 in Cloud9 environments
- 🔧 Sets up workspace directory structure
- 📋 Comprehensive verification and next steps
- 🎨 Colored output with progress indicators
- ⚡ Quick aliases for common .NET commands
- ⚠️ Automatic disk space checking with EBS recommendations

---

### 📚 Storage Management Documentation

For Cloud9 storage issues (disk space limitations), see the comprehensive guide:

**📖 [Cloud9 Storage Management Guide](../docs/CLOUD9-STORAGE.md)**

**Covers:**
- ✅ **Resize existing root volume** (recommended approach)
- ✅ **Add secondary EBS volume** (advanced scenarios)
- ✅ **Step-by-step AWS Console instructions**
- ✅ **Filesystem extension commands**
- ✅ **Troubleshooting common issues**
- ✅ **Best practices for .NET development**

**Quick Solution for Most Users:**
1. Resize root volume to 30GB via AWS Console
2. Run filesystem extension commands
3. Continue with .NET development

This documentation-based approach is more reliable than script automation for storage management.

---

## 🔄 Updated Makefile Integration

All scripts are integrated into the Makefile for easy access:

```bash
# Environment setup
make setup-dev            # Configure local development

# Development workflow  
make restore              # Restore NuGet packages
make build                # Build solution
make test                 # Run tests
make run                  # Start API server
make run-watch            # Start with hot reload

# Code quality
make format               # Format code
make lint                 # Code analysis
make security             # Security scan
# Development tools (requires manual tool installation)
make diagnostic-tools     # Show diagnostic tools info
make install-diagnostic-tools # Install diagnostic tools
make tools-list           # List installed .NET tools

# Docker operations
make docker-build         # Build Docker image (linux/amd64)
make docker-run           # Run in container (linux/amd64)

# AWS deployment
make deploy               # Full AWS deployment
make deploy-cloudformation # Infrastructure only

# Utilities
make clean                # Clean build artifacts
make troubleshoot         # Troubleshooting guide
make help                 # Show all commands
```

## 🏗️ Key Improvements from Python Version

### ✅ **Technology-Specific Adaptations**
- **Python** → **.NET**: Replaced `pip`, `pytest`, `uvicorn` with `dotnet` equivalents
- **FastAPI** → **ASP.NET Core**: Updated port (8000 → 5000), endpoint patterns
- **Streamlit** → **Swagger UI**: Focused on API documentation instead of demo UI

### ✅ **Enhanced Environment Detection**
- Multi-platform support (Linux, macOS, Windows via WSL)
- Automatic package manager detection (yum, apt, brew)
- Version compatibility checking for .NET 8

### ✅ **Improved Development Experience**
- HTTPS certificate setup for local development
- IDE configuration files (.editorconfig, launch settings)
- Development-specific appsettings.json
- Hot reload support with `dotnet watch`

### ✅ **Production-Ready Deployment**
- ECS Fargate instead of simple containers
- Application Load Balancer with health checks
- Proper VPC networking and security groups
- Tagged resources for cost tracking

### ✅ **Better Error Handling**
- Detailed error messages with fix instructions
- Rollback guidance for failed deployments
- Prerequisite checking before operations
- Comprehensive troubleshooting guides

## 📊 Usage Examples

### Quick Start (New Environment)
```bash
# 1. Setup local development
make setup-dev  

# 2. Build and test
make build
make test

# 3. Start developing
make run-watch
```

### CI/CD Pipeline
```bash
# Simulate full CI pipeline
make ci

# Individual steps
make restore
make build  
make lint
make test
make security
```

### AWS Deployment
```bash
# Setup AWS credentials first
aws configure

# Deploy to AWS
export AWS_REGION=us-east-1
export ECR_REPO=my-clean-api
make deploy
```

### Cloud9 Storage Setup (Recommended for Cloud Guru)
```bash
# Step 1: Fix storage if needed (see docs/CLOUD9-STORAGE.md)
# - Resize root volume to 30GB via AWS Console  
# - Extend filesystem: sudo growpart /dev/nvme0n1 1 && sudo xfs_growfs /

# Step 2: Setup .NET development environment
./scripts/setup-cloud9-dotnet.sh

# Step 3: Start developing with adequate space!
make run-watch
```

## 🔧 Troubleshooting

If any script fails:

1. **Check prerequisites**: Run `make check-env`
2. **View detailed help**: Run `make troubleshoot`  
3. **Check specific script**: Most scripts have `--help` option
4. **Review logs**: Scripts provide detailed error messages with fix suggestions

## 📝 Notes

- All scripts are idempotent (safe to run multiple times)
- Scripts detect existing installations and skip when appropriate
- Environment variables take precedence over command line arguments
- All AWS resources are tagged for easy identification and cost tracking

---

**🎯 These scripts provide a complete, production-ready deployment pipeline for Clean Architecture .NET 8 applications on AWS!**