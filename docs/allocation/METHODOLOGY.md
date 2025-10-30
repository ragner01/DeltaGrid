# Allocation Methodology (Draft)

Scope

- Proportional-by-test baseline with deterministic rounding
- Future: regression-based and constraint-based methods

Definitions

- Test Rates: Oil_t, Gas_t, Water_t (m³/day)
- Measured Battery Volumes: Oil_m, Gas_m, Water_m (m³)

Proportional-by-test allocation

- Share_i = TestRate_i / Σ TestRate
- AllocOil_i = round(Oil_m × Share_i, 3, toward zero)
- Deterministic remainder adjustment: distribute +0.001 m³ in index order to match mass balance exactly

Mass balance constraint

- Σ AllocOil_i == Oil_m (after adjustment)
- Similarly for Gas, Water

Lineage & Versioning

- Each run stores inputs (tests, meter measurement), method name, version, timestamp
- Reruns increment version; prior runs remain immutable

Reconciliation

- Variance% = ((Σ AllocOil - Oil_m) / Oil_m) × 100
- Workflow thresholds: configurable, with approval steps for re-runs

Security

- Approvals required by role (Admin/ProductionEngineer)
