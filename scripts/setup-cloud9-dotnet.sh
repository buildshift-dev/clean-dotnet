#!/bin/bash

# Setup script for Cloud9 AWS Linux 2023 instances
# This script installs .NET 8 SDK, Docker, and Git for Clean Architecture .NET project

set -e

echo "=================================="
echo "Cloud9 AWS Linux 2023 Setup Script"
echo "for Clean Architecture .NET 8 Project"
echo "=================================="

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

# Setup common aliases
setup_common_aliases() {
    print_status "Setting up common aliases..."
    
    # Check if aliases section already exists
    if ! grep -q "# Common shell aliases" ~/.bashrc; then
        echo "" >> ~/.bashrc
        echo "# Common shell aliases" >> ~/.bashrc
        echo "alias cls='clear'" >> ~/.bashrc
        echo "alias ll='ls -alF'" >> ~/.bashrc
        echo "alias la='ls -A'" >> ~/.bashrc
        echo "alias l='ls -CF'" >> ~/.bashrc
        echo "alias lt='ls -t -1 -long'" >> ~/.bashrc
        echo "alias ls='ls --color=auto'" >> ~/.bashrc
        echo "alias h='history'" >> ~/.bashrc
        echo "alias tree1='tree -a -L 1'" >> ~/.bashrc
        echo "alias tree2='tree -a -L 2'" >> ~/.bashrc
        echo "alias repos='cd ~/source/repos'" >> ~/.bashrc
        echo "alias c='clear'" >> ~/.bashrc
        print_success "Common aliases added to ~/.bashrc"
    else
        print_success "Common aliases already exist in ~/.bashrc"
    fi
}

# Check if running on Amazon Linux
check_os() {
    if [[ ! -f /etc/os-release ]] || ! grep -q "Amazon Linux" /etc/os-release; then
        print_warning "This script is designed for Amazon Linux 2023. Proceeding anyway..."
    else
        print_status "Amazon Linux detected. Proceeding with setup..."
    fi
}

# Update system packages
update_system() {
    print_status "Updating system packages..."
    sudo yum update -y
    print_success "System packages updated"
}

# Install .NET 8 SDK
install_dotnet8() {
    print_status "Checking for .NET 8 SDK..."
    
    if command -v dotnet &> /dev/null && dotnet --version | grep -q "^8\."; then
        print_success ".NET 8 SDK is already installed"
        # Upgrade to latest version
        print_status "Checking for .NET 8 updates..."
        sudo yum update -y dotnet-sdk-8.0
        print_success ".NET 8 SDK updated to latest version"
    else
        print_status "Installing .NET 8 SDK..."
        # Add Microsoft package repository for Amazon Linux
        sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
        sudo yum install -y dotnet-sdk-8.0
        print_success ".NET 8 SDK installed"
    fi
    
    # Verify installation
    dotnet --version
    
    # Install Entity Framework tools
    print_status "Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef --version 8.0.0
    print_success "Entity Framework Core tools installed"
    
    # Install additional development tools
    print_status "Installing additional .NET development tools..."
    
    # Install dotnet-outdated for package management
    print_status "Installing dotnet-outdated for package management..."
    dotnet tool install --global dotnet-outdated-tool
    print_success "dotnet-outdated installed"
    
    # Install dotnet-script for C# scripting
    print_status "Installing dotnet-script for C# scripting..."
    dotnet tool install --global dotnet-script
    print_success "dotnet-script installed"
    
    # Install diagnostic tools
    print_status "Installing .NET diagnostic tools..."
    dotnet tool install --global dotnet-counters
    dotnet tool install --global dotnet-dump
    dotnet tool install --global dotnet-trace
    print_success "Diagnostic tools installed (counters, dump, trace)"
    
    # Install report generator for code coverage
    print_status "Installing ReportGenerator for code coverage..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
    print_success "ReportGenerator installed"
}

# Set up .NET aliases
setup_dotnet_aliases() {
    print_status "Setting up .NET aliases..."
    
    # Add aliases to .bashrc if they don't exist
    if ! grep -q "alias dr=" ~/.bashrc; then
        echo "# .NET 8 SDK aliases for Clean Architecture project" >> ~/.bashrc
        echo "alias dr='dotnet restore'" >> ~/.bashrc
        echo "alias db='dotnet build'" >> ~/.bashrc
        echo "alias dt='dotnet test'" >> ~/.bashrc
        echo "alias drun='dotnet run'" >> ~/.bashrc
        echo "alias dwatch='dotnet watch'" >> ~/.bashrc
        print_success ".NET aliases added to ~/.bashrc"
    else
        print_success ".NET aliases already exist in ~/.bashrc"
    fi
    
    # Set aliases for current session
    alias dr='dotnet restore'
    alias db='dotnet build'
    alias dt='dotnet test'
    alias drun='dotnet run'
    alias dwatch='dotnet watch'
    
    print_status "Current .NET version: $(dotnet --version)"
}

