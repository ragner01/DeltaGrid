# Testing Strategy

## Architecture Fitness Functions

- Enforce layering: UI → Gateway → Services → Data; no lateral UI→Data access
- Bounded context isolation: no direct calls across contexts without gateway or messaging
- Event contracts: versioned, schema validated, consumer tests must pass
- Observability: traces present on all external calls; span attributes include tenant/site/asset

Suggested Tooling: ArchUnitNET for .NET assemblies to validate namespaces/dependencies.

## Model-Based Tests (Event Flows)

- Generate sequences for `TelemetryIngested` → `AnomalyDetected` → `CreateWorkOrder`
- Allocation: inputs (well tests, meter runs) → `AllocationClosed` → reporting read model updated
- Custody: `ProvingCompleted` → meter factor updated → tickets immutable ledger

## CI/CD

- Contract tests for APIs and events
- Performance tests for ADX queries and allocation batch timelines


