#!/usr/bin/env python3

"""Validate anti-overfit metadata on ASP.NET Core review skill evals."""

import argparse
import copy
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Iterable


KEBAB_CASE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
PROVENANCE_KINDS = {"pr", "historical", "synthetic"}
TIERS = {"train", "held_out"}
DISCOVERY_MODES = {"discovery", "verification"}
HIGH_OVERLAP_THRESHOLD = 0.60
MEANINGFUL_TOKEN = re.compile(r"[a-z0-9][a-z0-9_-]{3,}")
ISSUE_OR_PULL_REQUEST = re.compile(
    r"(?:\b(?:pull request|pr|issue)\s*#?\d+|#\d{3,})",
    re.IGNORECASE,
)
COMMIT_SHA = re.compile(
    r"\b(?=[0-9a-f]{7,40}\b)(?=[0-9a-f]*\d)[0-9a-f]{7,40}\b",
    re.IGNORECASE,
)
SHA256 = re.compile(r"^[0-9a-f]{64}$")


@dataclass(frozen=True)
class EvalRecord:
    source: str
    index: int
    identifier: str
    tier: str
    family: str
    provenance: str
    area: str
    prompt_overlap: float


@dataclass
class ValidationResult:
    errors: list[str]
    warnings: list[str]
    records: list[EvalRecord]


def is_nonempty_string(value: Any) -> bool:
    return isinstance(value, str) and bool(value.strip())


def indexed_name(source: str, index: int) -> str:
    return f"{source}: evals[{index}]"


def meaningful_tokens(value: str) -> set[str]:
    return set(MEANINGFUL_TOKEN.findall(value.casefold()))


def prompt_expectation_overlap(prompt: str, expectations: list[str]) -> float:
    prompt_tokens = meaningful_tokens(prompt)
    expectation_tokens = meaningful_tokens(" ".join(expectations))
    if not prompt_tokens or not expectation_tokens:
        return 0.0
    return len(prompt_tokens & expectation_tokens) / len(expectation_tokens)


def held_out_hash(eval_data: dict[str, Any]) -> str:
    normalized = copy.deepcopy(eval_data)
    metadata = normalized.get("eval_metadata")
    if isinstance(metadata, dict):
        metadata.pop("frozen_hash", None)
    encoded = json.dumps(
        normalized,
        ensure_ascii=True,
        separators=(",", ":"),
        sort_keys=True,
    ).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def resolve_fixture_path(source: str, fixture: str) -> Path | None:
    fixture_path = Path(fixture)
    if fixture_path.is_absolute() and fixture_path.is_file():
        return fixture_path

    source_path = Path(source).resolve()
    for parent in (source_path.parent, *source_path.parents):
        candidate = parent / fixture_path
        if candidate.is_file():
            return candidate
    return None


