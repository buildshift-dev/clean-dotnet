#!/bin/bash

# Setup script for Clean Architecture .NET 8 Development Environment
# Supports: Amazon Linux 2023, Ubuntu/Debian, CentOS/RHEL, macOS
# This script installs .NET 8 SDK, Docker, Git, and development tools

set -e

echo "=========================================="
echo "Clean Architecture .NET 8 Environment Setup"
echo "=========================================="

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

# Detect operating system
detect_os() {
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        if [[ -f /etc/os-release ]]; then
            . /etc/os-release
            OS=$NAME
            VER=$VERSION_ID
        elif type lsb_release >/dev/null 2>&1; then
            OS=$(lsb_release -si)
            VER=$(lsb_release -sr)
        elif [[ -f /etc/redhat-release ]]; then
            OS="Red Hat Enterprise Linux"
            VER=$(cat /etc/redhat-release | sed s/.*release\ // | sed s/\ .*//)
        else
            OS=$(uname -s)
            VER=$(uname -r)
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macOS"
        VER=$(sw_vers -productVersion)
    else
        OS=$(uname -s)
        VER=$(uname -r)
    fi
    
    print_status "Detected OS: $OS $VER"
}

# Update system packages
update_system() {
    print_header "Updating system packages..."
    
    if [[ "$OS" == *"Amazon Linux"* ]]; then
        sudo yum update -y
    elif [[ "$OS" == *"Ubuntu"* ]] || [[ "$OS" == *"Debian"* ]]; then
        sudo apt-get update -y && sudo apt-get upgrade -y
    elif [[ "$OS" == *"CentOS"* ]] || [[ "$OS" == *"Red Hat"* ]]; then
        sudo yum update -y
    elif [[ "$OS" == "macOS" ]]; then
        print_status "Skipping system update on macOS (run 'softwareupdate -ia' manually if needed)"
        return
    else
        print_warning "Unsupported OS for automatic updates: $OS"
        return
    fi
    
    print_success "System packages updated"
}

# Install .NET 8 SDK
install_dotnet8() {
    print_header "Installing .NET 8 SDK..."
    
    # Check if .NET 8 is already installed
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        if [[ "$DOTNET_VERSION" == 8.* ]]; then
            print_success ".NET 8 SDK is already installed: $DOTNET_VERSION"
            return
        else
            print_status "Found .NET version $DOTNET_VERSION, installing .NET 8 alongside..."
        fi
    fi
    
    if [[ "$OS" == *"Amazon Linux"* ]]; then
        # Amazon Linux 2023
        print_status "Installing .NET 8 on Amazon Linux..."
        
        # Add Microsoft package repository
        sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
        sudo wget -O /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/centos/7/prod.repo
        
        # Install .NET 8 SDK
        sudo yum install -y dotnet-sdk-8.0
        
    elif [[ "$OS" == *"Ubuntu"* ]]; then
        # Ubuntu
        print_status "Installing .NET 8 on Ubuntu..."
        
        # Get Ubuntu version
        UBUNTU_VER=$(lsb_release -rs)
        
        # Add Microsoft package repository
        wget https://packages.microsoft.com/config/ubuntu/${UBUNTU_VER}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        
        # Update package index and install .NET 8 SDK
        sudo apt-get update -y
        sudo apt-get install -y dotnet-sdk-8.0
        
    elif [[ "$OS" == *"Debian"* ]]; then
        # Debian
        print_status "Installing .NET 8 on Debian..."
        
        # Add Microsoft package repository
        wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        
        # Update package index and install .NET 8 SDK
        sudo apt-get update -y
        sudo apt-get install -y dotnet-sdk-8.0
        
    elif [[ "$OS" == *"CentOS"* ]] || [[ "$OS" == *"Red Hat"* ]]; then
        # CentOS/RHEL
        print_status "Installing .NET 8 on CentOS/RHEL..."
        
        # Add Microsoft package repository
        sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
        sudo wget -O /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/centos/8/prod.repo
        
        # Install .NET 8 SDK
        sudo yum install -y dotnet-sdk-8.0
        
    elif [[ "$OS" == "macOS" ]]; then
        # macOS
        print_status "Installing .NET 8 on macOS..."
        
        if command -v brew &> /dev/null; then
            # Use Homebrew if available
            print_status "Using Homebrew to install .NET 8..."
            brew install --cask dotnet-sdk
            # Specifically install .NET 8 if not the default
            if ! dotnet --list-sdks | grep -q "8\."; then
                brew install dotnet@8
            fi
        else
            # Direct download and install
            print_status "Downloading .NET 8 SDK installer..."
            ARCH=$(uname -m)
            if [[ "$ARCH" == "arm64" ]]; then
                curl -L -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
                chmod +x dotnet-install.sh
                ./dotnet-install.sh --version latest --channel 8.0
                rm dotnet-install.sh
                
                # Add to PATH if not already there
                if ! grep -q "/.dotnet" ~/.zshrc 2>/dev/null; then
                    echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc
                fi
                if ! grep -q "/.dotnet" ~/.bash_profile 2>/dev/null; then
                    echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bash_profile
                fi
            else
                curl -L -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
                chmod +x dotnet-install.sh
                ./dotnet-install.sh --version latest --channel 8.0
                rm dotnet-install.sh
            fi
        fi
    else
        print_error "Unsupported operating system for automatic .NET installation: $OS"
        print_status "Please visit https://dotnet.microsoft.com/download/dotnet/8.0 to install manually"
        return
    fi
    
    # Verify installation
    if command -v dotnet &> /dev/null; then
        INSTALLED_VERSION=$(dotnet --version)
        print_success ".NET SDK installed successfully: $INSTALLED_VERSION"
        
        # List all installed SDKs
        print_status "Installed .NET SDKs:"
        dotnet --list-sdks | head -5
    else
        print_error ".NET SDK installation failed"
        exit 1
    fi
}

# Install Git
install_git() {
    print_header "Installing Git..."
    
    if command -v git &> /dev/null; then
        print_success "Git is already installed: $(git --version)"
        return
    fi
    
    if [[ "$OS" == *"Amazon Linux"* ]] || [[ "$OS" == *"CentOS"* ]] || [[ "$OS" == *"Red Hat"* ]]; then
        sudo yum install -y git
    elif [[ "$OS" == *"Ubuntu"* ]] || [[ "$OS" == *"Debian"* ]]; then
        sudo apt-get install -y git
    elif [[ "$OS" == "macOS" ]]; then
        if command -v brew &> /dev/null; then
            brew install git
        else
            print_status "Install Git via Xcode Command Line Tools: xcode-select --install"
        fi
    fi
    
    print_success "Git installed: $(git --version)"
}

# Install Docker
install_docker() {
    print_header "Installing Docker..."
    
    if command -v docker &> /dev/null; then
        print_success "Docker is already installed: $(docker --version)"
        return
    fi
    
    if [[ "$OS" == *"Amazon Linux"* ]]; then
        sudo yum install -y docker
        sudo systemctl enable docker
        sudo systemctl start docker
        sudo usermod -a -G docker $USER
        
    elif [[ "$OS" == *"Ubuntu"* ]]; then
        # Install Docker on Ubuntu
        curl -fsSL https://get.docker.com -o get-docker.sh
        sudo sh get-docker.sh
        rm get-docker.sh
        sudo usermod -a -G docker $USER
        
    elif [[ "$OS" == *"CentOS"* ]] || [[ "$OS" == *"Red Hat"* ]]; then
        sudo yum install -y yum-utils
        sudo yum-config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
        sudo yum install -y docker-ce docker-ce-cli containerd.io
        sudo systemctl enable docker
        sudo systemctl start docker
        sudo usermod -a -G docker $USER
        
    elif [[ "$OS" == "macOS" ]]; then
        print_warning "Please install Docker Desktop for Mac manually from: https://www.docker.com/products/docker-desktop"
        print_status "Or use Homebrew: brew install --cask docker"
        return
    fi
    
    print_success "Docker installed. You may need to log out and back in to use Docker without sudo."
}

# Install development tools
install_dev_tools() {
    print_header "Installing development tools..."
    
    if [[ "$OS" == *"Amazon Linux"* ]] || [[ "$OS" == *"CentOS"* ]] || [[ "$OS" == *"Red Hat"* ]]; then
        # Install essential development tools
        sudo yum groupinstall -y "Development Tools"
        sudo yum install -y wget curl unzip zip tree vim htop tmux jq make
        
    elif [[ "$OS" == *"Ubuntu"* ]] || [[ "$OS" == *"Debian"* ]]; then
        sudo apt-get install -y build-essential wget curl unzip zip tree vim htop tmux jq make
        
    elif [[ "$OS" == "macOS" ]]; then
        if command -v brew &> /dev/null; then
            brew install wget curl tree vim htop tmux jq make
        else
            print_status "Consider installing Homebrew for additional tools: /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
        fi
    fi
    
    print_success "Development tools installed"
}

# Install AWS CLI
install_aws_cli() {
    print_header "Installing AWS CLI..."
    
    if command -v aws &> /dev/null; then
        print_success "AWS CLI is already installed: $(aws --version)"
        return
    fi
    
    print_status "Installing AWS CLI v2..."
    
    if [[ "$OS" == "macOS" ]]; then
        if [[ "$(uname -m)" == "arm64" ]]; then
            curl "https://awscli.amazonaws.com/AWSCLIV2.pkg" -o "AWSCLIV2.pkg"
            sudo installer -pkg AWSCLIV2.pkg -target /
            rm AWSCLIV2.pkg
        else
            curl "https://awscli.amazonaws.com/AWSCLIV2.pkg" -o "AWSCLIV2.pkg"
            sudo installer -pkg AWSCLIV2.pkg -target /
            rm AWSCLIV2.pkg
        fi
    else
        # Linux
        curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
        unzip awscliv2.zip
        sudo ./aws/install
        rm -rf awscliv2.zip aws/
    fi
    
    if command -v aws &> /dev/null; then
        print_success "AWS CLI installed: $(aws --version)"
    else
        print_error "AWS CLI installation failed"
    fi
}

# Setup development environment
setup_dev_environment() {
    print_header "Setting up development environment..."
    
    # Create common development directories
    mkdir -p ~/source/repos
    mkdir -p ~/.local/bin
    
    # Setup common aliases
    setup_shell_config
    
    print_success "Development environment configured"
}

# Setup shell configuration and aliases
setup_shell_config() {
    print_status "Setting up shell configuration..."
    
    SHELL_RC=""
    if [[ "$SHELL" == *"zsh"* ]]; then
        SHELL_RC="$HOME/.zshrc"
    else
        SHELL_RC="$HOME/.bashrc"
    fi
    
    # Create shell config if it doesn't exist
    touch "$SHELL_RC"
    
    # Add .NET and development aliases
    if ! grep -q "# .NET Development Aliases" "$SHELL_RC"; then
        cat >> "$SHELL_RC" << 'EOF'

# .NET Development Aliases
alias dotnet-info='dotnet --info'
alias dotnet-sdks='dotnet --list-sdks'
alias dotnet-runtimes='dotnet --list-runtimes'
alias build='dotnet build'
alias test='dotnet test'
alias run='dotnet run'
alias clean='dotnet clean'
alias restore='dotnet restore'

# Common development aliases
alias cls='clear'
alias c='clear'
alias ll='ls -alF'
alias la='ls -A'
alias l='ls -CF'
alias h='history'
alias repos='cd ~/source/repos'
alias tree1='tree -a -L 1'
alias tree2='tree -a -L 2'
alias ..='cd ..'
alias ...='cd ../..'

# Docker aliases
alias dps='docker ps'
alias dpsa='docker ps -a'
alias di='docker images'
alias dstop='docker stop $(docker ps -q)'
alias drm='docker rm $(docker ps -aq)'

# Git aliases
alias gs='git status'
alias ga='git add'
alias gc='git commit'
alias gp='git push'
alias gl='git pull'
alias gb='git branch'
alias gco='git checkout'

EOF
        print_success "Development aliases added to $SHELL_RC"
    else
        print_success "Development aliases already configured"
    fi
    
    # Setup command completions
    if ! grep -q "# Command completions" "$SHELL_RC"; then
        cat >> "$SHELL_RC" << 'EOF'

# Command completions
if command -v aws &> /dev/null; then
    complete -C aws_completer aws
fi

# .NET completions (if dotnet-suggest is installed)
if command -v dotnet-suggest &> /dev/null; then
    _dotnet_complete() {
        local word=${COMP_WORDS[COMP_CWORD]}
        local completions="$(dotnet complete "${COMP_WORDS[@]}")"
        COMPREPLY=( $(compgen -W "$completions" -- "$word") )
    }
    complete -f -F _dotnet_complete dotnet
fi

EOF
        print_success "Command completions configured"
    fi
}

# Create project template
create_project_template() {
    print_header "Creating project template..."
    
    TEMPLATE_DIR="$HOME/source/repos/dotnet-clean-architecture-template"
    
    if [[ -d "$TEMPLATE_DIR" ]]; then
        print_success "Project template directory already exists: $TEMPLATE_DIR"
        return
    fi
    
    mkdir -p "$TEMPLATE_DIR"
    cd "$TEMPLATE_DIR"
    
    # Create basic Clean Architecture structure
    dotnet new sln -n CleanArchitecture
    
    # Create projects
    mkdir -p src/{Domain,Application,Infrastructure,WebApi}
    mkdir -p tests/{Domain.UnitTests,Application.UnitTests}
    
    # Domain project
    cd src/Domain
    dotnet new classlib -n Domain
    cd ../..
    
    # Application project  
    cd src/Application
    dotnet new classlib -n Application
    cd ../..
    
    # Infrastructure project
    cd src/Infrastructure
    dotnet new classlib -n Infrastructure
    cd ../..
    
    # WebApi project
    cd src/WebApi
    dotnet new webapi -n WebApi
    cd ../..
    
    # Test projects
    cd tests/Domain.UnitTests
    dotnet new xunit -n Domain.UnitTests
    cd ../..
    
    cd tests/Application.UnitTests
    dotnet new xunit -n Application.UnitTests
    cd ../..
    
    # Add projects to solution
    dotnet sln add src/Domain/Domain.csproj
    dotnet sln add src/Application/Application.csproj
    dotnet sln add src/Infrastructure/Infrastructure.csproj
    dotnet sln add src/WebApi/WebApi.csproj
    dotnet sln add tests/Domain.UnitTests/Domain.UnitTests.csproj
    dotnet sln add tests/Application.UnitTests/Application.UnitTests.csproj
    
    # Add project references
    dotnet add src/Application/Application.csproj reference src/Domain/Domain.csproj
    dotnet add src/Infrastructure/Infrastructure.csproj reference src/Domain/Domain.csproj
    dotnet add src/Infrastructure/Infrastructure.csproj reference src/Application/Application.csproj
    dotnet add src/WebApi/WebApi.csproj reference src/Application/Application.csproj
    dotnet add src/WebApi/WebApi.csproj reference src/Infrastructure/Infrastructure.csproj
    dotnet add tests/Domain.UnitTests/Domain.UnitTests.csproj reference src/Domain/Domain.csproj
    dotnet add tests/Application.UnitTests/Application.UnitTests.csproj reference src/Application/Application.csproj
    
    # Build the template
    dotnet restore
    dotnet build
    
    print_success "Project template created at: $TEMPLATE_DIR"
}

# Verify installation
verify_installation() {
    print_header "Verifying installation..."
    
    echo ""
    print_status "Environment Check:"
    echo "Operating System: $OS $VER"
    
    # Check .NET
    if command -v dotnet &> /dev/null; then
        echo "✅ .NET SDK: $(dotnet --version)"
        echo "   Available SDKs:"
        dotnet --list-sdks | head -3 | sed 's/^/     /'
    else
        echo "❌ .NET SDK: Not found"
    fi
    
    # Check Git
    if command -v git &> /dev/null; then
        echo "✅ Git: $(git --version | cut -d' ' -f3)"
    else
        echo "❌ Git: Not found"
    fi
    
    # Check Docker
    if command -v docker &> /dev/null; then
        echo "✅ Docker: $(docker --version | cut -d' ' -f3 | sed 's/,$//')"
    else
        echo "⚠️  Docker: Not found (optional)"
    fi
    
    # Check AWS CLI
    if command -v aws &> /dev/null; then
        echo "✅ AWS CLI: $(aws --version | cut -d' ' -f1 | cut -d'/' -f2)"
    else
        echo "⚠️  AWS CLI: Not found (optional)"
    fi
    
    # Check Make
    if command -v make &> /dev/null; then
        echo "✅ Make: $(make --version | head -1 | cut -d' ' -f3)"
    else
        echo "❌ Make: Not found"
    fi
    
    echo ""
}

# Display next steps
show_next_steps() {
    echo ""
    echo "============================================"
    print_success "🎉 Setup completed successfully!"
    echo "============================================"
    echo ""
    echo -e "${BLUE}Next Steps:${NC}"
    echo ""
    echo "1. Reload your shell to use new aliases:"
    if [[ "$SHELL" == *"zsh"* ]]; then
        echo -e "   ${YELLOW}source ~/.zshrc${NC}"
    else
        echo -e "   ${YELLOW}source ~/.bashrc${NC}"
    fi
    echo ""
    echo "2. Verify your .NET installation:"
    echo -e "   ${YELLOW}dotnet --version    # Should show 8.x.x${NC}"
    echo -e "   ${YELLOW}dotnet --info       # Show detailed info${NC}"
    echo ""
    echo "3. Test the environment:"
    echo -e "   ${YELLOW}cd ~/source/repos${NC}"
    echo -e "   ${YELLOW}dotnet new webapi -n TestApi${NC}"
    echo -e "   ${YELLOW}cd TestApi && dotnet run${NC}"
    echo ""
    echo "4. Available Make commands (if using the Clean Architecture project):"
    echo -e "   ${YELLOW}make help           # Show all available commands${NC}"
    echo -e "   ${YELLOW}make build          # Build the solution${NC}"
    echo -e "   ${YELLOW}make test           # Run tests${NC}"
    echo -e "   ${YELLOW}make run            # Run the API${NC}"
    echo ""
    echo "5. Clone the Clean Architecture project:"
    echo -e "   ${YELLOW}git clone <repository-url> ~/source/repos/clean-dotnet${NC}"
    echo -e "   ${YELLOW}cd ~/source/repos/clean-dotnet${NC}"
    echo -e "   ${YELLOW}make restore && make build && make run${NC}"
    echo ""
    if [[ "$OS" != "macOS" ]] && command -v docker &> /dev/null; then
        echo "⚠️  Note: You may need to log out and back in to use Docker without sudo."
        echo ""
    fi
    print_success "🚀 Happy coding with .NET 8 and Clean Architecture!"
}

# Main execution
main() {
    print_status "Starting Clean Architecture .NET 8 environment setup..."
    
    detect_os
    update_system
    install_dotnet8
    install_git
    install_docker
    install_dev_tools
    install_aws_cli
    setup_dev_environment
    verify_installation
    show_next_steps
}

# Run main function
main "$@"