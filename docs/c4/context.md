# C4 — Context Diagram

```mermaid
graph LR
    %% External Actors
    Operators["Field Operators"]
    Engineers["Production/Facilities Engineers"]
    Schedulers["Schedulers/Dispatch"]
    HSE["HSE/Compliance"]
    Finance["Finance/Accounting"]
    Regulators["Regulators"]
    LabVendors["Lab Vendors/LIMS"]
    ERP["ERP/Trading"]
    OT["OT Sources (PI/OPC, SCADA RTUs)"]

    subgraph IOC[IOC Platform]
      Gateway["API Gateway (YARP)"]
      OpsConsole["Ops Console (Blazor Server)"]
      FieldApp["Field App (.NET MAUI)"]
      Backend["Core Backend Services (.NET 9)"]
      Data["Data Stores (SQL, ADX/TS, Data Lake)"]
    end

    Operators -->|Work orders, readings, PTW| Gateway
    Engineers -->|Configs, analysis, events| Gateway
    Schedulers -->|Schedules, nominations| Gateway
    HSE -->|Incidents, audits| Gateway
    Finance -->|Volumes, allocations| Gateway
    Regulators -->|Reports, filings| Gateway
    LabVendors -->|Assays, results| Gateway
    ERP -->|Master data, trades| Gateway
    OT -->|Telemetry, tags, alarms| Gateway

    Gateway --> OpsConsole
    Gateway --> FieldApp
    Gateway --> Backend
    Backend --> Data
```

Notes

- Multi-tenant (company/site/asset) isolation enforced at gateway and service layers.
- All quantities in ISO units (e.g., m³, kg, Pa). Non-ISO entry points note conversions.


