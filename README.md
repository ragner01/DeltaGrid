# DeltaGrid — Oil & Gas Operations Platform (Nigeria)

Enterprise-grade, domain-driven platform for a Nigerian upstream oil & gas operator. It spans OT ingestion, time series, integrity, PTW, allocation, optimization, events, custody, lab management, digital twin, pipeline leak detection, Ops Console (Blazor), Field Tech offline app (MAUI), gateway with resilience, observability, lakehouse, and MLOps.

## Quick start

Requirements
- .NET SDK 9.0.305 (global.json pins)
- macOS/Windows/Linux

Build & test
```
dotnet restore
dotnet build IOC.sln
dotnet test
```

Run key services
- API: `dotnet run --project src/WebApi`
- Gateway: `dotnet run --project src/Gateway`
- Events: `dotnet run --project src/Events/Events.Service.csproj`
- Optimization: `dotnet run --project src/Optimization/Optimization.Service.csproj`
- Ingestion: `dotnet run --project src/Ingestion/Ingestion.Worker.csproj`
- Ops Console (Blazor): `dotnet run --project src/OpsConsole`

## Highlights by phase
- Identity & Security: Multi-tenant RBAC/ABAC; Duende IdentityServer; path-scoped twin auth.
- Ingestion: OPC UA/MQTT connectors, QoS pipeline, Event Hubs publisher.
- Time Series: ADX client and KQL schemas with rollups and retention.
- Well & Allocation: State engine, proportional-by-test, reconciliation with custody.
- Optimization: Rules + ONNX inference service (gRPC), guardrails.
- Events: Router with suppression (dedupe, flood, shelving), notifications.
- PTW: Hash-chained permit archive for non-repudiation.
- Integrity: Inspection plans, thickness, corrosion rates, RBI alignment.
- Pipeline: Balance-based leak detection with adaptive thresholds and incidents.
- Custody & Lab: Proving, CTPL, immutable tickets; lab results with lineage/signatures and property push to Allocation/Optimization.
- Twin: Region→...→Tag hierarchy, versioned topology, snapshots with KPI overlay.
- Ops Console: Blazor Server dashboard, SignalR live updates, quick actions.
- Field App: .NET MAUI offline-first (SQLite, vector clocks), schema-driven forms.
- Gateway: YARP v1/v2 versioning, canary, correlation/idempotency, Polly resilience.
- Observability: OpenTelemetry conventions, SLOs/burn-rates, on-call runbooks.
- Lakehouse: ADLS Gen2 + Delta medallion, CDC, Purview, data contracts.
- MLOps: Feature pipelines, model cards, ONNX deploy (canary/shadow), drift.

## Nigerian context
Designed for onshore/near-shore assets with intermittent connectivity, JV multi-tenancy, and local regulatory constraints. Field app supports offline capture; Ops Console optimized for low-latency links.

## Docs
- Architecture: `docs/ARCHITECTURE.md`
- Gateway/versioning: `docs/gateway/VERSIONING.md`
- Twin API: `docs/twin/API.md`
- Lab calculators: `docs/lab/CALCULATORS.md`
- Observability: `docs/observability/*`
- Lakehouse: `docs/lakehouse/*`
- MLOps: `docs/ml/PLAN.md`
- Field provisioning: `docs/field/PROVISIONING.md`

## CI
GitHub Actions workflow builds/tests on push. See `.github/workflows/ci.yml`.

## Security
- Request signing for critical writes (`X-Signature` HMAC)
- PII scrubbing, CSP, antiforgery, clickjacking protection
- Path-scoped twin access, per-tenant rate limits at gateway

## License
Proprietary. © Current year Nigerian O&G operator.


