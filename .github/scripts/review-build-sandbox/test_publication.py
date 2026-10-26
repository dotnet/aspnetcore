import json
import os
from pathlib import Path
import subprocess
import tempfile
import textwrap
import unittest

from receipt import public_summary


WORKFLOW = Path(__file__).resolve().parents[2] / "workflows" / "pull-request-review.md"
HEAD = "a" * 40
RUN_URL = "https://github.com/PureWeen/aspnetcore/actions/runs/123"
HARNESS = r"""
const fs = require('fs');
const vm = require('vm');
const input = JSON.parse(fs.readFileSync(0, 'utf8'));
const failures = [];
vm.runInNewContext('(function () {\n' + input.script + '\n})()', {
  require,
  process: { env: input.env },
  core: { setFailed: message => failures.push(message) },
}, { timeout: 1000 });
process.stdout.write(JSON.stringify(failures));
"""


class PublicationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = WORKFLOW.read_text(encoding="utf-8")
        step = source.split("      - name: Reject incomplete or partial publication sets\n", 1)[1]
        body = step.split("          script: |\n", 1)[1]
        lines = []
        for line in body.splitlines():
            if line and not line.startswith("            "):
                break
            lines.append(line)
        cls.script = textwrap.dedent("\n".join(lines))
        if not cls.script:
            raise AssertionError("The actual publication preflight script was not extracted.")

    def gate(self, items, execution=None):
        with tempfile.TemporaryDirectory(prefix="review-publication-") as directory:
            root = Path(directory) / "review-publication-gate"
            root.mkdir()
            (root / "agent_output.json").write_text(json.dumps({"items": items}), encoding="utf-8")
            if execution is not None:
                (root / "execution.json").write_text(json.dumps(execution), encoding="utf-8")
            result = subprocess.run(
                ["node", "-e", HARNESS],
                input=json.dumps({
                    "script": self.script,
                    "env": {"RUNNER_TEMP": directory, "REVIEW_HEAD": HEAD, "REVIEW_EXECUTION_RUN_URL": RUN_URL},
                }),
                text=True, capture_output=True, check=True, timeout=5,
                env={key: os.environ[key] for key in ("PATH", "SYSTEMROOT") if key in os.environ},
            )
            return json.loads(result.stdout)

    def execution(self, classification):
        result = {
            "schemaVersion": 1, "supportingEvidenceOnly": True, "headSha": HEAD,
            "mergeBaseSha": "b" * 40, "baseTipSha": "c" * 40,
            "available": classification == "head-green", "classification": classification,
            "reason": "No supported execution evidence.",
            "counts": {"passed": 1, "skipped": 0},
            "profile": {"id": "response-caching", "project": "ResponseCaching.Tests.csproj"},
            "sdk": "11.0.100-rc.1.26420.103", "uncoveredChangedPathCount": 0,
        }
        result["publicSummary"] = public_summary(result, RUN_URL)
        return result

    def findings(self, count, execution):
        return [
            *({"type": "create_pull_request_review_comment"} for _ in range(count)),
            {"type": "submit_pull_request_review", "body": "Findings.\n\n" + execution["publicSummary"]},
        ]

    def test_clean_noop_does_not_require_execution_artifact(self):
        self.assertEqual([], self.gate([{"type": "noop"}]))

    def test_clean_noop_accepts_unavailable_execution(self):
        for classification in ("unsupported", "unavailable"):
            with self.subTest(classification=classification):
                self.assertEqual([], self.gate([{"type": "noop"}], self.execution(classification)))

    def test_clean_noop_still_accepts_green_execution(self):
        self.assertEqual([], self.gate([{"type": "noop"}], self.execution("head-green")))

    def test_execution_status_does_not_add_a_clean_review_comment_modality(self):
        execution = self.execution("unsupported")
        items = [{
            "type": "add_comment",
            "body": (
                "Review completed source-only with no new findings; execution evidence unavailable "
                f"({execution['classification']}): {execution['reason']}"
            ),
        }]
        self.assertEqual(["Incomplete or partial review output cannot be published."], self.gate(items, execution))

    def test_complete_findings_require_matching_execution_summary(self):
        for classification in ("head-green", "unsupported", "unavailable"):
            for count in (1, 5):
                with self.subTest(classification=classification, count=count):
                    execution = self.execution(classification)
                    self.assertEqual([], self.gate(self.findings(count, execution), execution))
        execution = self.execution("head-green")
        items = self.findings(1, execution)
        items[-1]["body"] = "Findings without the execution disclosure."
        self.assertEqual(["Incomplete or partial review output cannot be published."], self.gate(items, execution))
        self.assertEqual(["The validated execution status is not valid JSON."], self.gate(items))

    def test_partial_mixed_and_excess_output_remains_blocked(self):
        execution = self.execution("head-green")
        for items in ([], self.findings(0, execution), self.findings(6, execution),
                      self.findings(1, execution) + [{"type": "noop"}]):
            with self.subTest(items=items):
                self.assertEqual(["Incomplete or partial review output cannot be published."], self.gate(items, execution))

    def test_invalid_execution_metadata_is_rejected(self):
        for field, value in (("mergeBaseSha", "invalid"), ("baseTipSha", "invalid"), ("reason", "two\nlines")):
            with self.subTest(field=field):
                execution = self.execution("head-green")
                execution[field] = value
                self.assertEqual(
                    ["The validated execution status is malformed."],
                    self.gate(self.findings(1, execution), execution),
                )

    def test_summary_must_disclose_the_recorded_trees_and_availability(self):
        for classification in ("head-green", "unsupported", "unavailable"):
            replacements = [("b" * 40, "d" * 40), ("c" * 40, "e" * 40)]
            if classification != "head-green":
                replacements.append(("No supported execution evidence.", "A different reason."))
            for old, new in replacements:
                with self.subTest(classification=classification, old=old):
                    execution = self.execution(classification)
                    self.assertIn(old, execution["publicSummary"])
                    execution["publicSummary"] = execution["publicSummary"].replace(old, new)
                    self.assertEqual(
                        ["Incomplete or partial review output cannot be published."],
                        self.gate(self.findings(1, execution), execution),
                    )

    def test_incomplete_status_remains_independent_of_execution(self):
        reason = "A required guide could not complete."
        items = [
            {"type": "report_incomplete", "reason": reason},
            {"type": "add_comment", "body": f"Review not published (BLOCKED): {reason}\n\nNo partial findings were published."},
        ]
        self.assertEqual([], self.gate(items))


if __name__ == "__main__":
    unittest.main()
