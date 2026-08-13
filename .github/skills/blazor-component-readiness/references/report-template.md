# Report templates

## Readiness report

Keep the decision document concise. Put complete coverage evidence in annexes.

1. Executive result
2. Scope, exact snapshots, rubric version, review mode, and timebox
3. Verified positives
4. Highest-priority defects
5. Maintainer evidence requests
6. Security, accessibility, render-mode, trimming, and release conclusions
7. Recommended next decision
8. Annex A: complete scorecard
9. Annex B: evidence ledger
10. Annex C: structural validation receipt
11. Annex D: repository cleanliness and limitations

Lead with what was exercised successfully, whether the component is ready for the stated use,
decisive blockers, material evidence gaps, and the bounded next decision. Avoid certification
language unless the user defines a formal gate.

## Complete scorecard annex

Use exactly:

| Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |
|---|---|---|---|---|---|---|
| LP-01 | Uses an OSI-approved, non-copyleft license. | repository-wide | verified | [E-001] | - | - |

Generate the complete table with:

```bash
python3 scripts/validate_scorecard.py --emit-template
```

Add `--overlay scaffolder` or `--overlay ai-skill` only when applicable.

Rules:

- Include all 110 core IDs exactly once in checklist order.
- Include every ID from each selected overlay; omit unselected overlays entirely.
- Use only the five statuses defined by the skill.
- Explain every status directly or through a resolved evidence anchor.
- Give a concrete maintainer action for `maintainer evidence required`.
- Give a bounded reviewer follow-up for `not tested`.
- Link concrete defects to detailed finding blocks.
- Validate with
  `python3 scripts/validate_scorecard.py <report.md> --receipt <validation-receipt.json>`.

## Targeted follow-up

Lead with a prominent scope statement:

```markdown
**Review mode:** Targeted follow-up for BEQ-12 and BEQ-15 only. This is not a complete readiness
review, and unchanged requirements were not reverified.
```

Emit and validate only the named IDs:

```bash
python3 scripts/validate_scorecard.py --ids BEQ-12,BEQ-15 --emit-template
python3 scripts/validate_scorecard.py --ids BEQ-12,BEQ-15 <targeted-report.md>
```

Use the same scorecard columns, evidence anchors, and finding blocks. A targeted report contains:

1. exact snapshot and prior evidence source;
2. named requirement IDs and reason for follow-up;
3. changed evidence and findings;
4. targeted scorecard;
5. bounded next action.

Do not repeat unchanged repository-wide findings or imply a complete adoption/release decision.

## Shared repository-wide evidence

For batched controls with the same repository SHA and package ID/version/digest, create one shared
ledger using the evidence-ledger columns below. Import exact rows into each control report and record
the source ledger/report in `Reproduction/source`; use `Rechecked now?` to distinguish direct recheck
from imported evidence. A later report may supersede a row only with stronger exact-snapshot proof.
Never reuse component-specific runtime evidence across controls.

## Evidence anchors

Use anchors to avoid repeating the same exact package, workflow, or attestation evidence across
multiple rows:

| Evidence ID | Claim | Repository/SHA or package | Evidence type | Reproduction/source | Rechecked now? |
|---|---|---|---|---|---|
| E-001 | Repository license is MIT. | owner/repo@SHA | source | `LICENSE` | yes |

Anchors must match `E-\d{3}`. Every scorecard reference such as `[E-001]` must resolve to exactly
one ledger row. The ledger entry must contain the substantive proof, gap, omission, or rationale;
an anchor is not permission to use generic evidence.

## Structural validation receipt

Generate a receipt with:

```bash
python3 scripts/validate_scorecard.py <report.md> --receipt <validation-receipt.json>
```

Attach or summarize:

```markdown
**Structural validation:** Passed for rubric [version], [complete/targeted] selection, [row count]
canonical rows. Receipt: `[path]`, report SHA-256 `[digest]`.

This proves scorecard structure, selected coverage, canonical order, status vocabulary, and
evidence-anchor resolution. It does not prove that the evidence or classifications are factually
correct.
```

## Maintainer handoff

```markdown
# [Component] readiness handoff

**Verdict:** [One sentence.]

**Strongest positives:** [Verified behavior and artifact evidence.]

**Highest-priority defects:** [Actionable, reproducible defects only.]

**Evidence maintainers should supply:** [Attestations and inaccessible records.]

**Maintainer questions:** [Five to seven bounded questions.]

**Recommended next step:** [Decision and exact release-candidate request.]
```

## Finding block

```markdown
### [Finding ID] [Title]

- **Requirement IDs:**
- **Repository/SHA or package:**
- **Affected path/member/artifact:**
- **Expected:**
- **Observed:**
- **Reproduction/direct proof:**
- **Owning layer:**
- **Requirement scope:** component-specific / repository-wide
- **Root-cause scope:** component / generator or schema / shared runtime / release infrastructure
- **Confidence:**
- **Remediation direction:**
```

## Run-observations note

Use the compact template in `references/feedback.md`. Keep workflow feedback separate from the
component verdict so it can be shared with skill maintainers without exposing reviewed code.
