# Evaluation policy

`evals/regression.vally.yaml` is the single eval source of truth. Keep prompts, rubrics, fixtures,
coverage, provenance, train/held-out tiers, score families, and controls in that Vally file.

## Governance

- Every canonical requirement prefix must be covered by at least one eval.
- Every eval records provenance, train/held-out tier, score family, and positive/negative controls.
- Retain scope-control and no-defect canaries.
- Add a regression when a general rule changes; do not encode one component's nouns as global
  guidance.
- Held-out failures may motivate a separately provenanced train case, but do not tune the held-out
  prompt itself.
- Expectations must accept a correct paraphrase and reject a plausible overclaim or omission.

## Generate and validate

```bash
python3 scripts/validate_skill.py
python3 -m unittest discover -s tests -p 'test_*.py'
```

Official runs use the publicly available `@microsoft/vally-cli@0.13.0` package:

```bash
npx --yes --package @microsoft/vally-cli@0.13.0 vally --version
npx --yes --package @microsoft/vally-cli@0.13.0 vally lint \
  --eval-spec evals/regression.vally.yaml --strict
```

## Diagnostic run

Run one selected case while developing:

```bash
npx --yes --package @microsoft/vally-cli@0.13.0 vally eval \
  -e evals/regression.vally.yaml \
  --skill-dir <skill-parent-directory> \
  --tag eval_id=10 \
  --runs 1 \
  --timeout 1200s \
  --model gpt-5.6-sol \
  --judge-model claude-opus-5 \
  --output jsonl
```

## Retained comparison

Use all cases and five trials with the pinned executor and judge models. Preserve JSONL output,
Vally diagnostics, resolved CLI version, skill revision, and timestamp. Compare train and held-out
results separately as well as overall; do not allow several similar cases in one score family to
hide failure in another family.

Vally prompt grading is probabilistic. Deterministic scripts remain authoritative for checklist
structure, requirement coverage, status vocabulary, and generated-spec synchronization.
