# Reviewer regression suite

This specialized Vally suite freezes two development PRs and checks the
`review-pull-request` skill's final publication decisions. It is separate from
the protocol/trace `eval.vally.yaml` suite proposed in #70055.

## First-PR scope

- `reviewer-regression.cases.json` retains the 49-row labels-v4 development
  corpus: 29 GOOD, 13 BAD, and 7 OTHER.
- The seven OTHER rows are pending and report-only.
- The first executable slice uses #70106 and #69782. V21 and L69 must remain
  `DISCARDED`; V23 must be `VERIFIED` or `QUESTION`. `PLAUSIBLE` maps to
  UNRESOLVED and does not count as surfaced.
- V32 and V36 are recorded for calibration but are not enforced.
- `fixtures/reviewer-regression/**` contains source-bound focused projections,
  not full repository clones.

The deterministic program grader records separate execution, surfaced,
tier-justified, acceptance, and integrity outcomes for every expected case ID.
Missing rows, malformed output, grader errors, and required-case failures fail
closed. Novel plausible findings produce `ADJUDICATION_PENDING` rather than
being rejected automatically. The prompt grader is secondary evidence only;
Vally 0.19.0 retains its rubric rows under `gradeResult.details` and
`metadata.rubric_scores`, but not arbitrary judge-defined JSON.

`assert_reviewer_regression.ps1` requires all five runs for both arms and both
stimuli, checks the required deterministic outcomes, and applies a per-stimulus
candidate-not-worse-than-current comparison. Equal pass counts are reported as
advisory `INDETERMINATE` until null calibration exists.

Use `run_reviewer_regression.ps1`. The wrapper materializes a temporary
experiment so current and candidate skill directories can differ without
changing the shared `eng/skill-evals/run.ps1`.

```powershell
./generate_reviewer_regression.ps1 -Check
./check_reviewer_regression.ps1
./run_reviewer_regression.ps1 DryRun
./run_reviewer_regression.ps1 Run -CurrentSkill <path> -CandidateSkill <path>
./assert_reviewer_regression.ps1 -Results <results.jsonl-or-directory>
```

No model or judge run is required for generator, checker, dry-run, or assertion
self-validation. Cost and latency come from retained Vally trial records and
must be reported per variant and stimulus when paid runs are authorized.

## Reuse from #70205

Reused and relocated:

- focused common/source fixtures for #70106 and #57972;
- grading-environment label hiding;
- binary cross-family semantic rubric structure;
- the five judge-development control concepts.

Modified:

- `trials` is corrected to `defaults.runs`;
- normal skill subagents are allowed;
- deterministic publication and tier checks are the hard gate;
- the invented-provider control is treated as a false evidence claim, not as
  a successful DISCARDED-polarity outcome.

Dropped:

- evidence-scout and claim-validation treatments;
- forked/stale `SKILL.md` copies;
- the forced single-reviewer prompt;
- full-clone fixtures.
