#!/usr/bin/env python3

import pathlib
import re
import unittest


WORKFLOW_ROOT = pathlib.Path(__file__).parents[2]
SOURCE_WORKFLOW = WORKFLOW_ROOT / "test-quarantine.md"


class WorkflowPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = SOURCE_WORKFLOW.read_text(encoding="utf-8")
        cls.normalized_source = re.sub(r"\s+", " ", cls.source)

    def test_part_two_analysis_is_mandatory(self):
        expected = (
            "An empty Case A/Case B eligibility list completes only Part 1; "
            "it does not make Part 2 optional."
        )
        self.assertIn(expected, self.normalized_source)

    def test_incomplete_analysis_cannot_report_noop(self):
        expected_clauses = (
            "still query pipelines 84 and 87 and complete the Part 2 candidate analysis before reporting `noop`.",
            "A `noop` message must explicitly summarize the Part 1 Case A and Case B candidate counts",
            "and the Part 2 pipeline 84 and pipeline 87 analysis results.",
            "Never use `noop` to disclose that a required phase was skipped.",
        )

        for expected in expected_clauses:
            with self.subTest(expected=expected):
                self.assertIn(expected, self.normalized_source)

    def test_incomplete_analysis_requires_report_incomplete(self):
        expected_clauses = (
            "If required data, tools, time, or turn budget prevent either analysis phase from completing, call `report_incomplete` instead of `noop`.",
            "If either phase is incomplete, emit `report_incomplete` with the missing phase and reason.",
        )

        for expected in expected_clauses:
            with self.subTest(expected=expected):
                self.assertIn(expected, self.normalized_source)

    def test_priority_defers_writes_not_analysis(self):
        expected = (
            "This priority governs writes only: Part 2 analysis remains mandatory"
        )
        self.assertIn(expected, self.normalized_source)

    def test_unquarantine_analysis_is_not_unconditionally_deferred(self):
        self.assertNotIn(
            "Unquarantining is just cleanup — it can always wait until the next run.",
            self.normalized_source,
        )
        self.assertNotIn(
            "Deferred work will be handled by the next daily run",
            self.normalized_source,
        )


if __name__ == "__main__":
    unittest.main()
