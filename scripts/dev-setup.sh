#!/bin/bash

# Local Development Setup Script for Clean Architecture .NET 8
# This script sets up the local development environment and runs common development tasks

set -e

echo "========================================"
echo "Clean Architecture .NET 8 - Dev Setup"
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${PURPLE}[STEP]${NC} $1"
}

# Check if .NET 8 is installed
check_dotnet() {
    print_header "Checking .NET installation..."
    
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found!"
        print_status "Please install .NET 8 SDK:"
        print_status "  • Run: ./scripts/setup-dotnet-environment.sh"
        print_status "  • Or visit: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
    
    DOTNET_VERSION=$(dotnet --version)
    print_status "Found .NET SDK: $DOTNET_VERSION"
    
    # Check if .NET 8 is available
    if ! dotnet --list-sdks | grep -q "8\."; then
        print_warning "No .NET 8 SDK found. Current version: $DOTNET_VERSION"
        print_status "This project targets .NET 8. Consider installing .NET 8 SDK."
        read -p "Continue with current version? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        print_success "✅ .NET 8 SDK is available"
    fi
}

# Check project structure
check_project_structure() {
    print_header "Checking project structure..."
    
    if [[ ! -f "CleanArchitecture.sln" ]]; then
        print_error "CleanArchitecture.sln not found. Are you in the project root?"
        exit 1
    fi
    
    # Check required directories
    REQUIRED_DIRS=("src/Domain" "src/Application" "src/Infrastructure" "src/WebApi" "tests")
    for dir in "${REQUIRED_DIRS[@]}"; do
        if [[ ! -d "$dir" ]]; then
            print_error "Required directory not found: $dir"
            exit 1
        fi
    done
    
    print_success "✅ Project structure is valid"
}

# Restore packages
restore_packages() {
    print_header "Restoring NuGet packages..."
    
    if ! dotnet restore; then
        print_error "Package restore failed"
        exit 1
    fi
    
    print_success "✅ Packages restored successfully"
}

# Build solution
build_solution() {
    print_header "Building solution..."
    
    if ! dotnet build --no-restore; then
        print_error "Build failed"
        print_status "Fix the build errors above and try again"
        exit 1
    fi
    
    print_success "✅ Solution built successfully"
}

# Run tests
run_tests() {
    print_header "Running tests..."
    
    if ! dotnet test --no-build --logger "console;verbosity=normal"; then
        print_warning "Some tests failed"
        read -p "Continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        print_success "✅ All tests passed"
    fi
}

# Setup HTTPS development certificate
setup_https_cert() {
    print_header "Setting up HTTPS development certificate..."
    
    # Check if certificate already exists and is trusted
    if dotnet dev-certs https --check --trust &> /dev/null; then
        print_success "✅ HTTPS development certificate is already trusted"
        return
    fi
    
    print_status "Creating and trusting HTTPS development certificate..."
    
    # Generate and trust the certificate
    if dotnet dev-certs https --trust; then
        print_success "✅ HTTPS development certificate created and trusted"
    else
        print_warning "Failed to create/trust HTTPS certificate"
        print_status "You may need to run this manually or approve the certificate trust dialog"
    fi
}

# Create development configuration files
create_dev_configs() {
    print_header "Creating development configuration files..."
    
    # Create appsettings.Development.json if it doesn't exist
    DEV_SETTINGS="src/WebApi/appsettings.Development.json"
    if [[ ! -f "$DEV_SETTINGS" ]]; then
        print_status "Creating $DEV_SETTINGS..."
        cat > "$DEV_SETTINGS" << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleanArchitectureDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "AllowedHosts": "*"
}
EOF
        print_success "✅ Development settings created"
    else
        print_success "✅ Development settings already exist"
    fi
    
    # Create launch settings if they don't exist
    LAUNCH_SETTINGS="src/WebApi/Properties/launchSettings.json"
    if [[ ! -f "$LAUNCH_SETTINGS" ]]; then
        print_status "Creating $LAUNCH_SETTINGS..."
        mkdir -p "src/WebApi/Properties"
        cat > "$LAUNCH_SETTINGS" << 'EOF'
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF
        print_success "✅ Launch settings created"
    else
        print_success "✅ Launch settings already exist"
    fi
}

