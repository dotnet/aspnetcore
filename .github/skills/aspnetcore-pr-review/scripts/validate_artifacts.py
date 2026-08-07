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
    "empirical/head.log",
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
    "**Frozen-head result:**",
    "**Finding proof:**",
    "**Scenario proof:**",
    "**Candidate proof:**",
    "**Product oracle:**",
    "**Oracle fidelity:**",
    "**Mechanism fidelity:**",
    "**Scenario fidelity:**",
    "**Regression assertion disposition:**",
    "**Diagnostic mutation disposition:**",
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


def count_marker(content: str, marker: str) -> int:
    if marker.startswith("#"):
        pattern = rf"^{re.escape(marker)}\s*$"
    else:
        pattern = rf"^{re.escape(marker)}.*$"
    return len(re.findall(pattern, content, re.MULTILINE))


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
        occurrences = count_marker(content, marker)
        if occurrences == 0:
            errors.append(f"final review missing marker: {marker}")
        elif occurrences > 1:
            errors.append(f"final review contains duplicate marker: {marker}")

    head_result = extract_label(content, "Frozen-head result")
    finding_proof = extract_label(content, "Finding proof")
    scenario_proof = extract_label(content, "Scenario proof")
    product_oracle = extract_label(content, "Product oracle")
    oracle = extract_label(content, "Oracle fidelity")
    mechanism = extract_label(content, "Mechanism fidelity")
    scenario = extract_label(content, "Scenario fidelity")
    candidate = extract_label(content, "Candidate proof")
    regression_assertion = extract_label(content, "Regression assertion disposition")
    diagnostic_mutation = extract_label(content, "Diagnostic mutation disposition")
    readiness = extract_label(content, "Merge readiness")
    confidence = extract_label(content, "Implementation confidence")
    implementation_verdict = extract_label(content, "Implementation verdict")
    behavioral_evidence = extract_label(content, "Behavioral evidence")

    allowed_labels = {
        "Frozen-head result": (
            head_result,
            {"behavioral-fail", "structural-defect", "pass", "blocked", "not-applicable"},
        ),
        "Finding proof": (finding_proof, {"empirical", "structural", "missing"}),
        "Scenario proof": (scenario_proof, {"empirical", "structural", "missing"}),
        "Candidate proof": (
            candidate,
            {
                "production-proven",
                "targeted-proven",
                "diagnostic-only",
                "rejected",
                "blocked",
                "none",
            },
        ),
        "Product oracle": (
            product_oracle,
            {"documented", "author-confirmed", "test-encoded", "inferred", "unknown"},
        ),
        "Oracle fidelity": (
            oracle,
            {"authoritative", "corroborated", "hypothesis", "unknown"},
        ),
        "Mechanism fidelity": (
            mechanism,
            {"reproduced", "structural", "inferred", "unknown"},
        ),
        "Scenario fidelity": (scenario, {"exact", "proxy", "synthetic", "missing"}),
        "Implementation verdict": (
            implementation_verdict,
            {"keep current fix", "revise", "replace"},
        ),
        "Behavioral evidence": (
            behavioral_evidence,
            {"empirical", "structural", "missing"},
        ),
        "Merge readiness": (
            readiness,
            {
                "ready",
                "recommendation only",
                "blocked on evidence",
                "blocked on product oracle",
                "blocked on implementation",
            },
        ),
        "Implementation confidence": (confidence, {"high", "medium", "low"}),
    }
    for label, (value, allowed) in allowed_labels.items():
        if value not in allowed:
            errors.append(f"invalid calibrated value for {label}: {value or 'missing'}")

    weak_oracle = oracle in {"hypothesis", "unknown"}
    weak_mechanism = mechanism in {"inferred", "unknown"}
    weak_scenario = scenario in {"synthetic", "missing"}
    proven_head_defect = head_result in {"behavioral-fail", "structural-defect"}
    proof_matches_head = (
        head_result == "behavioral-fail"
        and finding_proof == "empirical"
        and scenario_proof == "empirical"
    ) or (
        head_result == "structural-defect"
        and finding_proof in {"empirical", "structural"}
        and scenario_proof in {"empirical", "structural"}
    )
    if readiness == "blocked on implementation" and (
        weak_oracle
        or weak_mechanism
        or weak_scenario
        or not proven_head_defect
        or not proof_matches_head
    ):
        errors.append(
            "blocked on implementation requires a proven frozen-head defect and "
            "stronger oracle, mechanism, scenario, and finding proof"
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

        executed_headings = list(
            re.finditer(r"^## Executed cases\s*$", stress, re.MULTILINE)
        )
        if not executed_headings:
            errors.append(
                "production-proven requires an explicit Executed cases section"
            )
        elif len(executed_headings) > 1:
            errors.append(
                "production-proven requires exactly one Executed cases section"
            )
        else:
            section_start = executed_headings[0].end()
            next_heading = re.search(
                r"^## ",
                stress[section_start:],
                re.MULTILINE,
            )
            section_end = (
                section_start + next_heading.start()
                if next_heading is not None
                else len(stress)
            )
            table_rows = [
                line.strip()
                for line in stress[section_start:section_end].splitlines()
                if line.strip().startswith("|") and "---" not in line
            ]
            data_rows = table_rows[1:] if table_rows else []
            distinct_rows = set(data_rows)
            if len(distinct_rows) < 2:
                errors.append(
                    "production-proven requires multiple distinct executed cases"
                )

    if regression_assertion not in {
        "required-regression",
        "optional-regression",
        "rejected",
    }:
        errors.append(
            "regression assertion disposition must use a calibrated severity value"
        )

    if diagnostic_mutation not in {"diagnostic-only", "rejected", "not-applicable"}:
        errors.append(
            "diagnostic mutation disposition must use a calibrated severity value"
        )

    if candidate == "production-proven" and not proven_head_defect:
        errors.append("production-proven requires a proven frozen-head defect")

    if candidate == "production-proven" and (
        weak_oracle or weak_mechanism or weak_scenario
    ):
        errors.append(
            "production-proven is incompatible with weak oracle, mechanism, or "
            "scenario fidelity"
        )

    if candidate == "production-proven" and (
        finding_proof != "empirical" or scenario_proof != "empirical"
    ):
        errors.append(
            "production-proven requires empirical finding and scenario proof"
        )

    if candidate == "production-proven" and (
        regression_assertion != "required-regression"
    ):
        errors.append(
            "production-proven requires a required-regression assertion disposition"
        )

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
