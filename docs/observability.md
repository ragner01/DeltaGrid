# Observability

## Signals per Container

- Gateway
  - Traces: request spans with tenant/site/asset
  - Metrics: p95/p99 latency, error rate, rate limits
  - Logs: access logs, auth failures

- Ingestion
  - Traces: ingestion pipeline spans (source→event hubs→processing)
  - Metrics: ingress rate, lag, DLQ size
  - Logs: parse errors with sample payload hashes

- Time-Series
  - Traces: query spans annotated with series and window
  - Metrics: cache hit rate, query latency histogram

- Allocation
  - Traces: batch job spans per asset/day
  - Metrics: run duration, success/failure, recompute counts

- Events & Alarms
  - Metrics: alarm fan-out latency, ack time distribution

- Work Mgmt & PTW
  - Metrics: offline sync queue size, conflict rate

- Pipeline/Leak Detection
  - Metrics: detection latency, false-positive/negative rates

- Custody Transfer
  - Metrics: proving turnaround, ticket issuance latency

## SLO/Error Budget Policy

- SLOs defined in `docs/nfrs.md`.
- Error budget burn alerts: 2%, 5%, 10% monthly thresholds.
- Freeze policy: if burn > 25% in a month, feature freeze and reliability sprint.

## OpenTelemetry

- Propagate context across gRPC/REST; export to Azure Monitor/OTel collector.


