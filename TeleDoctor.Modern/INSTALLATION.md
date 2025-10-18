# TeleDoctor Modern - Installation Guide

## System Requirements

### Operating System
- Linux (Ubuntu 22.04 or later)
- macOS (10.15 or later)
- Windows 10/11 or Windows Server 2019+

### Software Prerequisites

**Required:**
- .NET 8 SDK and Runtime
- Docker Desktop (or Docker Engine + Docker Compose)
- Git

**Optional (for AI features):**
- Azure subscription with OpenAI access
- Azure CLI (for cloud deployment)

## Installing .NET 8

### Ubuntu/Debian Linux

```bash
# Download Microsoft package repository configuration
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

# Install the repository configuration
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package lists
sudo apt-get update

# Install .NET SDK 8.0 (includes runtime)
sudo apt-get install -y dotnet-sdk-8.0

# Install .NET Runtime 8.0 (if SDK installation fails)
sudo apt-get install -y aspnetcore-runtime-8.0

# Verify installation
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
```

### macOS

```bash
# Using Homebrew
brew install --cask dotnet-sdk

# Or download installer from:
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### Windows

1. Download .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer
3. Restart your terminal/IDE
4. Verify: `dotnet --version`

## Installing Docker

### Ubuntu Linux

```bash
# Update package index
sudo apt-get update

# Install prerequisites
sudo apt-get install -y ca-certificates curl gnupg lsb-release

# Add Docker's official GPG key
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Set up the repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Add your user to docker group (optional, to run without sudo)
sudo usermod -aG docker $USER
newgrp docker

# Verify installation
docker --version
docker compose version
```

### macOS/Windows

Download and install Docker Desktop from: https://www.docker.com/products/docker-desktop

## Project Setup

### 1. Clone the Repository

```bash
git clone https://github.com/saidulIslam1602/Tele-Doctor.git
cd Tele-Doctor/TeleDoctor.Modern
```

### 2. Start Database and Cache Services

```bash
# Start SQL Server and Redis
docker compose -f docker-compose.dev.yml up -d

# Verify services are running
docker ps --filter "name=teledoctor"

# Wait for SQL Server to be ready (about 30 seconds)
sleep 30
```

### 3. Restore NuGet Packages

```bash
# Restore all project dependencies
dotnet restore TeleDoctor.Modern.sln
```

### 4. Configure Application Settings

**Option A: Using User Secrets (Recommended for development)**

```bash
cd src/TeleDoctor.WebAPI

# Initialize user secrets
dotnet user-secrets init

# For basic testing without AI (optional):
dotnet user-secrets set "AI:Medical:EnableClinicalDecisionSupport" "false"
dotnet user-secrets set "AI:Norwegian:EnableHelsenorgeIntegration" "false"

# For full AI features (requires Azure OpenAI):
dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AI:AzureOpenAI:ChatDeploymentName" "gpt-4"

cd ../..
```

**Option B: Using appsettings.Development.json**

Edit `src/TeleDoctor.WebAPI/appsettings.Development.json` and update the Azure OpenAI credentials.

### 5. Run Database Migrations

```bash
# Install EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# Run migrations
cd src/TeleDoctor.WebAPI
dotnet ef database update
cd ../..
```

### 6. Build the Solution

```bash
# Build all projects
dotnet build TeleDoctor.Modern.sln
```

### 7. Run the Application

```bash
# Run the WebAPI
dotnet run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj
```

The application will start and be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger
- Health Check: http://localhost:5000/health

## Verifying the Installation

### 1. Check Health Endpoint

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [...]
}
```

### 2. Access Swagger UI

Open in browser: https://localhost:5001/swagger

You should see all API endpoints documented with the ability to test them.

### 3. Check Docker Services

```bash
docker ps --filter "name=teledoctor"
```

Both `teledoctor-sqlserver` and `teledoctor-redis` should show as "healthy".

### 4. Check Database

```bash
# Connect to SQL Server
docker exec -it teledoctor-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "TeleDoctor2024!" \
  -Q "SELECT name FROM sys.databases WHERE name = 'TeleDoctorModernDB'"
```

## Troubleshooting

### Issue: .NET Runtime Not Found

```bash
# Install .NET 8 runtime
sudo apt-get install -y aspnetcore-runtime-8.0
```

### Issue: Docker Services Won't Start

```bash
# Check Docker logs
docker logs teledoctor-sqlserver
docker logs teledoctor-redis

# Restart Docker daemon
sudo systemctl restart docker

# Try starting services again
docker compose -f docker-compose.dev.yml up -d
```

### Issue: Port Conflicts

Edit `docker-compose.dev.yml` and change ports to unused ones.

### Issue: Build Errors

```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build --no-incremental
```

### Issue: Database Migration Errors

```bash
# Drop and recreate database
cd src/TeleDoctor.WebAPI
dotnet ef database drop --force
dotnet ef database update
```

## Development Workflow

### Running in Watch Mode

```bash
# API automatically restarts on code changes
dotnet watch run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj
```

### Viewing Logs

```bash
# Application logs are in: logs/teledoctor-*.txt
tail -f logs/teledoctor-$(date +%Y%m%d).txt
```

### Stopping All Services

```bash
# Stop API: Press Ctrl+C in the terminal

# Stop Docker services
docker compose -f docker-compose.dev.yml down
```

## Getting Azure OpenAI Access

If you want to enable full AI features:

1. **Azure Subscription**: Sign up at https://azure.microsoft.com
2. **Create Azure OpenAI Resource**:
   - Go to Azure Portal
   - Create "Azure OpenAI" resource
   - Choose region (Norway East recommended)
   - Deploy GPT-4 model
3. **Get Credentials**:
   - Copy endpoint URL
   - Copy API key
   - Note your deployment name
4. **Configure Application**: Use user secrets or appsettings

## Next Steps

1. **Explore the API**: Use Swagger UI to test endpoints
2. **Review Documentation**: Read README.md for detailed information
3. **Check Implementation**: See IMPLEMENTATION_COMPLETE.md
4. **Deploy**: Follow DEPLOYMENT.md for production deployment

## Support

For issues or questions:
- Check TROUBLESHOOTING section above
- Review TECHNICAL_OVERVIEW.md
- Check GitHub Issues
- Email: saidulislambinalisayed@outlook.com

---

**Installation Complete!** The application is ready to run.
