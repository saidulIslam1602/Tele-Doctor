# TeleDoctor Modern - AI-Powered Healthcare Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-green.svg)](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
[![Terraform](https://img.shields.io/badge/Terraform-1.6+-purple.svg)](https://www.terraform.io/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-1.28+-blue.svg)](https://kubernetes.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![Version](https://img.shields.io/badge/Version-2.0.0-brightgreen.svg)](../CHANGELOG.md)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Infrastructure](#infrastructure)
- [AI Features](#ai-features)
- [Healthcare Integration](#healthcare-integration)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Deployment](#deployment)
- [Testing](#testing)
- [API Documentation](#api-documentation)
- [Contributing](#contributing)
- [License](#license)

## Overview

TeleDoctor Modern is an enterprise-grade, AI-first telemedicine platform built with .NET 8 and Azure OpenAI. The platform demonstrates advanced software engineering practices including:

- Advanced generative AI integration for clinical workflows
- RAG (Retrieval-Augmented Generation) for medical knowledge queries
- Multi-agent orchestration systems for workflow automation
- Production-grade infrastructure automation with Terraform
- Comprehensive healthcare system integration
- Enterprise DevOps and SRE practices
- Production-ready deployment configurations

The platform is designed specifically for Norwegian healthcare requirements but can be adapted for other healthcare systems.

**Version**: 2.0.0  
**Infrastructure**: Production-ready with Terraform, Kubernetes, and comprehensive monitoring

## Architecture

### Clean Architecture Layers

The application follows clean architecture principles with clear separation of concerns:

```
TeleDoctor.Modern/
├── TeleDoctor.Core/                    # Domain Layer
│   ├── Entities/                       # Domain entities
│   ├── Enums/                          # Enumerations
│   ├── ValueObjects/                   # Value objects
│   └── Interfaces/                     # Domain interfaces
│
├── TeleDoctor.AI.Services/             # AI/ML Services Layer
│   ├── RAG/                            # Retrieval-Augmented Generation
│   ├── ModelEvaluation/                # AI model evaluation framework
│   ├── AgenticFlows/                   # Multi-agent orchestration
│   ├── DigitalLabor/                   # Automation agents
│   ├── Services/                       # AI service implementations
│   └── Configuration/                  # AI configuration
│
├── TeleDoctor.Norwegian.Integration/   # Healthcare Integration Layer
│   ├── Services/                       # Integration services
│   ├── Models/                         # Integration models
│   └── Configuration/                  # Integration configuration
│
├── TeleDoctor.Application/             # Application Layer
│   ├── Commands/                       # CQRS commands
│   ├── Queries/                        # CQRS queries
│   ├── DTOs/                           # Data transfer objects
│   ├── Services/                       # Application services
│   ├── Mappings/                       # AutoMapper profiles
│   └── Validators/                     # FluentValidation validators
│
├── TeleDoctor.Infrastructure/          # Infrastructure Layer
│   ├── Data/                           # EF Core DbContext
│   ├── Repositories/                   # Repository implementations
│   └── Services/                       # External service integrations
│
├── TeleDoctor.WebAPI/                  # Presentation Layer - API
│   ├── Controllers/                    # API controllers
│   ├── Hubs/                           # SignalR hubs
│   └── Middleware/                     # Custom middleware
│
└── TeleDoctor.BlazorUI/                # Presentation Layer - Web UI
    ├── Pages/                          # Blazor pages
    ├── Components/                     # Reusable components
    └── Services/                       # Frontend services
```

### Design Patterns

- **Clean Architecture**: Dependency inversion and separation of concerns
- **CQRS**: Command Query Responsibility Segregation with MediatR
- **Repository Pattern**: Data access abstraction
- **Unit of Work**: Transaction management
- **Dependency Injection**: Built-in .NET DI container

## Infrastructure

### Infrastructure as Code

Complete production-grade infrastructure implementation using Terraform and Ansible.

**Location**: `infrastructure/`

**Key Components**:
- Hub-spoke network architecture with Azure Firewall and VPN Gateway
- Azure Kubernetes Service (AKS) with multi-zone deployment
- Comprehensive monitoring with Prometheus and Grafana
- Zero Trust Network Access (ZTNA) implementation
- CI/CD pipeline with security scanning
- SRE practices with 99.9% availability SLO

**Documentation**:
- [Infrastructure Guide](infrastructure/README.md) - Complete infrastructure documentation
- [Network Architecture](infrastructure/NETWORK_ARCHITECTURE.md) - Network design and topology
- [SRE Practices](infrastructure/SRE_PRACTICES.md) - Operations and incident management
- [Quick Start](infrastructure/QUICK_START.md) - 25-minute deployment guide

**Terraform Modules**:
- **Networking**: Hub-spoke VNet, Azure Firewall, VPN Gateway, NSGs
- **AKS**: Kubernetes cluster with Azure CNI and Calico policies
- **Monitoring**: Log Analytics, Application Insights, Prometheus
- **SQL**: Azure SQL Database with private endpoints
- **Redis**: Redis Cache with zone redundancy
- **KeyVault**: Azure Key Vault for secrets management

**Technology Stack**:
- Terraform 1.6+ for infrastructure provisioning
- Ansible 2.15+ for configuration management
- GitHub Actions for CI/CD automation
- Azure as primary cloud platform

**Quick Deploy**:
```bash
cd infrastructure/terraform
terraform init
terraform plan -var-file="environments/production/terraform.tfvars"
terraform apply -var-file="environments/production/terraform.tfvars"
```

See [infrastructure/README.md](infrastructure/README.md) for complete deployment instructions.

## AI Features

### 1. Clinical Decision Support System

Provides AI-assisted clinical analysis with the following capabilities:

```csharp
/// <summary>
/// Analyzes patient symptoms and provides clinical recommendations
/// </summary>
/// <param name="request">Clinical analysis request containing symptoms and patient history</param>
/// <returns>Clinical analysis response with differential diagnoses and recommendations</returns>
public async Task<ClinicalAnalysisResponse> AnalyzeSymptomsAsync(ClinicalAnalysisRequest request)
{
    // Generate AI-powered symptom analysis
    // Returns differential diagnoses with probability scores
    // Provides evidence-based treatment recommendations
    // Includes ICD-10 diagnostic codes
    // Supports Norwegian and English languages
}
```

**Features:**
- Differential diagnosis suggestions with confidence scores
- Risk assessment and urgency evaluation
- Evidence-based treatment recommendations
- ICD-10 code suggestions
- Multi-language support

### 2. RAG (Retrieval-Augmented Generation)

Medical knowledge retrieval and generation system:

```csharp
/// <summary>
/// Queries medical knowledge base using RAG pattern
/// </summary>
/// <param name="question">Medical question in natural language</param>
/// <param name="patientContext">Patient-specific context for personalized responses</param>
/// <param name="language">Language code (default: Norwegian)</param>
/// <returns>Evidence-based answer with source citations</returns>
public async Task<MedicalRAGResponse> QueryMedicalKnowledgeAsync(
    string question, 
    string patientContext, 
    string language = "no")
{
    // Step 1: Generate query embedding
    // Step 2: Retrieve relevant medical documents from vector database
    // Step 3: Query Norwegian medical guidelines
    // Step 4: Generate contextual answer using retrieved documents
    // Step 5: Validate against medical standards
    // Returns answer with confidence score and source citations
}
```

**Features:**
- Vector search for semantic medical document retrieval
- Integration with medical guideline databases
- Confidence scoring and source attribution
- Validation against medical standards

### 3. AI Model Evaluation Framework

Comprehensive system for evaluating AI model performance:

```csharp
/// <summary>
/// Evaluates AI model accuracy on clinical test cases
/// </summary>
/// <param name="modelId">Identifier of the AI model to evaluate</param>
/// <param name="testCases">Collection of clinical test cases with ground truth</param>
/// <returns>Evaluation results with accuracy metrics</returns>
public async Task<ModelEvaluationResult> EvaluateClinicalAccuracyAsync(
    string modelId, 
    List<ClinicalTestCase> testCases)
{
    // Evaluates model predictions against ground truth
    // Calculates precision, recall, and F1 scores
    // Measures language quality for Norwegian text
    // Assesses safety and compliance
    // Monitors for model drift
}
```

**Evaluation Metrics:**
- Clinical accuracy (precision, recall, F1-score)
- Language quality assessment
- Performance metrics (latency, throughput)
- Safety evaluation
- Compliance validation
- Continuous monitoring

### 4. Multi-Agent Orchestration

Coordinates multiple specialized AI agents for complex workflows:

```csharp
/// <summary>
/// Executes a healthcare workflow using multiple AI agents
/// </summary>
/// <param name="workflowType">Type of workflow to execute</param>
/// <param name="context">Workflow execution context</param>
/// <returns>Workflow execution results</returns>
public async Task<AgentWorkflowResult> ExecuteWorkflowAsync(
    string workflowType, 
    Dictionary<string, object> context)
{
    // Creates workflow with appropriate steps
    // Coordinates multiple AI agents
    // Manages inter-agent communication
    // Handles failures and retries
    // Returns comprehensive execution results
}
```

**Available Agents:**
- **SchedulingAgent**: Intelligent appointment scheduling and resource optimization
- **DocumentationAgent**: Automated SOAP notes and medical documentation
- **TriageAgent**: AI-powered patient triage and urgency assessment
- **CommunicationAgent**: Automated patient communication
- **AdministrativeAgent**: Billing, insurance, and compliance automation
- **ClinicalDecisionAgent**: Diagnosis support and treatment recommendations

### 5. Digital Labor Automation

Specialized AI agents that automate routine healthcare tasks:

**SchedulingAgent**
```csharp
/// <summary>
/// Finds optimal appointment slot based on multiple factors
/// </summary>
/// <param name="context">Scheduling context including patient needs and doctor availability</param>
/// <returns>Recommended appointment slot with optimization score</returns>
private async Task<Dictionary<string, object>> FindOptimalAppointmentSlotAsync(AgentContext context)
{
    // Analyzes patient symptoms and urgency
    // Evaluates doctor specialization match
    // Optimizes for clinic efficiency
    // Considers patient preferences
    // Returns optimal time slot with alternatives
}
```

**DocumentationAgent**
```csharp
/// <summary>
/// Generates structured SOAP notes from consultation data
/// </summary>
/// <param name="context">Consultation context including transcription</param>
/// <returns>Structured SOAP note in Norwegian</returns>
private async Task<Dictionary<string, object>> GenerateSOAPNoteAsync(AgentContext context)
{
    // Extracts key information from consultation
    // Generates structured SOAP format:
    //   - Subjective: Patient's description
    //   - Objective: Clinical findings
    //   - Assessment: Diagnosis and evaluation
    //   - Plan: Treatment and follow-up
    // Follows Norwegian documentation standards
}
```

**TriageAgent**
```csharp
/// <summary>
/// Performs AI-powered patient triage assessment
/// </summary>
/// <param name="context">Patient information and symptoms</param>
/// <returns>Triage category and urgency assessment</returns>
private async Task<Dictionary<string, object>> PerformTriageAsync(AgentContext context)
{
    // Evaluates patient condition severity
    // Assigns triage category (Red/Orange/Yellow/Green/Blue)
    // Calculates urgency score (0-1 scale)
    // Identifies red flags requiring immediate attention
    // Follows Norwegian triage guidelines
}
```

## Healthcare Integration

### National Health Portal Integration

```csharp
/// <summary>
/// Retrieves patient data from national health portal
/// </summary>
/// <param name="personalNumber">Patient's national identification number</param>
/// <returns>Comprehensive patient data including medical history</returns>
public async Task<HelsenorgePatientData?> GetPatientDataAsync(string personalNumber)
{
    // Authenticates with national health portal API
    // Retrieves patient demographics and medical history
    // Fetches current medications and allergies
    // Returns structured patient data
    // Ensures GDPR compliance
}
```

**Integration Features:**
- Patient medical history retrieval
- Current medication information
- Appointment synchronization
- Electronic prescription management
- Consultation summary submission

### Electronic Prescription System

- Integration with national e-prescription infrastructure
- Automated prescription generation and validation
- Drug interaction checking
- Pharmacy coordination

## Technology Stack

### Backend Technologies
- **.NET 8**: Latest framework with native AOT and performance improvements
- **Entity Framework Core 8**: Modern ORM with query optimization
- **MediatR 12.x**: CQRS and mediator pattern implementation
- **AutoMapper 12.x**: Object-object mapping
- **FluentValidation 11.x**: Validation framework
- **Serilog**: Structured logging
- **SignalR**: Real-time bi-directional communication

### AI & Machine Learning
- **Azure OpenAI (GPT-4)**: Advanced language model for clinical AI
- **Azure Cognitive Services**: Vision, Text Analytics, Translation
- **Microsoft Semantic Kernel**: AI orchestration framework
- **Vector Databases**: Semantic search implementation
- **Custom Fine-tuned Models**: Healthcare-specific model training

### Frontend Technologies
- **Blazor WebAssembly**: Modern SPA framework
- **MudBlazor**: Material Design component library
- **SignalR Client**: Real-time updates
- **Progressive Web App (PWA)**: Mobile-first, installable application

### Infrastructure & DevOps
- **Docker**: Containerization for all services
- **Docker Compose**: Multi-container orchestration
- **Kubernetes**: Production-grade container orchestration
- **SQL Server 2022**: Enterprise database
- **Redis**: Distributed caching and session management
- **Nginx**: Reverse proxy and load balancing
- **Elasticsearch**: Centralized logging
- **Kibana**: Log visualization and analytics

### Cloud Services
- **Azure Container Registry**: Docker image storage
- **Azure Container Apps**: Serverless container hosting
- **Azure Kubernetes Service**: Managed Kubernetes
- **Azure Key Vault**: Secrets management
- **Application Insights**: Application performance monitoring
- **Azure SQL Database**: Managed database service

## Getting Started

### Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **SQL Server** - Or use Docker container
- **Azure OpenAI Access** - Required for AI features
- **Visual Studio 2022** or **VS Code** - Recommended IDEs

### Installation

#### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/saidulIslam1602/Tele-Doctor.git
cd Tele-Doctor/TeleDoctor.Modern

# Start all services
docker-compose up -d

# Verify services are running
docker-compose ps

# View logs
docker-compose logs -f
```

**Services Available:**
- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Blazor UI: http://localhost:7000
- SQL Server: localhost:1433
- Redis: localhost:6379
- Elasticsearch: http://localhost:9200
- Kibana: http://localhost:5601

#### Option 2: Local Development

```bash
# Navigate to project directory
cd TeleDoctor.Modern

# Restore dependencies
dotnet restore

# Configure user secrets for API
cd src/TeleDoctor.WebAPI
dotnet user-secrets init

# Set Azure OpenAI credentials
dotnet user-secrets set "AI:AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AI:AzureOpenAI:ChatDeploymentName" "gpt-4"

# Run database migrations
dotnet ef database update

# Start the API
dotnet run

# In a separate terminal, start the Blazor UI
cd ../TeleDoctor.BlazorUI
dotnet run
```

### Automated Setup

```bash
# Run the automated setup script
./scripts/setup.sh

# The script will:
# - Check prerequisites
# - Install dependencies
# - Configure certificates
# - Setup database
# - Create development scripts
```

## Configuration

### Application Settings

Configure the application in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TeleDoctorDB;Trusted_Connection=true;"
  },
  "AI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key",
      "ChatDeploymentName": "gpt-4",
      "Temperature": 0.3,
      "MaxTokens": 4000
    }
  }
}
```

### Environment Variables

For production deployment, use environment variables:

```bash
export AI__AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AI__AzureOpenAI__ApiKey="your-api-key"
export ConnectionStrings__DefaultConnection="your-connection-string"
```

### User Secrets (Development)

For local development, use .NET user secrets:

```bash
cd src/TeleDoctor.WebAPI
dotnet user-secrets set "AI:AzureOpenAI:ApiKey" "your-api-key"
```

## Deployment

### Docker Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment instructions including:

- Local Docker deployment
- Azure Container Apps deployment
- Azure Kubernetes Service deployment
- CI/CD pipeline configuration
- Monitoring and observability setup

### Quick Docker Deploy

```bash
# Build and start all services
docker-compose up -d

# Scale services
docker-compose up -d --scale teledoctor-api=3

# View logs
docker-compose logs -f teledoctor-api

# Stop services
docker-compose down
```

### Kubernetes Deployment

```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n teledoctor

# View logs
kubectl logs -f deployment/teledoctor-api -n teledoctor
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TeleDoctor.Tests.Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Unit Tests**: Business logic and domain model tests
- **Integration Tests**: API endpoint and database tests
- **AI Model Tests**: AI service validation and evaluation tests
- **End-to-End Tests**: Complete workflow testing

## API Documentation

### Swagger/OpenAPI

When running in development mode, Swagger UI is available at:
- http://localhost:8080/swagger

### Key Endpoints

#### Authentication
```http
POST /api/auth/login
POST /api/auth/register
POST /api/auth/refresh-token
```

#### Appointments
```http
GET    /api/appointments
POST   /api/appointments
PUT    /api/appointments/{id}
DELETE /api/appointments/{id}
```

#### AI Services
```http
POST /api/ai/clinical-analysis
POST /api/ai/rag/query
POST /api/ai/symptom-checker
POST /api/ai/medication-interaction
```

#### Healthcare Integration
```http
GET  /api/integration/patient/{personalNumber}
POST /api/integration/consultation-summary
GET  /api/integration/medications/{personalNumber}
```

### Authentication

The API uses JWT bearer token authentication:

```http
Authorization: Bearer <your-jwt-token>
```

## Project Highlights

### Advanced AI Implementation

- **Generative AI**: Production-ready GPT-4 integration
- **RAG System**: Evidence-based medical information retrieval
- **Model Evaluation**: Comprehensive AI testing framework
- **Multi-Agent Systems**: Coordinated AI agents for automation

### Healthcare Domain Expertise

- **Medical Workflows**: Complete telemedicine workflows
- **Compliance**: GDPR and healthcare data regulations
- **Integration**: National health system APIs
- **Standards**: ICD-10, FHIR, medical terminology

### Software Engineering Excellence

- **Clean Code**: SOLID principles and design patterns
- **Architecture**: Scalable, maintainable structure
- **Testing**: Comprehensive test coverage
- **Documentation**: Complete technical documentation
- **DevOps**: CI/CD pipelines and deployment automation

## Performance

### Optimizations

- Async/await patterns throughout the application
- Database query optimization with EF Core
- Redis caching for frequently accessed data
- Response compression and minification
- CDN integration for static assets

### Scalability

- Horizontal scaling support
- Load balancing with Nginx
- Database connection pooling
- Distributed caching with Redis
- Microservices-ready architecture

## Security

### Authentication & Authorization
- JWT token-based authentication
- Role-based access control (RBAC)
- Multi-factor authentication support
- Secure session management

### Data Protection
- Encryption at rest and in transit (TLS 1.3)
- GDPR compliance features
- Audit logging for all operations
- Data anonymization capabilities
- Secure API endpoints with rate limiting

## Monitoring & Observability

### Logging
- Structured logging with Serilog
- Centralized logging with Elasticsearch
- Log visualization with Kibana

### Monitoring
- Application Insights integration
- Performance metrics tracking
- Health check endpoints
- Custom telemetry

### Alerts
- Automated alerting for system issues
- Performance degradation detection
- Error rate monitoring

## Contributing

We welcome contributions! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch from `develop`
3. **Follow** coding standards and patterns
4. **Add** tests for new features
5. **Update** documentation as needed
6. **Submit** a pull request

### Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Write unit tests for business logic
- Keep methods focused and concise

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with .NET 8 and modern software engineering practices
- Powered by Azure OpenAI and Azure Cognitive Services
- Designed for healthcare professionals and patients
- Implements international healthcare standards

## Support

For questions, issues, or feature requests:
- **Email**: saidulislambinalisayed@outlook.com
- **GitHub Issues**: [Create an issue](https://github.com/saidulIslam1602/Tele-Doctor/issues)
- **Documentation**: See docs folder for detailed guides

---

**TeleDoctor Modern** - Advanced AI-powered healthcare platform built with modern technology stack