# Skill learning loop

Use this only after a completed review and only when asked to improve the skill or capture lessons.

## Classify each lesson

| Class | Destination |
|---|---|
| Public baseline requirement change | `references/checklist.md` |
| General review-method improvement | `SKILL.md` or an area playbook |
| Output-shape improvement | `references/report-template.md` |
| Feedback-collection improvement | `references/feedback.md` |
| Component/repository-specific fact | Keep with the review evidence, not in the public core |
| One-off investigation detail | Do not add to the skill |

Do not turn a component defect into a general requirement. Extract the reusable evidence rule,
probe recipe, or classification boundary.

## Preserve evidence boundaries

For component-specific lessons, record exact SHA/package, date, evidence type, smallest
reproduction, freshness requirements, and whether it is a positive, defect, evidence gap, or
non-finding. Never present a component fact as current without checking drift.

## Require repeated evidence for core changes

Prefer a core workflow change when it:

- generalized across at least two independent runs or follows directly from a public standard;
- removes demonstrated friction without weakening evidence quality;
- remains useful across maintainers and component architectures;
- has a regression that would have failed before the change.

Keep tentative ideas in run observations until they meet that bar.

## Add or update an eval

Store evals only in `evals/regression.vally.yaml`. Retain scope-control and no-defect canaries so
new guidance does not manufacture findings or expand into catalog audits.

Validate skill maintenance changes with:

```bash
python3 scripts/validate_skill.py
python3 -m unittest discover -s tests -p 'test_*.py'
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec evals/regression.vally.yaml --strict
```

## Review for overfitting

- Would this help another maintainer and architecture?
- Does it preserve maintainer/reviewer ownership?
- Does it distinguish missing evidence from a defect?
- Does it keep one-component scope?
- Can a strong component still receive a mostly positive result?
