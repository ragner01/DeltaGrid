# DeltaGrid — Enterprise Oil & Gas Operations Platform

> **Enterprise-grade, domain-driven operations platform for Nigerian upstream oil & gas operators**

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)
[![Azure](https://img.shields.io/badge/cloud-Azure-blue.svg)](https://azure.microsoft.com/)

## 🎯 Overview

**DeltaGrid** is a comprehensive, enterprise-grade operations platform designed specifically for Nigerian upstream oil & gas operators. Built with **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** principles, it provides end-to-end coverage from operational technology (OT) ingestion through advanced analytics, ensuring compliance, reliability, and operational excellence.

### What DeltaGrid Provides

- **Real-time OT Data Ingestion**: Connect to OPC UA, MQTT, and PI/AVEVA systems with quality filtering and normalization
- **Time-Series Intelligence**: High-frequency telemetry storage with automatic rollups and retention policies
- **Production Operations**: Well state management, production allocation, lift optimization, and reconciliation
- **Safety & Compliance**: Permit-to-Work (PTW) with non-repudiation, integrity management, and custody transfer
- **Event Processing**: Intelligent alarm rationalization with suppression, deduplication, and escalation
- **Digital Twin**: Complete asset hierarchy (Region → Field → Facility → Train → Unit → Equipment → Tag)
- **Advanced Analytics**: ML-powered optimization, anomaly detection, and predictive maintenance
- **Enterprise Search**: Semantic search with "Ask Ops" Q&A across SOPs, permits, and lab results
- **Compliance Reporting**: Scheduled reports with PDF/Excel export, signature workflows, and audit trails
- **Operations Console**: Real-time Blazor Server dashboards with SignalR live updates
- **Field Tech App**: Offline-first .NET MAUI app for field data capture and work execution

---

## 🏗️ Architecture

### Technology Stack

**Core Framework**
- .NET 8.0 (C#)
- ASP.NET Core
- MediatR (CQRS)
- FluentValidation
- AutoMapper

**Cloud Infrastructure**
- Azure Event Hubs (buffering)
- Azure Data Explorer (ADX) / TimescaleDB (time-series)
- Azure Cognitive Search (enterprise search)
- Azure Key Vault (secrets management)
- Azure Storage (ADLS Gen2 for lakehouse)
- Azure OpenAI (semantic search embeddings)

**Services & Communication**
- YARP (API Gateway)
- gRPC (optimization service)
- SignalR (real-time updates)
- Duende IdentityServer (authentication)

**Frontend**
- Blazor Server (Operations Console)
- .NET MAUI (Field Tech App)

**Data & Storage**
- SQL Server (transactional data)
- Redis (caching)
- SQLite (offline sync)

**DevOps & Infrastructure**
- Docker & Kubernetes (containerization)
- Terraform/Bicep (Infrastructure as Code)
- GitHub Actions (CI/CD)
- DbUp (database migrations)

### Solution Structure

```
DeltaGrid/
├── src/
│   ├── Core/                    # Domain Layer (entities, aggregates, value objects)
│   ├── Application/            # Application Layer (CQRS commands/queries)
│   ├── Infrastructure/          # Infrastructure Layer (repositories, external services)
│   ├── BuildingBlocks/          # Cross-cutting concerns (Result<T>, Guard, DomainEvents)
│   ├── Security/                # Identity, RBAC/ABAC, JWT hardening, Key Vault integration
│   ├── WebApi/                   # Main REST API
│   ├── Gateway/                 # YARP API Gateway (versioning, rate limiting, resilience)
│   ├── Identity/                # Duende IdentityServer (authentication)
│   ├── Ingestion/               # OT Ingestion Service (OPC UA, MQTT, PI)
│   ├── TimeSeries/              # Time-Series Storage (ADX client, rollups)
│   ├── Optimization/            # Lift Optimization Service (gRPC, ONNX inference)
│   ├── Events/                  # Event Processing & Alarm Rationalization
│   ├── Search/                  # Enterprise Search (Azure Cognitive Search)
│   ├── Reporting/               # Reporting & Scheduling (Hangfire, QuestPDF)
│   ├── OpsConsole/              # Operations Console (Blazor Server)
│   ├── FieldApp/                # Field Tech App (.NET MAUI, offline-first)
│   ├── DataGovernance/          # Data Quality & Governance Service
│   ├── DisasterRecovery/        # DR Management Service
│   ├── Cutover/                 # Cutover & Enablement Service
│   └── Migrations/              # Database Migrations (DbUp)
│
├── tests/
│   ├── UnitTests/               # Unit tests
│   └── IntegrationTests/       # Integration tests
│
├── docs/                        # Comprehensive documentation
│   ├── ARCHITECTURE.md         # Architecture overview
│   ├── c4/                     # C4 diagrams
│   ├── domain/                 # Domain documentation
│   └── [service-specific]/     # Service documentation
│
├── infrastructure/              # Infrastructure as Code
│   ├── Dockerfile              # Container templates
│   ├── docker-compose.yml      # Local development
│   └── terraform/              # Azure infrastructure
│
└── .github/workflows/           # CI/CD pipelines
```

### Architecture Layers

```
┌─────────────────────────────────────────┐
│     Presentation Layer                   │
│  WebApi | Gateway | OpsConsole |        │
│  FieldApp | Identity                     │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│     Application Layer                    │
│  Commands | Queries | Handlers | DTOs   │
│  (MediatR, FluentValidation)            │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│     Domain Layer                         │
│  Entities | Aggregates | Value Objects   │
│  Domain Events | Business Logic          │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│     Infrastructure Layer                 │
│  Repositories | External Services        │
│  Data Access | Message Bus              │
└─────────────────────────────────────────┘
```

### Microservices Architecture

```
┌────────────────────────────────────────────────────────┐
│              Clients                                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │ Ops      │  │ Field    │  │ External │            │
│  │ Console  │  │ App      │  │ APIs     │            │
│  └──────────┘  └──────────┘  └──────────┘            │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│         YARP API Gateway                                │
│  Versioning | Rate Limiting | Resilience | Auth       │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│              Core Services                               │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │
│  │Identity│ │Ingest│ │Time  │ │Optim │ │Events│      │
│  └──────┘ └──────┘ │Series│ │ize   │ └──────┘      │
│  ┌──────┐ ┌──────┐ └──────┘ └──────┘ ┌──────┐      │
│  │Search│ │Report│ ┌──────┐ ┌──────┐ │WebApi│      │
│  └──────┘ └──────┘ │Gov   │ │DR    │ └──────┘      │
│                    │ern   │ │Mgmt  │                │
│                    └──────┘ └──────┘                │
└────────────────────────────────────────────────────────┘
                    ↓
┌────────────────────────────────────────────────────────┐
│         Infrastructure                                  │
│  SQL Server | ADX | Redis | Key Vault | Event Hubs    │
└────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites

- **.NET SDK 8.0** or later (see `global.json`)
- **SQL Server** (or use Docker)
- **Azure account** (for cloud services, optional for local dev)
- **Docker** (for containerized development)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/ragner01/DeltaGrid.git
cd DeltaGrid

# Restore dependencies
dotnet restore

# Build the solution
dotnet build IOC.sln

# Run tests
dotnet test

# Run key services
dotnet run --project src/WebApi
dotnet run --project src/Gateway
dotnet run --project src/OpsConsole
```

### Docker Compose (All Services)

```bash
docker-compose -f infrastructure/docker-compose.yml up
```

This starts all services in containers for local development.

### Development Environment Setup

```bash
# Run development setup script
./scripts/dev.sh
```

---

## ✨ Key Features

### Phase 1-15: Core Operations

| Phase | Feature | Description |
|-------|---------|-------------|
| **3** | **Identity & Multi-tenancy** | Duende IdentityServer, RBAC/ABAC, tenant isolation, path-scoped authorization |
| **4** | **OT Ingestion** | OPC UA, MQTT, PI/AVEVA connectors with QoS pipeline (deadbanding, normalization) |
| **5** | **Time-Series Storage** | Azure Data Explorer with 1s→1m→15m→1h rollups, retention policies |
| **6** | **Well Domain** | State engine (Shut-In, Ramp-Up, Stable, Decline, Trip, Alarm), limits enforcement |
| **7** | **Production Allocation** | Proportional-by-test, regression-based, constraint-based solvers with reconciliation |
| **8** | **Lift Optimization** | Rules engine + ONNX ML inference (gRPC), guardrails, decision logs |
| **9** | **Event Processing** | Alarm suppression (deduplication, flood control, shelving), SLA timers, escalations |
| **10** | **Work Management & PTW** | Digital permits (hot/cold/confined space), isolations/LOTO, hash-chained archive |
| **11** | **Integrity Management** | Inspection plans, thickness readings, corrosion rates, RBI scoring |
| **12** | **Pipeline Operations** | Leak detection (statistical volume balance), adaptive thresholds, incident replay |
| **13** | **Custody Transfer** | Meter proving, CTPL/API calculations, immutable tickets with approvals |
| **14** | **Lab Management** | Chain-of-custody, PVT/BS&W/salinity/viscosity results, property calculators |
| **15** | **Digital Twin** | Asset hierarchy with versioned topology, KPI overlays, graph queries |

### Phase 16-23: User Experience & Platform

| Phase | Feature | Description |
|-------|---------|-------------|
| **16** | **Operations Console** | Blazor Server dashboards, SignalR live updates, quick actions (ack, shelve, create WO) |
| **17** | **Field Tech App** | .NET MAUI offline-first (SQLite, vector clocks), schema-driven forms, barcode/QR |
| **18** | **API Gateway** | YARP with v1/v2 versioning, canary deployments, correlation IDs, idempotency, Polly resilience |
| **19** | **Observability** | OpenTelemetry traces/metrics/logs, SLOs with burn-rates, on-call runbooks |
| **20** | **Data Lakehouse** | ADLS Gen2 + Delta Lake medallion (Raw→Curated→Marts), CDC, Purview catalog |
| **21** | **MLOps** | Feature pipelines, model cards, ONNX deployment (canary/shadow), drift monitoring |
| **22** | **Enterprise Search** | Azure Cognitive Search with semantic/vector search, "Ask Ops" Q&A, security trimming |
| **23** | **Reporting & Scheduling** | Parameterized templates (PDF/Excel), Hangfire scheduling, signature workflows, compliance packs |

### Phase 24-28: Production Readiness

| Phase | Feature | Description |
|-------|---------|-------------|
| **24** | **Security Hardening** | Key Vault secrets, JWT key rotation, security headers (HSTS/CSP), threat modeling (STRIDE) |
| **25** | **CI/CD & IaC** | GitHub Actions pipelines, Terraform/Bicep, blue-green/canary deployments, DbUp migrations |
| **26** | **Disaster Recovery** | DR tiers with RTO/RPO targets, geo-redundant storage, automated backups, DR drills |
| **27** | **Data Quality & Governance** | DQ rule engine (completeness, timeliness, validity, consistency), stewardship workflows |
| **28** | **Cutover & Enablement** | Seed data service, cutover checklists, feature flags, operator training, hypercare |

---

## 📚 Documentation

### Architecture & Design
- **[Architecture Overview](docs/ARCHITECTURE.md)** - System architecture and design decisions
- **[C4 Diagrams](docs/c4/)** - Context and Container diagrams
- **[Domain Model](docs/domain/)** - Bounded contexts, domain events, glossary
- **[ADRs](docs/adr/)** - Architecture Decision Records

### Service Documentation
- **[API Gateway](docs/gateway/VERSIONING.md)** - Versioning policy and canary deployments
- **[Digital Twin](docs/twin/API.md)** - Asset hierarchy API and queries
- **[Time-Series](docs/timeseries/ADX_Schema.kql)** - ADX schema and KQL queries
- **[Allocation](docs/allocation/METHODOLOGY.md)** - Allocation methods and formulas
- **[Lab Management](docs/lab/CALCULATORS.md)** - Property calculators and calculations
- **[Events & Alarms](docs/events/ALARM_PHILOSOPHY.md)** - Alarm rationalization philosophy

### Platform Capabilities
- **[Observability](docs/observability/)** - OpenTelemetry setup, SLOs, on-call runbooks
- **[Data Lakehouse](docs/lakehouse/)** - ADLS Gen2, Delta Lake medallion architecture
- **[MLOps](docs/ml/PLAN.md)** - ML model cards, ONNX deployment, drift monitoring
- **[Enterprise Search](docs/search/)** - Azure Cognitive Search, Q&A, security trimming
- **[Reporting](docs/reporting/)** - Template engine, scheduling, compliance packs
- **[Security](docs/security/)** - Standards, deployment practices, threat modeling
- **[Deployment](docs/deployment/)** - CI/CD pipelines, runbooks, DORA metrics
- **[Disaster Recovery](docs/dr/)** - DR policy, runbooks, readiness dashboard
- **[Data Quality & Governance](docs/governance/)** - DQ playbook, stewardship workflows
- **[Cutover & Enablement](docs/cutover/)** - Cutover checklist, training materials

### Operations
- **[Runbooks](docs/runbooks/)** - On-call procedures, site connection guides
- **[Testing Guide](docs/testing.md)** - Testing strategy and practices
- **[NFRs](docs/nfrs.md)** - Non-functional requirements
- **[Risks & Assumptions](docs/risks.md)** - Risk register and assumptions log

### Roadmap
- **[Next Steps](docs/NEXT_STEPS.md)** - Future phases and roadmap

---

## 🔐 Security

DeltaGrid implements enterprise-grade security practices:

- **Multi-tenant RBAC/ABAC**: Role-based and attribute-based access control with tenant isolation
- **JWT Hardening**: Algorithm pinning, key rotation, token validation
- **Azure Key Vault**: Centralized secrets management with managed identities
- **Security Headers**: HSTS, CSP, X-Frame-Options, and more
- **Threat Modeling**: STRIDE-based threat analysis with mitigation tracking
- **Admin Audit Logging**: Immutable audit trail for compliance
- **CI/CD Security Scanning**: SAST, DAST, SCA, and secret scanning

See [Security Documentation](docs/security/) for detailed security practices.

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
```

Test coverage includes:
- **Unit Tests**: Domain logic, services, utilities
- **Integration Tests**: API endpoints, database integration
- **Contract Tests**: API contract validation

---

## 🔄 CI/CD

### Pipeline Flow

1. **Development**: Auto-deploy on push to `develop` branch
2. **Staging**: Manual promotion with approval gate (blue-green deployment)
3. **Production**: Manual promotion with canary deployment

### GitHub Actions Workflows

- **`.github/workflows/ci.yml`** - Build and test on push/PR
- **`.github/workflows/security-scan.yml`** - Security scanning (SAST/DAST/SCA)
- **`.github/workflows/deploy-dev.yml`** - Deploy to development environment
- **`.github/workflows/promote-staging.yml`** - Promote to staging with approval
- **`.github/workflows/promote-production.yml`** - Promote to production with canary

See [CI/CD Documentation](docs/deployment/PIPELINES.md) for detailed pipeline documentation.

---

## 🇳🇬 Nigerian Context

DeltaGrid is specifically designed for Nigerian upstream operations:

- **Offshore/Nearshore Assets**: Handles intermittent connectivity with offline-first field app
- **Multi-tenancy**: Supports joint venture (JV) scenarios with tenant isolation
- **Regulatory Compliance**: Built-in compliance packs for Nigerian regulatory requirements
- **Localization**: Supports local regulatory constraints and reporting formats
- **Field Operations**: Optimized for low-latency links and offline field data capture

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

### Development Workflow

1. Create a feature branch from `main`
2. Make changes following Clean Architecture principles
3. Add tests (unit and/or integration)
4. Ensure all tests pass and build succeeds
5. Submit a pull request

---

## 📄 License

Proprietary. © Current year Nigerian O&G operator.

---

## 📞 Support

For questions, issues, or contributions:
- **GitHub Issues**: [Create an issue](https://github.com/ragner01/DeltaGrid/issues)
- **Documentation**: See `docs/` directory
- **Architecture Questions**: See `docs/ARCHITECTURE.md` and `docs/adr/`

---

## 🎯 Roadmap Status

### Completed Phases (1-28)
✅ All core operations, user experience, platform capabilities, and production readiness phases are **complete**.

### Future Phases (29-30)
See [Next Steps](docs/NEXT_STEPS.md) for future roadmap items including:
- Performance optimization
- Advanced integrations
- Enhanced analytics
- Extended mobile capabilities

---

**DeltaGrid** — *Enterprise Oil & Gas Operations Platform for Nigerian Upstream Operators*
