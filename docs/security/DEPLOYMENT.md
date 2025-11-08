# Phase 24 — Production Deployment & Security Hardening

## Overview
Phase 24 combines production deployment infrastructure with security hardening and secrets management. This phase ensures DeltaGrid is ready for production with secure defaults, operational security controls, and deployment automation.

## Components

### 1. Containerization
- **Dockerfiles**: Base Dockerfile template for all services
- **Docker Compose**: Local development environment with all services
- **Kubernetes Manifests**: Production deployment manifests (future)

### 2. Infrastructure as Code
- **Terraform**: Azure infrastructure provisioning
- **Bicep**: Alternative IaC for Azure (future)
- **Resources**: Key Vault, App Services, SQL Database, Private Endpoints

### 3. CI/CD Pipelines
- **GitHub Actions**: Build, test, deploy pipelines
- **Security Scanning**: SAST, DAST, SCA, secret scanning
- **Automated Testing**: Unit, integration, security tests

### 4. Security Hardening
- **Security Headers**: HSTS, CSP, X-Frame-Options, etc.
- **JWT Hardening**: Algorithm pinning, key rotation, strict validation
- **HTTPS/HSTS**: Everywhere enforced
- **CORS**: Strict policy with whitelist

### 5. Secrets Management
- **Azure Key Vault**: All secrets stored in Key Vault
- **Managed Identities**: Services use managed identities for Key Vault access
- **Key Rotation**: Automated key rotation playbooks
- **Secret Scanning**: CI pipeline scans for secrets in code

### 6. Threat Modeling
- **STRIDE Analysis**: Threat model maintained and updated
- **Threat Registry**: All threats tracked with mitigation status
- **Mitigation Tracking**: Threat mitigation status tracked

### 7. Admin Audit Logging
- **Admin Actions**: All admin actions logged
- **Audit Trail**: Full audit trail for compliance
- **Access Logs**: Access to sensitive resources logged

## Security Standards

See `docs/security/STANDARDS.md` for comprehensive security standards and practices.

### Key Requirements
- HTTPS/HSTS everywhere
- JWT hardening with key rotation
- Key Vault-managed secrets
- Private endpoints for data stores
- Admin action audit logs
- No critical findings in security scans
- Keys rotated successfully in staging

## Deployment

### Local Development
```bash
# Build and run with Docker Compose
docker-compose -f infrastructure/docker-compose.yml up -d

# Or run individual services
dotnet run --project src/WebApi
```

### Azure Deployment
```bash
# Initialize Terraform
cd infrastructure/terraform
terraform init

# Plan deployment
terraform plan

# Apply infrastructure
terraform apply

# Deploy services (via CI/CD or manually)
# See .github/workflows/deploy.yml (to be created)
```

## Security Scanning

### CI/CD Integration
- **Secret Scanning**: TruffleHog on every commit
- **Dependency Scanning**: OWASP Dependency-Check weekly
- **Code Scanning**: Security Code Scan on every build
- **Security Audit**: dotnet list package vulnerabilities

### Manual Scanning
```bash
# Run secret scanning
trufflehog filesystem .

# Run dependency scanning
dependency-check.sh --project DeltaGrid --scan .

# Run code scanning
dotnet build
security-scan --project IOC.sln
```

## Key Rotation

### JWT Signing Keys
```csharp
// Rotate JWT signing key
var rotationService = serviceProvider.GetRequiredService<IJwtKeyRotationService>();
await rotationService.RotateKeyAsync();
```

### Database Passwords
```bash
# Rotate database password in Key Vault
az keyvault secret set --vault-name kv-deltagrid-prod --name sql-admin-password --value <new-password>

# Update connection strings in App Services
az webapp config connection-string set --name app-deltagrid-api-prod --connection-string-type SQLAzure --settings DefaultConnection="Server=...;Password=<new-password>"
```

### API Keys
```bash
# Rotate API key in Key Vault
az keyvault secret set --vault-name kv-deltagrid-prod --name api-key --value <new-key>

# Update configuration in App Services
az webapp config appsettings set --name app-deltagrid-api-prod --settings API_KEY=@Microsoft.KeyVault(SecretUri=https://kv-deltagrid-prod.vault.azure.net/secrets/api-key/)
```

## Threat Model Updates

### Threat Registration
```csharp
// Register a new threat
var threatRegistry = serviceProvider.GetRequiredService<IThreatModelRegistry>();
await threatRegistry.RegisterThreatAsync(new Threat
{
    Id = "threat-new-vulnerability",
    Component = "API Gateway",
    Type = ThreatType.Spoofing,
    Description = "New vulnerability description",
    Severity = Severity.High,
    Mitigation = new Mitigation
    {
        Description = "Mitigation description",
        Status = MitigationStatus.InProgress,
        Controls = new List<string> { "Control 1", "Control 2" }
    }
});
```

### Threat Mitigation Update
```csharp
// Update threat mitigation status
await threatRegistry.UpdateMitigationAsync(
    "threat-new-vulnerability",
    MitigationStatus.Mitigated,
    "Mitigation completed on 2025-01-30"
);
```

## Admin Audit Logging

### Log Admin Action
```csharp
// Log admin action
var auditLogger = serviceProvider.GetRequiredService<IAdminAuditLogger>();
await auditLogger.LogAdminActionAsync(
    user,
    "CREATE_USER",
    "User",
    userId: "user-123",
    success: true
);
```

### Query Admin Logs
```csharp
// Query admin action logs
var logs = await auditLogger.QueryLogsAsync(new AdminAuditQuery
{
    UserId = "user-123",
    Action = "CREATE_USER",
    FromDate = DateTimeOffset.UtcNow.AddDays(-30)
});
```

## Acceptance Criteria

### Security
- ✅ No critical findings in security scans (SAST, DAST, SCA)
- ✅ Keys rotated successfully in staging
- ✅ All secrets in Key Vault
- ✅ Private endpoints configured for data stores
- ✅ HTTPS/HSTS enforced everywhere
- ✅ Security headers on all endpoints

### Deployment
- ✅ All services containerized (Dockerfiles)
- ✅ Docker Compose for local development
- ✅ Terraform infrastructure as code
- ✅ CI/CD pipelines with security scanning
- ✅ Admin audit logging operational
- ✅ Threat model maintained and updated

### Testing
- ✅ Security tests pass
- ✅ Penetration test remediation completed
- ✅ Dependency vulnerabilities resolved
- ✅ Secret scanning passes
- ✅ Key rotation tested in staging

## Next Steps

1. **Kubernetes Deployment**: Create Kubernetes manifests for production
2. **Bicep Templates**: Add Bicep templates as alternative to Terraform
3. **Blue-Green Deployment**: Implement blue-green deployment strategy
4. **Disaster Recovery**: Set up disaster recovery plan
5. **Security Monitoring**: Set up security event dashboards

## References

- [Security Standards](STANDARDS.md)
- [Infrastructure as Code](../infrastructure/terraform/main.tf)
- [CI/CD Pipelines](../../.github/workflows/)
- [Docker Compose](../infrastructure/docker-compose.yml)


