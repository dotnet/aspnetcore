# Report templates

## Validated source report

The source report is the evidence record used for structural validation. Keep direct observations
separate from optional reviewer synthesis.

1. Scope, exact snapshots, rubric version, review mode, and timebox
2. Classification counts
3. Direct findings needed to substantiate scorecard classifications
4. Annex A: complete scorecard
5. Annex B: evidence ledger
6. Annex C: structural validation receipt
7. Annex D: repository cleanliness and limitations

Do not add a verdict, ranked findings, remediation plan, acceptance gate, or next decision by
default. Those are reviewer synthesis rather than the assessment record. Produce them only when the
user explicitly requests a decision brief.

## Evidence-only evaluation result

Use this as the default presentation when copying evaluation results into an issue, project draft,
ticket, or comparable tracker. It has exactly one shape. Do not introduce local variations: two
reports of the same rubric must differ only in their content.

Emit these `##` sections, in this order, and no others:

1. `# [Component] readiness assessment — [package] [version]` title line, followed by the
   privacy/scope callout and the AI-review limitation callout.
2. `## Areas we believe need to be fixed`
3. `## Full report`
4. `## Exact review scope`
5. `## Review-result counts`
6. `## Status terminology`
7. `## Complete rubric requirement mapping`
8. `## Evidence ledger`
9. `## Structural validation and limitations`

Validate the finished body before writing it to any tracker:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  tracker --skill-dir .github/skills/blazor-component-readiness \
  --evidence-bundle <evidence.json> --source-report <report.md> \
  --provenance-input <target-manifest.json> \
  --shared-row-projection <shared-row-projection.json> <tracker-body.md>
```

Omit `--shared-row-projection` only for a review that does not import a shared repository
foundation. If the tracker declares the source-report digest, `--source-report` supplies the live
bytes that declaration must match. Omit the example `--provenance-input` when the tracker declares
no additional artifact digest; otherwise repeat it once per live digest-bearing input.

GitHub persists a tracker body without a terminal newline. Write the artifact without one, and read
it back with `jq -j` rather than `--jq`, which appends a newline and hides a one-byte difference.

### Areas we believe need to be fixed

Open the section with exactly this sentence, substituting digits:

```markdown
The {defect count} canonical `defect` rows in the full report consolidate into the {area count}
areas below. These areas are not ordered by priority and require human confirmation. Each should be
confirmed against the linked evidence before it is treated as a final product or release
determination.
```

Then give a concise table that groups every canonical `defect` row into evidence-backed themes:

| Area | What we believe needs attention | Requirement IDs | Evidence |
|---|---|---|---|
| [Descriptive area] | [Concise observation derived from defect evidence.] | `[IDs]` | [Evidence anchors] |

Represent every `defect` ID exactly once, retain evidence anchors, and do not include
`maintainer evidence required` or `not tested` rows as things that need to be fixed. This is a
bijection: the validator fails if any defect is missing or any non-defect appears. Do not add
remediation steps, acceptance gates, a verdict, or inferred next actions.

Follow the summary with:

> **Feedback requested:** Please let us know if any item above is a false positive, misses important
> context, or is not useful. Specific examples will help us correct this report and improve future
> reviews.

### Full report

Place the complete canonical assessment under `## Full report` at the bottom of the tracker item,
led by the sentence `The complete 110-requirement assessment and evidence ledger follow unchanged.`
Preserve its content rather than replacing it with the summary. Include exact scope and artifact
identity, status counts and terminology, every selected canonical requirement row, the complete
evidence ledger, and the structural coverage statement.

Use exactly these columns in the presented requirement table, with the canonical status in
backticks:

| Requirement ID | Requirement | Requirement scope | Canonical status | Review result | Evidence |
|---|---|---|---|---|---|
| LP-01 | Uses an OSI-approved, non-copyleft license. | repository-wide | `verified` | Copilot-reviewed positive evidence | [EV1-<64 lowercase hex>] |

`Review result` is a cautious display label for a partner audience. It is derived, not judged: it is
a total function of the canonical status and carries no independent meaning. Use this mapping
verbatim, in `## Review-result counts` as well as in the requirement table:

| Canonical status | Review result |
|---|---|
| `verified` | Copilot-reviewed positive evidence |
| `defect` | Potential issue identified |
| `maintainer evidence required` | Maintainer confirmation requested |
| `not tested` | Not tested by this review |
| `not applicable` | Not applicable to reviewed scope |

Never drop the canonical status, reorder the statuses, or invent an alternative label. The counts in
`## Review-result counts` must be derived from the presented rows rather than written by hand; the
validator recomputes them and fails on any disagreement.

