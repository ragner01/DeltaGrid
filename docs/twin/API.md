# Twin API Guide

Hierarchy
- Levels: Region→Field→Facility→Train→Unit→Equipment→Tag
- Node path: `IdPath` like `/Region/R1/Field/F1/Facility/FA/Unit/U-10/Equipment/P-101`
- Soft deletes bump topology version.

Endpoints (v1)
- POST `/api/v1/twin/import` body: ["/Region/R1,Region,R1", "/Region/R1/Field/F1,Field,F1"] → { version }
- POST `/api/v1/twin/nodes` body: { IdPath, Level, Name, Metadata }
- DELETE `/api/v1/twin/nodes?idPath=/Region/R1/...`
- GET `/api/v1/twin/snapshot?idPath=/Region/R1/...` → static metadata + KPI overlay + children
- GET `/api/v1/twin/impact?idPath=/Region/R1/...&relation=contains` → impacted paths

Notes
- Path invariants: must start with `/`; parent edges auto-created on import.
- Versioning: each import/upsert/delete increments topology version.
- KPIs: demo provider; replace with live metrics.
