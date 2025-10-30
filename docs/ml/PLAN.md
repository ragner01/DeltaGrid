# MLOps Plan

Workspace
- Registry for models; feature store sourcing from lakehouse curated tables

Models
- ESP failure prediction; production forecast; anomaly detection (events/telemetry)

Pipelines
- Feature pipelines with Delta inputs; training/eval; model card generation

Deployment
- ONNX export; deploy to Optimization inference service with canary/shadow

Monitoring
- Drift detection; accuracy/precision/recall dashboards; auto-retrain hooks

Security
- Signed artifacts; dataset access policies
