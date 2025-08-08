# Makefile for Clean Architecture .NET

.PHONY: help restore build clean test test-unit test-integration run lint format run-watch docker-build docker-run deploy-local troubleshoot check-env setup-dev deploy diagnostic-tools install-diagnostic-tools tools-list

# .NET executable path (auto-detect or fallback)
DOTNET := $(shell which dotnet 2>/dev/null || echo "/opt/homebrew/Cellar/dotnet@8/8.0.119/bin/dotnet")

help: ## Show this help message
	@echo "Available commands:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'
	@echo ""
	@echo "💡 If any command fails, it will show specific fix instructions."
	@echo "💡 For comprehensive troubleshooting, run: make troubleshoot"
	@echo "💡 Using dotnet: $(DOTNET)"

restore: ## Restore NuGet packages for all projects
	@echo "🔄 Restoring NuGet packages..."
	@if ! $(DOTNET) restore; then \
		echo ""; \
		echo "❌ Package restore failed. To fix:"; \
		echo "  • Check internet connection"; \
		echo "  • Clear NuGet cache: dotnet nuget locals all --clear"; \
		echo "  • Check NuGet feeds: dotnet nuget list source"; \
		exit 1; \
	fi
	@echo "✅ Package restore completed!"

build: restore ## Build the entire solution
	@echo "🔨 Building solution..."
	@if ! $(DOTNET) build --no-restore; then \
		echo ""; \
		echo "❌ Build failed. To fix:"; \
		echo "  • Check compilation errors above"; \
		echo "  • Run 'make clean' then 'make build'"; \
		echo "  • Check project references and dependencies"; \
		exit 1; \
	fi
	@echo "✅ Build completed successfully!"

clean: ## Clean build artifacts and cache
	@echo "🧹 Cleaning build artifacts..."
	$(DOTNET) clean
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	rm -rf TestResults/
	rm -rf coverage/
	@echo "✅ Clean completed!"

test: build ## Run all tests
	@echo "🧪 Running all tests..."
	@if ! $(DOTNET) test --no-build --logger "console;verbosity=normal"; then \
		echo ""; \
		echo "❌ Tests failed. To fix:"; \
		echo "  • Check test output above for specific failures"; \
		echo "  • Run specific test: dotnet test tests/Domain.UnitTests/Domain.UnitTests.csproj"; \
		echo "  • Run with detailed output: dotnet test --verbosity detailed"; \
		echo "  • Debug single test: dotnet test --filter \"TestMethodName\""; \
		exit 1; \
	fi
	@echo "✅ All tests passed!"

test-unit: build ## Run unit tests only
	@echo "🧪 Running unit tests..."
	@if ! $(DOTNET) test tests/Domain.UnitTests/Domain.UnitTests.csproj --no-build; then \
		echo ""; \
		echo "❌ Domain unit tests failed. To fix:"; \
		echo "  • Check specific test failures above"; \
		echo "  • Run domain tests: dotnet test tests/Domain.UnitTests/"; \
		exit 1; \
	fi
	@if ! $(DOTNET) test tests/Application.UnitTests/Application.UnitTests.csproj --no-build; then \
		echo ""; \
		echo "❌ Application unit tests failed. To fix:"; \
		echo "  • Check specific test failures above"; \
		echo "  • Run application tests: dotnet test tests/Application.UnitTests/"; \
		exit 1; \
	fi
	@echo "✅ Unit tests passed!"

test-integration: build ## Run integration tests (when they exist)
	@echo "🧪 Looking for integration tests..."
	@if [ -d "tests/Integration.Tests" ]; then \
		$(DOTNET) test tests/Integration.Tests/ --no-build; \
	else \
		echo "No integration tests found. Create in tests/Integration.Tests/"; \
	fi

test-coverage: ## Run tests with code coverage
	@echo "📊 Running tests with coverage..."
	@echo "Installing coverlet.msbuild if not present..."
	@if ! $(DOTNET) test --collect:"XPlat Code Coverage" --results-directory TestResults/; then \
		echo ""; \
		echo "❌ Coverage collection failed. To fix:"; \
		echo "  • Install coverage tools: dotnet add package coverlet.msbuild"; \
		echo "  • Generate reports: dotnet tool install -g dotnet-reportgenerator-globaltool"; \
		exit 1; \
	fi
	@echo "✅ Coverage data generated in TestResults/"
	@echo "To view HTML report:"
	@echo "  • Install: dotnet tool install -g dotnet-reportgenerator-globaltool"
	@echo "  • Generate: reportgenerator -reports:TestResults/*/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html"