# Install Docker
install_docker() {
    print_status "Checking for Docker..."
    
    if command -v docker &> /dev/null; then
        print_success "Docker is already installed: $(docker --version)"
    else
        print_status "Installing Docker..."
        sudo yum install -y docker
        sudo systemctl start docker
        sudo systemctl enable docker
        sudo usermod -a -G docker $USER
        print_success "Docker installed: $(docker --version)"
    fi
}

# Install Git (usually pre-installed on Cloud9)
install_git() {
    print_status "Checking for Git..."
    
    if command -v git &> /dev/null; then
        print_success "Git is already installed: $(git --version)"
    else
        print_status "Installing Git..."
        sudo yum install -y git
        print_success "Git installed: $(git --version)"
    fi
}

# Install additional useful tools
install_additional_tools() {
    print_status "Installing additional useful tools..."
    
    # Install jq for JSON parsing (useful for testing API responses)
    if command -v jq &> /dev/null; then
        print_success "jq is already installed"
    else
        sudo yum install -y jq
        print_success "jq installed"
    fi
    
    # Install zip/unzip (usually pre-installed, but make sure)
    sudo yum install -y zip unzip
    print_success "zip/unzip tools verified"
    
    # Install make utility
    if command -v make &> /dev/null; then
        print_success "make is already installed"
    else
        sudo yum install -y make
        print_success "make installed"
    fi
    
    # Install essential development tools
    print_status "Installing essential development tools..."
    sudo yum groupinstall -y "Development Tools"
    print_success "Development Tools group installed"
    
    # Install additional useful utilities
    sudo yum install -y wget tree vim htop tmux
    print_success "Additional utilities (wget, tree, vim, htop, tmux) installed"
    
    # Upgrade curl to full version for better functionality
    if ! rpm -q curl-full &> /dev/null; then
        print_status "Upgrading curl to full version..."
        sudo yum install -y --allowerasing curl-full libcurl-full
        print_success "curl upgraded to full version"
    else
        print_success "curl-full already installed"
    fi
}

# Verify AWS CLI
verify_aws_cli() {
    print_status "Verifying AWS CLI..."
    
    if command -v aws &> /dev/null; then
        print_success "AWS CLI found: $(aws --version)"
        
        # Check if AWS credentials are configured
        if aws sts get-caller-identity &> /dev/null; then
            print_success "AWS credentials are configured"
            aws sts get-caller-identity --query 'Account' --output text | xargs echo "AWS Account:"
        else
            print_warning "AWS credentials not configured or insufficient permissions"
            print_status "Run 'aws configure' to set up credentials if needed"
        fi
    else
        print_warning "AWS CLI not found. Installing..."
        curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
        unzip awscliv2.zip
        sudo ./aws/install
        rm -rf awscliv2.zip aws/
        print_success "AWS CLI installed"
    fi
}

# Setup shell completions
setup_completions() {
    print_status "Setting up command completions..."
    
    # Setup AWS CLI completion
    if command -v aws &> /dev/null; then
        # Check if AWS completion is already in .bashrc
        if ! grep -q "aws_completer" ~/.bashrc; then
            print_status "Adding AWS CLI completion..."
            echo "" >> ~/.bashrc
            echo "# AWS CLI completion" >> ~/.bashrc
            echo "complete -C '/usr/local/bin/aws_completer' aws" >> ~/.bashrc
            print_success "AWS CLI completion added to ~/.bashrc"
        else
            print_success "AWS CLI completion already configured"
        fi
    fi
    
    # Setup .NET CLI completion
    if command -v dotnet &> /dev/null; then
        # Check if .NET completion is already in .bashrc
        if ! grep -q "dotnet.*complete" ~/.bashrc; then
            print_status "Adding .NET CLI completion..."
            echo "" >> ~/.bashrc
            echo "# .NET CLI completion" >> ~/.bashrc
            echo "complete -C dotnet dotnet" >> ~/.bashrc
            print_success ".NET CLI completion added to ~/.bashrc"
        else
            print_success ".NET CLI completion already configured"
        fi
    fi
    
    print_success "Shell completions configured"
}

