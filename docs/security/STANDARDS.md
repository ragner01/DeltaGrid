# DeltaGrid Security Standards

## Overview
This document defines security standards and practices for DeltaGrid platform. All services must comply with these standards for production deployment.

## Security Principles

### Defense in Depth
- Multiple layers of security controls
- No single point of failure
- Assume breach mindset

### Least Privilege
- Users and services have minimum required permissions
- Role-based access control (RBAC) enforced
- Regular access reviews

### Secure by Default
- HTTPS/HSTS everywhere
- Security headers on all endpoints
- Private endpoints for data stores
- No public exposure of sensitive services

### Zero Trust
- Verify identity and authorization for all requests
- Assume network is compromised
- Encrypt data in transit and at rest

## Network Security

### HTTPS/HSTS
- **Requirement**: All external-facing endpoints must use HTTPS
- **HSTS**: Strict-Transport-Security header with max-age ≥ 1 year
- **Implementation**: Middleware enforced (see `SecurityHeadersMiddleware`)

### CORS
- **Requirement**: Strict CORS policy
- **Allowed Origins**: Whitelist only (no wildcards)
- **Allowed Methods**: Minimum required (GET, POST, PUT, DELETE)
- **Credentials**: Allow only when necessary

### Private Endpoints
- **Requirement**: All data stores (SQL, Key Vault, Storage) use private endpoints
- **Implementation**: Azure Private Link
- **Exception**: Public endpoints only with business justification

### Network Segmentation
- **Requirement**: Services isolated in private subnets
- **Implementation**: Virtual Network (VNet) with subnet isolation
- **NSG Rules**: Deny by default, allow only required traffic

## Authentication & Authorization

### JWT Hardening
- **Requirement**: JWT tokens validated with strict parameters
- **Issuer Validation**: Required
- **Audience Validation**: Required
- **Lifetime Validation**: Required, no clock skew tolerance
- **Algorithm Pinning**: Only allow approved algorithms (RS256, HS256)
- **Key Storage**: Azure Key Vault (never in code or config)

### Key Rotation
- **Requirement**: JWT signing keys rotated every 90 days
- **Overlap Period**: 7 days for graceful transition
- **Implementation**: Automated rotation via `JwtKeyRotationService`
- **Verification**: Key rotation tested in staging before production

### Managed Identities
- **Requirement**: Azure services use Managed Identity for Key Vault access
- **Implementation**: System-assigned managed identities
- **Exception**: Local development may use Azure CLI credentials

## Secrets Management

### Key Vault
- **Requirement**: All secrets stored in Azure Key Vault
- **Access Policy**: Least privilege (only required services)
- **Private Endpoint**: Required for production
- **Soft Delete**: Enabled (7-day retention)
- **Purge Protection**: Enabled

### Secret Scanning
- **Requirement**: CI pipeline scans for secrets in code
- **Tools**: TruffleHog, GitGuardian, or similar
- **Blocking**: Fail CI if secrets detected
- **Frequency**: On every commit and weekly scan

### Secret Rotation
- **Requirement**: Secrets rotated according to schedule
- **Database Passwords**: Every 90 days
- **API Keys**: Every 180 days
- **JWT Signing Keys**: Every 90 days
- **Certificates**: Before expiration (30-day buffer)

## Data Protection

### Encryption at Rest
- **Requirement**: All data encrypted at rest
- **SQL Database**: Transparent Data Encryption (TDE) enabled
- **Storage Accounts**: Encryption enabled
- **Backups**: Encrypted

### Encryption in Transit
- **Requirement**: All traffic encrypted (TLS 1.2+)
- **TLS Version**: Minimum TLS 1.2, preferred TLS 1.3
- **Certificate**: Valid, non-expired certificates
- **Implementation**: HTTPS everywhere, mTLS for service-to-service

### PII Handling
- **Requirement**: PII minimized and tokenized where applicable
- **Redaction**: PII redacted in logs (see `PiiScrubber`)
- **Access**: PII only accessible to authorized roles
- **Retention**: PII retained only as required by regulation

## Application Security

### Security Headers
- **Requirement**: Security headers on all endpoints
- **Headers**: HSTS, X-Frame-Options, X-Content-Type-Options, CSP, Referrer-Policy
- **Implementation**: `SecurityHeadersMiddleware`

### Input Validation
- **Requirement**: All inputs validated and sanitized
- **Implementation**: FluentValidation rules
- **SQL Injection**: Parameterized queries only (EF Core)
- **XSS**: Output encoding (Razor automatic)

### Rate Limiting
- **Requirement**: Rate limiting on all public endpoints
- **Implementation**: Per-tenant rate limits at gateway
- **Thresholds**: Configurable per endpoint and tenant tier

