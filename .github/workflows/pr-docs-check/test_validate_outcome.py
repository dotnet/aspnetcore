import json
import tempfile
import unittest
from pathlib import Path

from validate_outcome import OutcomeValidationError, build_outcome, main, validate_preflight


class ValidateOutcomeTests(unittest.TestCase):
    def test_created_draft_is_validated(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
        )

        result = build_outcome(
            payload,
            "dotnet/aspnetcore",
            42,
            "https://github.com/dotnet/AspNetCore.Docs/pull/9",
            self._metadata(9, "docs/aspnetcore-pr-42"),
            {"found": False, "blocked": False},
            "aspnetcore-docs-bot[bot]",
        )

        self.assertEqual("drafted", result["render_kind"])
        self.assertEqual(9, result["docs_pr_number"])

    def test_updated_draft_requires_push_and_metadata_update(self):
        payload = self._payload(
            "drafted",
            "updated",
            {"type": "push_to_pull_request_branch", "pull_request_number": 9},
            {"type": "update_pull_request", "pull_request_number": 9},
            existing_docs_pr_number=9,
        )

        result = build_outcome(
            payload,
            "dotnet/aspnetcore",
            42,
            "",
            self._metadata(9, "docs/aspnetcore-pr-42-abcd"),
            {
                "found": True,
                "blocked": False,
                "selected": {"number": 9},
            },
            "aspnetcore-docs-bot[bot]",
        )

        self.assertEqual("updated", result["docs_pr_action"])
        self.assertEqual("https://github.com/dotnet/AspNetCore.Docs/pull/9", result["docs_pr_url"])

    def test_preflight_rejects_low_confidence_creation(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
            confidence=59,
        )

        with self.assertRaisesRegex(OutcomeValidationError, "at least 60"):
            validate_preflight(payload, 42, {"found": False, "blocked": False})

    def test_preflight_accepts_created_draft(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
            confidence=75,
        )

        validate_preflight(payload, 42, {"found": False, "blocked": False})

    def test_preflight_accepts_updated_draft(self):
        payload = self._payload(
            "drafted",
            "updated",
            {"type": "push_to_pull_request_branch", "pull_request_number": 9},
            {"type": "update_pull_request", "pull_request_number": 9},
            existing_docs_pr_number=9,
            confidence=75,
        )

        validate_preflight(
            payload,
            42,
            {
                "found": True,
                "blocked": False,
                "selected": {"number": 9},
            },
        )

    def test_preflight_rejects_update_targeting_another_pull_request(self):
        for mismatched_type in ("push_to_pull_request_branch", "update_pull_request"):
            with self.subTest(mismatched_type=mismatched_type):
                payload = self._payload(
                    "drafted",
                    "updated",
                    {
                        "type": "push_to_pull_request_branch",
                        "pull_request_number": 10 if mismatched_type == "push_to_pull_request_branch" else 9,
                    },
                    {
                        "type": "update_pull_request",
                        "pull_request_number": 10 if mismatched_type == "update_pull_request" else 9,
                    },
                    existing_docs_pr_number=9,
                )

                with self.assertRaisesRegex(OutcomeValidationError, "targeted 10; expected 9"):
                    validate_preflight(
                        payload,
                        42,
                        {
                            "found": True,
                            "blocked": False,
                            "selected": {"number": 9},
                        },
                    )

    def test_failed_safe_output_does_not_report_successful_update(self):
        for result_value, failed_count in (("failure", "0"), ("success", "1")):
            with self.subTest(result=result_value, failed_count=failed_count):
                payload = self._payload(
                    "drafted",
                    "updated",
                    {"type": "push_to_pull_request_branch", "pull_request_number": 9},
                    {"type": "update_pull_request", "pull_request_number": 9},
                    existing_docs_pr_number=9,
                )

                result = build_outcome(
                    payload,
                    "dotnet/aspnetcore",
                    42,
                    "",
                    self._metadata(9, "docs/aspnetcore-pr-42"),
                    {
                        "found": True,
                        "blocked": False,
                        "selected": {"number": 9},
                    },
                    "aspnetcore-docs-bot[bot]",
                    result_value,
                    failed_count,
                )

                self.assertEqual("draft_failed", result["render_kind"])
                self.assertEqual("none", result["docs_pr_action"])

    def test_restricted_outcome_rejects_code_output(self):
        payload = self._payload(
            "restricted",
            "none",
            {"type": "create_pull_request"},
            confidence=0,
            required=False,
        )

        with self.assertRaisesRegex(OutcomeValidationError, "cannot request documentation"):
            build_outcome(payload, "dotnet/aspnetcore", 42, "", None)

        with self.assertRaisesRegex(OutcomeValidationError, "cannot request documentation or code-writing outputs"):
            validate_preflight(
                payload,
                42,
                {"found": False, "blocked": False},
            )

    def test_missing_created_pr_becomes_draft_failure_render(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
        )

        result = build_outcome(payload, "dotnet/aspnetcore", 42, "", None)

        self.assertEqual("drafted_missing_pr", result["render_kind"])

    def test_rejects_creation_when_existing_draft_was_found(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
        )

        with self.assertRaisesRegex(OutcomeValidationError, "must be updated"):
            build_outcome(
                payload,
                "dotnet/aspnetcore",
                42,
                "",
                None,
                {
                    "found": True,
                    "blocked": False,
                    "selected": {"number": 9},
                },
            )

    def test_rejects_wrong_head_repository(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
        )
        metadata = self._metadata(9, "docs/aspnetcore-pr-42")
        metadata["head"]["repo"]["full_name"] = "dotnet/AspNetCore.Docs"

        with self.assertRaisesRegex(OutcomeValidationError, "automation fork"):
            build_outcome(
                payload,
                "dotnet/aspnetcore",
                42,
                "https://github.com/dotnet/AspNetCore.Docs/pull/9",
                metadata,
                {"found": False, "blocked": False},
                "aspnetcore-docs-bot[bot]",
            )

    def test_rejects_wrong_base_repository(self):
        payload = self._payload(
            "drafted",
            "created",
            {"type": "create_pull_request", "branch": "docs/aspnetcore-pr-42"},
        )
        metadata = self._metadata(9, "docs/aspnetcore-pr-42")
        metadata["base"]["repo"]["full_name"] = "someone/AspNetCore.Docs"

        with self.assertRaisesRegex(OutcomeValidationError, "configured docs repository"):
            build_outcome(
                payload,
                "dotnet/aspnetcore",
                42,
                "https://github.com/dotnet/AspNetCore.Docs/pull/9",
                metadata,
                {"found": False, "blocked": False},
                "aspnetcore-docs-bot[bot]",
            )

    def test_preflight_validation_error_returns_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            agent_output = root / "agent-output.json"
            existing_draft = root / "existing-draft.json"
            agent_output.write_text('{"items":[]}', encoding="utf-8")
            existing_draft.write_text(
                json.dumps({"found": False, "blocked": False}),
                encoding="utf-8",
            )

            exit_code = main(
                [
                    "--preflight",
                    "--agent-output",
                    str(agent_output),
                    "--source-repository",
                    "dotnet/aspnetcore",
                    "--source-pr-number",
                    "42",
                    "--expected-existing-draft",
                    str(existing_draft),
                ]
            )

        self.assertEqual(1, exit_code)

    @staticmethod
    def _payload(
        result,
        action,
        *items,
        existing_docs_pr_number=None,
        confidence=80,
        required=True,
    ):
        notification = {
            "type": "notify_source_pr",
            "source_pr_number": 42,
            "result": result,
            "docs_pr_action": action,
            "docs_needed_confidence": confidence,
            "conceptual_required": required,
            "conceptual_reason": "Reason",
            "migration_required": False,
            "migration_reason": "Reason",
            "breaking_change_required": False,
            "breaking_change_reason": "Reason",
            "summary": "Summary",
        }
        if existing_docs_pr_number is not None:
            notification["existing_docs_pr_number"] = existing_docs_pr_number
        return {"items": [*items, notification]}

    @staticmethod
    def _metadata(number, head_ref):
        return {
            "number": number,
            "html_url": f"https://github.com/dotnet/AspNetCore.Docs/pull/{number}",
            "state": "open",
            "draft": True,
            "title": "[docs] Update docs",
            "body": "Source: dotnet/aspnetcore#42\n\nDetails",
            "base": {
                "ref": "main",
                "repo": {"full_name": "dotnet/AspNetCore.Docs"},
            },
            "head": {
                "ref": head_ref,
                "repo": {"full_name": "dotnet/AspNetCore.Docs.Automation"},
            },
            "user": {"login": "aspnetcore-docs-bot[bot]"},
            "labels": [{"name": "documentation"}],
        }


if __name__ == "__main__":
    unittest.main()
