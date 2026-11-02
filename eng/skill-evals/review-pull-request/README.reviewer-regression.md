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
The normal review artifact is authoritative. The appended
`REVIEWER_REGRESSION_RESULT` block is only a convenience index: the grader
cross-checks each indexed tier against `NEW_FINDINGS`, questions, `UNRESOLVED`,
and `DISCARDED` using the candidate ID or exact path and line. A missing
artifact section or any mismatch produces `INTEGRITY_FAIL`. Missing rows,
malformed output, grader errors, and required-case failures also fail closed.
Novel findings without a valid confidence tier fail their result contract.
Known partial-projection seams at `FormDataReader.cs:143` and
`FileConverter.cs:56` through `FileConverter.cs:63` are adjudicated as allowed
`PLAUSIBLE` observations for #69782; other novel `VERIFIED`, `QUESTION`, or
`PLAUSIBLE` findings produce `ADJUDICATION_PENDING`. The prompt grader is
secondary evidence only. Its threshold is 1, and the assertion script derives
semantic pass counts from `score > 0` rather than trusting the retained
`passed` Boolean. Vally 0.19.0 treated completed score-0 prompt results as
`passed=true` when this suite previously used threshold 0, even though prompt
weight was zero. Vally 0.19.0 retains its rubric rows under `gradeResult.details` and
`metadata.rubric_scores`, but not arbitrary judge-defined JSON.

`assert_reviewer_regression.ps1` requires all five runs for both arms and both
stimuli. Execution, result-contract, integrity, unresolved adjudication, and
#70106 restraint failures remain absolute gates. The directional comparison is
reported separately as `NO_REGRESSION`, `INDETERMINATE`, or `REGRESSION`.
#69782 compares `acceptance/V23` directly, so V23 is gated by candidate
non-inferiority to current rather than a hard five-of-five floor. Equal arms
are `INDETERMINATE`, not a directional regression, even if a separately named
absolute gate fails.

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