### Error Handling
- **Requirement**: No sensitive information in error messages
- **Implementation**: RFC 7807 ProblemDetails
- **Logging**: Errors logged with PII scrubbing

## Audit & Compliance

### Admin Action Audit Log
- **Requirement**: All admin actions logged
- **Fields**: User, action, resource, timestamp, IP, user agent
- **Retention**: 7 years for compliance
- **Access**: Read-only, immutable logs

### Access Logs
- **Requirement**: Access to sensitive resources logged
- **Examples**: Report downloads, permit archives, lab results
- **Retention**: 1 year minimum

### Threat Modeling
- **Requirement**: STRIDE threat model maintained
- **Frequency**: Updated quarterly or on major changes
- **Mitigation**: All threats have mitigation status tracked
- **Review**: Security team reviews quarterly

## Dependency Management

### Vulnerability Scanning
- **Requirement**: Dependencies scanned for vulnerabilities
- **Tools**: OWASP Dependency-Check, Snyk, or similar
- **Frequency**: On every build and weekly
- **Blocking**: Fail CI if critical vulnerabilities found

### Dependency Updates
- **Requirement**: Dependencies kept up to date
- **Frequency**: Monthly review, quarterly updates
- **Security Patches**: Applied within 30 days of release
- **Testing**: Full regression test after updates

## Security Testing

### Penetration Testing
- **Requirement**: Annual penetration testing
- **Scope**: External-facing endpoints, authentication, authorization
- **Remediation**: Critical findings remediated within 30 days
- **Report**: Findings documented in threat model

### DAST (Dynamic Application Security Testing)
- **Requirement**: DAST scans in CI/CD pipeline
- **Tools**: OWASP ZAP, Burp Suite, or similar
- **Frequency**: On every deployment to staging
- **Blocking**: Fail deployment if critical findings

### SAST (Static Application Security Testing)
- **Requirement**: SAST scans in CI/CD pipeline
- **Tools**: SonarQube, Security Code Scan, or similar
- **Frequency**: On every commit
- **Blocking**: Fail CI if critical findings

## Deployment Security

### Container Security
- **Requirement**: Containers scanned for vulnerabilities
- **Tools**: Trivy, Snyk, or similar
- **Base Images**: Official Microsoft base images only
- **Updates**: Base images updated quarterly

### Infrastructure as Code
- **Requirement**: All infrastructure defined in code (Terraform/Bicep)
- **Version Control**: Infrastructure code in Git
- **Review**: Infrastructure changes require security review
- **Deployment**: Infrastructure deployed via CI/CD

### CI/CD Security
- **Requirement**: CI/CD pipeline secrets in secure vault
- **Access**: Limited to authorized personnel
- **Audit**: CI/CD actions audited
- **Approval**: Production deployments require approval

## Incident Response

### Security Incident Response Plan
- **Requirement**: Security incident response plan documented
- **Contacts**: Security team contacts available 24/7
- **Procedures**: Escalation procedures documented
- **Testing**: Incident response tested annually

### Vulnerability Disclosure
- **Requirement**: Responsible disclosure process
- **Contact**: security@deltagrid.com (example)
- **Response**: Acknowledge within 48 hours
- **Remediation**: Critical vulnerabilities remediated within 30 days

## Exceptions Register

### Exception Process
- **Exception**: Deviation from security standards requires justification
- **Approval**: Security team approval required
- **Documentation**: Exception documented with rationale and mitigation
- **Review**: Exceptions reviewed quarterly

### Current Exceptions
| ID | Component | Exception | Justification | Mitigation | Review Date |
|----|-----------|-----------|---------------|------------|-------------|
| None | - | - | - | - | - |

## Compliance

### Nigerian Regulatory Requirements
- **DPR**: Department of Petroleum Resources compliance
- **NUPRC**: Nigerian Upstream Petroleum Regulatory Commission compliance
- **NOSDRA**: National Oil Spill Detection and Response Agency reporting
- **Data Protection**: Nigeria Data Protection Regulation (NDPR) compliance

### International Standards
- **ISO 27001**: Information Security Management System
- **SOC 2**: Security, Availability, Processing Integrity
- **OWASP Top 10**: OWASP security best practices

## Training & Awareness

### Security Training
- **Requirement**: All developers complete security training
- **Frequency**: Annual refresher training
- **Topics**: Secure coding, threat modeling, incident response

### Security Champions
- **Requirement**: Security champions in each team
- **Role**: Advocate for security, review security practices
- **Training**: Security champions receive additional training

## Review & Updates

### Security Standards Review
- **Frequency**: Quarterly review of security standards
- **Process**: Security team reviews and updates standards
- **Communication**: Changes communicated to all teams

### Last Updated
- **Date**: 2025-01-30
- **Version**: 1.0
- **Next Review**: 2025-04-30