Canonical status tokens are exact and case-sensitive. Use `not applicable`; do not emit `N/A`,
capitalization variants, or aliases.

Copy requirement wording, status, and evidence references from the validated source report. Copy
core requirement scope from the pinned rubric, not from a report-authored classification; the
source and tracker validators reject overrides.
Do not copy the source report's maintainer-action or reviewer-follow-up columns into this result
unless the user explicitly asks for recommendations. The grouped defect-area summary is not a
priority narrative; exclude ranked priorities, remediation directions, recommended next steps, and
acceptance gates by default.

Retain public reproduction anchors such as source paths, commands, workflow run IDs, artifact
digests, and probe descriptions in the evidence ledger. Scrub credentials, private URLs, local
absolute paths, and unrelated workplace context rather than dropping the entire source column. For
a private probe artifact, retain a non-sensitive artifact basename and the probe method instead of
leaving a generic "private artifact" reference.

Attribute the requirements to the bundled rubric. Never claim that rubric wording was copied,
derived, or row-mapped from an external policy or requirements document unless a requirement-level
crosswalk was completed and retained. A supplied category summary may inform scope but is not
requirement-level provenance.

## Complete scorecard annex

Use exactly:

| Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |
|---|---|---|---|---|---|---|
| LP-01 | Uses an OSI-approved, non-copyleft license. | repository-wide | verified | [EV1-<64 lowercase hex>] | - | - |

Generate the complete table with:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  scorecard --skill-dir .github/skills/blazor-component-readiness --emit-template
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
  `dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- scorecard --skill-dir .github/skills/blazor-component-readiness --evidence-bundle <evidence.json> --shared-row-projection <shared-row-projection.json> --provenance-input <target-manifest.json> <report.md> --receipt <validation-receipt.json>` for a shared-foundation batch that declares the target-manifest digest. Omit the projection option for an independent review and omit the provenance-input option when no additional live artifact digest is declared.

## Targeted follow-up

Lead with a prominent scope statement:

```markdown
**Review mode:** Targeted follow-up for BEQ-12 and BEQ-15 only. This is not a complete readiness
review, and unchanged requirements were not reverified.
```

Emit and validate only the named IDs:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  scorecard --skill-dir .github/skills/blazor-component-readiness \
  --ids BEQ-12,BEQ-15 --emit-template
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  scorecard --skill-dir .github/skills/blazor-component-readiness \
  --ids BEQ-12,BEQ-15 --evidence-bundle <evidence.json> <targeted-report.md>
```

Use the same scorecard columns, evidence anchors, and finding blocks. A targeted report contains:

1. exact snapshot and prior evidence source;
2. named requirement IDs and reason for follow-up;
3. changed evidence and findings;
4. targeted scorecard;
5. bounded next action.

Do not repeat unchanged repository-wide findings or imply a complete adoption/release decision.

## Shared repository-wide evidence

For batched controls with the same repository SHA and package ID/version/digest, build one immutable
repository source ledger. Each report companion embeds its complete canonical bytes and selects an
explicit subset. Released-package repository ledgers may cross controls under exact identity;
source-only repository and component ledgers require the exact component ID. There is no authored
direct/imported/rechecked state.

Also retain one semantic-versioned shared-row projection keyed to that repository ledger. The
producer prefix is intentionally open, but the schema must end in `shared-row-projection/v1`:

```json
{
  "schema": "<producer>/shared-row-projection/v1",
  "purpose": "Canonical repository-wide projection for a batch.",
  "owner": "<coordinator>",
  "import_rule": "Copy every projected field unchanged.",
  "identity": {
    "repository_uri": "https://github.com/example/components",
    "reviewed_assessment_commit": "<40 lowercase hex>"
  },
  "bound_artifacts": {
    "repository_ledger_path": "repository-ledger.json",
    "repository_ledger_sha256": "<64 lowercase hex>"
  },
  "rubric": {
    "version": "1.3.0",
    "scope_schema_version": 1,
    "row_count": 1
  },
  "rows": [
    {
      "requirement_id": "LP-01",
      "requirement": "Uses an OSI-approved, non-copyleft license.",
      "requirement_scope": "repository-wide",
      "status": "verified",
      "evidence": ["EV1-<64 lowercase hex>"],
      "evidence_anchors": "[EV1-<64 lowercase hex>]",
      "maintainer_action": "-",
      "notes": "-"
    }
  ]
}
```

The projection contains every repository-wide row represented by the report or tracker. Source
validation compares all six imported fields; tracker validation compares its four presented fields.
This prevents a child report from silently dropping or locally deriving shared actions, follow-ups,
or classifications.

## Stable evidence companion and projection

Every record originates in a canonical `repository` or `component` source ledger and has a full
content-addressed `EV1-` plus 64 lowercase hex ID. Build a self-contained companion with:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  ledger build --kind repository --subject <repository-subject.json> \
  --nupkg <exact.nupkg> <repository-draft.json> --output <repository-ledger.json>
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  ledger build --kind component --subject <assessment.json> \
  --nupkg <exact.nupkg> <component-draft.json> --output <component-ledger.json>
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  ledger bundle --assessment <assessment.json> \
  --source-ledger <repository-ledger.json> --source-ledger <component-ledger.json> \
  --ids <EV1,...> --output <evidence.json>
```

