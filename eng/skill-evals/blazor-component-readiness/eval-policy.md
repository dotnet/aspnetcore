# Evaluation policy

The two retained suites have distinct ownership:

- `representative.vally.yaml` is the bounded five-case corpus. Its duplicated cases provide
  high-discrimination coverage for artifact truth, accessibility layering, cross-area completeness,
  a no-defect control, and targeted status boundaries.
- `regression.vally.yaml` is the explicitly invoked exhaustive governance suite. It owns all 24
  cases, requirement-prefix coverage, train/held-out tiers, score families, controls, provenance,
  architecture portability, and held-out refresh.

The five representative cases intentionally duplicate regression cases 01, 02, 10, 11, and 18. The
C# agent validator requires their names, prompts, tags, fixture bindings, and rubrics to remain
identical so the bounded corpus cannot silently diverge from the governed corpus.

## Vally 0.13 custom-agent bridge

Vally 0.13.0 does not natively select a repository `.agent.md` custom-agent profile. The repository
bridge in `copilot-agent-executor.mjs` supplies that missing executor boundary by invoking the
Copilot CLI with the unchanged stimulus and native
`--agent blazor-component-readiness` selection. Both suites select
`blazor-component-readiness-agent` as their Vally executor and must not declare
`environment.skills`.

The bridge stages the exact repository profile and its resources into each isolated Vally
workspace, validates and records the profile digest and CLI version, probes an unknown agent to
prove resolution fails closed with the expected profile as the sole available custom agent, and
rejects malformed or incomplete CLI event streams. Copilot's session event schema does not expose
the selected custom-agent name, so this isolated native-resolution probe is the strongest available
identity attestation; the bridge does not invent one or inject profile text into the stimulus.

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
  validate-agent --agent-profile .github/agents/blazor-component-readiness.agent.md
dotnet test \
  eng/tools/BlazorComponentReadiness.Tests/BlazorComponentReadiness.Tests.csproj
```

The repository runner pins the exact `@microsoft/vally-cli@0.13.0` package:

```powershell
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Test
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Validate
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 Lint
```

`Validate` proves that the generic experiment does not discover these custom-agent corpora. `Lint`
strictly validates both Vally specifications and their fixtures without invoking a model.

Model-bearing execution is always explicit. The default command runs the bounded representative
suite with one worker; additional Vally arguments can narrow trials further:

```powershell
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 RunAgent
pwsh -NoLogo -NoProfile -File eng/skill-evals/run.ps1 RunAgent `
  -Eval eng/skill-evals/blazor-component-readiness/regression.vally.yaml
```

The exhaustive regression command is deliberately cost-gated by its explicit path. For a governed
comparison, use all cases and five trials with the pinned executor and judge models. Preserve JSONL
output, bridge artifacts, Vally diagnostics, resolved CLI version, profile digest, model and
reasoning identities, and timestamp. Compare train and held-out results separately as well as
overall; do not allow several similar cases in one score family to hide failure in another family.

Vally prompt grading is probabilistic. Deterministic scripts remain authoritative for checklist
structure, requirement coverage, status vocabulary, and generated-spec synchronization.
