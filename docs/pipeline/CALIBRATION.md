# Pipeline Leak Detection — Calibration Procedure

Inputs

- Historical balance series (upstream-downstream, compensated) without known leaks
- Meter uncertainties (% of reading)

Steps

1. Compensation
   - Convert raw flows to compensated values accounting for temperature and elevation per segment.
2. Baseline
   - Compute mean and standard deviation of compensated net balance; persist as segment baseline.
3. Adaptive Thresholds
   - Use k·σ (default k=3) for change-point detection; adjust k by false positive rate targets.
4. Validation
   - Replay synthetic and historical scenarios; track FP/FN and latency metrics.
5. Recalibration
   - Periodically recompute baselines to capture seasonal behavior, with guardrails.
