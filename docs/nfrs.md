# Non-Functional Requirements (NFRs)

All performance targets are at steady-state unless otherwise noted. ISO units required; conversions documented at interfaces.

## Reliability & Recovery

- Availability
  - Gateway, Events & Alarms: SLO 99.95%
  - Other core services: SLO 99.9%
- RTO/RPO
  - Control-plane services: RTO ≤ 30 min, RPO ≤ 5 min
  - Data-plane (time-series): RTO ≤ 2 h, RPO ≤ 1 min (stream checkpoints)

## Performance

- P99 API latency (read): ≤ 300 ms for typical views
- P99 API latency (write): ≤ 500 ms for commands with validation
- Time-series query (24h window, 10k series): P99 ≤ 2 s (ADX)
- Allocation batch (10k wells/day): complete ≤ 15 min; parallelizable
- Alarm fan-out: P99 ≤ 2 s end-to-end

## Scalability & Backpressure

- Ingestion throughput: target ≥ 500k messages/min per Event Hub namespace
- Backpressure behavior: bounded queues, drop policies only for non-critical signals, observability alerts at 70/85/95% utilization

## Data Management

- Retention
  - Hot time-series: 90 days; Warm: 13 months; Cold: 7+ years in lake
  - Audit logs: 7+ years immutable (WORM)
- Immutability: custody tickets, audit, approvals stored append-only with cryptographic digests
- Multi-tenant isolation: logical per-tenant schemas + encryption with per-tenant keys

## Security

- AuthN: Azure AD (OIDC/OAuth2), MFA enforced for privileged roles
- AuthZ: Role- and attribute-based (ABAC) with tenant/site/asset scoping
- Secrets: Managed identities, Azure Key Vault, rotation ≤ 90 days where applicable

## Observability

- Traces: 100% sampling for critical paths; 5–20% baseline otherwise
- Metrics: RED/USE + domain SLOs per service
- Logs: Structured JSON; PII minimized and tagged; retention 30–180 days

## Compliance

- Regulatory reporting deadlines met with automated pre-checks and evidence
- All units ISO; UI/API provide explicit conversions for non-ISO


