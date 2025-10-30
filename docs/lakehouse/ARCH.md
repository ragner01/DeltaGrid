# Lakehouse Architecture (ADLS Gen2 + Delta)

Layers
- Raw: telemetry/events/allocations/custody/labs landed as append-only Delta tables
- Curated: conformed schemas with business keys, SCD2 where applicable
- Marts: finance, HSE, production reporting

Ingestion
- CDC from OLTP (tickets, PTW, lab, custody) via Data Factory/Change Feed
- Telemetry via ADX export or Event Hubs → Delta sink

Partitioning
- By tenant/site/date (YYYY/MM/DD); schema evolution via Delta; vacuum/purge per policy

Governance
- Purview catalog with glossary and lineage; access policies per tenant/role via ACLs

Data Quality
- Great Expectations (or similar) checks; SLAs tracked in dashboards
