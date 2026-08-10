# Evaluation anti-overfit policy

This policy applies to both `aspnetcore-pr-review` and `aspnetcore-try-fix`. It
protects their evaluation sets from optimizing for a small, recognizable
collection of prompts.

## Retention and scoring

Retain a regression once it is discovered. A lower score weight is not a reason
to delete, weaken, or stop running a regression. Score changes only affect
aggregation; they do not change the required behavioral evidence.

Aggregate scores by taking the mean within each `(tier, score_family)` and then
macro-average families in that tier. Consequently, every eval has normalized
family weight `1 / (number of families in its tier * number of evals in its
tier and family)`; adding near-duplicates cannot increase that family's
influence.

Designate held-out cases before changing the skill, and do not tune prompts,
examples, instructions, or scoring against them. A held-out failure may motivate
a new, separately provenanced train regression, but the original held-out case
remains unchanged.

## Instruction promotion

A regression does not automatically justify another global instruction. Promote
a rule into the always-loaded skill only when:

1. A retained before-change result fails for the reason the rule addresses.
2. The same mechanism transfers to an independently provenanced case outside
   the source PR or subsystem.
3. Held-out no-defect and bounded-stateless canaries do not acquire extra
   blockers or unnecessary lifecycle work.
4. The rule can be stated without source-PR nouns. Otherwise keep it in a
   conditional domain reference.
5. The addition consolidates or replaces narrower guidance when possible,
   rather than growing the skill indefinitely.

Passing only the regression that motivated a rule shows memorization, not
generalization.

## Metadata and controls

Every eval has `eval_metadata`. `mechanism` and `score_family` are lower
kebab-case labels; provenance identifies a PR, historical case, or synthetic
source. `controls.positive` and `controls.negative` are disjoint, nonempty,
zero-based indexes into `expectations`. Positive controls identify evidence that
must be present; negative controls identify an overclaim, unrelated scaffold,
mutation, or side effect the evaluator must reject or avoid. These are
expectation-level grading controls, not substitutes for matched scenario
controls.

Every new defect regression also needs a matched no-defect, alternate-cause, or
scope-control scenario in the same score family before its lesson becomes a
global instruction. The held-out no-defect and bounded-stateless cases are
permanent complexity-inflation canaries.

Discovery prompts must list nonempty `forbidden_prompt_terms`. Those terms must
not occur in the prompt, case-insensitively. Verification prompts may use an
empty list, but every term listed is still forbidden. Keep issue numbers,
implementation names, answer phrases, and other answer-revealing vocabulary out
of discovery prompts. Discovery evals receive frozen evidence through `files`;
removing facts from a prompt without supplying a fixture makes the eval
ungradeable rather than de-leaked.

Held-out evals carry a `frozen_hash`. The validator recomputes it from the full
eval, excluding the hash field itself, so changes to a held-out fixture contract
are explicit. Train and held-out provenance must remain disjoint within a suite.

## Maintenance

Use ablations before accepting a new mechanism or scoring rule: remove the
claimed signal and confirm that the score changes for the intended reason. Prune
only a duplicate or disproven eval, recording the replacement or rationale;
never prune a regression merely because it is inconvenient.

Each expectation must reject a crafted bad result and accept a correct
paraphrase. An expectation that rejects neither is non-discriminating; one that
rejects the paraphrase is a wording matcher. Keep discovery prompts limited to
the evidence a reviewer would receive. Put the mechanism to discover in the
expected result, not in the prompt.

The validator reports family, tier, provenance, and prompt/expectation-overlap
concentration as warnings. These are investigation signals, not arbitrary
acceptance quotas: unusual distributions can be legitimate and must be judged
with provenance and transfer evidence.

Report family-macro and provenance-macro results separately. A source PR can
teach several real mechanisms; one scalar must not let its spread across
families hide poor transfer to other provenance.

Before accepting eval changes, run:

```bash
python3 .github/skills/aspnetcore-pr-review/scripts/validate_evals.py \
  .github/skills/aspnetcore-pr-review/evals/evals.json \
  .github/skills/aspnetcore-try-fix/evals/evals.json
```

Other repository eval harnesses use different schemas. This suite therefore
uses a small explicit score contract rather than guessing how to consume their
results: a JSON object keyed by `skill_name`, then eval ID, with numeric scores
from zero to one. Run `scripts/aggregate_eval_scores.py` with both eval files
and `--scores <path>` to report raw, family-macro, provenance-macro, and
train-to-held-out transfer results separately.
