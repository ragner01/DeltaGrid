# Security & Threat Model

## STRIDE Summary

- Spoofing: OIDC/JWT with Azure AD; mTLS for service-to-service; managed identities
- Tampering: Append-only audit, hashing of custody artifacts, signed events
- Repudiation: Immutable logs (WORM), time-synced, user+tenant scoped
- Information Disclosure: ABAC with tenant/site/asset filters; field-level encryption for PII
- Denial of Service: WAF, rate limits at gateway, backpressure controls
- Elevation of Privilege: Least privilege RBAC, Just-In-Time admin, privileged access workstations

## AuthN/AuthZ Posture

- AuthN: Azure AD, MFA enforced; device compliance for admins
- AuthZ: RBAC + ABAC (tenant, site, asset, role); policy server with centralized decision logs

## Secrets Inventory

- Managed identities for all workloads where possible
- Azure Key Vault for secrets/certs; rotation ≤ 90 days; HSM-backed keys for signing
- Per-tenant data encryption keys; envelope encryption at rest

## Secure Development

- Dependency scanning, SAST/DAST in CI
- Signed container images; provenance (SLSA)


