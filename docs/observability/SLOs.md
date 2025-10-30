# SLOs and Alert Policy

Services and key SLOs
- Ingestion worker: P50 latency from source→EventHub < 2s (99.5%); data freshness < 60s (99%)
- Web API: Availability 99.9%; P95 latency < 300ms
- Gateway: Availability 99.95%; P95 routing latency < 50ms
- Dashboards (Ops Console): Availability 99.9%

Burn-rate alerts (multi-window)
- 2% / 1h + 5% / 6h + 10% / 24h for each SLO

Golden signals dashboards
- RED (Rate, Errors, Duration) per service; USE for ingestion pipeline.

Noise reduction
- Grouped alerts; flapping suppression; maintenance windows.

Telemetry
- OpenTelemetry traces/metrics/logs exported to Azure Monitor and/or Grafana.
- PII scrubbing applied at source; redact emails, names, phone numbers.
