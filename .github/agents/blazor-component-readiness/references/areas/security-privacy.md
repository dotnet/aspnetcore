# Security and privacy

Applies to `SEC-*`.

## Threat-boundary model

Describe the assets, actors, trust boundaries, preconditions, impact, and unknown compensating
controls before labeling a security concern. Cover:

- .NET component and host application;
- browser DOM, events, storage, and remote resources;
- JS modules and serialization boundaries;
- Static SSR, Interactive Server, WebAssembly, and Auto transitions;
- repository, CI, package signing, publication, and dependency ingestion.

Invoke the security-review specialist for concrete vulnerability claims. Give it the exact source
SHA/package, bounded paths, proposed attacker capability, reproduction evidence, and read-only
constraints. Independently adjudicate its result.

## Evidence to collect

- Threat model and completed review records, including disposition of findings.
- Public disclosure route and coordinated response process.
- Exact-release dependency and vulnerability scan evidence.
- Patch cadence and emergency release capability.
- Source and network evidence for telemetry, remote assets, and phone-home behavior.
- Authorization assumptions around browser-originated values in server-rendered modes.

## Scoring boundaries

- A threat-model PR is not a completed security review.
- A current point-in-time scan does not prove release-time gating.
- Missing private review records are `maintainer evidence required`, not vulnerabilities.
- Report a product `defect` only for a concrete insecure behavior, unsafe documented contract, or
  missing required public control.
- Supply-chain findings must state the repository/release authority needed by an attacker and any
  unknown environment approval.
- Browser events and component state are application input, never authorization evidence.

## Common traps

- Calling repository write access an unauthenticated remote exploit.
- Treating lack of telemetry found in source as proof that every distribution is telemetry-free.
- Ignoring hosted scripts, themes, fonts, or sample infrastructure.
- Reporting theoretical DOM injection without a reachable untrusted-data path and sink.