Released-package builds require `--nupkg` so the routed tool derives and compares nuspec ID/version
and the total package digest. Source-only subjects omit `--nupkg`.
Individual authored ledger JSON inputs remain capped at 4 MiB. The final self-contained bundle and
all public report/bundle/receipt reads share a 64 MiB serialized-artifact ceiling.

The report contains exactly one canonical `bcr-assessment-v1` block and one selected-record table:

| Display order | Evidence ID | Claim | Requirement scope | Component ID | Source ledger kind | Source ledger SHA-256 | Provenance kind | Reproduction/source | Captured at UTC | Content SHA-256 |
|---:|---|---|---|---|---|---|---|---|---|---|
| 1 | EV1-<64 lowercase hex> | Repository license is MIT. | repository-wide |  | repository | <64 lowercase hex> | repository-path | `LICENSE` | 2026-08-16T20:00:00Z | <64 lowercase hex> |

Every selected record appears exactly once in companion order, every scorecard evidence cell contains
at least one selected full anchor, and every selected record is referenced. The validator compares
the assessment and every projection field with the companion. Content SHA-256 is a commitment only;
it does not establish retained or available source bytes. Complete embedded ledgers may include
unselected historical records; those records remain audit context and must not appear in the selected
projection unless the current scorecard cites them.

Any 64-lowercase-hex SHA-256 literal written into the stable report or tracker is a provenance claim.
Validation fails unless it resolves to a supplied live input or canonical embedded identity. Do not
copy superseded report, bundle, or ledger hashes into validated prose. For an additional declared
artifact such as a target manifest or retained probe receipt, pass its exact bytes with repeated
`--provenance-input <path>` options. This is explicit trust: digests merely mentioned inside an input
do not become recursively allowed.

## Structural validation receipt

Generate a receipt with:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  scorecard --skill-dir .github/skills/blazor-component-readiness \
  --evidence-bundle <evidence.json> \
  --provenance-input <target-manifest.json> \
  --shared-row-projection <shared-row-projection.json> <report.md> \
  --receipt <validation-receipt.json>
```

The projection is optional for an independent review. When supplied, its digest is part of the
receipt's closed validation-input manifest. The provenance-input example is optional when the report
declares no additional input digest. Each repeated provenance input is captured as
`provenance-inputs/####`; preserve argument order for later revalidation. Supply at most 32 explicit
provenance inputs totaling no more than 64 MiB.

Attach or summarize:

```markdown
**Structural validation:** Passed for rubric [version], [complete/targeted] selection, [row count]
canonical rows. Receipt schema 3: `[basename]`, checklist SHA-256 `[checklist digest]`, scope schema
[version], report SHA-256 `[report digest]`, evidence bundle SHA-256 `[bundle digest]`.

This proves scorecard structure, selected coverage, canonical order, status vocabulary, and
evidence-anchor resolution. It does not prove that the evidence or classifications are factually
correct.
```

Before publishing, validate the receipt against the exact historical skill inputs:

```bash
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  receipt validate --skill-dir <exact-historical-skill-snapshot> \
  --evidence-bundle <evidence.json> \
  --provenance-input <target-manifest.json> \
  --shared-row-projection <shared-row-projection.json> --report <report.md> \
  <validation-receipt.json>
```

Pass the same provenance inputs in the same order used during receipt creation, or omit them when the
receipt contains none. This detects later report/companion/input mutation. The unsigned self-reported validator hash does not
authenticate execution; `--producer-validator <archived-assembly>` checks byte correspondence only.
Historical schema-2 artifacts remain byte-compatible through explicit `--legacy-evidence`; their
limited success does not establish exact historical overlay/input provenance.

## Optional decision brief or maintainer handoff

Create this only when the user explicitly requests synthesis, recommendations, prioritization,
remediation guidance, a verdict, or next steps. Label it as reviewer synthesis rather than canonical
rubric content.

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
