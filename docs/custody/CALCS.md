# Custody Transfer Calculations (CTPL/API) — Reference

Scope: Proving adjustments, CTPL composite factor, unit conversions, and rounding.

- Units: Primary standard volume in m³; API references noted for conversion to bbl @60°F.
- Rounding: Use banker’s rounding to 3 decimals for volumes on tickets; internal calcs keep 6+ decimals.

Formulas (simplified placeholders; replace with exact standard refs in implementation):
- Composite CTPL = f(API gravity @60°F) × f(Temperature) × f(Pressure)
- StandardVolume = ObservedVolume × CTPL
- MeterFactor(final) = MeterFactor(initial) × (ProverBaseVolume / StandardVolume)

Testing
- Golden cases: Temperature sweep at constant pressure; compare CTPL vs reference table.
- Rounding: Verify banker’s rounding at .5 boundaries.

Audit
- Ticket payload hash computed over ordered fields; any change yields a new hash and invalidates signature/approval.