lint: ## Run code analysis and formatting checks
	@echo "🔍 Running code analysis..."
	@if ! $(DOTNET) format --verify-no-changes --verbosity diagnostic; then \
		echo ""; \
		echo "❌ Code formatting issues found. To fix:"; \
		echo "  • Auto-format: make format"; \
		echo "  • Check specific files: dotnet format <file-path>"; \
		exit 1; \
	fi
	@echo "🔍 Running static analysis..."
	@if ! $(DOTNET) build --verbosity quiet --no-restore 2>/dev/null; then \
		echo ""; \
		echo "❌ Build errors found. To fix:"; \
		echo "  • Check compilation errors: make build"; \
		echo "  • Clean and rebuild: make clean && make build"; \
		exit 1; \
	fi
	@echo "✅ Code analysis completed!"

format: ## Format code using dotnet format
	@echo "🎨 Formatting code..."
	@if ! $(DOTNET) format; then \
		echo ""; \
		echo "❌ Code formatting failed. To fix:"; \
		echo "  • Check file permissions"; \
		echo "  • Ensure valid C# syntax"; \
		echo "  • Check .editorconfig settings"; \
		exit 1; \
	fi
	@echo "✅ Code formatting completed!"

run: build ## Run the Web API application
	@echo "🚀 Starting Clean Architecture Web API..."
	@echo "API will be available at: http://localhost:5000"
	@echo "Swagger UI will be available at: http://localhost:5000"
	@echo "Health check available at: http://localhost:5000/health"
	@echo "Press CTRL+C to stop the server"
	@echo ""
	$(DOTNET) run --project src/WebApi --no-build

run-watch: ## Run the API with hot reload (file watching)
	@echo "🚀 Starting API with hot reload..."
	@echo "API will be available at: http://localhost:5000"
	@echo "Files will be watched for changes"
	@echo "Press CTRL+C to stop the server"
	@echo ""
	$(DOTNET) watch --project src/WebApi run

run-https: build ## Run the API with HTTPS
	@echo "🔒 Starting API with HTTPS..."
	@echo "Trust development certificate if prompted"
	$(DOTNET) dev-certs https --trust
	$(DOTNET) run --project src/WebApi --no-build --launch-profile https

docker-build: ## Build Docker image (for linux/amd64)
	@echo "🐳 Building Docker image..."
	@echo "Building for linux/amd64 platform (ECS Fargate compatible)..."
	@if ! docker build --platform linux/amd64 -t clean-architecture-dotnet .; then \
		echo ""; \
		echo "❌ Docker build failed. To fix:"; \
		echo "  • Ensure Docker is running"; \
		echo "  • Check Dockerfile syntax"; \
		echo "  • Verify .dockerignore excludes unnecessary files"; \
		echo "  • On Apple Silicon: Use docker buildx create --use"; \
		exit 1; \
	fi
	@echo "✅ Docker image built successfully!"

docker-run: docker-build ## Run application in Docker container
	@echo "🐳 Running in Docker container..."
	@echo "API will be available at: http://localhost:8080"
	@echo "Press CTRL+C to stop the container"
	@echo ""
	docker run -it --rm -p 8080:8080 clean-architecture-dotnet

publish: ## Publish application for deployment
	@echo "📦 Publishing application..."
	$(DOTNET) publish src/WebApi/WebApi.csproj -c Release -o ./publish
	@echo "✅ Application published to ./publish/"

# AWS deployment targets
# REMOVED: deploy-ecr command - use 'make deploy' or cloudformation/build-and-deploy.sh instead

