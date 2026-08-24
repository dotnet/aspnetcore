# Evaluation policy

The two suites have distinct ownership:

- `eval.vally.yaml` is the auto-discovered baseline-versus-skilled lane. Its five duplicated cases
  provide bounded, high-discrimination signal for artifact truth, accessibility layering,
  cross-area completeness, a no-defect control, and targeted status boundaries.
- `regression.vally.yaml` is the explicitly invoked exhaustive governance suite. It owns all 24
  cases, requirement-prefix coverage, train/held-out tiers, score families, controls, provenance,
  architecture portability, and held-out refresh.

The five standard cases intentionally duplicate regression cases 01, 02, 10, 11, and 18. The C#
skill validator requires their names, prompts, tags, fixture bindings, and rubrics to remain
identical so the fast lane cannot silently diverge from the governed corpus.

## Governance

- The specialized regression suite must cover every canonical requirement prefix.
- Every eval records provenance, train/held-out tier, score family, and positive/negative controls.
- Retain scope-control and no-defect canaries.
- Treat the core rubric as the owner of requirement scope; eval outputs must not reclassify core IDs.
- Require the exact `not applicable` status token rather than aliases such as `N/A`.
- Require full immutable EV1 identities, complete embedded source-ledger membership, and exact
  repository/package/component compatibility; report-local `E-###` is legacy-only.
- Require explicit stable or legacy validation mode, schema-3 report/companion/input binding, and
  limited schema-2 compatibility wording.
- Add a regression when a general rule changes; do not encode one component's nouns as global
  guidance.
- Held-out failures may motivate a separately provenanced train case, but do not tune the held-out
  prompt itself.
- Expectations must accept a correct paraphrase and reject a plausible overclaim or omission.

## Generate and validate

```bash
source activate.sh
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  validate-skill --skill-dir .github/skills/blazor-component-readiness
dotnet test \
  eng/tools/BlazorComponentReadiness.Tests/BlazorComponentReadiness.Tests.csproj
```

The repository runner pins the exact `@microsoft/vally-cli@0.13.0` package:

```powershell
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Test
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Validate
```

## Explicit regression run

Run the complete specialized suite:

```powershell
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Run `
  -Eval eng/skill-evals/blazor-component-readiness/regression.vally.yaml
```

The specialized spec declares exactly the component-readiness runtime skill so this direct Vally
run exercises the skill. The standard spec does not declare a skill because the repository
experiment owns its baseline-versus-skilled variants.

## Retained comparison

Use all cases and five trials with the pinned executor and judge models. Preserve JSONL output,
Vally diagnostics, resolved CLI version, skill revision, and timestamp. Compare train and held-out
results separately as well as overall; do not allow several similar cases in one score family to
hide failure in another family.

Vally prompt grading is probabilistic. Deterministic scripts remain authoritative for checklist
structure, requirement coverage, status vocabulary, and generated-spec synchronization.