def file_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_eval(eval_data: Any, source: str, index: int) -> tuple[list[str], EvalRecord | None]:
    errors: list[str] = []
    name = indexed_name(source, index)
    if not isinstance(eval_data, dict):
        return [f"{name} must be an object"], None

    identifier_value = eval_data.get("id")
    if not (
        isinstance(identifier_value, int)
        and not isinstance(identifier_value, bool)
        and identifier_value > 0
    ):
        errors.append(f"{name}.id must be a positive integer")

    prompt = eval_data.get("prompt")
    if not is_nonempty_string(prompt):
        errors.append(f"{name}.prompt must be a nonempty string")
        prompt = ""

    expectations_data = eval_data.get("expectations")
    if not isinstance(expectations_data, list) or not expectations_data:
        errors.append(f"{name}.expectations must be a nonempty array")
        expectations: list[str] = []
    elif not all(is_nonempty_string(item) for item in expectations_data):
        errors.append(f"{name}.expectations must contain only nonempty strings")
        expectations = []
    else:
        expectations = expectations_data

    files = eval_data.get("files")
    if not isinstance(files, list) or not all(is_nonempty_string(item) for item in files):
        errors.append(f"{name}.files must be an array of nonempty strings")
        files = []
    elif Path(source).is_file():
        for fixture in files:
            if resolve_fixture_path(source, fixture) is None:
                errors.append(f"{name}.files fixture does not exist: {fixture}")

    metadata = eval_data.get("eval_metadata")
    if not isinstance(metadata, dict):
        return errors + [f"{name}.eval_metadata must be an object"], None

    mechanism = metadata.get("mechanism")
    if not is_nonempty_string(mechanism) or not KEBAB_CASE.fullmatch(mechanism):
        errors.append(f"{name}.eval_metadata.mechanism must be nonempty kebab-case")

    area = metadata.get("area")
    if not is_nonempty_string(area):
        errors.append(f"{name}.eval_metadata.area must be a nonempty string")

    family = metadata.get("score_family")
    if not is_nonempty_string(family) or not KEBAB_CASE.fullmatch(family):
        errors.append(f"{name}.eval_metadata.score_family must be nonempty kebab-case")

    tier = metadata.get("tier")
    if tier not in TIERS:
        errors.append(f"{name}.eval_metadata.tier must be train or held_out")

    discovery_mode = metadata.get("discovery_mode")
    if discovery_mode not in DISCOVERY_MODES:
        errors.append(
            f"{name}.eval_metadata.discovery_mode must be discovery or verification"
        )

    provenance = metadata.get("provenance")
    provenance_label = ""
    if not isinstance(provenance, dict):
        errors.append(f"{name}.eval_metadata.provenance must be an object")
    else:
        kind = provenance.get("kind")
        source_value = provenance.get("source")
        if kind not in PROVENANCE_KINDS:
            errors.append(
                f"{name}.eval_metadata.provenance.kind must be pr, historical, or synthetic"
            )
        if not is_nonempty_string(source_value):
            errors.append(f"{name}.eval_metadata.provenance.source must be a nonempty string")
        if kind in PROVENANCE_KINDS and is_nonempty_string(source_value):
            provenance_label = f"{kind}:{source_value.strip()}"

    controls = metadata.get("controls")
    if not isinstance(controls, dict):
        errors.append(f"{name}.eval_metadata.controls must be an object")
    else:
        control_sets: dict[str, set[int]] = {}
        for control_name in ("positive", "negative"):
            values = controls.get(control_name)
            if (
                not isinstance(values, list)
                or not values
                or not all(isinstance(item, int) and not isinstance(item, bool) for item in values)
            ):
                errors.append(
                    f"{name}.eval_metadata.controls.{control_name} must be a nonempty integer array"
                )
                continue
            if len(values) != len(set(values)):
                errors.append(
                    f"{name}.eval_metadata.controls.{control_name} must not repeat indexes"
                )
            control_sets[control_name] = set(values)
            for value in values:
                if value < 0 or value >= len(expectations):
                    errors.append(
                        f"{name}.eval_metadata.controls.{control_name} index {value} "
                        "must reference expectations"
                    )
        if (
            "positive" in control_sets
            and "negative" in control_sets
            and control_sets["positive"] & control_sets["negative"]
        ):
            errors.append(f"{name}.eval_metadata.controls positive and negative must be disjoint")

    forbidden_terms = metadata.get("forbidden_prompt_terms")
    if not isinstance(forbidden_terms, list) or not all(
        is_nonempty_string(term) for term in forbidden_terms
    ):
        errors.append(
            f"{name}.eval_metadata.forbidden_prompt_terms must be an array of nonempty strings"
        )
        forbidden_terms = []
    elif discovery_mode == "discovery" and not forbidden_terms:
        errors.append(
            f"{name}.eval_metadata.forbidden_prompt_terms must be nonempty for discovery"
        )

    if discovery_mode == "discovery":
        if not files:
            errors.append(f"{name}.files must provide a discovery fixture")
        if ISSUE_OR_PULL_REQUEST.search(prompt) or COMMIT_SHA.search(prompt):
            errors.append(
                f"{name}.prompt must not expose issue, pull request, or commit "
                "identities in discovery mode"
            )

    frozen_hash = metadata.get("frozen_hash")
    if tier == "held_out":
        fixture_hashes = metadata.get("fixture_hashes")
        if not isinstance(fixture_hashes, dict) or set(fixture_hashes) != set(files):
            errors.append(
                f"{name}.eval_metadata.fixture_hashes must map every held-out fixture"
            )
        else:
            for fixture, expected_hash in fixture_hashes.items():
                if not is_nonempty_string(expected_hash) or not SHA256.fullmatch(
                    expected_hash
                ):
                    errors.append(
                        f"{name}.eval_metadata.fixture_hashes[{fixture!r}] "
                        "must be a lowercase SHA-256"
                    )
                    continue
                fixture_path = resolve_fixture_path(source, fixture)
                if fixture_path is None:
                    errors.append(f"{name}.files fixture does not exist: {fixture}")
                elif file_sha256(fixture_path) != expected_hash:
                    errors.append(
                        f"{name}.eval_metadata.fixture_hashes[{fixture!r}] "
                        "does not match the fixture"
                    )

        if not is_nonempty_string(frozen_hash) or not SHA256.fullmatch(frozen_hash):
            errors.append(
                f"{name}.eval_metadata.frozen_hash must be a lowercase SHA-256 for held_out evals"
            )
        elif frozen_hash != held_out_hash(eval_data):
            errors.append(f"{name}.eval_metadata.frozen_hash does not match the held-out eval")

    prompt_folded = prompt.casefold()
    for term in forbidden_terms:
        if term.casefold() in prompt_folded:
            errors.append(
                f"{name}.eval_metadata.forbidden_prompt_terms contains prompt term: {term!r}"
            )

    if errors:
        return errors, None

    identifier = str(identifier_value)
    return [], EvalRecord(
        source=source,
        index=index,
        identifier=identifier,
        tier=tier,
        family=family,
        provenance=provenance_label,
        area=area,
        prompt_overlap=prompt_expectation_overlap(prompt, expectations),
    )