deploy-cloudformation: ## Deploy CloudFormation stacks
	@echo "☁️  Deploying CloudFormation stacks..."
	@if [ -z "$(AWS_REGION)" ]; then \
		echo "❌ Set AWS_REGION environment variable"; \
		exit 1; \
	fi
	@echo "Deploying ECR repository..."
	aws cloudformation deploy --template-file cloudformation/ecr.yaml --stack-name clean-arch-ecr --region $(AWS_REGION)
	@echo "Deploying VPC and ALB..."
	aws cloudformation deploy --template-file cloudformation/vpc-alb.yaml --stack-name clean-arch-vpc-alb --region $(AWS_REGION)
	@echo "✅ Infrastructure deployed!"
	@echo "Now run: make deploy"
	@echo "Then deploy ECS: aws cloudformation deploy --template-file cloudformation/ecs-fargate.yaml --stack-name clean-arch-ecs --region $(AWS_REGION)"

# Development quality checks
security: ## Run security analysis (basic checks)
	@echo "🔒 Running security analysis..."
	@echo "Checking for hardcoded secrets..."
	@if grep -r "password\|secret\|token\|key" src/ --include="*.cs" --include="*.json" | grep -v "// " | grep -v "/// "; then \
		echo "⚠️  Potential hardcoded secrets found above"; \
	else \
		echo "✅ No obvious hardcoded secrets found"; \
	fi
	@echo "For comprehensive security scanning, consider:"
	@echo "  • SonarQube: https://www.sonarqube.org/"
	@echo "  • DevSkim: https://github.com/Microsoft/DevSkim"

dependency-check: ## Check for vulnerable NuGet packages
	@echo "🔍 Checking for vulnerable packages..."
	@if command -v dotnet-list-outdated >/dev/null 2>&1; then \
		$(DOTNET) list package --vulnerable --include-transitive; \
	else \
		echo "Install: dotnet tool install --global dotnet-outdated-tool"; \
		echo "Then run: dotnet outdated"; \
	fi
	@echo "Consider using: dotnet list package --outdated"

# REMOVED: outdated command - requires optional dotnet-outdated-tool

# REMOVED: script command - requires optional dotnet-script tool

diagnostic-tools: ## Show installed diagnostic tools and usage
	@echo "🔬 .NET Diagnostic Tools:"
	@echo "========================"
	@echo ""
	@if command -v dotnet-counters >/dev/null 2>&1; then \
		echo "✅ dotnet-counters: Performance monitoring"; \
		echo "   Usage: dotnet counters monitor --process-id <pid>"; \
		echo "   Or:    dotnet counters monitor --name WebApi"; \
	else \
		echo "❌ dotnet-counters not installed"; \
	fi
	@echo ""
	@if command -v dotnet-dump >/dev/null 2>&1; then \
		echo "✅ dotnet-dump: Memory dump analysis"; \
		echo "   Usage: dotnet dump collect --process-id <pid>"; \
		echo "   Analyze: dotnet dump analyze <dump-file>"; \
	else \
		echo "❌ dotnet-dump not installed"; \
	fi
	@echo ""
	@if command -v dotnet-trace >/dev/null 2>&1; then \
		echo "✅ dotnet-trace: Performance tracing"; \
		echo "   Usage: dotnet trace collect --process-id <pid>"; \
		echo "   Or:    dotnet trace collect --name WebApi"; \
	else \
		echo "❌ dotnet-trace not installed"; \
	fi
	@echo ""
	@echo "To install all diagnostic tools:"
	@echo "  make install-diagnostic-tools"

install-diagnostic-tools: ## Install .NET diagnostic tools
	@echo "🔧 Installing .NET diagnostic tools..."
	@echo "Installing dotnet-counters..."
	@dotnet tool install --global dotnet-counters || echo "dotnet-counters already installed"
	@echo "Installing dotnet-dump..."
	@dotnet tool install --global dotnet-dump || echo "dotnet-dump already installed"
	@echo "Installing dotnet-trace..."
	@dotnet tool install --global dotnet-trace || echo "dotnet-trace already installed"
	@echo "✅ Diagnostic tools installation complete!"

tools-list: ## List all installed .NET global tools
	@echo "🛠️  Installed .NET Global Tools:"
	@echo "================================"
	@dotnet tool list --global || echo "No global tools installed"

# Pre-commit style checks
pre-commit: format lint test-unit ## Run all pre-commit checks