# Setup IDE configuration files
setup_ide_configs() {
    print_header "Setting up IDE configuration files..."
    
    # Create .editorconfig if it doesn't exist
    if [[ ! -f ".editorconfig" ]]; then
        print_status "Creating .editorconfig..."
        cat > ".editorconfig" << 'EOF'
root = true

[*]
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_style = space
indent_size = 4
end_of_line = crlf

[*.{json,js,yml,yaml}]
indent_style = space
indent_size = 2

[*.{md}]
trim_trailing_whitespace = false

# .NET formatting rules
[*.{cs,vb}]
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# C# formatting rules
[*.cs]
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_init = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_around_binary_operators = before_and_after
EOF
        print_success "✅ .editorconfig created"
    else
        print_success "✅ .editorconfig already exists"
    fi
    
    # Create .gitignore if it doesn't exist
    if [[ ! -f ".gitignore" ]]; then
        print_status "Creating .gitignore..."
        cat > ".gitignore" << 'EOF'
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# Visual Studio Code
.vscode/

# JetBrains Rider
.idea/

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Windows image file caches
Thumbs.db
ehthumbs.db

# Folder config file
Desktop.ini

# Recycle Bin used on file shares
$RECYCLE.BIN/

# Mac bundle stuff
*.dmg
*.app

# content below from: https://github.com/github/gitignore/blob/master/Global/macOS.gitignore
# General
.DS_Store
.AppleDouble
.LSOverride

# Thumbnails
._*

# Files that might appear in the root of a volume
.DocumentRevisions-V100
.fseventsd
.Spotlight-V100
.TemporaryItems
.Trashes
.VolumeIcon.icns
.com.apple.timemachine.donotpresent

# Directories potentially created on remote AFP share
.AppleDB
.AppleDesktop
Network Trash Folder
Temporary Items
.apdisk

# content below from: https://github.com/github/gitignore/blob/master/Global/Windows.gitignore
# Windows thumbnail cache files
Thumbs.db
ehthumbs.db
ehthumbs_vista.db

# Dump file
*.stackdump

# Folder config file
[Dd]esktop.ini

# Recycle Bin used on file shares
$RECYCLE.BIN/

# Windows Installer files
*.cab
*.msi
*.msix
*.msm
*.msp

# Windows shortcuts
*.lnk

# JetBrains Rider
.idea/
*.sln.iml

# CodeRush personal settings
.cr/personal

# Python tools for Visual Studio (PTVS)
__pycache__/
*.pyc

# Cake - Uncomment if you are using it
# tools/**
# !tools/packages.config

# Tabs Studio
*.tss

# Telerik's JustMock configuration file
*.jmconfig

# BizTalk build output
*.btp.cs
*.btm.cs
*.odx.cs
*.xsd.cs

# OpenCover UI analysis results
OpenCover/

# Azure Stream Analytics local run output
ASALocalRun/

# MSBuild Binary and Structured Log
*.binlog

# NVidia Nsight GPU debugger configuration file
*.nvuser

# MFractors (Xamarin productivity tool) working folder
.mfractor/

# Local History for Visual Studio
.localhistory/

# BeatPulse healthcheck temp database
healthchecksdb

# Backup folder for Package Reference Convert tool in Visual Studio 2017
MigrationBackup/

# Ionide (cross platform F# VS Code tools) working folder
.ionide/

# Test results
TestResults/
coverage/
*.coverage
*.coveragexml

# Publish profiles
PublishProfiles/

# User secrets
**/appsettings.*.json
!**/appsettings.json
!**/appsettings.Development.json
EOF
        print_success "✅ .gitignore created"
    else
        print_success "✅ .gitignore already exists"
    fi
}

# Install development tools
install_dev_tools() {
    print_header "Installing development tools..."
    
    # Install Entity Framework tools globally if not present
    if ! dotnet tool list -g | grep -q "dotnet-ef"; then
        print_status "Installing Entity Framework tools..."
        dotnet tool install --global dotnet-ef
        print_success "✅ Entity Framework tools installed"
    else
        print_success "✅ Entity Framework tools already installed"
    fi
    
    # Install code formatting tool if not present
    if ! dotnet tool list -g | grep -q "dotnet-format"; then
        print_status "Installing dotnet-format tool..."
        # dotnet-format is now built into .NET SDK 6.0+, but let's ensure it's available
        print_status "dotnet format is built into .NET SDK 6.0+"
    fi
    
    # Install report generator for code coverage if not present
    if ! dotnet tool list -g | grep -q "dotnet-reportgenerator-globaltool"; then
        print_status "Installing ReportGenerator for code coverage..."
        dotnet tool install --global dotnet-reportgenerator-globaltool
        print_success "✅ ReportGenerator installed"
    else
        print_success "✅ ReportGenerator already installed"
    fi
    
    # Install dotnet-outdated for package management
    if ! dotnet tool list -g | grep -q "dotnet-outdated-tool"; then
        print_status "Installing dotnet-outdated for package management..."
        dotnet tool install --global dotnet-outdated-tool
        print_success "✅ dotnet-outdated installed"
    else
        print_success "✅ dotnet-outdated already installed"
    fi
    
    # Install dotnet-script for running C# scripts
    if ! dotnet tool list -g | grep -q "dotnet-script"; then
        print_status "Installing dotnet-script for C# scripting..."
        dotnet tool install --global dotnet-script
        print_success "✅ dotnet-script installed"
    else
        print_success "✅ dotnet-script already installed"
    fi
    
    # Install diagnostic tools for production troubleshooting
    print_status "Installing .NET diagnostic tools..."
    
    # Install dotnet-counters for performance monitoring
    if ! dotnet tool list -g | grep -q "dotnet-counters"; then
        print_status "Installing dotnet-counters for performance monitoring..."
        dotnet tool install --global dotnet-counters
        print_success "✅ dotnet-counters installed"
    else
        print_success "✅ dotnet-counters already installed"
    fi
    
    # Install dotnet-dump for memory analysis
    if ! dotnet tool list -g | grep -q "dotnet-dump"; then
        print_status "Installing dotnet-dump for memory analysis..."
        dotnet tool install --global dotnet-dump
        print_success "✅ dotnet-dump installed"
    else
        print_success "✅ dotnet-dump already installed"
    fi
    
    # Install dotnet-trace for performance tracing
    if ! dotnet tool list -g | grep -q "dotnet-trace"; then
        print_status "Installing dotnet-trace for performance tracing..."
        dotnet tool install --global dotnet-trace
        print_success "✅ dotnet-trace installed"
    else
        print_success "✅ dotnet-trace already installed"
    fi
    
    # Install security scanning tool (optional - may not be available in all environments)
    if ! dotnet tool list -g | grep -q "security-scan"; then
        print_status "Attempting to install security-scan for vulnerability scanning..."
        if dotnet tool install --global security-scan 2>/dev/null; then
            print_success "✅ security-scan installed"
        else
            print_warning "security-scan not available - skipping (this is optional)"
        fi
    else
        print_success "✅ security-scan already installed"
    fi
}

