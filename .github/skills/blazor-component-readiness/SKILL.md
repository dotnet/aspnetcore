---
name: blazor-component-readiness
description: >
  Review one released Blazor UI component package vertically across runtime behavior, accessibility,
  security boundaries, packaging, provenance, trimming/AOT, performance, release
  practices, and support evidence. Use this skill whenever a user asks whether a
  Blazor control, component package, or component library is ready to adopt, ship,
  recommend, promote, or release; asks for a readiness scorecard or maintainer
  feedback; or needs evidence-backed evaluation of render modes, callbacks, disposal,
  JS interop, keyboard behavior, ARIA, signing, SBOMs, trimming, CI, or servicing.
  Review one representative component per run unless the user explicitly expands
  scope. Do not use for implementing fixes, general PR review, or formal certification.
license: MIT
compatibility: Requires access to the reviewed source or package and Python 3 for deterministic scorecard validation.
---

# Blazor component readiness

Review one Blazor UI component vertically through the bundled public readiness rubric. The goal
is actionable, evidence-backed adoption or release guidance, not a catalog audit or formal
certification.

The complete core is calibrated for released, distributed component packages. For a source-only,
application-local, or experimental component, use targeted mode unless the user explicitly needs a
full release-readiness assessment; do not turn absent commercial release machinery into a blanket
negative adoption verdict.

## Core principles

1. **One component per run.** Keep the control/package boundary explicit. Apply repository-wide
   requirements once, but do not silently expand into sibling controls.
2. **Evidence before opinion.** Prefer exact released artifacts, deterministic consumer probes,
   direct source proof, and public configuration over inferred quality.
3. **Artifact truth beats workflow intent.** A configured signing or SBOM step does not prove that
   the released package satisfies the requirement.
4. **Separate defects from unavailable records.** Missing private evidence is a maintainer evidence
   request, not automatically a product defect.
5. **Preserve security preconditions.** State attacker capability, trust boundary, compensating-
   control unknowns, and bounded impact.
6. **Accessibility source is not conformance.** ARIA, roles, handlers, scanners, browser behavior,
   assistive-technology evidence, and formal conformance are separate evidence layers.
7. **Keep ownership clear.** The reviewer investigates and reports. Maintainers own remediation,
   attestations, support, servicing, release governance, and certification.

## Default safety boundary

Unless the user explicitly authorizes another mode:

- Treat the review as read-only.
- Do not modify repositories, refs, issues, pull requests, comments, or reviews.
- Put disposable probes outside the reviewed worktree and remove them afterward.
- Write artifacts only to a user-approved output path.
- Record exact `HEAD`, package version/digest, and worktree status before and after.
- Keep credentials, private URLs, workplace data, and unrelated repository content out of outputs.

## Required references

Read before a complete review:

- `references/checklist.md`: versioned public 110-ID released-package core.
- `references/overlays/`: opt-in scaffolder and AI-skill requirements.
- `references/areas/index.md`: evidence precedence and quality-area playbooks.
- `references/report-template.md`: concise report, annex, handoff, and evidence-anchor shape.

Read each mapped area playbook while scoring that family. Read `references/feedback.md` when
capturing run feedback, and `references/learning-loop.md` only when improving this skill.

## Review modes

- **Complete readiness review:** include all 110 core IDs exactly once, plus only the overlays
  actually present in the bounded deliverable, and validate the scorecard.
- **Targeted follow-up:** investigate named IDs/findings only. State that it is not a complete
  readiness review, validate only those IDs, and do not imply unchanged rows were reverified.
- **Inventory/selection pass:** identify representative components without scoring readiness.

## Workflow

### 1. Bound the target

Record:

- repository, component/control/package, and maintainer;
- default branch, exact reviewed SHA, and current public head when available;
- released package/version, digest, and source commit when available;
- why this component is representative;
- explicit exclusions and timebox;
- review mode and rubric version.

Use the narrowest independently consumable unit. In a generated suite, choose one control and
distinguish its generated output, handwritten partials, shared runtime, and upstream web component.

### 2. Pin the bundled rubric

`references/checklist.md` is the self-contained core source of truth. Record its rubric version and
selected overlay versions in the report. The skill does not require an external policy document or
private service.

If the user supplies a newer or organization-specific policy:

1. compare it with stable IDs;
2. record added, removed, or changed intent as rubric drift;
3. finish against the pinned bundled version unless the user explicitly requests a rubric update;
4. never silently renumber, reuse, or reinterpret an ID;
5. do not claim coverage of the supplied policy until its drift is reconciled.

Organization-specific requirements belong in an explicit overlay, not hidden assumptions.

### 3. Reuse evidence without laundering it

Read prior exact-snapshot reports and ledgers first. For every reused claim, record its source,
SHA/package, whether it was independently rechecked, and whether the current snapshot changed.
Re-run only the smallest probe needed for stale, disputed, missing, or potentially fixed evidence.

For a rubric migration replay:

1. diff stable IDs and canonical wording before changing the old scorecard;
2. preserve unchanged classifications and exact-snapshot evidence without upgrading confidence;
3. investigate a new ID with the smallest existing-snapshot check, or mark it `not tested`;
4. publish a migration delta separating terminology changes, policy changes, evidence-driven
   classification changes, and new requirements;
5. report current-head drift and never present the replay as a fresh audit;
6. treat aggregate count changes without new product evidence as suspicious until explained.

### 4. Create the scorecard annex

