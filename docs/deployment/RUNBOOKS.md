# Deployment Runbooks

## Overview
Runbooks for deploying DeltaGrid across environments with rollback procedures.

## Runbook 1: Deploy to Development

### Prerequisites
- Code merged to `develop` branch
- CI/CD pipeline configured
- Azure resources provisioned

### Steps
1. **Trigger Pipeline**
   ```bash
   # Automatic on push to develop, or manual:
   gh workflow run deploy-dev.yml
   ```

2. **Monitor Pipeline**
   - Watch GitHub Actions UI
   - Check build/test results
   - Verify security scans pass

3. **Verify Deployment**
   ```bash
   curl https://app-deltagrid-api-dev.azurewebsites.net/health
   ```

4. **Rollback (if needed)**
   ```bash
   # Revert to previous commit
   git revert HEAD
   git push origin develop
   ```

### Success Criteria
- ✅ All tests pass
- ✅ Security scans pass
- ✅ Health endpoints respond
- ✅ Smoke tests pass

## Runbook 2: Promote to Staging

### Prerequisites
- Version tested in development
- Version number identified
- Approval obtained (if required)

### Steps
1. **Trigger Promotion**
   ```bash
   gh workflow run promote-staging.yml -f version=20250130-a1b2c3d4
   ```

2. **Wait for Approval**
   - Manual approval gate (unless skipped)
   - Approve via GitHub Actions UI

3. **Monitor Deployment**
   - Watch blue-green deployment
   - Check integration tests
   - Verify slot swap

4. **Verify Deployment**
   ```bash
   curl https://app-deltagrid-api-staging.azurewebsites.net/health
   ```

5. **Rollback (if needed)**
   ```bash
   # Pipeline automatically rolls back on failure
   # Or manual rollback:
   az webapp deployment slot swap \
     --name app-deltagrid-api-staging \
     --resource-group rg-deltagrid-staging \
     --slot production \
     --target-slot staging
   ```

### Success Criteria
- ✅ Pre-deployment checks pass
- ✅ Integration tests pass
- ✅ Blue-green swap successful
- ✅ Health endpoints respond

## Runbook 3: Promote to Production

### Prerequisites
- Version tested in staging for ≥24 hours
- Production approval obtained
- Canary percentage decided (default: 10%)

### Steps
1. **Trigger Promotion**
   ```bash
   gh workflow run promote-production.yml \
     -f version=20250130-a1b2c3d4 \
     -f canary_percentage=10
   ```

2. **Wait for Production Approval**
   - Required production approval gate
   - Approve via GitHub Actions UI

3. **Monitor Canary**
   - Check error rates (target: <0.1%)
   - Check latency (target: P95 < 500ms)
   - Monitor for 15 minutes
   - Review metrics dashboard

4. **Full Rollout (if canary succeeds)**
   - Pipeline automatically proceeds to full rollout
   - Monitor production metrics

5. **Rollback (if needed)**
   ```bash
   # Pipeline automatically rolls back on failure
   # Or manual rollback:
   az webapp deployment slot swap \
     --name app-deltagrid-api-prod \
     --resource-group rg-deltagrid-prod \
     --slot production \
     --target-slot previous
   
   # Also rollback canary traffic to 0%
   az network traffic-manager endpoint update \
     --name api-canary \
     --profile-name tm-deltagrid-prod \
     --resource-group rg-deltagrid-prod \
     --weight 0
   ```

### Success Criteria
- ✅ Pre-deployment checks pass
- ✅ Canary metrics acceptable
- ✅ Full rollout successful
- ✅ Production health endpoints respond

## Runbook 4: Emergency Rollback

### When to Use
- Critical production issue
- High error rate (>1%)
- Performance degradation
- Data corruption risk

### Steps
1. **Stop Traffic to Canary** (if active)
   ```bash
   az network traffic-manager endpoint update \
     --name api-canary \
     --profile-name tm-deltagrid-prod \
     --resource-group rg-deltagrid-prod \
     --weight 0
   ```

2. **Rollback Deployment**
   ```bash
   az webapp deployment slot swap \
     --name app-deltagrid-api-prod \
     --resource-group rg-deltagrid-prod \
     --slot production \
     --target-slot previous
   ```

3. **Verify Rollback**
   ```bash
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   # Check metrics dashboard
   ```

4. **Notify Team**
   - Post in incident channel
   - Document rollback reason
   - Schedule post-mortem

### Success Criteria
- ✅ Previous version restored
- ✅ Error rates return to normal
- ✅ Health endpoints respond
- ✅ Team notified

## Runbook 5: Database Migration

### Prerequisites
- Migration scripts reviewed
- Backup taken
- Migration tested in staging

### Steps
1. **Backup Database**
   ```bash
   az sql db export \
     --resource-group rg-deltagrid-prod \
     --server sql-deltagrid-prod \
     --name db-deltagrid-prod \
     --admin-user <admin> \
     --admin-password <password> \
     --storage-key-type StorageAccessKey \
     --storage-key <key> \
     --storage-uri https://<storage>.blob.core.windows.net/backups/prod-$(date +%Y%m%d).bacpac
   ```

2. **Run Migration**
   ```bash
   dotnet run --project src/Migrations \
     --connection-string "$PROD_SQL_CONNECTION_STRING" \
     --environment prod
   ```

3. **Verify Migration**
   ```sql
   -- Check schema version
   SELECT * FROM deltagrid.SchemaVersion ORDER BY Version DESC;
   
   -- Verify tables exist
   SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('deltagrid');
   ```

4. **Rollback (if needed)**
   ```bash
   # Restore from backup
   az sql db import \
     --resource-group rg-deltagrid-prod \
     --server sql-deltagrid-prod \
     --name db-deltagrid-prod \
     --admin-user <admin> \
     --admin-password <password> \
     --storage-key-type StorageAccessKey \
     --storage-key <key> \
     --storage-uri https://<storage>.blob.core.windows.net/backups/prod-YYYYMMDD.bacpac
   ```

### Success Criteria
- ✅ Backup completed
- ✅ Migration successful
- ✅ Schema version updated
- ✅ Application functions correctly

## Runbook 6: Infrastructure Drift Detection

### When to Run
- Weekly automated check
- After infrastructure changes
- Before major deployments

### Steps
1. **Terraform Plan**
   ```bash
   cd infrastructure/terraform
   terraform plan
   ```

2. **Review Drift**
   - Check for unexpected changes
   - Verify no manual changes made

3. **Remediate Drift**
   ```bash
   # If drift detected, apply Terraform
   terraform apply -auto-approve
   ```

4. **Document Drift**
   - Record what changed
   - Document why drift occurred
   - Update runbook if needed

### Success Criteria
- ✅ No unexpected drift
- ✅ Infrastructure matches code
- ✅ Drift documented (if found)

## Emergency Contacts

- **On-Call Engineer**: Check PagerDuty/OnCall rotation
- **Platform Team**: platform@deltagrid.com
- **Security Team**: security@deltagrid.com
- **Escalation**: CTO (for critical issues)

## Post-Deployment Checklist

- [ ] Health endpoints responding
- [ ] Error rates normal
- [ ] Performance metrics acceptable
- [ ] No security alerts
- [ ] Logs showing normal operation
- [ ] Team notified of deployment
- [ ] Documentation updated