# Create workspace directories
setup_workspace() {
    print_status "Setting up workspace directories..."
    
    # Create a workspace directory if it doesn't exist
    mkdir -p ~/clean-architecture-dotnet
    
    print_status "Workspace created at ~/clean-architecture-dotnet"
    print_status "You can clone or copy the project files there"
}

# Check available disk space and warn if low
check_disk_space() {
    print_header "Checking available disk space..."
    
    # Get available space in GB
    AVAILABLE_SPACE=$(df / | awk 'NR==2 {print int($4/1024/1024)}')
    
    print_status "Available disk space: ${AVAILABLE_SPACE}GB"
    
    if [[ $AVAILABLE_SPACE -lt 5 ]]; then
        print_error "⚠️  WARNING: Low disk space detected!"
        echo ""
        echo -e "${YELLOW}Your Cloud9 environment has less than 5GB available space.${NC}"
        echo -e "${YELLOW}.NET builds and packages require significant storage.${NC}"
        echo ""
        echo -e "${BLUE}Recommended Solution: Resize root volume${NC}"
        echo "1. See storage management guide: docs/CLOUD9-STORAGE.md"
        echo "2. Quick fix - resize root volume to 30GB via AWS Console"
        echo "3. Extend filesystem:"
        echo -e "   ${YELLOW}sudo growpart /dev/nvme0n1 1 && sudo xfs_growfs /${NC}"
        echo "4. Then re-run this setup script"
        echo ""
        read -p "Do you want to continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_status "Setup cancelled. Please mount EBS storage first."
            exit 0
        fi
        print_warning "Continuing with limited disk space..."
    else
        print_success "Sufficient disk space available"
    fi
}

# Display next steps
show_next_steps() {
    echo ""
    echo "=================================="
    print_success "Setup completed successfully!"
    echo "=================================="
    echo ""
    echo -e "${BLUE}Next Steps:${NC}"
    echo ""
    echo "1. Reload your shell to use the new aliases:"
    echo -e "   ${YELLOW}source ~/.bashrc${NC}"
    echo ""
    echo "2. Verify installations:"
    echo -e "   ${YELLOW}dotnet --version     # Should show .NET 8.x.x${NC}"
    echo -e "   ${YELLOW}docker --version     # Should show Docker version${NC}"
    echo -e "   ${YELLOW}dotnet ef --version  # Should show EF Core tools${NC}"
    echo -e "   ${YELLOW}git --version        # Should show Git version${NC}"
    echo -e "   ${YELLOW}dotnet tool list -g  # Should show all installed tools${NC}"
    echo ""
    echo "3. Test tab completion (after reload):"
    echo -e "   ${YELLOW}aws <TAB><TAB>        # Shows AWS CLI commands${NC}"
    echo -e "   ${YELLOW}dotnet <TAB><TAB>     # Shows .NET CLI commands${NC}"
    echo ""
    echo "4. Test .NET aliases:"
    echo -e "   ${YELLOW}dr                   # dotnet restore${NC}"
    echo -e "   ${YELLOW}db                   # dotnet build${NC}"
    echo -e "   ${YELLOW}dt                   # dotnet test${NC}"
    echo -e "   ${YELLOW}drun                 # dotnet run${NC}"
    echo ""
    echo "5. Start working with the Clean Architecture project:"
    echo -e "   ${YELLOW}cd ~/clean-architecture-dotnet/${NC}"
    echo -e "   ${YELLOW}make setup-dev       # Setup development environment${NC}"
    echo -e "   ${YELLOW}make run             # Start the API${NC}"
    echo ""
    echo "6. Use additional development tools:"
    echo -e "   ${YELLOW}dotnet outdated              # Check for outdated packages${NC}"
    echo -e "   ${YELLOW}dotnet script script.csx     # Run C# scripts${NC}"
    echo -e "   ${YELLOW}dotnet counters monitor <pid> # Monitor performance${NC}"
    echo ""
    echo "7. Follow the README.md in the project directory"
    echo ""
    print_success "Happy coding with .NET 8!"
}

# Main execution
main() {
    print_status "Starting Cloud9 setup for Clean Architecture .NET 8 project..."
    
    check_os
    check_disk_space
    update_system
    install_dotnet8
    setup_dotnet_aliases
    install_docker
    install_git
    install_additional_tools
    verify_aws_cli
    setup_completions
    setup_common_aliases
    setup_workspace
    show_next_steps
}

# Run main function
main "$@"