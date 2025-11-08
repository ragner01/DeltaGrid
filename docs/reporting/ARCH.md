# Reporting, Scheduling, and Compliance Packs Architecture

## Overview
Phase 23 delivers operational and regulatory reporting with scheduling, PDF/Excel export, signature workflows, and compliance variants by region (Nigerian O&G context).

## Architecture

### Components
1. **Template Engine**: Renders parameterized report templates (JSON-based, extensible)
2. **Exporters**: PDF (QuestPDF), Excel (ClosedXML), CSV exporters
3. **Report Service**: Generates reports, manages signatures, archives
4. **Scheduler**: Hangfire-based recurring report generation and distribution
5. **Archive**: Immutable report archive with access logs

### Report Types
- **Daily Production**: Daily production summaries by site/asset
- **Drilling Operations**: Drilling activity reports
- **HSE KPIs**: Health, Safety, Environment key performance indicators
- **Flaring**: Flare volumes and compliance
- **Losses**: Production losses and deferments
- **Deferments**: Production deferment tracking
- **OPEX**: Operational expenditure reports
- **Custody Transfer**: Custody transfer summaries
- **Allocation**: Production allocation reports
- **Integrity**: Integrity inspection reports
- **Lab Results**: Laboratory analysis summaries
- **Compliance**: Regulatory compliance packs

### Template System
- **Versioned Templates**: Templates have versions for backward compatibility
- **Parameterized**: Templates accept parameters (date, site, asset, etc.)
- **Region-Specific**: Compliance variants by region (Nigeria, etc.)
- **JSON-Based**: Simple JSON template format (extensible to YAML/Markdown)

### Export Formats
- **PDF**: QuestPDF with watermarking support
- **Excel**: ClosedXML with formulas and formatting
- **CSV**: Simple CSV export for data analysis
- **HTML**: Future enhancement for web viewing

### Signature Workflow
1. Report generated with `RequireSignature = true`
2. Report status: `PendingApproval`
3. Signers approve via `/reports/{id}/sign` endpoint
4. Signature hash computed (HMAC-SHA256 of report + signer)
5. Report status updated to `Approved` when all required signatures present
6. Report published/archived

### Scheduling
- **Hangfire**: Recurring jobs with cron expressions
- **Distribution Lists**: Email addresses for automatic distribution
- **Access Checks**: Role-based access to scheduled reports
- **Immediate Trigger**: Manual trigger of scheduled reports

### Archive
- **Immutable**: Archived reports cannot be modified
- **Access Logs**: All access (viewed, downloaded, exported) logged
- **Audit Trail**: Full audit trail for compliance

### Watermarking
- **DRAFT**: Applied when signature required
- **CONFIDENTIAL**: Optional watermark for sensitive reports
- **Custom**: Configurable watermark text

### Security
- **Tenant Isolation**: Reports scoped by tenant
- **Role-Based Access**: Reports require specific roles
- **Access Logging**: All access logged for audit
- **Signature Verification**: Signatures verified via HMAC

## Configuration

```json
{
  "Hangfire": {
    "ConnectionString": "Server=...;Database=Hangfire;...",
    "DashboardPath": "/hangfire"
  },
  "Reporting": {
    "WatermarkText": "DRAFT",
    "ArchiveRetentionDays": 3650,
    "SignatureRequiredRoles": ["ProductionEngineer", "HSELead", "Admin"]
  }
}
```

## API Endpoints

### POST /api/v1/reports/generate
Generate a report from a template.

Request:
```json
{
  "templateId": "daily-prod-1",
  "tenantId": "tenant-1",
  "parameters": {
    "date": "2025-10-30",
    "siteId": "site-1"
  },
  "format": "PDF",
  "requireSignature": false,
  "distributionList": ["ops@deltagrid.com"]
}
```

Response:
```json
{
  "reportId": "report-123",
  "fileName": "report_daily-prod-1_20251030120000.pdf",
  "status": "Published"
}
```

### GET /api/v1/reports/{reportId}
Download a generated report (logs access).

Response: File download (PDF/Excel/CSV)

### POST /api/v1/reports/{reportId}/sign
Sign a report for approval.

Request:
```json
{
  "comment": "Approved"
}
```

Response:
```json
{
  "signatureId": "abc123...",
  "signedAt": "2025-10-30T12:00:00Z"
}
```

### POST /api/v1/reports/{reportId}/archive
Archive a report (immutable).

### GET /api/v1/reports/{reportId}/archive
Retrieve archived report with access logs.

### POST /api/v1/reports/schedule
Schedule a recurring report.

Request:
```json
{
  "id": "daily-prod-schedule",
  "templateId": "daily-prod-1",
  "tenantId": "tenant-1",
  "cronExpression": "0 6 * * *",
  "parameters": {
    "date": "{{yesterday}}",
    "siteId": "site-1"
  },
  "distributionList": ["ops@deltagrid.com"],
  "format": "PDF",
  "isEnabled": true
}
```

Response:
```json
{
  "scheduleId": "daily-prod-schedule"
}
```

### POST /api/v1/reports/schedules/{scheduleId}/trigger
Trigger immediate execution of scheduled report.

## Report Catalogue

### Daily Production Report
- **Owner**: ProductionEngineer
- **Formats**: PDF, Excel
- **Parameters**: date, siteId, assetId
- **Frequency**: Daily
- **Distribution**: Ops team, Management

### HSE KPI Report
- **Owner**: HSELead
- **Formats**: PDF, Excel
- **Parameters**: period (monthly), siteId
- **Frequency**: Monthly
- **Distribution**: HSE team, Regulators

### Compliance Pack (Nigeria)
- **Owner**: ComplianceOfficer
- **Formats**: PDF
- **Parameters**: period, siteId
- **Frequency**: Quarterly
- **Distribution**: Regulators, Internal Audit

## Testing

### Pixel Verification
- Render report to image
- Compare pixel-by-pixel with baseline
- Detect layout changes

### Calculation Verification
- Extract calculated values from reports
- Compare with source data calculations
- Verify formulas match expected outputs

### Signature Verification
- Verify signature hash matches report content + signer
- Detect tampering attempts
- Verify signature chain integrity

## Observability

### Report Generation SLAs
- **Target**: Generate report in < 5 seconds (PDF) or < 10 seconds (Excel)
- **Monitoring**: Track generation time per report type
- **Alerts**: Alert if P95 exceeds SLA threshold

### Metrics
- Report generation rate
- Report generation latency (P50, P95, P99)
- Signature workflow duration
- Archive access rate
- Scheduled report execution success rate

## Future Enhancements

- **Self-Service BI**: Interactive dashboards (Phase 24+)
- **Report Designer**: Visual template designer
- **Multi-language**: Templates with localization
- **Interactive PDFs**: Fillable forms in PDFs
- **Export to Power BI**: Direct export to Power BI datasets


