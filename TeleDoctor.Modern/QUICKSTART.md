# TeleDoctor Modern - Quick Start Guide

## Current Setup Status

**Database**: SQL Server running in Docker on port 14330  
**Cache**: Redis running in Docker on port 63790  
**API**: Ready to start  
**Status**: Dependencies installed, services running

## Prerequisites Installed

- Docker and Docker Compose (running)
- .NET 8 SDK installed
- SQL Server container running
- Redis container running

## Running the Application

### Option 1: Without AI Features (Quick Test)

The application can run without Azure OpenAI configured. AI features will be disabled but the core healthcare features will work.

```bash
# Navigate to the project
cd /home/saidul/Desktop/Portfolio_Project/Tele-Doctor/TeleDoctor.Modern

# Build the solution
dotnet build src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj

# Run the API
dotnet run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj
```

The API will start on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger: https://localhost:5001/swagger

### Option 2: With Full AI Features

To enable AI features, you need Azure OpenAI access:

```bash
# Configure Azure OpenAI credentials
cd src/TeleDoctor.WebAPI
dotnet user-secrets init
dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AI:AzureOpenAI:ChatDeploymentName" "gpt-4"

# Go back to root
cd ../..

# Run the application
dotnet run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj
```

## Database Setup

The application will automatically:
1. Create the database on first run
2. Run migrations
3. Seed initial data (roles, departments, sample users)

**Default Sample Users:**
- Patient: `patient@test.com` / `Patient123!`
- Doctor: `doctor@test.com` / `Doctor123!`

## Testing the API

### Health Check
```bash
curl http://localhost:5000/health
```

### Get Appointments (requires authentication)
```bash
# First, login to get a token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"doctor@test.com","password":"Doctor123!"}'

# Use the token to get appointments
curl -X GET http://localhost:5000/api/appointments \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Swagger UI (Easiest for testing)
Open in browser: https://localhost:5001/swagger

You can test all endpoints interactively through Swagger UI.

## Services Running

Check Docker services status:
```bash
docker ps --filter "name=teledoctor"
```

You should see:
- `teledoctor-sqlserver` (port 14330)
- `teledoctor-redis` (port 63790)

## Stopping the Application

### Stop API
Press `Ctrl+C` in the terminal where the API is running

### Stop Docker Services
```bash
cd /home/saidul/Desktop/Portfolio_Project/Tele-Doctor/TeleDoctor.Modern
docker compose -f docker-compose.dev.yml down
```

### Stop and Remove Volumes (Clean Reset)
```bash
docker compose -f docker-compose.dev.yml down -v
```

## Troubleshooting

### Port Already in Use
If you see "address already in use" errors:
```bash
# Check what's using the ports
sudo lsof -i :14330
sudo lsof -i :63790

# Or use different ports in docker-compose.dev.yml
```

### Database Connection Issues
```bash
# Test SQL Server connection
docker exec -it teledoctor-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "TeleDoctor2024!" -Q "SELECT 1"
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Next Steps

1. Configure Azure OpenAI for AI features (optional)
2. Explore the Swagger UI documentation
3. Test API endpoints
4. Review the comprehensive documentation in README.md
5. Deploy to production using DEPLOYMENT.md guide

## Quick Commands Reference

```bash
# Start Docker services
docker compose -f docker-compose.dev.yml up -d

# Build project
dotnet build

# Run API
dotnet run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj

# Run tests (when test projects are added)
dotnet test

# Stop Docker services
docker compose -f docker-compose.dev.yml down
```

---

**Note**: AI features require Azure OpenAI credentials. The application will run without them but AI endpoints will return errors. For development and testing, you can focus on the core healthcare features first.
