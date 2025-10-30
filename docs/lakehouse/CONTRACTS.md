# Data Contracts

Raw landing (Delta)
- telemetry_raw(tenant, site, tag, ts, value, quality)
- events_raw(tenant, site, id, ts, type, severity, attrs)
- allocation_raw(tenant, run_id, version, payload)
- custody_raw(tenant, ticket_no, payload)
- labs_raw(tenant, sample_id, payload)

Curated
- telemetry_curated(tenant, site, tag, ts, value, quality, unit)
- events_curated(tenant, id, ts, type, severity, equipment, status)
- allocation_curated(tenant, run_id, status, results_json)
- custody_curated(tenant, ticket_no, meter_id, std_vol_m3, status, hash)
- labs_curated(tenant, sample_id, api, gor, wc, viscosity, method, cert_url)

Marts
- mart_production(day, tenant, site, oil_m3, gas_mscf, water_m3)
- mart_finance(month, tenant, product, volume_std, revenue)
