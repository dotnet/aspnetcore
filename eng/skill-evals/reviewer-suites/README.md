# Reviewer suite support

This directory contains eval-only tooling shared by the specialized
`fix-challenge`, `fix-issue`, and shared `fix-candidate` capability and
regression suites.

The repository runner auto-discovers only `<skill>/eval.vally.yaml`. The named
reviewer specs remain explicit suites:

- `eng/skill-evals/fix-challenge/regression.vally.yaml`
- `eng/skill-evals/fix-challenge/model-guardrail.vally.yaml`
- `eng/skill-evals/fix-candidate/regression.vally.yaml`
- `eng/skill-evals/fix-issue/regression.vally.yaml`

Select one with the repository runner's `-Eval` option or pass it directly to
Vally with `--eval-spec`/`-e`. These suites stage both discoverable workflow skills together,
so they use `scripts/Stage-ReviewerSkills.ps1` rather than the standard
one-skill baseline-versus-skilled experiment lane.

The scripts here validate reviewer-specific governance, stage runtime-only skill
content plus the in-repository candidate contract, and aggregate
specialized-suite results. They use exactly
`@microsoft/vally-cli@0.13.0`; they are not runtime skill dependencies.
