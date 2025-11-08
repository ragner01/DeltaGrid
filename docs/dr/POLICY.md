# Disaster Recovery Policy

## Overview
This document defines the Disaster Recovery (DR) policy for DeltaGrid platform, including RTO/RPO targets, DR tiers, and recovery procedures.

## DR Tiers

### Tier 1: Critical
**RTO**: < 1 hour  
**RPO**: < 15 minutes

**Services**: API, SQL Database, Identity Provider, Gateway

**Requirements**:
- Geo-redundant storage configured
- Automated failover enabled
- Full backups every 6 hours
- Transaction log backups every 15 minutes
- DR drill every 3 months

### Tier 2: High
**RTO**: < 4 hours  
**RPO**: < 1 hour

**Services**: Ingestion, Time-Series, Optimization, Events

**Requirements**:
- Geo-redundant storage configured
- Manual failover
- Full backups daily
- Differential backups every 6 hours
- DR drill every 6 months

### Tier 3: Standard
**RTO**: < 24 hours  
**RPO**: < 4 hours

**Services**: Reporting, Search

**Requirements**:
- Full backups daily
- DR drill annually

### Tier 4: Low
**RTO**: < 72 hours  
**RPO**: < 24 hours

**Services**: Tools, Utilities

**Requirements**:
- Full backups weekly
- DR drill as needed

## Service Classifications

| Service | Tier | RTO | RPO | Geo-Redundant | Auto-Failover |
|---------|------|-----|-----|---------------|---------------|
| Web API | Critical | 1 hour | 15 minutes | Yes | Yes |
| SQL Database | Critical | 1 hour | 15 minutes | Yes | Yes |
| Identity Provider | Critical | 1 hour | 15 minutes | Yes | Yes |
| API Gateway | Critical | 1 hour | 15 minutes | Yes | Yes |
| OT Ingestion | High | 4 hours | 1 hour | Yes | No |
| Time-Series | High | 4 hours | 1 hour | Yes | No |
| Optimization | High | 4 hours | 1 hour | Yes | No |
| Events | High | 4 hours | 1 hour | Yes | No |
| Reporting | Standard | 24 hours | 4 hours | No | No |
| Search | Standard | 24 hours | 4 hours | No | No |

## Backup Strategy

### Database Backups
- **Full Backups**: Every 6 hours (Critical), Daily (High/Standard)
- **Differential Backups**: Every 6 hours (Critical), Every 12 hours (High)
- **Transaction Log Backups**: Every 15 minutes (Critical), Every 1 hour (High)
- **Retention**: 30 days (Critical), 14 days (High), 7 days (Standard)

### Storage Backups
- **Blob Storage**: Daily snapshots with 30-day retention
- **File Storage**: Weekly backups with 14-day retention
- **Encryption**: All backups encrypted at rest

### Configuration Backups
- **Infrastructure as Code**: Versioned in Git
- **Configuration Files**: Backed up daily with 30-day retention
- **Secrets**: Stored in Key Vault with geo-replication

## Failover Strategy

### Automatic Failover
- **Services**: Critical tier services
- **Trigger**: Primary region failure detected
- **Time**: < 5 minutes for failover
- **Validation**: Automatic health checks post-failover

### Manual Failover
- **Services**: High tier services
- **Trigger**: Manual command or detected failure
- **Time**: < 15 minutes for failover
- **Validation**: Manual health checks post-failover

### Test Failover
- **Frequency**: Quarterly for Critical, Semi-annually for High
- **Duration**: < 1 hour
- **Validation**: Full service validation post-failover

## Recovery Procedures

### Full Site Failure
1. **Detect Failure**: Automated detection within 5 minutes
2. **Failover to Secondary**: Automatic for Critical, Manual for High
3. **Restore Services**: From geo-redundant backups
4. **Validate Data**: Integrity checks on restored data
5. **Resume Operations**: Validate all services operational

### Database Failure
1. **Detect Failure**: Automated detection within 1 minute
2. **Failover Database**: Automatic failover group failover
3. **Restore from Backup**: If failover unavailable
4. **Replay Transaction Logs**: Catch up to latest data
5. **Validate Integrity**: Database integrity checks

### Storage Failure
1. **Detect Failure**: Automated detection within 5 minutes
2. **Failover Storage**: Switch to secondary storage account
3. **Restore from Backup**: If data loss detected
4. **Replay Events**: Replay missed events from Event Hubs
5. **Validate Integrity**: Storage integrity checks

### Network Failure
1. **Detect Failure**: Automated detection within 5 minutes
2. **Failover to Secondary Region**: Automatic for Critical services
3. **Validate Connectivity**: All services reachable
4. **Resume Operations**: Validate all services operational

### Service Failure
1. **Detect Failure**: Automated detection within 1 minute
2. **Restart Service**: Automatic restart (up to 3 attempts)
3. **Failover if Persistent**: Failover to secondary region
4. **Validate Service**: Health checks pass
5. **Resume Operations**: Service operational

## DR Drills

### Drill Schedule
- **Critical Services**: Every 3 months
- **High Services**: Every 6 months
- **Standard Services**: Annually
- **Low Services**: As needed

### Drill Types
- **Full Site Failure**: Simulate complete regional failure
- **Database Failure**: Simulate database failure
- **Storage Failure**: Simulate storage account failure
- **Network Failure**: Simulate network partition
- **Service Failure**: Simulate individual service failure

### Drill Execution
1. **Schedule Drill**: Plan drill date and participants
2. **Execute Drill**: Simulate disaster scenario
3. **Execute Recovery**: Follow recovery procedures
4. **Measure Metrics**: Record RTO/RPO actuals
5. **Verify Integrity**: Validate data integrity
6. **Postmortem**: Document findings and improvements

## Monitoring & Alerts

### DR Readiness Dashboard
- **Last Backup Time**: Per service
- **Last Restore Test**: Per service
- **Last DR Drill**: Per service
- **Geo-Redundancy Status**: Per service
- **Failover Configuration**: Per service

### Alerts
- **Backup Failure**: Alert on backup failure
- **Backup Overdue**: Alert if backup overdue (>RPO)
- **Restore Test Overdue**: Alert if restore test overdue (>90 days)
- **DR Drill Overdue**: Alert if DR drill overdue (>schedule)
- **Geo-Redundancy Not Configured**: Alert for Critical services

## Contact Tree

### Primary On-Call
- **Platform Team Lead**: +234-XXX-XXXX (24/7)
- **DevOps Engineer**: +234-XXX-XXXX (24/7)
- **Database Administrator**: +234-XXX-XXXX (24/7)

### Escalation
- **CTO**: +234-XXX-XXXX (Critical issues only)
- **Security Team**: +234-XXX-XXXX (Security incidents)

### External Contacts
- **Azure Support**: Premier support for Critical services
- **Nigerian Regulators**: DPR/NUPRC (regulatory incidents)

## Postmortem Template

### Incident Information
- **Incident ID**: [GUID]
- **Date**: [Date]
- **Duration**: [Duration]
- **Impact**: [Services affected]
- **Root Cause**: [Cause]

### Recovery Metrics
- **Actual RTO**: [Time]
- **Actual RPO**: [Time]
- **RTO Met**: [Yes/No]
- **RPO Met**: [Yes/No]
- **Data Integrity**: [Verified/Not Verified]

### Findings
- **What Went Well**: [Items]
- **What Went Wrong**: [Items]
- **What Could Be Improved**: [Items]

### Action Items
- **Immediate**: [Items]
- **Short-term**: [Items]
- **Long-term**: [Items]

### Lessons Learned
- [Lessons learned from incident]


