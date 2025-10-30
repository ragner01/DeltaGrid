# Lift Optimization Surrogate Model — Model Card

Overview

- Purpose: Surrogate for gas-lift/ESP recommendation. Supports rule+ML hybrid.
- Input: Last 60 telemetry points (pressure, temperature, flow, choke, esp freq) + constraints.
- Output: Recommended choke% and ESP freq with rationale.

Training Data

- Windowed features derived from cleaned time-series; exclusions for bad quality and upsets.
- Labels: operator-approved setpoints and subsequent stable production outcomes.

Evaluation

- Backtests across multiple wells; metrics: production uplift, constraint violations (must be 0%), acceptance rate.

Security

- Model file checksum (SHA-256) verified at load; signed artifacts recommended.

Retraining Steps

1. Extract features from ADX for target wells and periods.
2. Split by well and time; train surrogate; validate against holdout.
3. Export ONNX with stable input schema; compute SHA-256.
4. Publish to model registry and update service config (path + checksum).

Limitations

- Not autonomous; recommendations require approval.
- Accuracy degrades with distribution shift; monitor model drift.
