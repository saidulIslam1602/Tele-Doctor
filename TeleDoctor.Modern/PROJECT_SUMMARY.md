# TeleDoctor Modern - AI-Powered Healthcare Platform

## Overview

TeleDoctor Modern is an advanced telemedicine platform built with .NET 8 and Azure OpenAI, featuring comprehensive AI capabilities for Norwegian healthcare.

## Key Features

### AI & Machine Learning
- **Generative AI Integration**: GPT-4-powered clinical decision support
- **RAG System**: Retrieval-Augmented Generation for medical knowledge queries
- **Model Evaluation Framework**: Comprehensive AI model assessment and monitoring
- **Agentic Workflows**: Multi-agent system for healthcare automation
- **Digital Labor Agents**: 6 specialized AI agents for routine task automation

### Norwegian Healthcare Integration
- **Helsenorge API**: Integration with Norwegian national health portal
- **E-Resept**: Electronic prescription system support
- **Norwegian Medical Standards**: Compliance with local regulations
- **Bilingual Support**: Full Norwegian and English language support

### Technical Architecture
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Modern .NET 8**: Latest framework features and performance improvements
- **Microservices Ready**: Scalable, containerized architecture
- **Real-time Communication**: SignalR for live updates
- **Comprehensive Testing**: Unit, integration, and AI model tests

## Technology Stack

**Backend:**
- .NET 8, Entity Framework Core 8
- Azure OpenAI (GPT-4)
- Azure Cognitive Services
- SignalR, MediatR, AutoMapper

**Frontend:**
- Blazor WebAssembly
- MudBlazor, Progressive Web App

**Infrastructure:**
- Docker & Kubernetes
- Azure Container Apps
- SQL Server, Redis, Elasticsearch
- Nginx, Application Insights

## Project Structure

```
TeleDoctor.Modern/
├── src/
│   ├── TeleDoctor.Core/              # Domain entities
│   ├── TeleDoctor.AI.Services/        # AI/ML services
│   │   ├── RAG/                       # Retrieval-Augmented Generation
│   │   ├── ModelEvaluation/           # AI evaluation framework
│   │   ├── AgenticFlows/              # Multi-agent orchestration
│   │   └── DigitalLabor/              # Automation agents
│   ├── TeleDoctor.Application/        # Business logic
│   ├── TeleDoctor.Infrastructure/     # Data access
│   ├── TeleDoctor.Norwegian.Integration/ # Healthcare APIs
│   ├── TeleDoctor.WebAPI/             # REST API
│   └── TeleDoctor.BlazorUI/           # Web UI
├── docs/
└── docker-compose.yml
```

## Getting Started

```bash
# Clone repository
git clone https://github.com/yourusername/teledoctor-modern.git

# Run with Docker
cd TeleDoctor.Modern
docker-compose up -d

# Or run locally (requires .NET 8 SDK)
dotnet restore
dotnet run --project src/TeleDoctor.WebAPI
```

## Documentation

- **README.md**: Complete project documentation
- **DEPLOYMENT.md**: Deployment and operations guide
- **ARCHITECTURE.md**: Technical architecture details

## License

MIT License
