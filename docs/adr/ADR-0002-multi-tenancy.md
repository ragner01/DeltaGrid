# ADR-0002: Multi-Tenancy Model and Data Isolation

## Status
Accepted

## Context
The system must support multiple companies, sites, and assets with strict data isolation and configurable access controls.

## Decision
- Logical isolation by tenant in databases; per-tenant schemas/partitions
- ABAC combining tenant, site, asset, role; enforced at gateway and service layers
- Per-tenant encryption keys; managed identities

## Consequences
- Operational complexity in key management and schema management
- Clear audit and compliance posture for data separation


