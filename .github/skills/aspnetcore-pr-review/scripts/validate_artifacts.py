#!/usr/bin/env python3

import argparse
import re
import sys
from pathlib import Path


REQUIRED_NONEMPTY = (
    "evidence/manifest.md",
    "evidence/product-oracle.md",
    "evidence/head-drift.md",
    "candidates/candidate-a.md",
    "candidates/candidate-b.md",
    "candidates/candidate-c.md",
    "candidates/candidate-d.md",
    "cross-examination/candidate-a.md",
    "cross-examination/candidate-b.md",
    "cross-examination/candidate-c.md",
    "cross-examination/candidate-d.md",
    "empirical/manifest.md",
    "empirical/claim-matrix.md",
    "empirical/stress-matrix.md",
    "empirical/result.md",
    "final/repository-oracle.md",
    "final/review.md",
)

REQUIRED_EXISTING = (
    "evidence/tracked.diff",
    "empirical/before.diff",
    "empirical/diagnostic.diff",
    "empirical/implementation.diff",
    "empirical/red.log",
    "empirical/candidate.diff",
    "empirical/green.log",
)

REQUIRED_FINAL_MARKERS = (
    "# Multi-Model Review",
    "**Orchestrator:**",
    "## Current fix",
    "## Independent candidates",
    "## Adversarial consensus",
    "## Test assessment",
    "## Proof status",
    "**Finding proof:**",
    "**Scenario proof:**",
    "**Candidate proof:**",
    "**Product oracle:**",
    "**Oracle fidelity:**",
    "**Mechanism fidelity:**",
    "**Scenario fidelity:**",
    "**Assertion disposition:**",
    "## Final recommendation",
    "**Implementation verdict:**",
    "**Behavioral evidence:**",
    "**Merge readiness:**",
    "**Implementation confidence:**",
    "**Reason:**",
    "## Required follow-ups",
    "## Repository oracle gaps",
    "## Suggested review comments",
)

PRODUCTION_STRESS_DIMENSIONS = (
    "Real producer/runtime boundary",
    "Varied falsification dimensions",
    "Applicable configurations/platforms",
    "Neighboring suite",
    "Cleanup/interruption paths",
)


def extract_label(content: str, label: str) -> str | None:
    match = re.search(rf"^\*\*{re.escape(label)}:\*\*\s*(.+?)\s*$", content, re.MULTILINE)
    return match.group(1).strip().lower() if match else None


def validate(root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in REQUIRED_NONEMPTY:
        path = root / relative_path
        if not path.is_file():
            errors.append(f"missing required artifact: {relative_path}")
        elif not path.read_text(encoding="utf-8", errors="replace").strip():
            errors.append(f"required artifact is empty: {relative_path}")

    for relative_path in REQUIRED_EXISTING:
        if not (root / relative_path).is_file():
            errors.append(f"missing required artifact: {relative_path}")

    final_path = root / "final/review.md"
    if not final_path.is_file():
        return errors

    content = final_path.read_text(encoding="utf-8", errors="replace")
    for marker in REQUIRED_FINAL_MARKERS:
        if marker not in content:
            errors.append(f"final review missing marker: {marker}")

    oracle = extract_label(content, "Oracle fidelity")
    mechanism = extract_label(content, "Mechanism fidelity")
    scenario = extract_label(content, "Scenario fidelity")
    candidate = extract_label(content, "Candidate proof")
    assertion = extract_label(content, "Assertion disposition")
    readiness = extract_label(content, "Merge readiness")
    confidence = extract_label(content, "Implementation confidence")

    weak_oracle = oracle in {"hypothesis", "unknown"}
    weak_mechanism = mechanism in {"inferred", "unknown"}
    weak_scenario = scenario in {"synthetic", "missing"}
    if readiness == "blocked on implementation" and (
        weak_oracle or weak_mechanism or weak_scenario
    ):
        errors.append(
            "blocked on implementation requires stronger oracle, mechanism, and "
            "scenario fidelity"
        )

    if confidence == "high" and (weak_oracle or weak_mechanism or weak_scenario):
        errors.append(
            "high confidence is incompatible with weak oracle, mechanism, or "
            "scenario fidelity"
        )

    if candidate == "diagnostic-only" and confidence == "high":
        errors.append("diagnostic-only candidate proof is incompatible with high confidence")

    if candidate == "diagnostic-only" and readiness == "ready":
        errors.append("diagnostic-only candidate proof is incompatible with ready")

    stress_path = root / "empirical/stress-matrix.md"
    if candidate == "production-proven" and stress_path.is_file():
        stress = stress_path.read_text(encoding="utf-8", errors="replace")
        for dimension in PRODUCTION_STRESS_DIMENSIONS:
            status = extract_label(stress, dimension)
            if status is None or not (
                status.startswith("passed")
                or re.match(r"^not applicable\s*[-:]\s*\S", status)
            ):
                errors.append(
                    "production-proven requires an explicit passed or justified "
                    f"not-applicable status for: {dimension}"
                )

        table_rows = [
            line
            for line in stress.splitlines()
            if line.strip().startswith("|")
            and "---" not in line
        ]
        data_rows = table_rows[1:] if table_rows else []
        if len(data_rows) < 2:
            errors.append(
                "production-proven requires a stress matrix with multiple executed cases"
            )

    if candidate == "production-proven" and assertion != "merge-candidate":
        errors.append("production-proven requires a merge-candidate assertion disposition")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate ASP.NET Core review artifacts and proof calibration."
    )
    parser.add_argument("artifact_root", type=Path)
    args = parser.parse_args()

    errors = validate(args.artifact_root)
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("ASP.NET Core review artifacts are complete and calibrated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
