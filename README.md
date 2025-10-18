# TeleDoctor - AI-Powered Healthcare Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-green.svg)](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

Advanced AI-powered telemedicine platform built with .NET 8, demonstrating modern software engineering practices with comprehensive AI/ML integration, Norwegian healthcare system integration, and production-ready deployment.

## Quick Links

- **Full Documentation**: [TeleDoctor.Modern/README.md](TeleDoctor.Modern/README.md)
- **Quick Start**: [TeleDoctor.Modern/QUICKSTART.md](TeleDoctor.Modern/QUICKSTART.md)
- **Installation Guide**: [TeleDoctor.Modern/INSTALLATION.md](TeleDoctor.Modern/INSTALLATION.md)
- **Deployment Guide**: [TeleDoctor.Modern/DEPLOYMENT.md](TeleDoctor.Modern/DEPLOYMENT.md)
- **Technical Overview**: [TeleDoctor.Modern/TECHNICAL_OVERVIEW.md](TeleDoctor.Modern/TECHNICAL_OVERVIEW.md)

## Key Features

### AI & Machine Learning
- **Generative AI**: GPT-4-powered clinical decision support
- **RAG System**: Medical knowledge retrieval with evidence-based answers
- **Model Evaluation**: Comprehensive AI testing and monitoring framework
- **Multi-Agent System**: Six specialized AI agents for healthcare automation
- **Digital Labor**: Automated routine task handling

### Healthcare Platform
- Patient and provider management
- Real-time video consultations
- Appointment scheduling
- Electronic prescriptions
- Medical records management
- Norwegian healthcare system integration

### Technology Stack
- **.NET 8** with clean architecture
- **Azure OpenAI** for AI capabilities
- **Entity Framework Core 8** for data access
- **SignalR** for real-time communication
- **Docker & Kubernetes** for deployment
- **Blazor WebAssembly** for modern UI

## Project Structure

```
Tele-Doctor/
├── README.md                    (this file - overview)
├── Screenshots/                 (application screenshots)
└── TeleDoctor.Modern/          (complete implementation)
    ├── README.md               (detailed technical documentation)
    ├── QUICKSTART.md           (fast setup guide)
    ├── INSTALLATION.md         (installation instructions)
    ├── DEPLOYMENT.md           (deployment guide)
    ├── TECHNICAL_OVERVIEW.md   (architecture details)
    ├── PROJECT_SUMMARY.md      (executive summary)
    ├── docker-compose.dev.yml  (development setup)
    ├── docker-compose.yml      (production setup)
    └── src/                    (source code - 7 projects)
```

## Quick Start

```bash
# Navigate to the implementation
cd TeleDoctor.Modern

# Start database and cache services
docker compose -f docker-compose.dev.yml up -d

# Install .NET 8 runtime if needed
sudo apt-get install -y aspnetcore-runtime-8.0

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/TeleDoctor.WebAPI/TeleDoctor.WebAPI.csproj
```

**Access the application:**
- API: http://localhost:5000
- Swagger UI: https://localhost:5001/swagger
- Health Check: http://localhost:5000/health

## Documentation

See the [TeleDoctor.Modern](TeleDoctor.Modern/) directory for complete documentation:

- **README.md**: Complete technical documentation with all features
- **QUICKSTART.md**: Get started in 5 minutes
- **INSTALLATION.md**: Detailed installation instructions
- **DEPLOYMENT.md**: Production deployment guide
- **TECHNICAL_OVERVIEW.md**: Architecture and design decisions
- **PROJECT_SUMMARY.md**: Executive summary for stakeholders

## Screenshots

Application interface examples are available in the [Screenshots](Screenshots/) directory.

## Features Highlights

### Advanced AI Capabilities
- Clinical symptom analysis with differential diagnoses
- RAG-powered medical knowledge queries
- AI model evaluation and continuous monitoring
- Multi-agent workflow orchestration
- Automated medical documentation generation

### Healthcare Integration
- Norwegian health portal (Helsenorge) integration
- Electronic prescription system (E-Resept)
- FHIR healthcare data standards
- ICD-10 diagnostic coding
- GDPR compliance features

### Production Ready
- Docker containerization
- Kubernetes deployment manifests
- CI/CD pipeline configurations
- Comprehensive monitoring and logging
- Security best practices implemented

## Technology Demonstration

This project showcases expertise in:
- Advanced generative AI development
- Healthcare domain knowledge
- Modern .NET architecture
- Clean code principles
- Production deployment strategies
- Comprehensive documentation

## License

MIT License

## Contact

**Md Saidul Islam**
- Email: saidulislambinalisayed@outlook.com
- GitHub: [@saidulIslam1602](https://github.com/saidulIslam1602)

---

**For complete documentation and implementation details, see the [TeleDoctor.Modern](TeleDoctor.Modern/) directory.**