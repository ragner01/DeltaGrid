# Ops Handbook (Nigeria Upstream)

On-call rotation
- Primary and secondary engineers; weekly rotation; local time West Africa Time (WAT).

Runbooks
- Ingestion backlog spike: scale workers, verify Event Hubs throughput units, check connector connectivity.
- API error spikes: inspect ProblemDetails logs with correlation ID; roll back canary if needed.
- ADX freshness lag: check ingestion status, late arrivals, update policies.
- Ops Console outage: verify SignalR hub health via synthetic probe.

Chaos drills
- Quarterly failure injection for partial outages (API/ADX/Event Hubs) with documented outcomes.
