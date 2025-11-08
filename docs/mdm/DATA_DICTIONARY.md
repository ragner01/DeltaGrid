# MDM Data Dictionary and SLAs (Phase 30)

- Canonical entities: Asset, Meter, Well, Unit, Factor, Code
- Keys: stable domain keys; survivorship: authoritative-system-first then latest-timestamp
- Lineage fields: source-of-authority, steward, timestamp, reason
- SLAs: snapshot publish cadence daily; consumer compatibility guarantees (no breaking change without version bump)

