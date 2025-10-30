# Alarm Philosophy and Configuration Guide (Draft)

Principles

- Prioritize safety and integrity; minimize nuisance alarms.
- Use severity and consequence categories consistently.
- Apply suppression for chattering and floods; ensure escalation for unacknowledged alarms.

Canonical Schema

- Fields: tenant, site, asset, severity, consequence, category, message, source, tagId, occurredAt, priority, fingerprint, status, shelved.

Suppression

- Dedup window: suppress identical fingerprints within N seconds.
- Chattering: if frequency exceeds threshold within window, reduce to single event with count.
- Flood control: cap routed events per time slice.
- Maintenance mode: route to shelved state without notifications.

Escalation

- SLA timers by severity (e.g., High=2m, Critical=1m) to escalate if not acknowledged.
- Ownership and acknowledgment tracked; tamper-evident logs.

Replay

- Support ingestion of historical raw alarms for rationalization tuning and audits.

Metrics

- MTTA/MTTR, alarm rate per asset, suppression effectiveness, escalation counts.
