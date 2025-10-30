# OpenTelemetry Setup (Azure Monitor / Grafana)

.NET services
- Add OpenTelemetry SDK with traces, metrics, logs
- Resource: service.name, service.version, deployment.environment, tenant
- Exporters: Azure Monitor (prod) or OTLP to Grafana Tempo/Loki/Mimir (non-prod)

Semantic conventions
- span names: domain.operation (e.g., allocation.run, ptw.approve, lab.record)
- attributes: tenant.id, user.id (hashed), well.id, segment.id, ticket.number (hashed), correlation.id

Sampling
- 10% traces by default; 100% for errors; adjustable per route.

Dashboards
- RED/USE per service; KPIs: ingestion latency, data freshness, dashboard availability.
