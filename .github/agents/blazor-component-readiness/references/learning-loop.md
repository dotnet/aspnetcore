# Agent learning loop

Use this only after a completed review and only when asked to improve the agent or capture lessons.

## Classify each lesson

| Class | Destination |
|---|---|
| Public baseline requirement change | `references/checklist.md` |
| General review-method improvement | `.github/agents/blazor-component-readiness.agent.md` or an area playbook |
| Output-shape improvement | `references/report-template.md` |
| Feedback-collection improvement | `references/feedback.md` |
| Component/repository-specific fact | Keep with the review evidence, not in the public core |
| One-off investigation detail | Do not add to the agent |

Do not turn a component defect into a general requirement. Extract the reusable evidence rule,
probe recipe, or classification boundary.

## Preserve evidence boundaries

For component-specific lessons, record exact SHA/package, date, evidence type, smallest
reproduction, freshness requirements, and whether it is a positive, defect, evidence gap, or
non-finding. Never present a component fact as current without checking drift.

Retain reusable facts only as immutable atomic records in canonical source ledgers. Never edit or
rebind an EV1 identity. A changed observation gets a new identity-bearing record; use `supersedes`
only when reviewer judgment supports the structural relationship, and keep the prior record.

## Require repeated evidence for core changes

Prefer a core workflow change when it:

- generalized across at least two independent runs or follows directly from a public standard;
- removes demonstrated friction without weakening evidence quality;
- remains useful across maintainers and component architectures;
- has a regression that would have failed before the change.

Keep tentative ideas in run observations until they meet that bar.

## Add or update an eval

Use `eng/skill-evals/blazor-component-readiness/eval-policy.md` to select the owning suite. Keep the
five-case `representative.vally.yaml` corpus bounded to high-discrimination behavior. Add exhaustive
governance cases to `regression.vally.yaml`; when a representative case is duplicated, update both
copies together. Retain scope-control and no-defect canaries so new guidance does not manufacture
findings or expand into catalog audits.

Validate agent maintenance changes with:

```bash
source activate.sh
dotnet run --project eng/tools/BlazorComponentReadiness/BlazorComponentReadiness.csproj -- \
  validate-agent --agent-profile .github/agents/blazor-component-readiness.agent.md
dotnet test \
  eng/tools/BlazorComponentReadiness.Tests/BlazorComponentReadiness.Tests.csproj
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Lint
```

Those checks are model-free. Run the bounded representative corpus through the real custom agent
only when model-bearing validation is intended:

```powershell
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 RunAgent
```

Select `regression.vally.yaml` explicitly only for a cost-approved exhaustive governance run.

## Review for overfitting

- Would this help another maintainer and architecture?
- Does it preserve maintainer/reviewer ownership?
- Does it distinguish missing evidence from a defect?
- Does it keep one-component scope?
- Can a strong component still receive a mostly positive result?
