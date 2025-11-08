# CI/CD Pipelines and Deployment Guide

## Overview
DeltaGrid uses GitHub Actions for CI/CD with environment promotion (dev → staging → production) and automated security scanning.

## Pipeline Architecture

### Environment Promotion
1. **Development**: Auto-deploy on push to `develop` branch
2. **Staging**: Manual promotion with approval gate
3. **Production**: Manual promotion with canary deployment

### Pipeline Stages
1. **Build & Test**: Compile, run tests, generate code coverage
2. **Security Scanning**: SAST, DAST, SCA, secret scanning
3. **Container Build**: Build Docker images, push to ACR
4. **Infrastructure**: Deploy/update infrastructure via Terraform
5. **Database Migrations**: Run DbUp migrations
6. **Deploy Services**: Deploy containers to App Services
7. **Smoke Tests**: Verify deployment health

## Workflows

### 1. Deploy to Development (`deploy-dev.yml`)
- **Trigger**: Push to `develop` branch or manual
- **Stages**:
  - Build and test
  - Security scanning
  - Build containers
  - Deploy infrastructure
  - Run migrations
  - Deploy services
  - Smoke tests

### 2. Promote to Staging (`promote-staging.yml`)
- **Trigger**: Manual with version input
- **Approval**: Manual approval gate (optional skip)
- **Strategy**: Blue-green deployment
- **Stages**:
  - Pre-deployment checks
  - Deploy to staging slot
  - Integration tests
  - Swap slots
  - Rollback on failure

### 3. Promote to Production (`promote-production.yml`)
- **Trigger**: Manual with version and canary percentage
- **Approval**: Required production approval
- **Strategy**: Canary deployment → full rollout
- **Stages**:
  - Pre-deployment checks
  - Deploy canary (configurable %)
  - Monitor canary metrics (15 minutes)
  - Full rollout
  - Rollback on failure

## Deployment Strategies

### Blue-Green Deployment
- Deploy to staging slot (blue)
- Run integration tests
- Swap slots (blue → production)
- Rollback: Swap back to previous slot

### Canary Deployment
- Deploy canary slot with traffic routing
- Route X% of traffic to canary
- Monitor metrics (error rate, latency)
- If metrics good, full rollout
- If metrics bad, rollback canary

## Database Migrations

### DbUp Strategy
- **Location**: `src/Migrations/Scripts/`
- **Naming**: `V1_XXX_Description.sql`
- **Environment**: Prefix scripts with environment (e.g., `V1Dev`, `V1Prod`)
- **Versioning**: Sequential version numbers
- **Transaction**: Each migration runs in a transaction

### Running Migrations
```bash
# Local development
dotnet run --project src/Migrations \
  --connection-string "Server=...;Database=...;..."

# In CI/CD
dotnet run --project src/Migrations \
  --connection-string "${{ secrets.DEV_SQL_CONNECTION_STRING }}" \
  --environment dev
```

## Secrets Management

### OIDC Federation
- **Azure Login**: Uses OIDC for Azure authentication
- **Service Principals**: Least-privilege service principals
- **Key Vault**: Secrets fetched via managed identities
- **No Passwords**: No secrets in GitHub Actions

### Required Secrets
- `AZURE_CLIENT_ID`: Service principal client ID
- `AZURE_TENANT_ID`: Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID
- `ACR_LOGIN_SERVER`: Azure Container Registry URL
- `ACR_USERNAME`: ACR username (optional with managed identity)
- `ACR_PASSWORD`: ACR password (optional with managed identity)
- `DEV_SQL_CONNECTION_STRING`: Development SQL connection string
- `STAGING_SQL_CONNECTION_STRING`: Staging SQL connection string
- `PROD_SQL_CONNECTION_STRING`: Production SQL connection string

## Artifact Versioning

### Version Format
- Format: `YYYYMMDD-SHA`
- Example: `20250130-a1b2c3d4`
- Generated from: Date + Git SHA (first 8 chars)

### Artifact Provenance
- Docker images tagged with version
- Git tags for releases
- Artifact metadata in registry

## Rollback Procedures

### Automatic Rollback
- Pipeline detects failure during deployment
- Automatically rolls back to previous slot/version
- Notifies team of rollback

### Manual Rollback
```bash
# Rollback via Azure CLI
az webapp deployment slot swap \
  --name app-deltagrid-api-prod \
  --resource-group rg-deltagrid-prod \
  --slot production \
  --target-slot previous

# Rollback via pipeline (run rollback workflow)
gh workflow run rollback.yml -f version=previous-version
```

## Observability

### DORA Metrics
- **Deployment Frequency**: How often deployments occur
- **Change Failure Rate**: % of deployments that fail
- **MTTR (Mean Time to Recovery)**: Time to recover from failures
- **Lead Time**: Time from commit to production

### Monitoring
- Pipeline execution times
- Deployment success/failure rates
- Rollback frequency
- Infrastructure drift detection

## Runbooks

See `docs/deployment/RUNBOOKS.md` for detailed runbooks:
- Deploy to Development
- Promote to Staging
- Promote to Production
- Rollback Procedures
- Emergency Procedures

## Best Practices

1. **Always test in dev before staging**
2. **Use canary deployment for production**
3. **Monitor metrics during canary**
4. **Have rollback plan ready**
5. **Document all manual interventions**
6. **Review deployment logs regularly**
7. **Test rollback procedures quarterly**