def validate_documents(documents: Iterable[tuple[str, Any]]) -> ValidationResult:
    errors: list[str] = []
    records: list[EvalRecord] = []

    for source, document in documents:
        if not isinstance(document, dict):
            errors.append(f"{source} must contain a JSON object")
            continue
        evals = document.get("evals")
        if not isinstance(evals, list) or not evals:
            errors.append(f"{source}.evals must be a nonempty array")
            continue
        identifiers = [
            eval_data.get("id")
            for eval_data in evals
            if isinstance(eval_data, dict)
        ]
        duplicates = sorted(
            {
                str(identifier)
                for identifier, count in Counter(identifiers).items()
                if identifier is not None and count > 1
            }
        )
        if duplicates:
            errors.append(f"{source}.evals contains duplicate ids: {', '.join(duplicates)}")
        for index, eval_data in enumerate(evals):
            eval_errors, record = validate_eval(eval_data, source, index)
            errors.extend(eval_errors)
            if record is not None:
                records.append(record)

    records_by_source: dict[str, list[EvalRecord]] = defaultdict(list)
    for record in records:
        records_by_source[record.source].append(record)
    for source, source_records in records_by_source.items():
        train_provenance = {
            record.provenance for record in source_records if record.tier == "train"
        }
        held_out_provenance = {
            record.provenance for record in source_records if record.tier == "held_out"
        }
        overlap = sorted(train_provenance & held_out_provenance)
        if overlap:
            errors.append(
                f"{source}: train and held_out provenance must be disjoint: "
                f"{', '.join(overlap)}"
            )

    warnings = collect_warnings(records)
    return ValidationResult(errors=errors, warnings=warnings, records=records)


