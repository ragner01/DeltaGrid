# DR Readiness Dashboard

## Overview
The DR Readiness Dashboard provides real-time visibility into the disaster recovery status of all DeltaGrid services.

## Dashboard Components

### 1. Service Status
- **Total Services**: Count of all services
- **Ready Services**: Services meeting all DR requirements
- **Warning Services**: Services with minor DR issues
- **Critical Services**: Services with critical DR issues
- **Readiness Percentage**: % of services ready for disaster recovery

### 2. Service-by-Service Status
For each service:
- **Service Name**: Service identifier
- **DR Tier**: Critical, High, Standard, or Low
- **Readiness Level**: Ready, Warning, Critical, or Unknown
- **Last Backup**: Timestamp of last backup
- **Last Restore Test**: Timestamp of last restore test
- **Last DR Drill**: Timestamp of last DR drill
- **Geo-Redundant**: Yes/No
- **Failover Configured**: Yes/No
- **Issues**: List of DR issues

### 3. Recent DR Drills
- **Drill Name**: Name of DR drill
- **Type**: Full Site Failure, Database Failure, etc.
- **Date**: Scheduled/executed date
- **Status**: Scheduled, In Progress, Completed, Failed
- **RTO Met**: Yes/No
- **RPO Met**: Yes/No

### 4. Recent Backups
- **Service**: Service identifier
- **Last Backup**: Timestamp of last backup
- **Status**: Scheduled, In Progress, Completed, Failed
- **Validation**: Passed/Failed

## Metrics

### RTO/RPO Metrics
- **RTO Target**: Per service tier
- **RPO Target**: Per service tier
- **Actual RTO**: Measured during drills
- **Actual RPO**: Measured during drills
- **RTO Met %**: Percentage of drills meeting RTO
- **RPO Met %**: Percentage of drills meeting RPO

### Backup Metrics
- **Backup Success Rate**: % of successful backups
- **Backup Validation Rate**: % of validated backups
- **Restore Test Success Rate**: % of successful restore tests
- **Average Backup Duration**: Average time for backup

### Failover Metrics
- **Failover Success Rate**: % of successful failovers
- **Failover Test Success Rate**: % of successful failover tests
- **Average Failover Time**: Average time for failover

## Alerts

### Critical Alerts
- **Backup Failure**: Alert on backup failure
- **Backup Overdue**: Alert if backup overdue (>RPO)
- **Restore Test Overdue**: Alert if restore test overdue (>90 days)
- **DR Drill Overdue**: Alert if DR drill overdue (>schedule)
- **Geo-Redundancy Not Configured**: Alert for Critical services without geo-redundancy

### Warning Alerts
- **Backup Validation Failed**: Alert if backup validation fails
- **Restore Test Failed**: Alert if restore test fails
- **DR Drill Failed**: Alert if DR drill fails
- **RTO Not Met**: Alert if actual RTO > target RTO
- **RPO Not Met**: Alert if actual RPO > target RPO

## API Endpoints

### GET /api/v1/dr/dashboard
Get complete DR dashboard data.

Response:
```json
{
  "serviceStatuses": [
    {
      "serviceId": "api",
      "tier": "Critical",
      "level": "Ready",
      "lastBackupAt": "2025-01-30T12:00:00Z",
      "lastRestoreTestAt": "2025-01-15T10:00:00Z",
      "lastDrillAt": "2025-01-01T09:00:00Z",
      "geoRedundantConfigured": true,
      "failoverConfigured": true,
      "backupValidationPassed": true,
      "issues": []
    }
  ],
  "metrics": {
    "totalServices": 10,
    "readyServices": 8,
    "warningServices": 2,
    "criticalServices": 0,
    "readinessPercentage": 80.0,
    "servicesByTier": {
      "Critical": 4,
      "High": 4,
      "Standard": 2
    },
    "servicesByLevel": {
      "Ready": 8,
      "Warning": 2
    }
  },
  "recentDrills": [
    {
      "id": "drill-001",
      "name": "Q1 Full Site Failure Drill",
      "type": "FullSiteFailure",
      "scheduledAt": "2025-01-01T09:00:00Z",
      "status": "Completed",
      "metRto": true,
      "metRpo": true
    }
  ],
  "recentBackups": [
    {
      "id": "backup-001",
      "serviceId": "sql",
      "lastBackupAt": "2025-01-30T12:00:00Z",
      "status": "Completed",
      "validationPassed": true
    }
  ],
  "generatedAt": "2025-01-30T12:00:00Z"
}
```

### GET /api/v1/dr/metrics
Get DR metrics summary.

Response:
```json
{
  "totalServices": 10,
  "readyServices": 8,
  "warningServices": 2,
  "criticalServices": 0,
  "readinessPercentage": 80.0,
  "servicesByTier": {
    "Critical": 4,
    "High": 4,
    "Standard": 2
  },
  "servicesByLevel": {
    "Ready": 8,
    "Warning": 2
  }
}
```

### GET /api/v1/dr/readiness
Get DR readiness status for all services.

Response:
```json
[
  {
    "serviceId": "api",
    "tier": "Critical",
    "level": "Ready",
    "lastBackupAt": "2025-01-30T12:00:00Z",
    "lastRestoreTestAt": "2025-01-15T10:00:00Z",
    "lastDrillAt": "2025-01-01T09:00:00Z",
    "geoRedundantConfigured": true,
    "failoverConfigured": true,
    "backupValidationPassed": true,
    "issues": []
  }
]
```

## Monitoring

### Dashboard Refresh
- **Frequency**: Every 5 minutes
- **Source**: DR service readiness status
- **Alerts**: Push alerts on readiness level changes

### Metrics Collection
- **Frequency**: Every 1 minute
- **Storage**: Azure Monitor / Log Analytics
- **Retention**: 90 days

## Integration

### Azure Monitor Integration
```csharp
// Send DR readiness metrics to Azure Monitor
var meter = new Meter("IOC.DR", "1.0.0");
var readinessGauge = meter.CreateObservableGauge<double>("dr_readiness_percentage", () =>
{
    var metrics = await dashboard.GetMetricsAsync();
    return new Measurement<double>(metrics.ReadinessPercentage);
});
```

### Grafana Dashboard
- **DR Readiness Dashboard**: Visual dashboard of DR status
- **RTO/RPO Trends**: Historical RTO/RPO metrics
- **Backup Success Rate**: Backup success trends
- **Failover Success Rate**: Failover success trends


