# Integrity Methods Sheet — Corrosion and Inspection

Corrosion Rate Calculation

- Inputs: thickness readings T1 (at date D1) and T2 (at date D2) at the same location (chainage/clock position)
- Rate (mm/yr) = (T1 - T2) / ((D2 - D1) / 365.25)
- Assumptions: uniform thinning between readings; verified location consistency; measurement uncertainty ±0.1 mm

Forecast and Next Due

- Remaining life (years) = (T2 - Tmin_allowable) / Rate
- Next due date = min(plan interval, risk-based interval from remaining life thresholds)

Threshold Alerts

- Rate bands: Low < 0.1 mm/y; Medium 0.1–0.3; High > 0.3 (configurable)
- Alert when band increases or remaining life < threshold

Validation

- Reference datasets with known rates; tolerance ±0.02 mm/y
- Flag inconsistent locations (mismatched chainage/clock) or negative rates

Integration

- Create work orders for mitigation when rate exceeds threshold or anomalies raised
