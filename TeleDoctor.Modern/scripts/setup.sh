#!/bin/bash

# TeleDoctor Modern - Setup Script
# This script sets up the development environment for TeleDoctor Modern

set -e

echo "🏥 TeleDoctor Modern - Setup Script"
echo "=================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}$1${NC}"
}

# Check if running on supported OS
check_os() {
    print_header "🔍 Checking Operating System..."
    
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        print_status "Linux detected"
        OS="linux"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        print_status "macOS detected"
        OS="macos"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        print_status "Windows detected"
        OS="windows"
    else
        print_error "Unsupported operating system: $OSTYPE"
        exit 1
    fi
}

# Check prerequisites
check_prerequisites() {
    print_header "📋 Checking Prerequisites..."
    
    # Check .NET 8
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        if [[ $DOTNET_VERSION == 8.* ]]; then
            print_status ".NET 8 SDK found: $DOTNET_VERSION"
        else
            print_warning ".NET version $DOTNET_VERSION found, but .NET 8 is required"
            install_dotnet
        fi
    else
        print_warning ".NET SDK not found"
        install_dotnet
    fi
    
    # Check Docker
    if command -v docker &> /dev/null; then
        print_status "Docker found: $(docker --version)"
    else
        print_warning "Docker not found. Please install Docker Desktop"
        print_status "Download from: https://www.docker.com/products/docker-desktop"
    fi
    
    # Check Docker Compose
    if command -v docker-compose &> /dev/null; then
        print_status "Docker Compose found: $(docker-compose --version)"
    else
        print_warning "Docker Compose not found"
    fi
    
    # Check Git
    if command -v git &> /dev/null; then
        print_status "Git found: $(git --version)"
    else
        print_error "Git is required but not found. Please install Git"
        exit 1
    fi
}

# Install .NET 8
install_dotnet() {
    print_header "📦 Installing .NET 8 SDK..."
    
    if [[ "$OS" == "linux" ]]; then
        # Ubuntu/Debian
        if command -v apt-get &> /dev/null; then
            wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-8.0
        # CentOS/RHEL/Fedora
        elif command -v yum &> /dev/null; then
            sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
            sudo yum install -y dotnet-sdk-8.0
        else
            print_error "Unsupported Linux distribution. Please install .NET 8 manually"
            print_status "Visit: https://dotnet.microsoft.com/download/dotnet/8.0"
            exit 1
        fi
    elif [[ "$OS" == "macos" ]]; then
        if command -v brew &> /dev/null; then
            brew install --cask dotnet-sdk
        else
            print_error "Homebrew not found. Please install .NET 8 manually"
            print_status "Visit: https://dotnet.microsoft.com/download/dotnet/8.0"
            exit 1
        fi
    else
        print_error "Please install .NET 8 SDK manually"
        print_status "Visit: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
}

# Setup development certificates
setup_dev_certs() {
    print_header "🔐 Setting up Development Certificates..."
    
    dotnet dev-certs https --clean
    dotnet dev-certs https --trust
    
    print_status "Development certificates configured"
}

# Restore NuGet packages
restore_packages() {
    print_header "📦 Restoring NuGet Packages..."
    
    dotnet restore TeleDoctor.Modern.sln
    
    print_status "NuGet packages restored"
}

# Setup database
setup_database() {
    print_header "🗄️ Setting up Database..."
    
    # Check if SQL Server is running in Docker
    if docker ps | grep -q "teledoctor-sqlserver"; then
        print_status "SQL Server container already running"
    else
        print_status "Starting SQL Server container..."
        docker-compose up -d sqlserver
        
        # Wait for SQL Server to be ready
        print_status "Waiting for SQL Server to be ready..."
        sleep 30
    fi
    
    # Run database migrations
    print_status "Running database migrations..."
    cd src/TeleDoctor.WebAPI
    dotnet ef database update
    cd ../..
    
    print_status "Database setup completed"
}

