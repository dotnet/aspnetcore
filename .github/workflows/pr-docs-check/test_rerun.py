import copy
import json
import os
import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

import test_validate_outcome as fixtures
from prepare_run import prepare_run
from test_prepare_context import changed_file, pull_request
from validate_outcome import OutcomeValidationError, build_outcome, validate_preflight


class RerunTests(unittest.TestCase):
    def setUp(self):
        self.draft = {
            "found": True, "blocked": False, "selected": {"number": 9, "head_sha": "a" * 40},
        }
        self.payload = fixtures.ValidateOutcomeTests._payload(
            "drafted", "unchanged", {"type": "noop"},
            existing_docs_pr_number=9,
        )
        self.proof = {"base_sha": "a" * 40, "head_sha": "a" * 40, "clean": True}
        self.metadata = fixtures.ValidateOutcomeTests._metadata(9, "docs/aspnetcore-pr-42")
        self.metadata["head"]["sha"] = "a" * 40

    def test_sufficient_refresh_accepts_noop_without_artificial_push_or_title_update(self):
        validate_preflight(self.payload, 42, self.draft, workspace_evidence=self.proof)
        outcome = build_outcome(
            self.payload, "dotnet/aspnetcore", 42, "",
            self.metadata,
            self.draft, "aspnetcore-docs-bot[bot]", workspace_evidence=self.proof,
        )
        self.assertEqual("drafted", outcome["render_kind"])
        self.assertEqual("unchanged", outcome["docs_pr_action"])
        self.assertEqual("https://github.com/dotnet/AspNetCore.Docs/pull/9", outcome["docs_pr_url"])

    def test_unchanged_rejects_missing_dirty_or_mismatched_workspace_proof(self):
        for proof in (
            None, {}, {**self.proof, "clean": False},
            {**self.proof, "base_sha": "b" * 40}, {**self.proof, "head_sha": "b" * 40},
        ):
            with self.subTest(proof=proof):
                with self.assertRaisesRegex(OutcomeValidationError, "workspace"):
                    validate_preflight(self.payload, 42, self.draft, workspace_evidence=proof)

    def test_unchanged_rejects_mutations_missing_noop_and_wrong_target(self):
        for mutation in (
            {"type": "push_to_pull_request_branch", "pull_request_number": 9},
            {"type": "update_pull_request", "pull_request_number": 9, "title": "[docs] Refreshed"},
            {"type": "create_pull_request"}, None,
        ):
            with self.subTest(mutation=mutation):
                payload = copy.deepcopy(self.payload)
                payload["items"] = [item for item in payload["items"] if item["type"] != "noop"]
                if mutation:
                    payload["items"].append(mutation)
                with self.assertRaises(OutcomeValidationError):
                    validate_preflight(payload, 42, self.draft, workspace_evidence=self.proof)
        payload = copy.deepcopy(self.payload)
        payload["items"][-1]["existing_docs_pr_number"] = 10
        with self.assertRaises(OutcomeValidationError):
            validate_preflight(payload, 42, self.draft, workspace_evidence=self.proof)

    def test_unchanged_requires_trusted_open_draft_and_successful_outputs(self):
        for draft in (None, {"found": False, "blocked": False}, {"found": False, "blocked": True}):
            with self.subTest(draft=draft):
                with self.assertRaises(OutcomeValidationError):
                    validate_preflight(self.payload, 42, draft, workspace_evidence=self.proof)
        for field, value in (("state", "closed"), ("draft", False), ("title", "Human title")):
            with self.subTest(field=field):
                metadata = copy.deepcopy(self.metadata)
                metadata[field] = value
                with self.assertRaises(OutcomeValidationError):
                    build_outcome(
                        self.payload, "dotnet/aspnetcore", 42, "", metadata, self.draft,
                        "aspnetcore-docs-bot[bot]", workspace_evidence=self.proof,
                    )
        outcome = build_outcome(
            self.payload, "dotnet/aspnetcore", 42, "",
            self.metadata, self.draft,
            "aspnetcore-docs-bot[bot]", safe_outputs_result="failure", workspace_evidence=self.proof,
        )
        self.assertEqual("draft_failed", outcome["render_kind"])

    def test_source_and_compiled_default_skip_gate(self):
        root = Path(__file__).parents[1]
        for name in ("pr-docs-check.md", "pr-docs-check.lock.yml"):
            with self.subTest(name=name):
                workflow = (root / name).read_text(encoding="utf-8")
                self.assertIn("existing_draft:", workflow)
                self.assertIn("default: skip", workflow)
                self.assertIn("needs.docs_context.outputs.analyze == 'true'", workflow)
                self.assertIn("workspace-evidence.json", workflow)
                self.assertIn("options:", workflow)
                self.assertIn("- refresh", workflow)
                self.assertIn("needs: [docs_context]" if name.endswith(".md") else "- docs_context", workflow)
                if name.endswith(".yml"):
                    for job_name in ("safe_outputs", "notify_source_pr"):
                        job = re.split(r"\n  [a-zA-Z_]", workflow.split(f"\n  {job_name}:\n", 1)[1], 1)[0]
                        self.assertIn("needs.agent.result != 'skipped'", job)

    def test_title_only_updated_output_still_requires_a_real_push(self):
        payload = fixtures.ValidateOutcomeTests._payload(
            "drafted", "updated", {"type": "update_pull_request", "pull_request_number": 9},
            existing_docs_pr_number=9,
        )
        with self.assertRaisesRegex(OutcomeValidationError, "exactly one push"):
            validate_preflight(payload, 42, self.draft, workspace_evidence=self.proof)

    def test_default_skip_trusted_producer_reads_no_patches_or_version_inputs(self):
        source_reads = []
        docs_reads = []

        def source_api(endpoint):
            source_reads.append(endpoint)
            self.assertNotIn("/files?", endpoint)
            return pull_request()

        def docs_api(endpoint):
            docs_reads.append(endpoint)
            return [self.metadata] if "?" in endpoint else self.metadata

        with tempfile.TemporaryDirectory() as directory:
            result = prepare_run(
                "dotnet/aspnetcore", "42", Path(directory), "skip", "aspnetcore-docs-bot[bot]",
                source_api, docs_api,
            )
            self.assertFalse(result["analyze"])
            self.assertIn("https://github.com/dotnet/AspNetCore.Docs/pull/9", result["summary"])
            self.assertFalse((Path(directory) / "files.json").exists())
            self.assertFalse((Path(directory) / "target-version.json").exists())
        self.assertEqual(["/repos/dotnet/aspnetcore/pulls/42"], source_reads)
        self.assertEqual(2, len(docs_reads))

    def test_refresh_or_absent_draft_prepares_real_analysis(self):
        for mode, found in (("skip", False), ("refresh", False), ("refresh", True)):
            with self.subTest(mode=mode, found=found):
                reads = []

                def source_api(endpoint):
                    reads.append(endpoint)
                    return [changed_file("src/Foo.cs")] if "/files?" in endpoint else pull_request()

                with tempfile.TemporaryDirectory() as directory:
                    result = prepare_run(
                        "dotnet/aspnetcore", "42", Path(directory), mode, "aspnetcore-docs-bot[bot]",
                        source_api, lambda endpoint: (
                            [self.metadata] if found else []
                        ) if "?" in endpoint else self.metadata,
                    )
                    self.assertTrue(result["analyze"])
                    self.assertEqual("", result["summary"])
                    self.assertTrue((Path(directory) / "files.json").exists())
                self.assertEqual(2, len(reads))

    def test_eligibility_precedes_docs_lookup_and_patch_reads(self):
        for pr in (
            None, {**pull_request(), "merged": False},
            {**pull_request(), "title": "Advisory update"},
            {**pull_request(), "base": {**pull_request()["base"], "ref": "release/11.0"}},
        ):
            with self.subTest(pr=pr):
                with tempfile.TemporaryDirectory() as directory:
                    result = prepare_run(
                        "dotnet/aspnetcore", "42", Path(directory), "skip", "aspnetcore-docs-bot[bot]",
                        lambda endpoint: pr,
                        lambda endpoint: self.fail("Early source gate must precede docs reads"),
                    )
                    self.assertTrue(result["analyze"])
                    self.assertFalse((Path(directory) / "files.json").exists())

    def test_skip_cannot_trust_changed_or_malformed_live_identity(self):
        for field, value in (
            ("state", "closed"), ("draft", False), ("html_url", "https://example.com/pull/9"),
            ("user", {"login": "human"}), ("title", "Human title"), ("labels", []),
        ):
            with self.subTest(field=field):
                live = {**self.metadata, field: value}
                with tempfile.TemporaryDirectory() as directory:
                    with self.assertRaises((OutcomeValidationError, ValueError)):
                        prepare_run(
                            "dotnet/aspnetcore", "42", Path(directory), "skip", "aspnetcore-docs-bot[bot]",
                            lambda endpoint: pull_request(),
                            lambda endpoint: [self.metadata] if "?" in endpoint else live,
                        )

    def test_non_draft_and_untrusted_matches_block_creation_instead_of_skipping(self):
        for field, value in (
            ("draft", False), ("user", {"login": "human"}), ("title", "Human title"),
            ("labels", []),
        ):
            for mode in ("skip", "refresh"):
                with self.subTest(field=field, mode=mode):
                    metadata = {**self.metadata, field: value}
                    with tempfile.TemporaryDirectory() as directory:
                        root = Path(directory)
                        result = prepare_run(
                            "dotnet/aspnetcore", "42", root, mode, "aspnetcore-docs-bot[bot]",
                            lambda endpoint: [changed_file("src/Foo.cs")] if "/files?" in endpoint else pull_request(),
                            lambda endpoint: [metadata] if "?" in endpoint else self.fail("Do not trust this draft"),
                        )
                        self.assertTrue(result["analyze"])
                        draft = json.loads((root / "existing-draft.json").read_text())
                        self.assertTrue(draft["blocked"])
                        self.assertFalse(draft["found"])
                        payload = fixtures.ValidateOutcomeTests._payload(
                            "drafted", "created", {"type": "create_pull_request"},
                        )
                        with self.assertRaisesRegex(OutcomeValidationError, "must not be modified or replaced"):
                            validate_preflight(payload, 42, draft)

    def test_lookup_rejects_noncanonical_url_instead_of_skipping(self):
        metadata = {**self.metadata, "html_url": "https://example.com/pull/9"}
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "invalid number or URL"):
                prepare_run(
                    "dotnet/aspnetcore", "42", Path(directory), "skip", "aspnetcore-docs-bot[bot]",
                    lambda endpoint: pull_request(), lambda endpoint: [metadata],
                )

    def test_real_git_workspace_producer_rejects_abandoned_edits(self):
        bash = shutil.which("bash")
        if bash is None or shutil.which("jq") is None:
            self.skipTest("The workflow's native bash and jq tools are required.")
        workflow = (Path(__file__).parents[1] / "pr-docs-check.md").read_text(encoding="utf-8")
        step = workflow.split("  - name: Record workspace evidence after analysis\n", 1)[1]
        script = step.split("    run: |\n", 1)[1].split("  - name:", 1)[0]
        script = "\n".join(line.removeprefix("      ") for line in script.splitlines())
        for edit in ("clean", "unstaged", "staged", "staged_restored", "untracked", "committed", "deleted"):
            with self.subTest(edit=edit), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                repo = root / "repo"
                repo.mkdir()

                def git(*args):
                    return subprocess.check_output(
                        ["git", *args], cwd=repo, text=True, stderr=subprocess.STDOUT,
                    ).strip()

                git("init", "--quiet")
                git("config", "user.name", "Fixture")
                git("config", "user.email", "fixture@example.com")
                git("config", "core.autocrlf", "false")
                article = repo / "article.md"
                article.write_text("Existing complete documentation\n")
                git("add", ".")
                git("commit", "--quiet", "-m", "Existing draft")
                sha = git("rev-parse", "HEAD")
                if edit in {"unstaged", "staged", "staged_restored", "committed"}:
                    article.write_text("Required documentation edit\n")
                if edit in {"staged", "staged_restored", "committed"}:
                    git("add", ".")
                if edit == "staged_restored":
                    article.write_text("Existing complete documentation\n")
                if edit == "committed":
                    git("commit", "--quiet", "-m", "New docs")
                if edit == "untracked":
                    (repo / "new.md").write_text("Required new article\n")
                if edit == "deleted":
                    article.unlink()
                subprocess.run(
                    [bash, "-c", script], cwd=repo, check=True,
                    env={**os.environ, "RUNNER_TEMP": "../run", "EXPECTED_DOCS_HEAD_SHA": sha},
                )
                proof = json.loads((root / "run" / "pr-docs-check-workspace" / "workspace-evidence.json").read_text())
                self.draft["selected"]["head_sha"] = sha
                if edit == "clean":
                    validate_preflight(self.payload, 42, self.draft, workspace_evidence=proof)
                else:
                    with self.assertRaisesRegex(OutcomeValidationError, "workspace"):
                        validate_preflight(self.payload, 42, self.draft, workspace_evidence=proof)
                    update = fixtures.ValidateOutcomeTests._payload(
                        "drafted", "updated",
                        {"type": "push_to_pull_request_branch", "pull_request_number": 9},
                        {"type": "update_pull_request", "pull_request_number": 9},
                        existing_docs_pr_number=9,
                    )
                    validate_preflight(update, 42, self.draft, workspace_evidence=proof)


if __name__ == "__main__":
    unittest.main()
