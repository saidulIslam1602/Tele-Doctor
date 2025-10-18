# 🚀 TeleDoctor Modern - Deployment Guide

This guide covers deployment options for TeleDoctor Modern, from local development to production Azure deployment.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Local Development](#local-development)
- [Docker Deployment](#docker-deployment)
- [Azure Cloud Deployment](#azure-cloud-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [CI/CD Pipeline](#cicd-pipeline)
- [Monitoring & Observability](#monitoring--observability)
- [Security Configuration](#security-configuration)
- [Troubleshooting](#troubleshooting)

## 🛠️ Prerequisites

### Development Environment
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code
- SQL Server (local or Docker)
- Azure CLI (for cloud deployment)

### Azure Resources (for production)
- Azure OpenAI Service
- Azure SQL Database
- Azure Container Registry
- Azure Container Apps or AKS
- Azure Key Vault
- Application Insights

## 💻 Local Development

### 1. Quick Start
```bash
# Clone the repository
git clone https://github.com/yourusername/teledoctor-modern.git
cd teledoctor-modern

# Run setup script
./scripts/setup.sh

# Start development environment
docker-compose up -d
```

### 2. Manual Setup
```bash
# Restore packages
dotnet restore

# Setup user secrets
cd src/TeleDoctor.WebAPI
dotnet user-secrets init
dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-openai-resource.openai.azure.com/"
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-azure-openai-api-key"

# Run database migrations
dotnet ef database update

# Start the API
dotnet run
```

### 3. Development URLs
- **API**: https://localhost:7001
- **Swagger**: https://localhost:7001/swagger
- **Blazor UI**: https://localhost:7000

## 🐳 Docker Deployment

### 1. Build and Run with Docker Compose
```bash
# Build all services
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### 2. Individual Container Deployment
```bash
# Build API image
docker build -t teledoctor-modern-api .

# Run API container
docker run -d \
  --name teledoctor-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=TeleDoctorModernDB;User Id=sa;Password=TeleDoctor2024!;TrustServerCertificate=true" \
  teledoctor-modern-api
```

### 3. Docker Compose Services
```yaml
# docker-compose.yml includes:
- teledoctor-api      # Main API service
- teledoctor-blazor   # Blazor WebAssembly UI
- sqlserver          # SQL Server database
- redis              # Redis cache
- nginx              # Reverse proxy
- elasticsearch      # Logging
- kibana             # Log visualization
```

## ☁️ Azure Cloud Deployment

### 1. Azure Resources Setup
```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-teledoctor-prod --location "Norway East"

# Create Azure Container Registry
az acr create --resource-group rg-teledoctor-prod --name teledoctorregistry --sku Basic

# Create Azure SQL Database
az sql server create --name teledoctor-sql-server --resource-group rg-teledoctor-prod --location "Norway East" --admin-user teledoctoradmin --admin-password "TeleDoctor2024!"
az sql db create --resource-group rg-teledoctor-prod --server teledoctor-sql-server --name TeleDoctorModernDB --service-objective Basic

# Create Azure OpenAI Service
az cognitiveservices account create --name teledoctor-openai --resource-group rg-teledoctor-prod --kind OpenAI --sku S0 --location "Norway East"
```

### 2. Build and Push Docker Images
```bash
# Login to ACR
az acr login --name teledoctorregistry

# Build and push API image
docker build -t teledoctorregistry.azurecr.io/teledoctor-api:latest .
docker push teledoctorregistry.azurecr.io/teledoctor-api:latest

# Build and push Blazor image
docker build -f src/TeleDoctor.BlazorUI/Dockerfile -t teledoctorregistry.azurecr.io/teledoctor-blazor:latest .
docker push teledoctorregistry.azurecr.io/teledoctor-blazor:latest
```

### 3. Azure Container Apps Deployment
```bash
# Create Container Apps environment
az containerapp env create --name teledoctor-env --resource-group rg-teledoctor-prod --location "Norway East"

# Deploy API container app
az containerapp create \
  --name teledoctor-api \
  --resource-group rg-teledoctor-prod \
  --environment teledoctor-env \
  --image teledoctorregistry.azurecr.io/teledoctor-api:latest \
  --target-port 8080 \
  --ingress 'external' \
  --registry-server teledoctorregistry.azurecr.io \
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 10

# Deploy Blazor container app
az containerapp create \
  --name teledoctor-blazor \
  --resource-group rg-teledoctor-prod \
  --environment teledoctor-env \
  --image teledoctorregistry.azurecr.io/teledoctor-blazor:latest \
  --target-port 7000 \
  --ingress 'external' \
  --registry-server teledoctorregistry.azurecr.io \
  --cpu 0.5 \
  --memory 1.0Gi \
  --min-replicas 1 \
  --max-replicas 5
```

### 4. Azure Key Vault Configuration
```bash
# Create Key Vault
az keyvault create --name teledoctor-keyvault --resource-group rg-teledoctor-prod --location "Norway East"

# Add secrets
az keyvault secret set --vault-name teledoctor-keyvault --name "AzureOpenAI-ApiKey" --value "your-openai-api-key"
az keyvault secret set --vault-name teledoctor-keyvault --name "ConnectionStrings-DefaultConnection" --value "your-connection-string"
az keyvault secret set --vault-name teledoctor-keyvault --name "JwtSettings-SecretKey" --value "your-jwt-secret-key"
```

## ⚓ Kubernetes Deployment

### 1. AKS Cluster Setup
```bash
# Create AKS cluster
az aks create \
  --resource-group rg-teledoctor-prod \
  --name teledoctor-aks \
  --node-count 3 \
  --node-vm-size Standard_D2s_v3 \
  --location "Norway East" \
  --attach-acr teledoctorregistry \
  --enable-managed-identity

# Get AKS credentials
az aks get-credentials --resource-group rg-teledoctor-prod --name teledoctor-aks
```

### 2. Kubernetes Manifests
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: teledoctor

---
# k8s/api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: teledoctor-api
  namespace: teledoctor
spec:
  replicas: 3
  selector:
    matchLabels:
      app: teledoctor-api
  template:
    metadata:
      labels:
        app: teledoctor-api
    spec:
      containers:
      - name: api
        image: teledoctorregistry.azurecr.io/teledoctor-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: teledoctor-secrets
              key: connection-string
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5

---
# k8s/api-service.yaml
apiVersion: v1
kind: Service
metadata:
  name: teledoctor-api-service
  namespace: teledoctor
spec:
  selector:
    app: teledoctor-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: ClusterIP

---
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: teledoctor-ingress
  namespace: teledoctor
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  tls:
  - hosts:
    - api.teledoctor.no
    - app.teledoctor.no
    secretName: teledoctor-tls
  rules:
  - host: api.teledoctor.no
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: teledoctor-api-service
            port:
              number: 80
  - host: app.teledoctor.no
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: teledoctor-blazor-service
            port:
              number: 80
```

### 3. Deploy to Kubernetes
```bash
# Apply manifests
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n teledoctor
kubectl get services -n teledoctor
kubectl get ingress -n teledoctor

# View logs
kubectl logs -f deployment/teledoctor-api -n teledoctor
```

## 🔄 CI/CD Pipeline

### 1. GitHub Actions Workflow
```yaml
# .github/workflows/deploy.yml
name: Deploy TeleDoctor Modern

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal

  build-and-deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Login to Azure Container Registry
      uses: azure/docker-login@v1
      with:
        login-server: teledoctorregistry.azurecr.io
        username: ${{ secrets.ACR_USERNAME }}
        password: ${{ secrets.ACR_PASSWORD }}
    
    - name: Build and push Docker image
      run: |
        docker build -t teledoctorregistry.azurecr.io/teledoctor-api:${{ github.sha }} .
        docker push teledoctorregistry.azurecr.io/teledoctor-api:${{ github.sha }}
    
    - name: Deploy to Azure Container Apps
      uses: azure/CLI@v1
      with:
        azcliversion: 2.30.0
        inlineScript: |
          az containerapp update \
            --name teledoctor-api \
            --resource-group rg-teledoctor-prod \
            --image teledoctorregistry.azurecr.io/teledoctor-api:${{ github.sha }}
```

### 2. Azure DevOps Pipeline
```yaml
# azure-pipelines.yml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  containerRegistry: 'teledoctorregistry.azurecr.io'
  imageRepository: 'teledoctor-api'
  dockerfilePath: '**/Dockerfile'

stages:
- stage: Build
  displayName: Build and Test
  jobs:
  - job: Build
    displayName: Build
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'

    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'

    - task: DotNetCoreCLI@2
      displayName: 'Run tests'
      inputs:
        command: 'test'
        projects: '**/*Tests/*.csproj'
        arguments: '--configuration $(buildConfiguration) --collect "Code coverage"'

- stage: Deploy
  displayName: Deploy to Production
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: Deploy
    displayName: Deploy
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: Docker@2
            displayName: 'Build and push image'
            inputs:
              containerRegistry: 'teledoctor-acr'
              repository: $(imageRepository)
              command: 'buildAndPush'
              Dockerfile: $(dockerfilePath)
              tags: |
                $(Build.BuildId)
                latest

          - task: AzureContainerApps@1
            displayName: 'Deploy to Container Apps'
            inputs:
              azureSubscription: 'teledoctor-subscription'
              containerAppName: 'teledoctor-api'
              resourceGroup: 'rg-teledoctor-prod'
              imageToDeploy: '$(containerRegistry)/$(imageRepository):$(Build.BuildId)'
```

## 📊 Monitoring & Observability

### 1. Application Insights Setup
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
});

// Custom telemetry
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

### 2. Health Checks Configuration
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TeleDoctorDbContext>()
    .AddCheck("AI Services", () => HealthCheckResult.Healthy("AI services are running"))
    .AddCheck("Norwegian Integration", () => HealthCheckResult.Healthy("Norwegian services are running"));

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 3. Structured Logging with Serilog
```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TeleDoctor.Modern")
    .WriteTo.Console()
    .WriteTo.File("logs/teledoctor-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
    .CreateLogger();
```

### 4. Prometheus Metrics (Optional)
```bash
# Add to docker-compose.yml
prometheus:
  image: prom/prometheus:latest
  container_name: teledoctor-prometheus
  ports:
    - "9090:9090"
  volumes:
    - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml

grafana:
  image: grafana/grafana:latest
  container_name: teledoctor-grafana
  ports:
    - "3000:3000"
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
```

## 🔒 Security Configuration

### 1. Production Security Settings
```json
{
  "JwtSettings": {
    "SecretKey": "Use Azure Key Vault in production",
    "Issuer": "https://api.teledoctor.no",
    "Audience": "https://app.teledoctor.no",
    "ExpiryInMinutes": 60
  },
  "Security": {
    "RequireHttps": true,
    "EnableCors": true,
    "AllowedOrigins": ["https://app.teledoctor.no"],
    "EnableRateLimiting": true,
    "MaxRequestsPerMinute": 100
  }
}
```

### 2. HTTPS Configuration
```bash
# Generate SSL certificate for production
certbot certonly --webroot -w /var/www/html -d api.teledoctor.no -d app.teledoctor.no

# Or use Azure Application Gateway with SSL termination
```

### 3. Network Security
```yaml
# k8s/network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: teledoctor-network-policy
  namespace: teledoctor
spec:
  podSelector:
    matchLabels:
      app: teledoctor-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
```

## 🔧 Troubleshooting

### Common Issues

#### 1. Database Connection Issues
```bash
# Check SQL Server container
docker logs teledoctor-sqlserver

# Test connection
docker exec -it teledoctor-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "TeleDoctor2024!" -Q "SELECT 1"

# Reset database
docker-compose down -v
docker-compose up -d sqlserver
```

#### 2. AI Service Issues
```bash
# Check API logs
docker logs teledoctor-api

# Verify Azure OpenAI configuration
curl -H "Authorization: Bearer YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     "https://your-openai-resource.openai.azure.com/openai/deployments?api-version=2023-05-15"
```

#### 3. Container Issues
```bash
# Check container status
docker ps -a

# View container logs
docker logs teledoctor-api

# Restart containers
docker-compose restart

# Rebuild containers
docker-compose build --no-cache
docker-compose up -d
```

#### 4. Kubernetes Issues
```bash
# Check pod status
kubectl get pods -n teledoctor

# Describe pod for events
kubectl describe pod <pod-name> -n teledoctor

# Check logs
kubectl logs <pod-name> -n teledoctor

# Port forward for debugging
kubectl port-forward svc/teledoctor-api-service 8080:80 -n teledoctor
```

### Performance Optimization

#### 1. Database Optimization
```sql
-- Add indexes for frequently queried columns
CREATE INDEX IX_Appointments_PatientId_ScheduledDateTime ON Appointments (PatientId, ScheduledDateTime);
CREATE INDEX IX_MedicalRecords_PatientId_RecordDate ON MedicalRecords (PatientId, RecordDate);
CREATE INDEX IX_Prescriptions_PatientId_IssuedDate ON Prescriptions (PatientId, IssuedDate);
```

#### 2. Caching Configuration
```csharp
// Add Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add memory caching
builder.Services.AddMemoryCache();
```

#### 3. API Rate Limiting
```csharp
// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});
```

## 📞 Support

For deployment issues or questions:
- Email: support@teledoctor.no
- Documentation: [README.md](README.md)
- Issues: GitHub Issues

---

**Happy Deploying! 🚀**
