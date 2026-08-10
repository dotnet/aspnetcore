#!/usr/bin/env python3

"""Aggregate reviewer eval scores without rewarding correlated duplicates."""

import argparse
import json
import statistics
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any, Iterable


def mean(values: Iterable[float]) -> float:
    materialized = list(values)
    return statistics.fmean(materialized) if materialized else 0.0


def macro_average(evals: list[dict[str, Any]], scores: dict[str, float], field: str) -> float:
    grouped: dict[str, list[float]] = defaultdict(list)
    for eval_data in evals:
        metadata = eval_data["eval_metadata"]
        if field == "provenance":
            provenance = metadata["provenance"]
            key = f"{provenance['kind']}:{provenance['source']}"
        else:
            key = metadata[field]
        grouped[key].append(scores[str(eval_data["id"])])
    return mean(mean(group_scores) for group_scores in grouped.values())


def aggregate_document(
    document: dict[str, Any], score_document: dict[str, Any]
) -> tuple[dict[str, Any], list[str]]:
    errors: list[str] = []
    evals = document.get("evals", [])
    scores: dict[str, float] = {}
    expected_ids = {str(eval_data["id"]) for eval_data in evals}

    for identifier, value in score_document.items():
        if not isinstance(value, (int, float)) or isinstance(value, bool):
            errors.append(f"score for eval {identifier} must be numeric")
        elif not 0 <= value <= 1:
            errors.append(f"score for eval {identifier} must be between 0 and 1")
        else:
            scores[str(identifier)] = float(value)

    missing = sorted(expected_ids - set(scores))
    extra = sorted(set(scores) - expected_ids)
    if missing:
        errors.append(f"missing eval scores: {', '.join(missing)}")
    if extra:
        errors.append(f"unknown eval scores: {', '.join(extra)}")
    if errors:
        return {}, errors

    tiers: dict[str, Any] = {}
    for tier in ("train", "held_out"):
        tier_evals = [
            eval_data
            for eval_data in evals
            if eval_data["eval_metadata"]["tier"] == tier
        ]
        if not tier_evals:
            continue
        tiers[tier] = {
            "eval_count": len(tier_evals),
            "raw_mean": mean(scores[str(eval_data["id"])] for eval_data in tier_evals),
            "family_macro": macro_average(
                tier_evals, scores, "score_family"
            ),
            "provenance_macro": macro_average(
                tier_evals, scores, "provenance"
            ),
        }

    train = tiers.get("train", {})
    held_out = tiers.get("held_out", {})
    return {
        "raw_mean": mean(scores.values()),
        "tiers": tiers,
        "transfer_gap": {
            "family_macro": (
                train["family_macro"] - held_out["family_macro"]
                if train and held_out
                else None
            ),
            "provenance_macro": (
                train["provenance_macro"] - held_out["provenance_macro"]
                if train and held_out
                else None
            ),
        },
    }, []


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Macro-aggregate ASP.NET Core reviewer eval scores."
    )
    parser.add_argument("eval_files", nargs="+", type=Path)
    parser.add_argument(
        "--scores",
        required=True,
        type=Path,
        help="JSON object keyed by skill_name, then eval id, with scores from 0 to 1",
    )
    args = parser.parse_args()

    try:
        score_data = json.loads(args.scores.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        print(f"ERROR: unable to read scores: {error}", file=sys.stderr)
        return 1

    result: dict[str, Any] = {}
    errors: list[str] = []
    for eval_path in args.eval_files:
        try:
            document = json.loads(eval_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            errors.append(f"{eval_path}: unable to read evals: {error}")
            continue
        skill_name = document.get("skill_name")
        if not isinstance(skill_name, str) or not skill_name:
            errors.append(f"{eval_path}: skill_name must be a nonempty string")
            continue
        skill_scores = score_data.get(skill_name)
        if not isinstance(skill_scores, dict):
            errors.append(f"{skill_name}: scores must be an object keyed by eval id")
            continue
        aggregate, aggregate_errors = aggregate_document(document, skill_scores)
        errors.extend(f"{skill_name}: {error}" for error in aggregate_errors)
        if not aggregate_errors:
            result[skill_name] = aggregate

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
