# Access Control Matrix

Roles

- ControlRoomOperator
- ProductionEngineer
- MaintenancePlanner
- IntegrityEngineer
- HSELead
- Auditor
- Admin

Matrix (examples)

- Work Mgmt (create work order)
  - ControlRoomOperator: Allow if `tenant_id` matches and `site_id` in operator scope
  - MaintenancePlanner: Allow
  - Auditor: Deny
- Allocation (trigger run)
  - ProductionEngineer: Allow within tenant scope
  - Admin: Allow
  - Others: Deny
- Integrity Findings (create)
  - IntegrityEngineer: Allow within site scope
  - HSELead: Allow read

Least-Privilege Guidelines

- Always scope permissions by `tenant_id` and narrow by `site_id`/`asset_id`
- Use ABAC for discipline/shift/permit constraints where applicable
- Approvals require dual control for high permit levels
