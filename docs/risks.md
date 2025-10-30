# Risk Register

| ID | Risk | Category | Impact | Likelihood | Owner | Next Action |
|---|---|---|---|---|---|---|
| R-001 | OT connectivity instability (RTUs, PI/OPC) | Operational | High | Medium | Ops Lead | Pilot buffered collectors; store-and-forward config |
| R-002 | Cyber threat from exposed interfaces | Security | High | Medium | CISO | External pen test; WAF + DDoS plan |
| R-003 | Data quality (sensor drift, missing tags) | Data | High | High | Data Eng | Implement DQ monitors; auto-imputation policies |
| R-004 | Model drift in optimization | ML | Medium | Medium | DS Lead | Monitoring + shadow deployments + periodic re-train |
| R-005 | Regulatory variance across jurisdictions | Compliance | High | Medium | Compliance | Parameterize rules; jurisdiction profiles |
| R-006 | Multi-tenant data leakage | Security | Critical | Low | Platform | ABAC, per-tenant keys, isolation tests |
| R-007 | Event schema evolution breaking consumers | Architecture | Medium | Medium | Platform | Versioned events; CDC contracts; canary consumers |
| R-008 | Backpressure causing data loss | Reliability | High | Low | Platform | Dead-letter queues; retry with exponential backoff |
| R-009 | Key Vault/IdP outage | Security | Medium | Low | SRE | Cached credentials; break-glass procedures |
| R-010 | Cost overrun in ADX/lake storage | Financial | Medium | Medium | Finance | Tiering policies; budget alerts |

Assumptions are tracked in `docs/assumptions.md`.