For a complete review, emit the skeleton before investigating:

```bash
python3 scripts/validate_scorecard.py --emit-template
```

Add `--overlay scaffolder` or `--overlay ai-skill` only when the bounded deliverable includes that
feature. Do not emit placeholder rows for unselected overlays.

For a targeted review, name the exact IDs:

```bash
python3 scripts/validate_scorecard.py --ids BEQ-12,BEQ-15 --emit-template
```

Inspect repository-wide evidence once: licensing, package metadata, dependency inventory,
signatures, SBOM/provenance, security process, CI/release, documentation, support, servicing, and
release revalidation.

### 5. Exercise the component

Use the smallest real consumer applications that cover documented behavior:

- compile documented samples;
- Interactive Server;
- Interactive WebAssembly or standalone WASM;
- Auto when claimed;
- prerender and useful Static SSR semantics;
- trimmed WASM publish plus browser exercise;
- native AOT only when claimed or requested;
- representative data size, lifecycle, callbacks, cleanup, and accessibility interactions.

A successful build is not runtime proof. Source inspection is not browser or assistive-technology
proof. Stop when the declared timebox expires: mark remaining applicable rows `not tested`, add
bounded reviewer follow-ups, and do not silently extend scope.

### 6. Review implementation boundaries

Inspect parameters and binding pairs; callback awaiting and error routing; lifecycle, timers,
subscriptions, cancellation, JS references/modules/listeners; serialization and DOM sinks; style
scope; rendering identity/cost; keyboard/focus/ARIA/localization; and generated-code ownership.

Use the dynamic-state and registration recipes in `references/areas/blazor-runtime.md` when the
component has parent/child registration, selected values, custom elements, or delayed JS upgrade.

### 7. Calibrate security findings

For concrete vulnerability claims, use an available security-review specialist with the exact
snapshot, bounded paths, threat model, and reproduction. Independently adjudicate the result.
Every security finding must state preconditions, bounded impact, confidence, and unknown controls.

### 8. Score each requirement

Use only:

| Status | Use when |
|---|---|
| `verified` | Exact source, artifact, configuration, attestation, or deterministic behavior satisfies the requirement in scope. |
| `defect` | Observable product, artifact, documentation, or required public control conflicts with the requirement. |
| `maintainer evidence required` | The maintainer owns a private record, attestation, or inaccessible control that the reviewer cannot establish. |
| `not tested` | The requirement applies, but the reviewer did not obtain sufficient evidence or run the available probe. |
| `not applicable` | The requirement does not apply to the bounded deliverable or its documented support claims. |

Boundary rules:

- Missing required public metadata/artifact is normally a `defect`.
- An inaccessible private control or attestation is `maintainer evidence required`.
- Explicitly supported but untested behavior is `not tested`.
- Explicitly unsupported and unclaimed conditional behavior can be `not applicable`.
- Unknown support claims need maintainer clarification; do not silently treat them as unsupported.
- Signature presence/identity and certificate-chain or revocation evidence are separate claims.
- Passing source, scanner, or browser evidence does not establish formal accessibility conformance.

Each defect must identify the exact snapshot, path/member/artifact, expected and observed behavior,
reproduction or direct proof, owning layer, scope, confidence, and remediation direction.

### 9. Produce decision-first outputs

Follow `references/report-template.md`:

1. A concise readiness report with findings first and the complete scorecard/evidence ledger as
   annexes.
2. A short maintainer handoff.
3. During pilots or when feedback is requested, a lightweight run-observations note.

Scorecard evidence may use a validated ledger anchor such as `[E-017]`; define each anchor once in
the evidence ledger. Do not use generic or unresolved references.

For a complete review, run:

```bash
python3 scripts/validate_scorecard.py <readiness-report.md>
```

Include selected overlays with the matching `--overlay` options. For targeted work, validate with
the same `--ids` list used to emit the template.

The validator proves structural coverage, canonical order, status vocabulary, and evidence-anchor
resolution within the selected core, overlays, or targeted IDs. Targeted validation never proves
complete readiness. `scripts/validate_skill.py` is contributor infrastructure, not part of normal
component reviews.

### 10. Invite privacy-safe feedback

After delivering the report, offer to prepare a sanitized feedback issue for the maintainers of
this skill. Read `references/feedback.md` and keep this separate from the component verdict.

- Ask before creating or publishing anything.
- Prefer the scrubbed run-observations note over the full report or session transcript.
- Show the user the destination, title, and final issue body before publication.
- Use the issue tracker of the repository that hosts this skill when it is known; otherwise provide
  a ready-to-paste issue body.
- Do not share a session link by default. Session history can contain source, private URLs,
  organization context, or credentials. Include a session link only when the user explicitly asks
  for it, confirms its access scope, and approves the reviewed contents.
- If the user declines, do not repeat the request.

Use a short invitation such as:

> Would you like me to prepare a sanitized feedback issue for the maintainers of this skill? It
> will describe what helped and where the workflow was unclear without including reviewed code or
> private evidence. I will show you the issue before publishing it.

### 11. Decide whether to expand

Recommend another control only after judging whether this review was useful. Choose a materially
different risk profile, and never imply one component proves catalog-wide readiness.

## Improving the skill

When asked to improve the workflow after a run, follow `references/learning-loop.md`. Generalize
only repeated evidence-backed lessons, add a regression to `evals/regression.vally.yaml`, preserve
component-specific facts outside the public core, and keep a no-defect canary.