def collect_warnings(records: list[EvalRecord]) -> list[str]:
    if not records:
        return []

    warnings: list[str] = []
    by_source: dict[str, list[EvalRecord]] = defaultdict(list)
    for record in records:
        by_source[record.source].append(record)

    for source, source_records in sorted(by_source.items()):
        total = len(source_records)
        held_out = sum(record.tier == "held_out" for record in source_records)
        held_out_share = held_out / total
        if held_out_share < 0.20 or held_out_share > 0.50:
            warnings.append(
                f"{source}: held-out share is {held_out}/{total} "
                f"({held_out_share:.1%}); review tier balance"
            )

        by_tier_family: dict[str, Counter[str]] = defaultdict(Counter)
        for record in source_records:
            by_tier_family[record.tier][record.family] += 1
        for tier, families in sorted(by_tier_family.items()):
            tier_total = sum(families.values())
            family, count = families.most_common(1)[0]
            if count / tier_total > 0.50:
                warnings.append(
                    f"{source}: {tier} family concentration is {family} "
                    f"({count}/{tier_total}); review diversity"
                )

        provenance_counts = Counter(record.provenance for record in source_records)
        provenance, count = provenance_counts.most_common(1)[0]
        if count / total > 0.50:
            warnings.append(
                f"{source}: provenance concentration is {provenance} "
                f"({count}/{total}); review independence"
            )

        for tier in TIERS:
            tier_records = [
                record for record in source_records if record.tier == tier
            ]
            tier_families = {record.family for record in tier_records}
            provenance_families: dict[str, set[str]] = defaultdict(set)
            for record in tier_records:
                provenance_families[record.provenance].add(record.family)
            for provenance, families in sorted(provenance_families.items()):
                if len(tier_families) >= 3 and len(families) / len(tier_families) > 1 / 3:
                    warnings.append(
                        f"{source}: {tier} provenance {provenance} spans "
                        f"{len(families)}/{len(tier_families)} score families; "
                        "compare provenance-macro transfer"
                    )

        train_areas = {
            record.area for record in source_records if record.tier == "train"
        }
        held_out_areas = {
            record.area for record in source_records if record.tier == "held_out"
        }
        if held_out_areas and held_out_areas <= train_areas:
            warnings.append(
                f"{source}: held-out areas do not add transfer coverage beyond train"
            )

        train_families = {
            record.family for record in source_records if record.tier == "train"
        }
        held_out_families = {
            record.family for record in source_records if record.tier == "held_out"
        }
        uncovered_families = sorted(train_families - held_out_families)
        if uncovered_families:
            warnings.append(
                f"{source}: train families without held-out transfer cases: "
                f"{', '.join(uncovered_families)}"
            )

        for record in source_records:
            if record.prompt_overlap >= HIGH_OVERLAP_THRESHOLD:
                warnings.append(
                    f"{record.source}: evals[{record.index}] prompt/expectation term overlap "
                    f"is {record.prompt_overlap:.1%}; review for answer leakage"
                )
    return warnings