# Run development server
run_development_server() {
    print_header "Starting development server..."
    
    print_status "The API will be available at:"
    print_status "  • HTTP:  http://localhost:5000"
    print_status "  • HTTPS: https://localhost:5001 (if HTTPS is configured)"
    print_status "  • Swagger UI: http://localhost:5000/swagger"
    print_status "  • Health Check: http://localhost:5000/health"
    print_status ""
    print_status "Press CTRL+C to stop the server"
    print_status "Use 'dotnet watch run --project src/WebApi' for hot reload"
    echo ""
    
    # Change to WebApi directory and run
    cd src/WebApi
    dotnet run
}

# Show development tips
show_dev_tips() {
    echo ""
    echo "========================================"
    print_success "🎉 Development environment ready!"
    echo "========================================"
    echo ""
    echo -e "${BLUE}Quick Development Commands:${NC}"
    echo "  make run              # Start the API server"
    echo "  make run-watch        # Start with hot reload"
    echo "  make test             # Run all tests"
    echo "  make build            # Build solution"
    echo "  make clean            # Clean build artifacts"
    echo ""
    echo -e "${BLUE}Development Workflow:${NC}"
    echo "  1. Make changes to your code"
    echo "  2. Tests run automatically (if using watch mode)"
    echo "  3. Use make format to format code"
    echo "  4. Use make lint to check code quality"
    echo "  5. Commit and push changes"
    echo ""
    echo -e "${BLUE}API Endpoints (when running):${NC}"
    echo "  • Swagger UI: http://localhost:5000"
    echo "  • Health: http://localhost:5000/health"
    echo "  • Customers: http://localhost:5000/api/v1/customers"
    echo "  • Orders: http://localhost:5000/api/v1/orders"
    echo ""
    echo -e "${BLUE}Development Tools:${NC}"
    echo "  • Entity Framework: dotnet ef migrations add <name>"
    echo "  • Code Coverage: make test-coverage"
    echo "  • Format Code: make format"
    echo "  • Check Outdated Packages: dotnet outdated"
    echo "  • Run C# Scripts: dotnet script script.csx"
    echo "  • Docker: make docker-build && make docker-run"
    echo ""
    echo -e "${BLUE}Diagnostic Tools:${NC}"
    echo "  • Performance Counters: dotnet counters monitor --process-id <pid>"
    echo "  • Memory Dumps: dotnet dump collect --process-id <pid>"
    echo "  • Performance Tracing: dotnet trace collect --process-id <pid>"
    echo "  • Security Scan: security-scan (if available)"
    echo ""
    print_success "🚀 Happy coding with Clean Architecture .NET 8!"
}

# Main execution
main() {
    print_status "Setting up local development environment..."
    
    check_dotnet
    check_project_structure
    restore_packages
    build_solution
    run_tests
    setup_https_cert
    create_dev_configs
    setup_ide_configs
    install_dev_tools
    
    echo ""
    echo "========================================"
    print_success "✅ Setup completed successfully!"
    echo "========================================"
    echo ""
    
    # Ask if user wants to start the server
    read -p "Start the development server now? (Y/n): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Nn]$ ]]; then
        show_dev_tips
    else
        run_development_server
    fi
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --no-run)
            NO_RUN=true
            shift
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --no-run               Don't start the development server after setup"
            echo "  --help                 Show this help message"
            echo ""
            echo "This script sets up the local development environment for Clean Architecture .NET 8"
            echo ""
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Run main function
if [[ "$NO_RUN" == "true" ]]; then
    # Run setup without starting server
    check_dotnet
    check_project_structure
    restore_packages
    build_solution
    run_tests
    setup_https_cert
    create_dev_configs
    setup_ide_configs
    install_dev_tools
    show_dev_tips
else
    main "$@"
fi