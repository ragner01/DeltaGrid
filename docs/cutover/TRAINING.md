# Operator Training Materials

## Overview
This document provides training materials and scenario playbooks for DeltaGrid operators.

## Training Packs

### 1. Control Room Operator Training

#### Introduction
- Platform overview
- Role and responsibilities
- Access and permissions

#### Core Tasks
- **Viewing Well Status**
  - Navigate to well list
  - View well KPIs
  - Filter by asset/field
  - Export well data

- **Monitoring Alarms**
  - View alarm tiles
  - Acknowledge alarms
  - Shelve alarms (policy-bound)
  - Escalate alarms

- **Quick Actions**
  - Create work order
  - Create permit-to-work
  - Add notes
  - Shift handover

#### Scenario 1: Well Alarm Response
**Scenario**: High pressure alarm on Well-001

**Steps**:
1. Alarm appears on dashboard
2. Click alarm tile to view details
3. Review well status and telemetry
4. Acknowledge alarm
5. Check if work order required
6. Create work order if needed
7. Document actions in notes

**Expected Outcome**: Alarm acknowledged, appropriate actions taken

#### Scenario 2: Shift Handover
**Scenario**: End of shift handover

**Steps**:
1. Review open alarms
2. Review work orders in progress
3. Review permits active
4. Document shift notes
5. Complete handover checklist
6. Notify incoming operator

**Expected Outcome**: Smooth handover, no information lost

### 2. Production Engineer Training

#### Introduction
- Platform capabilities
- Data quality expectations
- Reporting responsibilities

#### Core Tasks
- **Well Management**
  - Edit well parameters
  - Update lift curves
  - Set operating limits
  - View well history

- **Allocation Review**
  - Review allocation results
  - Verify test data
  - Reconcile with custody meters
  - Export allocation reports

- **Optimization**
  - Review optimization recommendations
  - Accept/reject recommendations
  - View optimization history

#### Scenario 1: Daily Allocation Review
**Scenario**: Review daily production allocation

**Steps**:
1. Navigate to allocation module
2. Select date range
3. Review allocation by well/battery
4. Verify test data used
5. Check reconciliation deltas
6. Export allocation report
7. Document any discrepancies

**Expected Outcome**: Allocation reviewed, discrepancies documented

#### Scenario 2: Optimization Decision
**Scenario**: Optimization service recommends choke adjustment

**Steps**:
1. Review optimization recommendation
2. Check current well status
3. Review recommendation rationale
4. Verify constraints respected
5. Accept or reject recommendation
6. Document decision

**Expected Outcome**: Informed decision made, rationale documented

### 3. Data Steward Training

#### Introduction
- Data quality responsibilities
- Steward dashboard overview
- Exception management

#### Core Tasks
- **DQ Monitoring**
  - View DQ scores
  - Monitor breaches
  - Acknowledge breaches
  - Resolve breaches

- **Access Management**
  - Review access requests
  - Approve/reject requests
  - Manage exceptions

#### Scenario 1: DQ Breach Response
**Scenario**: Completeness breach detected on wells dataset

**Steps**:
1. Receive breach notification
2. View breach details
3. Investigate root cause
4. Acknowledge breach
5. Remediate data issue
6. Resolve breach

**Expected Outcome**: Breach resolved, data quality improved

### 4. Field Technician Training

#### Introduction
- Field app overview
- Offline capabilities
- Sync process

#### Core Tasks
- **Data Capture**
  - Capture readings
  - Take photos
  - Add annotations
  - Sign forms

- **Work Execution**
  - View assigned work orders
  - Complete work orders
  - Update status
  - Submit results

#### Scenario 1: Offline Data Capture
**Scenario**: Capture well readings without connectivity

**Steps**:
1. Open field app
2. Navigate to well
3. Capture readings
4. Add annotations
5. Save locally
6. Sync when connectivity available

**Expected Outcome**: Data captured offline, synced successfully

## Task-Based Flows

### Flow 1: Create Work Order
1. Navigate to Work Management
2. Click "Create Work Order"
3. Fill in work order details
4. Assign to technician
5. Set priority
6. Save work order
7. Notify assigned technician

### Flow 2: Create Permit-to-Work
1. Navigate to PTW module
2. Click "Create Permit"
3. Select permit type
4. Fill in permit details
5. Add isolation points
6. Submit for approval
7. Approve permit
8. Activate permit

### Flow 3: Export Report
1. Navigate to Reporting module
2. Select report type
3. Set parameters
4. Generate report
5. Review report
6. Export to PDF/Excel
7. Share report

### Flow 4: Resolve Data Quality Breach
1. Receive breach notification
2. View breach in steward dashboard
3. Investigate root cause
4. Acknowledge breach
5. Remediate issue
6. Resolve breach
7. Document resolution

## Nigerian Context

### Local Workflows
- **JV Partner Access**: Multi-tenant access management
- **Regulatory Reporting**: NUPRC reporting workflows
- **Local Units**: Unit conversion awareness
- **Offline Scenarios**: Field app offline usage

### Training Schedule
- **Week 1**: Control Room Operators
- **Week 2**: Production Engineers
- **Week 3**: Data Stewards
- **Week 4**: Field Technicians

### Support Resources
- **User Guides**: Available in system
- **Video Tutorials**: Available on portal
- **Support Contacts**: Listed in system
- **FAQ**: Available in help section


