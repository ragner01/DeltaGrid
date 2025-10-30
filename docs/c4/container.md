# C4 — Container Diagram

```mermaid
flowchart LR
    subgraph Client[Clients]
      OpsConsole[Ops Console (Blazor Server)]
      FieldApp[Field App (.NET MAUI)]
      ExternalAPI[External Integrations]
    end

    Gateway[YARP API Gateway]
    Auth[Auth/IdP (Azure AD, JWT/OAuth2)]

    subgraph Core[Core Backend]
      Ingest[Ingestion Service (Event Hubs -> gRPC/REST)]
      TimeSeries[Time-Series Store (ADX/Timeseries)]
      Allocation[Production Allocation Service]
      Optimization[Lift Optimization/Surveillance]
      EventsAlarms[Events & Alarms]
      WorkPTW[Work Mgmt & PTW]
      Integrity[Integrity Mgmt]
      Pipeline[Pipeline Ops & Leak Detection]
      Custody[Custody Transfer & Proving]
      Labs[Labs Integration]
      Reporting[Reporting & Compliance]
      MLInference[ML Inference]
      Lake[Data Lake/Lakehouse]
    end

    Cache[Redis Cache]
    SQL[(SQL Server)]
    ADX[(Azure Data Explorer)]
    LakeStore[(Data Lake)]
    KeyVault[Key Vault]
    Telemetry[OpenTelemetry Collector]

    Client --> Gateway
    ExternalAPI --> Gateway
    Gateway --> Auth
    Gateway --> Ingest
    Gateway --> Allocation
    Gateway --> Optimization
    Gateway --> EventsAlarms
    Gateway --> WorkPTW
    Gateway --> Integrity
    Gateway --> Pipeline
    Gateway --> Custody
    Gateway --> Labs
    Gateway --> Reporting
    Gateway --> MLInference

    Ingest --> ADX
    Ingest --> LakeStore
    TimeSeries --> ADX
    Allocation --> SQL
    Allocation --> ADX
    Optimization --> ADX
    EventsAlarms --> SQL
    WorkPTW --> SQL
    Integrity --> SQL
    Pipeline --> ADX
    Custody --> SQL
    Labs --> SQL
    Reporting --> SQL
    Reporting --> LakeStore
    MLInference --> ADX

    Core --> Cache
    Core --> KeyVault
    Core --> Telemetry
```

Notes

- MediatR used internally for request/response and pipeline behaviors (validation, logging, metrics).
- FluentValidation for command/query validation.
- OpenTelemetry traces, metrics, and logs emitted per container.
- Data residency and tenant isolation enforced at schema and key-level.


