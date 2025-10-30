# Event-Storming Outline

## Event Categories (Canonical)

- Ingestion
  - `TelemetryIngested`
  - `TagQualityDegraded`
  - `BackfillWindowCompleted`

- Time-Series
  - `SeriesDownsampled`
  - `FeatureWindowComputed`

- Allocation
  - `AllocationRunStarted`
  - `AllocationFactorUpdated`
  - `AllocationClosed`

- Optimization/Surveillance
  - `AnomalyDetected`
  - `LiftRecommendationIssued`
  - `RecommendationApproved`

- Events & Alarms
  - `AlarmRaised`
  - `AlarmAcknowledged`
  - `AlarmCleared`

- Work Mgmt & PTW
  - `WorkOrderCreated`
  - `PermitRequested`
  - `PermitApproved`
  - `WorkOrderCompleted`

- Integrity
  - `InspectionScheduled`
  - `FindingRecorded`
  - `MitigationImplemented`

- Pipeline & Leak Detection
  - `LeakSuspected`
  - `LeakConfirmed`
  - `LeakCleared`

- Custody Transfer
  - `ProvingCompleted`
  - `TicketIssued`
  - `TicketAmended`

- Labs
  - `AssayReceived`
  - `QualityOutOfSpec`

- Reporting & Compliance
  - `RegulatoryReportFiled`
  - `VarianceExplained`

## Commands

- `RunAllocation(day, assetId)`
- `ApproveRecommendation(recommendationId)`
- `CreateWorkOrder(task)`
- `RequestPermit(workOrderId)`
- `PublishRegulatoryReport(reportId)`

## Policies

- On `AlarmRaised` then `CreateWorkOrder` if severity ≥ High.
- On `AssayReceived` then recompute product quality KPIs.
- On `AllocationClosed` then trigger compliance report aggregation.

## High-Value Read Models

- Daily Production Balance by Asset
- Exception/Anomaly Dashboard by System
- Leak Detection Status Timeline
- Custody Tickets and Proving History
- PTW Status and Active Work Risk Profile