# Setup configuration files
setup_configuration() {
    print_header "⚙️ Setting up Configuration..."
    
    # Copy appsettings template if it doesn't exist
    if [ ! -f "src/TeleDoctor.WebAPI/appsettings.Development.json" ]; then
        cp "src/TeleDoctor.WebAPI/appsettings.json" "src/TeleDoctor.WebAPI/appsettings.Development.json"
        print_status "Created appsettings.Development.json"
    fi
    
    # Create user secrets
    cd src/TeleDoctor.WebAPI
    dotnet user-secrets init
    
    print_warning "Please configure your Azure OpenAI credentials:"
    print_status "dotnet user-secrets set \"AI:AzureOpenAI:Endpoint\" \"https://your-openai-resource.openai.azure.com/\""
    print_status "dotnet user-secrets set \"AI:AzureOpenAI:ApiKey\" \"your-azure-openai-api-key\""
    
    cd ../..
    
    print_status "Configuration setup completed"
}

# Build the solution
build_solution() {
    print_header "🔨 Building Solution..."
    
    dotnet build TeleDoctor.Modern.sln --configuration Debug
    
    if [ $? -eq 0 ]; then
        print_status "Build completed successfully"
    else
        print_error "Build failed"
        exit 1
    fi
}

# Run tests
run_tests() {
    print_header "🧪 Running Tests..."
    
    # Create test projects if they don't exist
    if [ ! -d "tests" ]; then
        mkdir tests
        cd tests
        
        dotnet new xunit -n TeleDoctor.Tests.Unit
        dotnet new xunit -n TeleDoctor.Tests.Integration
        dotnet new xunit -n TeleDoctor.AI.Tests
        
        cd ..
        
        # Add test projects to solution
        dotnet sln add tests/TeleDoctor.Tests.Unit/TeleDoctor.Tests.Unit.csproj
        dotnet sln add tests/TeleDoctor.Tests.Integration/TeleDoctor.Tests.Integration.csproj
        dotnet sln add tests/TeleDoctor.AI.Tests/TeleDoctor.AI.Tests.csproj
    fi
    
    dotnet test --configuration Debug --logger "console;verbosity=normal"
    
    print_status "Tests completed"
}

# Setup Docker environment
setup_docker() {
    print_header "🐳 Setting up Docker Environment..."
    
    # Build Docker images
    docker-compose build
    
    # Start services
    docker-compose up -d
    
    print_status "Docker environment setup completed"
    print_status "Services available at:"
    print_status "  - API: http://localhost:8080"
    print_status "  - Blazor UI: http://localhost:7000"
    print_status "  - SQL Server: localhost:1433"
    print_status "  - Redis: localhost:6379"
}

# Create development scripts
create_dev_scripts() {
    print_header "📝 Creating Development Scripts..."
    
    # Create run-api.sh
    cat > scripts/run-api.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting TeleDoctor API..."
cd src/TeleDoctor.WebAPI
dotnet run --configuration Debug
EOF
    
    # Create run-blazor.sh
    cat > scripts/run-blazor.sh << 'EOF'
#!/bin/bash
echo "🌐 Starting TeleDoctor Blazor UI..."
cd src/TeleDoctor.BlazorUI
dotnet run --configuration Debug
EOF
    
    # Create run-tests.sh
    cat > scripts/run-tests.sh << 'EOF'
#!/bin/bash
echo "🧪 Running TeleDoctor Tests..."
dotnet test --configuration Debug --logger "console;verbosity=normal"
EOF
    
    # Make scripts executable
    chmod +x scripts/*.sh
    
    print_status "Development scripts created in scripts/ directory"
}

# Main setup function
main() {
    print_header "🏥 TeleDoctor Modern - Development Environment Setup"
    echo ""
    
    check_os
    check_prerequisites
    setup_dev_certs
    restore_packages
    setup_configuration
    build_solution
    create_dev_scripts
    
    print_header "✅ Setup Completed Successfully!"
    echo ""
    print_status "Next steps:"
    print_status "1. Configure your Azure OpenAI credentials in user secrets"
    print_status "2. Run 'docker-compose up -d' to start the full environment"
    print_status "3. Or run individual services:"
    print_status "   - API: ./scripts/run-api.sh"
    print_status "   - Blazor UI: ./scripts/run-blazor.sh"
    print_status "   - Tests: ./scripts/run-tests.sh"
    echo ""
    print_status "Documentation: README.md"
    print_status "API Documentation: http://localhost:8080 (when running)"
    echo ""
    print_header "🎉 Happy Coding!"
}

# Run main function
main "$@"
