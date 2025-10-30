# Architecture Overview

This platform is designed for a Nigerian upstream oil & gas company operating multiple onshore/near-shore assets, with constraints aligned to local regulatory and infrastructure realities (limited backhaul, intermittent connectivity, and multi-tenant JV structures).

- Clean Architecture, DDD, CQRS across services
- Identity: multi-tenant RBAC/ABAC
- Ingestion: OPC UA/MQTT, Event Hubs
- Time-series: ADX/Timescale
- Integrity, PTW, Allocation, Optimization, Events, Custody, Lab, Twin, Ops Console, Field App
