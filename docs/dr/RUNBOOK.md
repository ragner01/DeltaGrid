# Disaster Recovery Runbook

## Overview
This runbook provides step-by-step procedures for disaster recovery scenarios for DeltaGrid platform.

## Runbook Index

1. [Full Site Failure](#full-site-failure)
2. [Database Failure](#database-failure)
3. [Storage Failure](#storage-failure)
4. [Network Failure](#network-failure)
5. [Service Failure](#service-failure)
6. [DR Drill Execution](#dr-drill-execution)
7. [Backup Validation](#backup-validation)
8. [Failover Testing](#failover-testing)

## Full Site Failure

### Scenario
Complete regional failure affecting all services in primary region.

### Prerequisites
- Access to Azure Portal (secondary region)
- DR service access
- Contact tree activated

### Steps

1. **Verify Failure**
   ```bash
   # Check primary region status
   az account show --query location
   
   # Check service health
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   ```

2. **Activate DR Team**
   - Notify Platform Team Lead
   - Activate on-call rotation
   - Open incident ticket

3. **Check DR Readiness**
   ```bash
   # Check DR readiness status
   curl https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/readiness
   ```

4. **Execute Failover**
   ```bash
   # Failover all critical services
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/failover/{serviceId}/execute \
     -H "Authorization: Bearer $TOKEN"
   ```

5. **Restore from Backup** (if needed)
   ```bash
   # Restore services from backup
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/backups/{backupId}/restore-test \
     -H "Authorization: Bearer $TOKEN"
   ```

6. **Replay Events**
   ```bash
   # Replay ingestion events
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/replay/ingestion \
     -H "Authorization: Bearer $TOKEN" \
     -d '{"fromTime": "...", "toTime": "..."}'
   ```

7. **Validate Services**
   ```bash
   # Health check all services
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   curl https://app-deltagrid-gateway-prod.azurewebsites.net/health
   # ... check all services
   ```

8. **Validate Data Integrity**
   ```bash
   # Run integrity checks
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/backups/{backupId}/validate \
     -H "Authorization: Bearer $TOKEN"
   ```

9. **Resume Operations**
   - Validate all services operational
   - Notify stakeholders
   - Update status page

10. **Postmortem**
    - Document incident
    - Measure actual RTO/RPO
    - Identify improvements

### Success Criteria
- ✅ All services operational in secondary region
- ✅ RTO met (< 1 hour for Critical)
- ✅ RPO met (< 15 minutes for Critical)
- ✅ Data integrity verified
- ✅ All health checks pass

## Database Failure

### Scenario
SQL Database failure or corruption.

### Steps

1. **Verify Failure**
   ```bash
   # Check database status
   az sql db show --name db-deltagrid-prod --resource-group rg-deltagrid-prod
   ```

2. **Execute Database Failover**
   ```bash
   # Failover to secondary database
   az sql db replica set-failover-allow-data-loss \
     --name db-deltagrid-prod \
     --resource-group rg-deltagrid-prod \
     --partner-server sql-deltagrid-prod-secondary
   ```

3. **Restore from Backup** (if failover unavailable)
   ```bash
   # Restore from latest backup
   az sql db restore \
     --dest-name db-deltagrid-prod-restored \
     --resource-group rg-deltagrid-prod \
     --server sql-deltagrid-prod \
     --restore-point-in-time "2025-01-30T12:00:00Z"
   ```

4. **Replay Transaction Logs**
   ```bash
   # Replay transaction logs to latest point
   # (Azure SQL handles automatically via point-in-time restore)
   ```

5. **Validate Database Integrity**
   ```sql
   -- Run DBCC CHECKDB
   DBCC CHECKDB('db-deltagrid-prod') WITH NO_INFOMSGS;
   ```

6. **Update Connection Strings**
   ```bash
   # Update App Services to use restored database
   az webapp config connection-string set \
     --name app-deltagrid-api-prod \
     --resource-group rg-deltagrid-prod \
     --connection-string-type SQLAzure \
     --settings DefaultConnection="Server=...;Database=db-deltagrid-prod-restored;..."
   ```

7. **Validate Services**
   ```bash
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   ```

### Success Criteria
- ✅ Database operational
- ✅ RTO met (< 1 hour)
- ✅ RPO met (< 15 minutes)
- ✅ Data integrity verified
- ✅ All services connected

## Storage Failure

### Scenario
Storage account failure or data corruption.

### Steps

1. **Verify Failure**
   ```bash
   # Check storage account status
   az storage account show --name stdeltagridprod --resource-group rg-deltagrid-prod
   ```

2. **Failover Storage Account**
   ```bash
   # Failover to secondary storage account
   az storage account failover --name stdeltagridprod --resource-group rg-deltagrid-prod
   ```

3. **Restore from Backup** (if needed)
   ```bash
   # Restore from blob backup
   az storage blob copy start \
     --account-name stdeltagridprodbackup \
     --container-name backups \
     --source-blob backup-2025-01-30.blob \
     --destination-blob restored-blob
   ```

4. **Replay Events**
   ```bash
   # Replay ingestion events from Event Hubs
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/replay/ingestion \
     -H "Authorization: Bearer $TOKEN" \
     -d '{"fromTime": "...", "toTime": "..."}'
   ```

5. **Validate Storage Integrity**
   ```bash
   # Check blob checksums
   az storage blob show --name restored-blob --container-name data \
     --account-name stdeltagridprod
   ```

### Success Criteria
- ✅ Storage account operational
- ✅ RTO met (< 4 hours for High tier)
- ✅ RPO met (< 1 hour for High tier)
- ✅ Data integrity verified
- ✅ All services connected

## Network Failure

### Scenario
Network partition or connectivity failure.

### Steps

1. **Verify Failure**
   ```bash
   # Check network connectivity
   ping app-deltagrid-api-prod.azurewebsites.net
   ```

2. **Execute Failover**
   ```bash
   # Failover to secondary region
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/failover/{serviceId}/execute \
     -H "Authorization: Bearer $TOKEN"
   ```

3. **Validate Connectivity**
   ```bash
   # Check all services reachable
   curl https://app-deltagrid-api-prod-secondary.azurewebsites.net/health
   ```

### Success Criteria
- ✅ All services reachable in secondary region
- ✅ RTO met (< 1 hour for Critical)
- ✅ Network connectivity restored
- ✅ All services operational

## Service Failure

### Scenario
Individual service failure (API, Gateway, etc.).

### Steps

1. **Verify Failure**
   ```bash
   # Check service health
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   ```

2. **Restart Service**
   ```bash
   # Restart App Service
   az webapp restart --name app-deltagrid-api-prod --resource-group rg-deltagrid-prod
   ```

3. **Failover if Persistent**
   ```bash
   # Failover to secondary region if restart fails
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/failover/{serviceId}/execute \
     -H "Authorization: Bearer $TOKEN"
   ```

4. **Validate Service**
   ```bash
   curl https://app-deltagrid-api-prod.azurewebsites.net/health
   ```

### Success Criteria
- ✅ Service operational
- ✅ Health checks pass
- ✅ All dependencies connected

## DR Drill Execution

### Prerequisites
- DR drill scheduled
- Team notified
- Test environment ready

### Steps

1. **Schedule Drill**
   ```bash
   # Create DR drill
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/drills \
     -H "Authorization: Bearer $TOKEN" \
     -d '{
       "id": "drill-001",
       "name": "Quarterly Full Site Failure Drill",
       "type": "FullSiteFailure",
       "scheduledAt": "2025-02-01T10:00:00Z",
       "services": ["api", "sql", "gateway"]
     }'
   ```

2. **Execute Drill**
   ```bash
   # Execute drill
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/drills/drill-001/execute \
     -H "Authorization: Bearer $TOKEN"
   ```

3. **Monitor Progress**
   ```bash
   # Check drill status
   curl https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/drills/drill-001
   ```

4. **Measure Metrics**
   - Actual RTO: [Time]
   - Actual RPO: [Time]
   - RTO Met: [Yes/No]
   - RPO Met: [Yes/No]

5. **Postmortem**
   - Document findings
   - Identify improvements
   - Update runbooks

### Success Criteria
- ✅ Drill executed successfully
- ✅ RTO met for all services
- ✅ RPO met for all services
- ✅ Data integrity verified
- ✅ Postmortem completed

## Backup Validation

### Steps

1. **Execute Backup**
   ```bash
   # Trigger backup
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/backups/{backupId}/execute \
     -H "Authorization: Bearer $TOKEN"
   ```

2. **Validate Backup**
   ```bash
   # Validate backup integrity
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/backups/{backupId}/validate \
     -H "Authorization: Bearer $TOKEN"
   ```

3. **Run Restore Test**
   ```bash
   # Run restore test
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/backups/{backupId}/restore-test \
     -H "Authorization: Bearer $TOKEN"
   ```

4. **Verify Integrity**
   - Check restored data integrity
   - Validate checksums
   - Compare with source data

### Success Criteria
- ✅ Backup created successfully
- ✅ Backup integrity validated
- ✅ Restore test passed
- ✅ Data integrity verified

## Failover Testing

### Steps

1. **Test Failover** (Non-destructive)
   ```bash
   # Test failover connectivity
   curl -X POST https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/failover/{failoverId}/test \
     -H "Authorization: Bearer $TOKEN"
   ```

2. **Check Failover Status**
   ```bash
   # Check failover status
   curl https://dr-deltagrid-prod.azurewebsites.net/api/v1/dr/failover/{serviceId}/status
   ```

3. **Validate Health**
   ```bash
   # Check service health in secondary region
   curl https://app-deltagrid-api-prod-secondary.azurewebsites.net/health
   ```

### Success Criteria
- ✅ Failover test passed
- ✅ Services healthy in secondary region
- ✅ No data loss detected
- ✅ Connectivity verified

## Emergency Contacts

### Primary
- **Platform Team Lead**: [Phone]
- **DevOps Engineer**: [Phone]
- **Database Administrator**: [Phone]

### Escalation
- **CTO**: [Phone] (Critical only)
- **Azure Support**: Premier support portal

### External
- **DPR/NUPRC**: [Contact] (Regulatory)


