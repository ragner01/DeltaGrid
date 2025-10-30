# Lab Calculators and Lineage

Scope
- PVT/API/BS&W/Salinity/Viscosity capture, lineage, validity windows.
- Derived adjustments: shrinkage, adjusted GOR, adjusted water cut.
- Security: Signed PDF certificates (HMAC-SHA256 demo), quality flags and re-test workflows.

Calculators (simplified placeholders)
- ShrinkageFactor(API): piecewise approx; see standard refs in appendix.
- AdjustedGOR(GOR, WC): GOR × (1 - WC × 0.02) bounded to [0.8..1.0] multiplier.
- AdjustedWC(WC, Salinity): WC × (1 + Salinity/200k) capped +10%.

Lineage & Validity
- Each `LabResult` has MethodVersion, Certificate URL, Signature (algo+value), EffectiveFrom..EffectiveTo.
- Pushing to Allocation/Optimization records the SampleId and timestamp for traceability.

Quality & Re-test
- QualityFlag: Valid, Suspect, Rejected; retest requests are appended to chain-of-custody with reason.

Testing
- Golden calculator cases vs reference tables; rounding: banker’s rounding where applicable.
- Provenance: active result switches close prior validity; certificate signature present.

Observability
- Turnaround time: Collected→Received→Result timestamps.
- Rework rate: count Suspect/Rejected flags and retest requests.
