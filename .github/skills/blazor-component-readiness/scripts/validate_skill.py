#!/usr/bin/env python3

"""Validate public rubric, playbook, and Vally governance completeness."""

from __future__ import annotations

import re
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

from validate_scorecard import (
    DEFAULT_CHECKLIST,
    OVERLAY_PATHS,
    load_requirement_set,
    load_requirements,
)


SKILL_ROOT = Path(__file__).resolve().parents[1]
AREAS_INDEX = SKILL_ROOT / "references/areas/index.md"
SKILL_PATH = SKILL_ROOT / "SKILL.md"
REPORT_TEMPLATE = SKILL_ROOT / "references/report-template.md"
VALLY_PATH = SKILL_ROOT / "evals/regression.vally.yaml"
VALLY_PACKAGE = "@microsoft/vally-cli@0.13.0"
EXPECTED_CORE_REQUIREMENT_COUNT = 110
EXPECTED_CORE_PREFIXES = {
    "LP",
    "PI",
    "SEC",
    "A11Y",
    "BEQ",
    "TA",
    "PERF",
    "CI",
    "SUP",
}
EXPECTED_OVERLAYS = {
    "scaffolder": ("SCF", 6),
    "ai-skill": ("AI", 6),
}
REQUIRED_TAGS = {
    "eval_id",
    "area",
    "score_family",
    "tier",
    "requirement_prefixes",
    "provenance_kind",
    "provenance_source",
    "positive_controls",
    "negative_controls",
}


@dataclass(frozen=True)
class VallyStimulus:
    name: str
    tags: dict[str, str]
    rubric_count: int
    fixture_sources: tuple[str, ...]


def parse_vally_stimuli(path: Path = VALLY_PATH) -> list[VallyStimulus]:
    content = path.read_text(encoding="utf-8")
    matches = list(
        re.finditer(r'^  - name: "([^"]+)"\s*$', content, re.MULTILINE)
    )
    stimuli: list[VallyStimulus] = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(content)
        block = content[match.end() : end]
        tags = dict(
            re.findall(
                r'^      ([a-z_]+): "([^"]*)"\s*$',
                block,
                re.MULTILINE,
            )
        )
        rubric_match = re.search(r"^    rubric:\s*$", block, re.MULTILINE)
        rubric_count = 0
        if rubric_match is not None:
            rubric_count = len(
                re.findall(
                    r'^      - ".+"\s*$',
                    block[rubric_match.end() :],
                    re.MULTILINE,
                )
            )
        fixture_sources = tuple(
            re.findall(r'^        - src: "([^"]+)"\s*$', block, re.MULTILINE)
        )
        stimuli.append(
            VallyStimulus(
                name=match.group(1),
                tags=tags,
                rubric_count=rubric_count,
                fixture_sources=fixture_sources,
            )
        )
    return stimuli


def parse_indexes(value: str) -> set[int]:
    return {int(part) for part in value.split(",") if part}


def validate_requirement_sequences(errors: list[str]) -> set[str]:
    core_requirements = load_requirements(DEFAULT_CHECKLIST)
    core_prefixes = {
        requirement.identifier.rsplit("-", 1)[0]
        for requirement in core_requirements
    }
    if core_prefixes != EXPECTED_CORE_PREFIXES:
        errors.append(
            "core requirement prefixes differ: expected "
            f"{sorted(EXPECTED_CORE_PREFIXES)}, found {sorted(core_prefixes)}"
        )
    if len(core_requirements) != EXPECTED_CORE_REQUIREMENT_COUNT:
        errors.append(
            f"expected {EXPECTED_CORE_REQUIREMENT_COUNT} core requirements; "
            f"found {len(core_requirements)}"
        )

    for overlay, (expected_prefix, expected_count) in EXPECTED_OVERLAYS.items():
        requirements = load_requirements(OVERLAY_PATHS[overlay])
        prefixes = {
            requirement.identifier.rsplit("-", 1)[0]
            for requirement in requirements
        }
        if prefixes != {expected_prefix}:
            errors.append(
                f"{overlay} overlay prefixes differ: expected {expected_prefix}, "
                f"found {sorted(prefixes)}"
            )
        if len(requirements) != expected_count:
            errors.append(
                f"expected {expected_count} requirements in {overlay} overlay; "
                f"found {len(requirements)}"
            )

    requirements = load_requirement_set(DEFAULT_CHECKLIST, tuple(OVERLAY_PATHS))
    numbers: dict[str, list[int]] = defaultdict(list)
    for requirement in requirements:
        prefix, number = requirement.identifier.rsplit("-", 1)
        numbers[prefix].append(int(number))

    prefixes = set(numbers)
    for prefix, values in numbers.items():
        if values != sorted(values):
            errors.append(f"{prefix} requirement IDs are not in ascending order: {values}")
    return prefixes