def summary(result: ValidationResult) -> dict[str, Any]:
    records = result.records
    tier_family_counts: dict[tuple[str, str], Counter[str]] = defaultdict(Counter)
    tier_provenance_counts: dict[tuple[str, str], Counter[str]] = defaultdict(Counter)
    source_counts = Counter(record.source for record in records)
    source_held_out = Counter(
        record.source for record in records if record.tier == "held_out"
    )
    for record in records:
        tier_family_counts[(record.source, record.tier)][record.family] += 1
        tier_provenance_counts[(record.source, record.tier)][record.provenance] += 1

    total = len(records)
    held_out = sum(record.tier == "held_out" for record in records)
    return {
        "raw_count": total,
        "family_counts": {
            source: {
                tier: dict(sorted(tier_family_counts[(source, tier)].items()))
                for tier in sorted(
                    {
                        record.tier
                        for record in records
                        if record.source == source
                    }
                )
            }
            for source in sorted(source_counts)
        },
        "held_out": {
            "count": held_out,
            "share": held_out / total if total else 0.0,
            "by_source": {
                source: {
                    "count": source_held_out[source],
                    "total": source_counts[source],
                    "share": source_held_out[source] / source_counts[source],
                }
                for source in sorted(source_counts)
            },
        },
        "provenance_concentration": dict(
            sorted(Counter(record.provenance for record in records).items())
        ),
        "family_weights": [
            {
                "source": record.source,
                "eval_index": record.index,
                "eval_id": record.identifier,
                "tier": record.tier,
                "score_family": record.family,
                "weight": 1
                / (
                    len(tier_family_counts[(record.source, record.tier)])
                    * tier_family_counts[(record.source, record.tier)][record.family]
                ),
            }
            for record in records
        ],
        "provenance_weights": [
            {
                "source": record.source,
                "eval_index": record.index,
                "eval_id": record.identifier,
                "tier": record.tier,
                "provenance": record.provenance,
                "weight": 1
                / (
                    len(tier_provenance_counts[(record.source, record.tier)])
                    * tier_provenance_counts[
                        (record.source, record.tier)
                    ][record.provenance]
                ),
            }
            for record in records
        ],
    }


def read_documents(paths: Iterable[Path]) -> tuple[list[tuple[str, Any]], list[str]]:
    documents: list[tuple[str, Any]] = []
    errors: list[str] = []
    for path in paths:
        try:
            documents.append((str(path), json.loads(path.read_text(encoding="utf-8"))))
        except OSError as error:
            errors.append(f"{path}: unable to read JSON: {error}")
        except json.JSONDecodeError as error:
            errors.append(f"{path}: invalid JSON: {error.msg} at line {error.lineno}")
    return documents, errors


def print_text(result: ValidationResult) -> None:
    report = summary(result)
    print(f"Eval count: {report['raw_count']}")
    print("Family counts:")
    for source, tiers in report["family_counts"].items():
        print(f"  {source}:")
        for tier, families in tiers.items():
            print(
                f"    {tier}: "
                f"{', '.join(f'{family}={count}' for family, count in families.items())}"
            )
    held_out = report["held_out"]
    print(f"Held-out share: {held_out['count']}/{report['raw_count']} ({held_out['share']:.1%})")
    for source, source_held_out in held_out["by_source"].items():
        print(
            f"  {source}: {source_held_out['count']}/{source_held_out['total']} "
            f"({source_held_out['share']:.1%})"
        )
    print("Provenance concentration:")
    for provenance, count in report["provenance_concentration"].items():
        print(f"  {provenance}: {count}")
    print("Per-eval family weights:")
    for weight in report["family_weights"]:
        print(
            f"  {weight['source']}: eval {weight['eval_id']} "
            f"({weight['tier']}/{weight['score_family']}) = {weight['weight']:.6g}"
        )
    print("Per-eval provenance weights:")
    for weight in report["provenance_weights"]:
        print(
            f"  {weight['source']}: eval {weight['eval_id']} "
            f"({weight['tier']}/{weight['provenance']}) = {weight['weight']:.6g}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate ASP.NET Core review eval anti-overfit metadata."
    )
    parser.add_argument("eval_files", nargs="+", type=Path)
    parser.add_argument("--json", action="store_true", help="emit a JSON report")
    args = parser.parse_args()

    documents, read_errors = read_documents(args.eval_files)
    result = validate_documents(documents)
    result.errors[:0] = read_errors
    report = summary(result)
    if args.json:
        print(json.dumps({**asdict(result), "summary": report}, indent=2))
    else:
        for error in result.errors:
            print(f"ERROR: {error}", file=sys.stderr)
        for warning in result.warnings:
            print(f"WARNING: {warning}", file=sys.stderr)
        print_text(result)
    return 1 if result.errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
