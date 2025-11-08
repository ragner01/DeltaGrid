# Data Quality Playbook

## Overview
This playbook provides guidelines for data quality management, stewardship workflows, and access request processes for DeltaGrid platform.

## Data Quality Dimensions

### 1. Completeness
**Definition**: Percentage of non-null values in a dataset.

**Thresholds**:
- **Critical Datasets**: ≥ 98%
- **High Priority**: ≥ 95%
- **Standard**: ≥ 90%

**Evaluation**:
```sql
-- Example completeness evaluation
SELECT 
    COUNT(*) as total_rows,
    COUNT(well_id) as non_null_rows,
    COUNT(well_id) * 100.0 / COUNT(*) as completeness_percentage
FROM wells
```

**Remediation**:
- Identify missing values
- Trace to source systems
- Request data providers to fill gaps
- Document known gaps as exceptions

### 2. Timeliness
**Definition**: Data freshness measured in minutes since last update.

**Thresholds**:
- **Critical Real-time**: ≤ 15 minutes
- **High Priority**: ≤ 60 minutes
- **Standard**: ≤ 4 hours

**Evaluation**:
```sql
-- Example timeliness evaluation
SELECT 
    DATEDIFF(MINUTE, last_update, GETDATE()) as minutes_since_update
FROM dataset_metadata
WHERE dataset_id = 'allocation'
```

**Remediation**:
- Check ingestion pipeline status
- Verify source system connectivity
- Investigate pipeline bottlenecks
- Escalate to platform team if persistent

### 3. Validity
**Definition**: Percentage of records matching format/rules/conformance.

**Thresholds**:
- **Critical**: ≥ 99%
- **High Priority**: ≥ 95%
- **Standard**: ≥ 90%

**Evaluation**:
```sql
-- Example validity evaluation (format check)
SELECT 
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE well_id ~ '^[A-Z0-9]{4,10}$') as valid_rows,
    COUNT(*) FILTER (WHERE well_id ~ '^[A-Z0-9]{4,10}$') * 100.0 / COUNT(*) as validity_percentage
FROM wells
```

**Remediation**:
- Identify invalid records
- Analyze pattern of invalid data
- Fix source system if systemic issue
- Apply data cleansing rules
- Document as exception if intentional

### 4. Consistency
**Definition**: Percentage of records consistent across fields/datasets.

**Thresholds**:
- **Critical**: ≥ 98%
- **High Priority**: ≥ 95%
- **Standard**: ≥ 90%

**Evaluation**:
```sql
-- Example consistency evaluation (cross-field check)
SELECT 
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE well_id = allocation_well_id) as consistent_rows,
    COUNT(*) FILTER (WHERE well_id = allocation_well_id) * 100.0 / COUNT(*) as consistency_percentage
FROM wells w
JOIN allocation a ON w.well_id = a.well_id
```

**Remediation**:
- Identify inconsistent records
- Trace to source of inconsistency
- Resolve data conflicts
- Document reconciliation rules
- Apply data quality rules

## Stewardship Roles

### Data Steward
**Responsibilities**:
- Define data quality rules
- Monitor DQ scores and breaches
- Acknowledge and resolve breaches
- Request exceptions when appropriate
- Approve/reject access requests for their datasets
- Review and approve DQ exception requests

**Permissions**:
- Create/edit DQ rules for owned datasets
- Evaluate DQ rules
- Acknowledge/resolve breaches
- Request DQ exceptions
- Approve/reject access requests
- View steward dashboard

### Data Administrator
**Responsibilities**:
- Manage dataset metadata
- Assign stewards to datasets
- Approve/reject DQ exception requests
- Monitor overall data quality
- Review access request trends
- Manage data lineage

**Permissions**:
- All steward permissions
- Approve/reject DQ exceptions
- Manage dataset metadata
- Assign stewards
- View system-wide DQ metrics

## Stewardship Workflows

### 1. Breach Detection Workflow

1. **Automatic Detection**
   - DQ engine evaluates rules nightly (or on schedule)
   - Breaches detected when scores fail thresholds
   - Breaches created automatically
   - Stewards notified

2. **Breach Acknowledgment**
   - Steward reviews breach
   - Acknowledges if understood
   - Updates status to "Acknowledged"

3. **Remediation**
   - Steward investigates root cause
   - Fixes data quality issue
   - Updates status to "In Progress"
   - Resolves breach with remediation notes

4. **Exception Request** (if remediation not possible)
   - Steward requests exception
   - Provides justification
   - Administrator reviews
   - Exception approved/rejected
   - Breach status updated to "Exception"

### 2. DQ Exception Workflow

1. **Request Exception**
   - Steward creates exception request
   - Provides reason and expiry date (if time-bound)
   - Exception status: "Pending"

2. **Review & Approve**
   - Administrator reviews request
   - Approves with optional expiry date
   - Exception status: "Approved"
   - Breach status: "Exception"

3. **Auto-Expiry** (if time-bound)
   - System checks expiry daily
   - Automatically expires when due
   - Exception status: "Expired"
   - Breach status: "Open" (if still failing)

### 3. Access Request Workflow

1. **Request Access**
   - User creates access request
   - Specifies dataset, access level, justification
   - Sets expiry date (optional)
   - Request status: "Pending"

2. **Steward Review**
   - Steward reviews request
   - Approves or rejects
   - Request status: "Approved" or "Rejected"

3. **Auto-Expiry** (if time-bound)
   - System checks expiry daily
   - Automatically expires when due
   - Request status: "Expired"