# CI pipeline simulation  
ci: ## Simulate CI pipeline
	@echo "=== Checking environment ==="
	make check-env
	@echo "=== Restoring packages ==="
	make restore
	@echo "=== Building solution ==="
	make build
	@echo "=== Running linting ==="
	make lint
	@echo "=== Running tests ==="
	make test
	@echo "=== Security checks ==="
	make security
	@echo "=== All CI checks passed! ==="

check-env: ## Check development environment
	@echo "🔧 Checking .NET environment..."
	@$(DOTNET) --version || (echo "❌ .NET not found" && exit 1)
	@$(DOTNET) --list-sdks | grep "8\." >/dev/null || echo "⚠️  .NET 8 SDK not found"
	@if docker --version >/dev/null 2>&1; then echo "✅ Docker available"; else echo "⚠️  Docker not found"; fi
	@if aws --version >/dev/null 2>&1; then echo "✅ AWS CLI available"; else echo "⚠️  AWS CLI not found (needed for deployment)"; fi

troubleshoot: ## Comprehensive troubleshooting guide
	@echo "🔧 Clean Architecture .NET - Troubleshooting Guide"
	@echo "=================================================="
	@echo ""
	@echo "📋 Environment Check:"
	@$(DOTNET) --version || echo "❌ .NET not found - install .NET 8 SDK"
	@$(DOTNET) --list-sdks | head -5
	@echo ""
	@echo "📋 Project Structure:"
	@if [ -f "CleanArchitecture.sln" ]; then \
		echo "✅ Solution file found"; \
	else \
		echo "❌ CleanArchitecture.sln missing"; \
	fi
	@if [ -d "src" ]; then \
		echo "✅ src directory exists"; \
		ls -la src/; \
	else \
		echo "❌ src directory missing"; \
	fi
	@echo ""
	@echo "📋 Dependencies:"
	@echo "Checking NuGet packages..."
	@$(DOTNET) list package 2>/dev/null | head -10 || echo "❌ Cannot list packages - run 'make restore'"
	@echo ""
	@echo "📋 Build Status:"
	@if $(DOTNET) build --verbosity quiet >/dev/null 2>&1; then \
		echo "✅ Solution builds successfully"; \
	else \
		echo "❌ Build errors exist - run 'make build' for details"; \
	fi
	@echo ""
	@echo "📋 API Endpoints (when running):"
	@echo "  • Swagger UI: http://localhost:5000"
	@echo "  • Health Check: http://localhost:5000/health"
	@echo "  • Customers API: http://localhost:5000/api/v1/customers"
	@echo "  • Orders API: http://localhost:5000/api/v1/orders"
	@echo ""
	@echo "📋 Common Fixes:"
	@echo "  • Build errors: make clean && make build"
	@echo "  • Package issues: make restore"
	@echo "  • Formatting: make format"
	@echo "  • Test failures: make test-unit"
	@echo ""
	@echo "📋 Quick Setup (if starting fresh):"
	@echo "  1. make restore"
	@echo "  2. make build"
	@echo "  3. make test"
	@echo "  4. make run"
	@echo ""
	@echo "📋 Docker Commands:"
	@echo "  • Build image: make docker-build"
	@echo "  • Run container: make docker-run"
	@echo ""
	@echo "📋 AWS Deployment:"
	@echo "  • Set environment: export AWS_REGION=us-east-1 ECR_REPO=my-repo"
	@echo "  • Deploy infrastructure: make deploy-cloudformation"
	@echo "  • Deploy application: make deploy"
	@echo ""
	@echo "📋 Development Tools:"
	@echo "  • Hot reload: make run-watch"
	@echo "  • HTTPS mode: make run-https"
	@echo "  • Code coverage: make test-coverage"
	@echo "  • Security check: make security"

setup-dev: ## Setup local development environment
	@echo "🔧 Setting up local development environment..."
	@./scripts/dev-setup.sh --no-run
	@echo "✅ Development environment setup complete!"

# REMOVED: setup-environment - manual setup required for .NET 8, see project documentation

deploy: ## Deploy to AWS (requires AWS CLI and Docker)
	@echo "🚀 Deploying to AWS..."
	@if [ -f "./scripts/deploy-aws.sh" ]; then \
		./scripts/deploy-aws.sh; \
	else \
		echo "❌ Deployment script not found: ./scripts/deploy-aws.sh"; \
		exit 1; \
	fi