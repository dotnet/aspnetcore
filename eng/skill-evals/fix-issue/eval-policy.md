# Fix-issue Vally evaluation policy

This suite follows the shared
[`fix-challenge` evaluation anti-overfit policy](../fix-challenge/eval-policy.md).
Its canonical cases live in
`eng/skill-evals/fix-issue/regression.vally.yaml`.

The suite covers a natural pre-fix issue, no-defect abstention, unresolved
product intent, and unavailable independent orchestration. Synthetic
defect/control calibration remains in `fix-challenge`; it is not the primary
`fix-issue` benchmark.

Before accepting `fix-issue` changes:

```powershell
pwsh eng/skill-evals/reviewer-suites/scripts/Validate-Evals.ps1 `
  -Path eng/skill-evals/fix-issue/regression.vally.yaml
pwsh eng/skill-evals/reviewer-suites/scripts/Test-ReviewerEvalTools.ps1 `
  -Suite Issue
```

Then strict-lint with the pinned Vally 0.13.0 package. Model-bearing eval runs
are optional hosted evidence and are not required for deterministic validation.