4. **Revocation**
   - Steward or administrator can revoke access
   - Request status: "Revoked"

## DQ Rule Management

### Creating a Rule

1. **Define Rule**
   - Rule name and description
   - Dataset identifier
   - DQ dimension (Completeness, Timeliness, Validity, Consistency)
   - Expression (SQL, regex, etc.)
   - Threshold value
   - Threshold operator (GreaterThan, LessThan, etc.)

2. **Set Owner**
   - Assign steward as owner
   - Owner receives breach notifications

3. **Activate Rule**
   - Rule evaluated on schedule
   - Breaches created automatically on failure

### Rule Examples

#### Completeness Rule
```json
{
  "id": "completeness-wells",
  "name": "Wells Completeness",
  "datasetId": "wells",
  "dimension": "Completeness",
  "expression": "COUNT(*) / COUNT(well_id) * 100",
  "threshold": 95.0,
  "operator": "GreaterThanOrEqual",
  "description": "Well completeness must be >= 95%",
  "owner": "production-engineer"
}
```

#### Timeliness Rule
```json
{
  "id": "timeliness-allocation",
  "name": "Allocation Timeliness",
  "datasetId": "allocation",
  "dimension": "Timeliness",
  "expression": "DATEDIFF(MINUTE, last_update, GETDATE())",
  "threshold": 60.0,
  "operator": "LessThanOrEqual",
  "description": "Allocation data must be updated within 60 minutes",
  "owner": "production-engineer"
}
```

#### Validity Rule
```json
{
  "id": "validity-well-id",
  "name": "Well ID Format",
  "datasetId": "wells",
  "dimension": "Validity",
  "expression": "well_id ~ '^[A-Z0-9]{4,10}$'",
  "threshold": 99.0,
  "operator": "GreaterThanOrEqual",
  "description": "Well ID must match format [A-Z0-9]{4,10}",
  "owner": "production-engineer"
}
```

## Lineage-Driven Impact Assessment

### Impact Assessment Process

1. **Breach Detected**
   - DQ breach created for dataset

2. **Lineage Analysis**
   - System queries data lineage
   - Identifies downstream datasets
   - Traces to reports and services

3. **Impact Assessment**
   - Determines impact severity:
     - **Critical**: Many downstream dependencies, critical services affected
     - **High**: Significant downstream dependencies
     - **Medium**: Moderate downstream dependencies
     - **Low**: Minimal downstream dependencies

4. **Recommendations**
   - System generates recommendations
   - Steward reviews assessment
   - Takes appropriate action

### Example Impact Assessment

**Breach**: Wells dataset completeness breach (85% vs 95% threshold)

**Impact**:
- **Affected Datasets**: Allocation, Optimization, Reporting
- **Affected Reports**: Daily Production Report, Allocation Report
- **Affected Services**: Optimization Service, Reporting Service
- **Severity**: High
- **Recommendations**: "Priority remediation required. Notify affected stakeholders. Consider blocking Optimization Service until resolved."

## Best Practices

### Rule Definition
- **Start Conservative**: Set thresholds slightly higher than current scores
- **Review Regularly**: Adjust thresholds based on trends
- **Document Rules**: Provide clear descriptions and examples
- **Test Rules**: Validate expressions before activating

### Breach Management
- **Acknowledge Promptly**: Acknowledge breaches within 24 hours
- **Investigate Root Cause**: Don't just fix symptoms
- **Document Remediation**: Record remediation steps in breach notes
- **Monitor Trends**: Track breach frequency and patterns

### Access Management
- **Principle of Least Privilege**: Grant minimum required access
- **Time-Bound Access**: Set expiry dates for temporary access
- **Regular Review**: Review access requests quarterly
- **Audit Trail**: Maintain complete audit trail of approvals/rejections

### Exception Management
- **Justify Exceptions**: Provide clear business justification
- **Time-Bound Exceptions**: Set expiry dates for exceptions
- **Review Regularly**: Review exceptions quarterly
- **Document Alternatives**: Document why remediation isn't possible

## Monitoring & Reporting

### Steward Dashboard
- **Open Breaches**: List of open breaches for steward's datasets
- **Pending Exceptions**: List of pending exception requests
- **Pending Access Requests**: List of pending access requests
- **DQ Statistics**: Summary of DQ scores and trends
- **Dataset Scores**: DQ scores by dimension per dataset

### DQ Trends
- **Breach Trends**: Breach count over time
- **Mean Time to Remediation**: Average time to resolve breaches
- **Breaches by Dimension**: Distribution of breaches by DQ dimension
- **Top Breach Datasets**: Datasets with most breaches

### Reports
- **Daily DQ Summary**: Daily summary of DQ scores and breaches
- **Weekly Steward Report**: Weekly report for each steward
- **Monthly DQ Dashboard**: Monthly executive dashboard
- **Quarterly Review**: Quarterly DQ review and recommendations

## Nigerian Context

### Regulatory Compliance
- **NUPRC Reporting**: DQ scores included in regulatory reports
- **Data Retention**: DQ scores retained for audit purposes
- **Access Logs**: All access requests logged for compliance
- **Exception Documentation**: Exceptions documented for regulatory review

### Local Considerations
- **Intermittent Connectivity**: Timeliness rules account for connectivity issues
- **Multi-JV Context**: DQ rules may differ by JV partner
- **Local Units**: Validity rules account for local unit conventions


