# ADR-0001: Tech Stack and Architectural Style

## Status
Accepted

## Context
The platform targets real-time/near-real-time OT data, multi-tenant isolation, and strong observability.

## Decision
- .NET 9, ASP.NET Core, MediatR, FluentValidation
- Azure Event Hubs, ADX/Timeseries, SQL Server, Redis, Key Vault
- YARP gateway, Blazor Server, .NET MAUI, gRPC/REST, OpenTelemetry
- Event-first integration with versioned schemas; API for commands

## Consequences
- Strong ecosystem alignment with Azure
- Requires cost governance for ADX/lake


