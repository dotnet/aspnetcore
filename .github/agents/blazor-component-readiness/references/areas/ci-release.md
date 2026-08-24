# CI, documentation, and release validation

Applies to `CI-*`.

## Pipeline trace

Trace PR validation and release as separate systems:

1. source and dependency acquisition;
2. restore, build, test, package, and browser exercise;
3. accessibility and vulnerability gates;
4. immutable artifact handoff;
5. signing and SBOM/provenance generation;
6. publication;
7. retained evidence and release revalidation.

Inspect required-check and environment configuration when public. Otherwise request maintainer
evidence rather than assuming protection.

## Evidence to collect

- Exact workflow revision used by the release.
- Test projects and deterministic regressions for accepted defects.
- Browser coverage for each claimed render mode.
- Compiling documentation samples with behavioral assertions.
- Toolchain versions and prerequisites.
- Artifact identities before and after privileged stages.
- Release checklist output mapped to canonical requirement IDs.

## Scoring boundaries

- Workflow presence is intent; successful required execution for the exact release is evidence.
- A passing build does not prove browser behavior, accessibility, or package integrity.
- Untrusted build logic sharing a job with signing authority is a bounded supply-chain concern;
  document required attacker authority and unknown approvals.
- Inaccessible branch-protection configuration is `maintainer evidence required`; an observable
  missing required public release control is a `defect`.
- A release process that rebuilds after verification or signs a different artifact is a defect.

## Common traps

- Treating optional jobs as required gates.
- Assuming documentation snippets compile because the product builds.
- Accepting mutable artifact names without digest verification.
- Using current default-branch CI as evidence for an older published package.
