# Gateway Versioning and Resilience

Versioning
- API supports side-by-side v1 and v2 under `/api/v1/*` and `/api/v2/*`.
- Canary rollout via header `X-Canary: true` can target v2 routes during phased deployments.
- Backward compatibility: breaking changes require a new major version; v(N-1) remains supported through the deprecation window.
- Deprecation calendar: announce at T0, warn via `Deprecation` header at T+30d, sunset at T+90d.

Resilience
- Correlation IDs (`X-Correlation-ID`) are issued/propagated for tracing.
- Idempotency: POST supports `Idempotency-Key` with in-memory store (swap for Redis in prod).
- Polly policies on outbound hops: retries (200ms,500ms,1s), circuit breaker (5 failures/30s), timeout (15s).

Security
- Per-tenant rate limits at the gateway; align with WAF and bot mitigation.
- Request signing: critical write paths require `X-Signature` (HMAC-SHA256 over body) validated by services.

Observability
- Per-route latency and error dashboards recommended; attach correlation IDs to logs and traces.

Contract Testing
- Maintain OpenAPI contracts per version; run diff checks to block breaking changes unless version bumped.
