# Support, servicing, and lifecycle

Applies to `SUP-*`.

These requirements are primarily repository-wide and maintainer-owned. `SUP-09` is intentionally
component-specific because a release-wide process can reverify some controls while omitting the
selected control. `SUP-01` proves named package/product support ownership and contact, not separate
implementation ownership for each control. Public documentation can verify a published commitment;
absence of private operational records is not a product defect.

## Evidence to collect

- Named maintainer/support ownership and public contact route.
- Response targets, supported versions, patch cadence, and EOL policy.
- Public non-security issue tracking and private coordinated security disclosure.
- Emergency servicing process and release ownership.
- Per-release revalidation records mapped to the canonical checklist.
- Readiness-regression suspension and revalidation/recovery expectations.

## Scoring boundaries

- Publicly documented support commitments can be `verified`.
- Private staffing, escalation, incident, and release records are `maintainer evidence required`.
- A contradictory or missing required public policy can be a documentation/release-control
  `defect`.
- Inaccessible suspension/recovery governance is `maintainer evidence required`; the reviewer
  holding a recommendation does not verify SUP-10.
- Recent issue activity is not proof of an SLA.
- A `SECURITY.md` file is not proof of emergency release capability.

## Handoff questions

Ask for owners, evidence locations, scope, dates, supported versions, response targets, and the
release candidate to which each attestation applies. Keep reviewer follow-up bounded to reviewing
the supplied evidence and retesting the exact candidate.
