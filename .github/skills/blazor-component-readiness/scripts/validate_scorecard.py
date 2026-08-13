#!/usr/bin/env python3

"""Validate structural requirement coverage in a readiness report."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


SKILL_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CHECKLIST = SKILL_ROOT / "references/checklist.md"
OVERLAY_PATHS = {
    "scaffolder": SKILL_ROOT / "references/overlays/scaffolder.md",
    "ai-skill": SKILL_ROOT / "references/overlays/ai-skill.md",
}
REQUIREMENT_PATTERN = re.compile(
    r"^- \*\*([A-Z][A-Z0-9]*-\d{2})\*\*\s+(.+?)\s*$",
    re.MULTILINE,
)
RUBRIC_VERSION_PATTERN = re.compile(r"^\*\*Rubric version:\*\*\s+(\S+)\s*$", re.MULTILINE)
STATUS_VALUES = {
    "verified",
    "defect",
    "maintainer evidence required",
    "not tested",
    "not applicable",
}
EVIDENCE_ID_PATTERN = re.compile(r"^E-\d{3}$")
EVIDENCE_REFERENCE_PATTERN = re.compile(r"\[(E-\d{3})\]")
PLACEHOLDERS = {
    "",
    "-",
    "n/a",
    "na",
    "none",
    "tbd",
    "todo",
    "[evidence]",
    "[maintainer action]",
    "[reviewer follow-up]",
}


@dataclass(frozen=True)
class Requirement:
    identifier: str
    text: str


@dataclass(frozen=True)
class ScorecardRow:
    identifier: str
    requirement: str
    scope: str
    status: str
    evidence: str
    maintainer_action: str
    reviewer_follow_up: str
    line_number: int


def load_requirements(checklist_path: Path = DEFAULT_CHECKLIST) -> list[Requirement]:
    content = checklist_path.read_text(encoding="utf-8")
    requirements = [
        Requirement(identifier=match.group(1), text=match.group(2))
        for match in REQUIREMENT_PATTERN.finditer(content)
    ]
    if not requirements:
        raise ValueError(f"no canonical requirements found in {checklist_path}")

    duplicates = [
        identifier
        for identifier, count in Counter(
            requirement.identifier for requirement in requirements
        ).items()
        if count > 1
    ]
    if duplicates:
        raise ValueError(
            "duplicate canonical requirement IDs: " + ", ".join(sorted(duplicates))
        )
    return requirements


def load_rubric_version(checklist_path: Path = DEFAULT_CHECKLIST) -> str:
    content = checklist_path.read_text(encoding="utf-8")
    match = RUBRIC_VERSION_PATTERN.search(content)
    if match is None:
        raise ValueError(f"rubric version not found in {checklist_path}")
    return match.group(1)


def load_requirement_set(
    checklist_path: Path = DEFAULT_CHECKLIST,
    overlays: list[str] | tuple[str, ...] = (),
) -> list[Requirement]:
    requirements = load_requirements(checklist_path)
    for overlay in overlays:
        requirements.extend(load_requirements(OVERLAY_PATHS[overlay]))

    duplicates = [
        identifier
        for identifier, count in Counter(
            requirement.identifier for requirement in requirements
        ).items()
        if count > 1
    ]
    if duplicates:
        raise ValueError(
            "duplicate IDs across core and overlays: " + ", ".join(sorted(duplicates))
        )
    return requirements


def select_requirements(
    requirements: list[Requirement],
    identifiers: str,
) -> list[Requirement]:
    requested = [identifier.strip() for identifier in identifiers.split(",") if identifier.strip()]
    if not requested:
        raise ValueError("--ids must contain at least one requirement ID")
    duplicates = [
        identifier
        for identifier, count in Counter(requested).items()
        if count > 1
    ]
    if duplicates:
        raise ValueError("duplicate targeted IDs: " + ", ".join(sorted(duplicates)))

    canonical = {requirement.identifier: requirement for requirement in requirements}
    unknown = [identifier for identifier in requested if identifier not in canonical]
    if unknown:
        raise ValueError("unknown targeted IDs: " + ", ".join(unknown))

    requested_set = set(requested)
    return [
        requirement
        for requirement in requirements
        if requirement.identifier in requested_set
    ]


def split_markdown_row(line: str) -> list[str]:
    cells: list[str] = []
    current: list[str] = []
    escaped = False
    for character in line.strip():
        if escaped:
            current.append(character)
            escaped = False
        elif character == "\\":
            escaped = True
        elif character == "|":
            cells.append("".join(current).strip())
            current = []
        else:
            current.append(character)
    cells.append("".join(current).strip())

    if cells and cells[0] == "":
        cells = cells[1:]
    if cells and cells[-1] == "":
        cells = cells[:-1]
    return cells


def normalize_status(value: str) -> str:
    return value.strip().strip("`").casefold()


def parse_scorecard(report_path: Path) -> list[ScorecardRow]:
    rows: list[ScorecardRow] = []
    in_scorecard = False
    expected_header = [
        "Requirement ID",
        "Requirement",
        "Requirement scope",
        "Status",
        "Evidence",
        "Maintainer action",
        "Reviewer follow-up",
    ]
    for line_number, line in enumerate(
        report_path.read_text(encoding="utf-8").splitlines(),
        start=1,
    ):
        if not line.lstrip().startswith("|"):
            if in_scorecard:
                in_scorecard = False
            continue
        cells = split_markdown_row(line)
        if cells == expected_header:
            in_scorecard = True
            continue
        if not in_scorecard:
            continue
        if cells and all(re.fullmatch(r":?-{3,}:?", cell) for cell in cells):
            continue
        if not cells or not re.fullmatch(
            r"[A-Z][A-Z0-9]*-\d{2}",
            cells[0].strip("` "),
        ):
            continue
        if len(cells) != 7:
            raise ValueError(
                f"{report_path}:{line_number}: requirement row must have 7 columns; "
                f"found {len(cells)}"
            )
        rows.append(
            ScorecardRow(
                identifier=cells[0].strip("` "),
                requirement=cells[1],
                scope=cells[2].strip("` "),
                status=normalize_status(cells[3]),
                evidence=cells[4],
                maintainer_action=cells[5],
                reviewer_follow_up=cells[6],
                line_number=line_number,
            )
        )
    return rows


def is_placeholder(value: str) -> bool:
    normalized = value.strip().strip("`").casefold()
    return normalized in PLACEHOLDERS


def parse_evidence_ledger(report_path: Path) -> tuple[dict[str, int], list[str]]:
    identifiers: dict[str, int] = {}
    errors: list[str] = []
    in_ledger = False
    expected_header = [
        "Evidence ID",
        "Claim",
        "Repository/SHA or package",
        "Evidence type",
        "Reproduction/source",
        "Rechecked now?",
    ]
    for line_number, line in enumerate(
        report_path.read_text(encoding="utf-8").splitlines(),
        start=1,
    ):
        if not line.lstrip().startswith("|"):
            if in_ledger:
                in_ledger = False
            continue
        cells = split_markdown_row(line)
        if cells == expected_header:
            in_ledger = True
            continue
        if not in_ledger:
            continue
        if cells and all(re.fullmatch(r":?-{3,}:?", cell) for cell in cells):
            continue
        if len(cells) != 6:
            continue
        identifier = cells[0].strip("` ")
        if not EVIDENCE_ID_PATTERN.fullmatch(identifier):
            continue
        if identifier in identifiers:
            errors.append(
                f"line {line_number}: duplicate evidence ledger ID {identifier}"
            )
        else:
            identifiers[identifier] = line_number
        for field_name, value in zip(expected_header[1:], cells[1:]):
            if is_placeholder(value):
                errors.append(
                    f"line {line_number} ({identifier}): {field_name} must be substantive"
                )
    return identifiers, errors


def validate_rows(
    requirements: list[Requirement],
    rows: list[ScorecardRow],
    evidence_ledger: dict[str, int] | None = None,
) -> list[str]:
    errors: list[str] = []
    canonical = {requirement.identifier: requirement for requirement in requirements}
    counts = Counter(row.identifier for row in rows)

    missing = [
        requirement.identifier
        for requirement in requirements
        if counts[requirement.identifier] == 0
    ]
    duplicates = sorted(
        identifier for identifier, count in counts.items() if count > 1
    )
    extras = sorted(identifier for identifier in counts if identifier not in canonical)

    if missing:
        errors.append("missing requirement rows: " + ", ".join(missing))
    if duplicates:
        errors.append("duplicate requirement rows: " + ", ".join(duplicates))
    if extras:
        errors.append("unknown requirement rows: " + ", ".join(extras))
    expected_order = [requirement.identifier for requirement in requirements]
    actual_order = [
        row.identifier
        for row in rows
        if row.identifier in canonical and counts[row.identifier] == 1
    ]
    if not missing and not duplicates and not extras and actual_order != expected_order:
        errors.append("requirement rows are not in canonical checklist order")

    for row in rows:
        if row.identifier not in canonical:
            continue
        location = f"line {row.line_number} ({row.identifier})"
        if row.status not in STATUS_VALUES:
            errors.append(
                f"{location}: invalid status {row.status!r}; expected one of "
                + ", ".join(sorted(STATUS_VALUES))
            )
        if row.scope not in {"repository-wide", "component-specific"}:
            errors.append(
                f"{location}: invalid scope {row.scope!r}; expected "
                "'repository-wide' or 'component-specific'"
            )
        if is_placeholder(row.requirement):
            errors.append(f"{location}: requirement text is empty or a placeholder")
        elif row.requirement != canonical[row.identifier].text:
            errors.append(
                f"{location}: requirement text differs from the canonical checklist"
            )
        if is_placeholder(row.evidence):
            errors.append(
                f"{location}: evidence must explain the proof, gap, test omission, "
                "or not-applicable rationale"
            )
        for evidence_id in EVIDENCE_REFERENCE_PATTERN.findall(row.evidence):
            if evidence_ledger is None or evidence_id not in evidence_ledger:
                errors.append(
                    f"{location}: unresolved evidence reference [{evidence_id}]"
                )
        if (
            row.status == "maintainer evidence required"
            and is_placeholder(row.maintainer_action)
        ):
            errors.append(
                f"{location}: maintainer evidence required needs a concrete "
                "maintainer action"
            )
        if row.status == "not tested" and is_placeholder(row.reviewer_follow_up):
            errors.append(
                f"{location}: not tested needs a bounded reviewer follow-up"
            )

    return errors


def render_template(requirements: list[Requirement]) -> str:
    lines = [
        "| Requirement ID | Requirement | Requirement scope | Status | Evidence | Maintainer action | Reviewer follow-up |",
        "|---|---|---|---|---|---|---|",
    ]
    for requirement in requirements:
        escaped_text = requirement.text.replace("|", "\\|")
        lines.append(
            f"| {requirement.identifier} | {escaped_text} | [scope] | [status] | "
            "[evidence] | [maintainer action] | [reviewer follow-up] |"
        )
    return "\n".join(lines) + "\n"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def build_validation_receipt(
    checklist_path: Path,
    report_path: Path,
    mode: str,
    requirements: list[Requirement],
    rows: list[ScorecardRow],
    overlays: list[str],
    validated_at: datetime | None = None,
) -> dict[str, object]:
    timestamp = validated_at or datetime.now(timezone.utc)
    if timestamp.tzinfo is None:
        raise ValueError("validation receipt timestamp must include a timezone")
    return {
        "schema_version": 1,
        "structural_validation": "passed",
        "rubric_version": load_rubric_version(checklist_path),
        "mode": mode,
        "selected_overlays": list(overlays),
        "selected_ids": (
            [requirement.identifier for requirement in requirements]
            if mode == "targeted"
            else []
        ),
        "canonical_row_count": len(requirements),
        "valid_row_count": len(rows),
        "validated_at_utc": timestamp.astimezone(timezone.utc)
        .isoformat()
        .replace("+00:00", "Z"),
        "report_filename": report_path.name,
        "report_sha256": sha256_file(report_path),
        "limitation": (
            "Structural validation does not establish factual evidence or "
            "classification quality."
        ),
    }


def write_validation_receipt(receipt_path: Path, receipt: dict[str, object]) -> None:
    receipt_path.write_text(
        json.dumps(receipt, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate or emit the canonical readiness scorecard."
    )
    parser.add_argument("report", nargs="?", type=Path)
    parser.add_argument(
        "--checklist",
        type=Path,
        default=DEFAULT_CHECKLIST,
        help="Canonical checklist path.",
    )
    parser.add_argument(
        "--emit-template",
        action="store_true",
        help="Print a blank complete or targeted scorecard table.",
    )
    parser.add_argument(
        "--overlay",
        action="append",
        choices=sorted(OVERLAY_PATHS),
        default=[],
        help="Include an optional requirement overlay in a complete review.",
    )
    parser.add_argument(
        "--ids",
        help=(
            "Validate or emit a targeted scorecard for comma-separated canonical IDs. "
            "Targeted validation does not establish complete readiness coverage."
        ),
    )
    parser.add_argument(
        "--receipt",
        type=Path,
        help=(
            "Write a machine-readable structural validation receipt. The receipt "
            "does not establish factual evidence or classification quality."
        ),
    )
    args = parser.parse_args()
    if args.ids and args.overlay:
        parser.error("--ids cannot be combined with --overlay; name overlay IDs directly")
    if args.emit_template and args.receipt is not None:
        parser.error("--receipt cannot be combined with --emit-template")

    try:
        mode = "complete"
        if args.ids:
            all_requirements = load_requirement_set(
                args.checklist,
                tuple(OVERLAY_PATHS),
            )
            requirements = select_requirements(all_requirements, args.ids)
            mode = "targeted"
        else:
            requirements = load_requirement_set(args.checklist, args.overlay)
        if args.emit_template:
            if args.report is not None:
                parser.error("report cannot be supplied with --emit-template")
            print(render_template(requirements), end="")
            return 0
        if args.report is None:
            parser.error("report is required unless --emit-template is used")
        if args.receipt is not None and args.receipt.resolve() == args.report.resolve():
            parser.error("--receipt must not overwrite the report")

        rows = parse_scorecard(args.report)
        evidence_ledger, ledger_errors = parse_evidence_ledger(args.report)
        errors = ledger_errors + validate_rows(requirements, rows, evidence_ledger)
    except (OSError, ValueError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if args.receipt is not None:
        try:
            receipt = build_validation_receipt(
                args.checklist,
                args.report,
                mode,
                requirements,
                rows,
                args.overlay,
            )
            write_validation_receipt(args.receipt, receipt)
        except (OSError, ValueError) as error:
            print(f"ERROR: failed to write validation receipt: {error}", file=sys.stderr)
            return 1

    print(
        f"Structural validation passed: {mode} scorecard has "
        f"{len(requirements)} canonical requirements and {len(rows)} valid rows."
    )
    print(
        "Structural validation does not establish factual evidence or "
        "classification quality."
    )
    if mode == "targeted":
        print("Targeted validation does not establish complete readiness coverage.")
    if args.receipt is not None:
        print(f"Structural validation receipt written to {args.receipt}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
