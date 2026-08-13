import json
import sys
import tempfile
import unittest
from datetime import datetime, timezone
from pathlib import Path


SKILL_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SKILL_ROOT / "scripts"))

from validate_scorecard import (
    OVERLAY_PATHS,
    STATUS_VALUES,
    ScorecardRow,
    build_validation_receipt,
    load_requirement_set,
    load_requirements,
    parse_evidence_ledger,
    parse_scorecard,
    render_template,
    select_requirements,
    validate_rows,
    write_validation_receipt,
)
from validate_skill import (
    VALLY_PACKAGE,
    VALLY_PATH,
    parse_vally_stimuli,
)


class SkillStructureTests(unittest.TestCase):
    def test_checklist_has_110_core_ids_and_12_overlay_ids(self):
        requirements = load_requirements()
        identifiers = [requirement.identifier for requirement in requirements]
        self.assertEqual(110, len(identifiers))
        self.assertEqual(len(identifiers), len(set(identifiers)))
        self.assertIn("TA-08", identifiers)
        self.assertNotIn("SCF-01", identifiers)
        self.assertNotIn("AI-01", identifiers)

        all_requirements = load_requirement_set(
            overlays=tuple(OVERLAY_PATHS),
        )
        all_identifiers = [requirement.identifier for requirement in all_requirements]
        self.assertEqual(122, len(all_identifiers))
        self.assertIn("SCF-01", all_identifiers)
        self.assertIn("AI-01", all_identifiers)
        checklist = (SKILL_ROOT / "references/checklist.md").read_text(
            encoding="utf-8"
        )
        self.assertIn("**Rubric version:** 1.2.0", checklist)

        by_prefix: dict[str, list[int]] = {}
        for identifier in all_identifiers:
            prefix, number = identifier.rsplit("-", 1)
            by_prefix.setdefault(prefix, []).append(int(number))
        for values in by_prefix.values():
            self.assertEqual(sorted(values), values)

    def test_complete_scorecard_passes(self):
        requirements = load_requirements()
        rows = [
            ScorecardRow(
                identifier=requirement.identifier,
                requirement=requirement.text,
                scope="component-specific",
                status="not applicable",
                evidence="The requirement does not apply to this bounded component.",
                maintainer_action="-",
                reviewer_follow_up="-",
                line_number=index,
            )
            for index, requirement in enumerate(requirements, start=1)
        ]
        self.assertEqual([], validate_rows(requirements, rows))

    def test_generated_markdown_table_is_parseable(self):
        requirements = load_requirements()
        report = render_template(requirements)
        report = report.replace("[scope]", "component-specific")
        report = report.replace("[status]", "not applicable")
        report = report.replace(
            "[evidence]",
            "This requirement does not apply to the bounded component.",
        )
        report = report.replace("[maintainer action]", "-")
        report = report.replace("[reviewer follow-up]", "-")
        with tempfile.TemporaryDirectory() as directory:
            report_path = Path(directory) / "report.md"
            report_path.write_text(report, encoding="utf-8")
            rows = parse_scorecard(report_path)
        self.assertEqual(110, len(rows))
        self.assertEqual([], validate_rows(requirements, rows))

    def test_non_scorecard_tables_are_ignored(self):
        requirements = load_requirements()
        report = (
            "| Finding ID | Title | Scope | Status | Evidence | Owner | Follow-up |\n"
            "|---|---|---|---|---|---|---|\n"
            "| FAIL-01 | Example | component | open | proof | maintainer | retest |\n\n"
            + render_template(requirements)
        )
        report = report.replace("[scope]", "component-specific")
        report = report.replace("[status]", "not applicable")
        report = report.replace(
            "[evidence]",
            "This requirement does not apply to the bounded component.",
        )
        report = report.replace("[maintainer action]", "-")
        report = report.replace("[reviewer follow-up]", "-")
        with tempfile.TemporaryDirectory() as directory:
            report_path = Path(directory) / "report.md"
            report_path.write_text(report, encoding="utf-8")
            rows = parse_scorecard(report_path)
        self.assertEqual(110, len(rows))

    def test_missing_duplicate_and_invalid_rows_fail(self):
        requirements = load_requirements()
        first = requirements[0]
        rows = [
            ScorecardRow(
                identifier=first.identifier,
                requirement=first.text,
                scope="wrong-scope",
                status="pass",
                evidence="TBD",
                maintainer_action="-",
                reviewer_follow_up="-",
                line_number=1,
            ),
            ScorecardRow(
                identifier=first.identifier,
                requirement=first.text,
                scope="repository-wide",
                status="verified",
                evidence="Exact public license.",
                maintainer_action="-",
                reviewer_follow_up="-",
                line_number=2,
            ),
        ]
        errors = validate_rows(requirements, rows)
        combined = "\n".join(errors)
        self.assertIn("missing requirement rows", combined)
        self.assertIn("duplicate requirement rows", combined)
        self.assertIn("invalid status", combined)
        self.assertIn("invalid scope", combined)
        self.assertIn("evidence must explain", combined)

    def test_only_five_status_values_exist(self):
        self.assertEqual(
            {
                "verified",
                "defect",
                "maintainer evidence required",
                "not tested",
                "not applicable",
            },
            STATUS_VALUES,
        )

    def test_shuffled_scorecard_fails(self):
        requirements = load_requirements()
        rows = [
            ScorecardRow(
                identifier=requirement.identifier,
                requirement=requirement.text,
                scope="component-specific",
                status="not applicable",
                evidence="Not part of the bounded deliverable.",
                maintainer_action="-",
                reviewer_follow_up="-",
                line_number=index,
            )
            for index, requirement in enumerate(reversed(requirements), start=1)
        ]
        self.assertIn(
            "requirement rows are not in canonical checklist order",
            validate_rows(requirements, rows),
        )

    def test_evidence_anchor_must_resolve(self):
        requirements = load_requirements()
        rows = [
            ScorecardRow(
                identifier=requirement.identifier,
                requirement=requirement.text,
                scope="component-specific",
                status="not applicable",
                evidence=(
                    "[E-001]"
                    if index == 1
                    else "Not part of the bounded deliverable."
                ),
                maintainer_action="-",
                reviewer_follow_up="-",
                line_number=index,
            )
            for index, requirement in enumerate(requirements, start=1)
        ]
        unresolved = "\n".join(validate_rows(requirements, rows, {}))
        self.assertIn("unresolved evidence reference [E-001]", unresolved)
        self.assertEqual([], validate_rows(requirements, rows, {"E-001": 200}))

    def test_evidence_ledger_rejects_duplicate_ids(self):
        report = (
            "| Evidence ID | Claim | Repository/SHA or package | Evidence type | "
            "Reproduction/source | Rechecked now? |\n"
            "|---|---|---|---|---|---|\n"
            "| E-001 | claim one | owner/repo@abc | source | LICENSE | yes |\n"
            "| E-001 | claim two | package 1.0 | artifact | nupkg | yes |\n"
        )
        with tempfile.TemporaryDirectory() as directory:
            report_path = Path(directory) / "report.md"
            report_path.write_text(report, encoding="utf-8")
            ledger, errors = parse_evidence_ledger(report_path)
        self.assertEqual({"E-001": 3}, ledger)
        self.assertIn("duplicate evidence ledger ID E-001", "\n".join(errors))

    def test_structural_validation_receipt_records_selection_and_digest(self):
        requirements = load_requirements()[:2]
        report = render_template(requirements)
        report = report.replace("[scope]", "component-specific")
        report = report.replace("[status]", "not tested")
        report = report.replace("[evidence]", "The bounded probe was not run.")
        report = report.replace("[maintainer action]", "-")
        report = report.replace("[reviewer follow-up]", "Run the bounded probe.")
        validated_at = datetime(2026, 8, 13, 18, 0, tzinfo=timezone.utc)

        with tempfile.TemporaryDirectory() as directory:
            report_path = Path(directory) / "targeted.md"
            receipt_path = Path(directory) / "receipt.json"
            report_path.write_text(report, encoding="utf-8")
            rows = parse_scorecard(report_path)
            receipt = build_validation_receipt(
                SKILL_ROOT / "references/checklist.md",
                report_path,
                "targeted",
                requirements,
                rows,
                [],
                validated_at,
            )
            write_validation_receipt(receipt_path, receipt)
            persisted = json.loads(receipt_path.read_text(encoding="utf-8"))

        self.assertEqual("1.2.0", persisted["rubric_version"])
        self.assertEqual("targeted", persisted["mode"])
        self.assertEqual(
            [requirement.identifier for requirement in requirements],
            persisted["selected_ids"],
        )
        self.assertEqual(2, persisted["canonical_row_count"])
        self.assertEqual(2, persisted["valid_row_count"])
        self.assertEqual("2026-08-13T18:00:00Z", persisted["validated_at_utc"])
        self.assertEqual(64, len(persisted["report_sha256"]))
        self.assertIn("does not establish factual evidence", persisted["limitation"])

    def test_targeted_scorecard_selects_only_named_ids_in_canonical_order(self):
        all_requirements = load_requirement_set(overlays=tuple(OVERLAY_PATHS))
        targeted = select_requirements(
            all_requirements,
            "BEQ-15,BEQ-12,SCF-02",
        )
        self.assertEqual(
            ["BEQ-12", "BEQ-15", "SCF-02"],
            [requirement.identifier for requirement in targeted],
        )

        report = render_template(targeted)
        report = report.replace("[scope]", "component-specific")
        report = report.replace("[status]", "not tested")
        report = report.replace("[evidence]", "The targeted probe was not run.")
        report = report.replace("[maintainer action]", "-")
        report = report.replace(
            "[reviewer follow-up]",
            "Run the named deterministic probe.",
        )
        with tempfile.TemporaryDirectory() as directory:
            report_path = Path(directory) / "targeted.md"
            report_path.write_text(report, encoding="utf-8")
            rows = parse_scorecard(report_path)
        self.assertEqual([], validate_rows(targeted, rows))

    def test_targeted_scorecard_rejects_unknown_and_duplicate_ids(self):
        requirements = load_requirement_set(overlays=tuple(OVERLAY_PATHS))
        with self.assertRaisesRegex(ValueError, "unknown targeted IDs"):
            select_requirements(requirements, "BEQ-12,NOPE-01")
        with self.assertRaisesRegex(ValueError, "duplicate targeted IDs"):
            select_requirements(requirements, "BEQ-12,BEQ-12")

    def test_vally_suite_covers_every_prefix(self):
        requirements = load_requirement_set(overlays=tuple(OVERLAY_PATHS))
        expected_prefixes = {
            requirement.identifier.rsplit("-", 1)[0]
            for requirement in requirements
        }
        actual_prefixes = {
            prefix
            for stimulus in parse_vally_stimuli()
            for prefix in stimulus.tags["requirement_prefixes"].split(",")
        }
        self.assertEqual(expected_prefixes, actual_prefixes)

    def test_vally_suite_is_pinned_and_governed(self):
        content = VALLY_PATH.read_text(encoding="utf-8")
        stimuli = parse_vally_stimuli()
        self.assertGreaterEqual(len(stimuli), 14)
        self.assertIn(f"# Validated with {VALLY_PACKAGE}.", content)
        self.assertIn("  runs: 5", content)
        self.assertIn("  judge_model: claude-opus-5", content)
        self.assertIn('dest: "eval-input/evidence.md"', content)
        for stimulus in stimuli:
            self.assertGreaterEqual(stimulus.rubric_count, 4)
            self.assertTrue(stimulus.tags["provenance_source"])
            self.assertTrue(stimulus.tags["positive_controls"])
            self.assertTrue(stimulus.tags["negative_controls"])

if __name__ == "__main__":
    unittest.main()