def validate_area_mapping(prefixes: set[str], errors: list[str]) -> None:
    content = AREAS_INDEX.read_text(encoding="utf-8")
    for prefix in prefixes:
        if f"`{prefix}-*`" not in content:
            errors.append(f"area index does not map {prefix}-*")

    for match in re.finditer(r"`([^`]+\.md)`", content):
        referenced = AREAS_INDEX.parent / match.group(1)
        if not referenced.is_file():
            errors.append(f"missing area playbook: {referenced}")


def validate_vally(prefixes: set[str], errors: list[str]) -> None:
    content = VALLY_PATH.read_text(encoding="utf-8")
    for marker in (
        f"# Validated with {VALLY_PACKAGE}.",
        "  runs: 5",
        "  model: gpt-5.6-sol",
        "  judge_model: claude-opus-5",
    ):
        if marker not in content:
            errors.append(f"Vally suite is missing pinned marker: {marker}")

    stimuli = parse_vally_stimuli()
    if not stimuli:
        errors.append("Vally suite contains no stimuli")
        return

    eval_ids: list[str] = []
    covered_prefixes: set[str] = set()
    score_families: Counter[str] = Counter()
    tiers: Counter[str] = Counter()
    for stimulus in stimuli:
        missing_tags = REQUIRED_TAGS - stimulus.tags.keys()
        if missing_tags:
            errors.append(
                f"{stimulus.name}: missing Vally tags {sorted(missing_tags)}"
            )
            continue

        tags = stimulus.tags
        eval_ids.append(tags["eval_id"])
        requirement_prefixes = {
            prefix for prefix in tags["requirement_prefixes"].split(",") if prefix
        }
        unknown_prefixes = requirement_prefixes - prefixes
        if unknown_prefixes:
            errors.append(
                f"{stimulus.name}: unknown requirement prefixes "
                f"{sorted(unknown_prefixes)}"
            )
        covered_prefixes.update(requirement_prefixes)
        score_families[tags["score_family"]] += 1
        tiers[tags["tier"]] += 1

        if not tags["provenance_kind"] or not tags["provenance_source"]:
            errors.append(f"{stimulus.name}: provenance tags must be non-empty")
        if stimulus.rubric_count < 4:
            errors.append(
                f"{stimulus.name}: expected outcome plus at least three rubric items required"
            )

        positive = parse_indexes(tags["positive_controls"])
        negative = parse_indexes(tags["negative_controls"])
        expectation_count = stimulus.rubric_count - 1
        valid_indexes = set(range(expectation_count))
        if not positive or not negative:
            errors.append(
                f"{stimulus.name}: positive and negative controls must be non-empty"
            )
        if positive & negative:
            errors.append(f"{stimulus.name}: control indexes overlap")
        if not (positive | negative) <= valid_indexes:
            errors.append(f"{stimulus.name}: control index is out of range")

        for fixture_source in stimulus.fixture_sources:
            fixture_path = VALLY_PATH.parent / fixture_source
            if not fixture_path.is_file():
                errors.append(
                    f"{stimulus.name}: missing fixture source {fixture_source}"
                )

    duplicate_ids = [
        identifier for identifier, count in Counter(eval_ids).items() if count > 1
    ]
    if duplicate_ids:
        errors.append(f"duplicate Vally eval IDs: {duplicate_ids}")
    if covered_prefixes != prefixes:
        errors.append(
            "Vally coverage differs from requirement prefixes: missing "
            f"{sorted(prefixes - covered_prefixes)}, extra "
            f"{sorted(covered_prefixes - prefixes)}"
        )
    if not {"train", "held-out"} <= tiers.keys():
        errors.append("Vally suite requires both train and held-out cases")
    if not {"scope-control", "no-defect-control"} <= score_families.keys():
        errors.append("Vally suite requires scope-control and no-defect-control canaries")


def validate_wiring(errors: list[str]) -> None:
    skill = SKILL_PATH.read_text(encoding="utf-8")
    report = REPORT_TEMPLATE.read_text(encoding="utf-8")
    for reference in (
        "references/areas/index.md",
        "references/artifact-acquisition.md",
        "references/feedback.md",
        "references/overlays/",
        "references/status-boundaries.md",
        "references/targeted-profiles.md",
        "scripts/validate_scorecard.py",
        "scripts/validate_skill.py",
        "evals/regression.vally.yaml",
    ):
        if reference not in skill:
            errors.append(f"SKILL.md does not reference {reference}")
    for heading in (
        "Requirement ID",
        "Requirement scope",
        "Reviewer follow-up",
    ):
        if heading not in report:
            errors.append(f"report template is missing scorecard column {heading}")

def main() -> int:
    errors: list[str] = []
    try:
        prefixes = validate_requirement_sequences(errors)
        validate_area_mapping(prefixes, errors)
        validate_vally(prefixes, errors)
        validate_wiring(errors)
    except (OSError, ValueError, TypeError) as error:
        errors.append(str(error))

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(
        f"Skill structure is valid: {EXPECTED_CORE_REQUIREMENT_COUNT} core "
        "requirements, 12 optional overlay requirements, complete area mapping, "
        "and governed Vally eval coverage."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
