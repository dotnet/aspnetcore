import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import prepare_run
import test_validate_outcome as fixtures
from test_prepare_context import changed_file, pull_request


class RuntimeWiringTests(unittest.TestCase):
    def test_checked_out_workflow_revision_executes_the_matching_preparation_cli(self):
        helpers = Path(__file__).parent
        compiled = (helpers.parent / "pr-docs-check.lock.yml").read_text(encoding="utf-8")
        checkout = compiled.split("- name: Check out trusted workflow helpers\n", 1)[1].split(
            "- name: Mint ASP.NET Core docs bot token", 1,
        )[0]
        ref = re.search(r"^\s+ref: (.+)$", checkout, re.M)[1].strip()
        command = re.search(
            r"python3 \.github/workflows/pr-docs-check/prepare_run\.py \\\n(.*?)\n          if grep",
            compiled, re.S,
        )[0].rsplit("\n          if grep", 1)[0].replace("\\\n", " ")
        runner = """
import json
import runpy
import subprocess
import sys
from pathlib import Path
from unittest.mock import patch
sys.argv = sys.argv[1:]
sys.path.insert(0, str(Path(sys.argv[0]).parent))
responses = json.loads(Path('responses.json').read_text())
def api(command, **kwargs):
    assert command[:4] == ['gh', 'api', '--method', 'GET'], command
    return subprocess.CompletedProcess(command, 0, json.dumps(responses[command[-1]]), '')
with patch('prepare_context.subprocess.run', side_effect=api):
    runpy.run_path(sys.argv[0], run_name='__main__')
"""
        for event_name, head in (
            ("workflow_dispatch", None),
            ("pull_request_target", "contributor/aspnetcore"),
            ("pull_request_target", "dotnet/aspnetcore"),
        ):
            with self.subTest(event_name=event_name, head=head), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)

                def git(*args):
                    return subprocess.run(
                        ["git", *args], cwd=root, check=True, capture_output=True, text=True,
                        env={**os.environ, "GIT_AUTHOR_NAME": "Test", "GIT_AUTHOR_EMAIL": "test@example.com",
                             "GIT_COMMITTER_NAME": "Test", "GIT_COMMITTER_EMAIL": "test@example.com"},
                    ).stdout.strip()

                git("init", "-b", "main")
                destination = root / ".github" / "workflows" / "pr-docs-check"
                shutil.copytree(helpers, destination, ignore=shutil.ignore_patterns("test_*", "__pycache__"))
                git("add", ".")
                git("commit", "-m", "Workflow revision")
                workflow_sha = git("rev-parse", "HEAD")
                (destination / "prepare_run.py").write_text(
                    "raise RuntimeError('Mutable main or source PR head helper selected')\n", encoding="utf-8",
                )
                git("add", ".")
                git("commit", "-m", "Different revision")
                if ref == "${{ github.event_name == 'workflow_dispatch' && github.workflow_sha || '' }}":
                    checkout_ref = workflow_sha if event_name == "workflow_dispatch" else ""
                    selected = checkout_ref or workflow_sha
                else:
                    selected = ref
                git("checkout", "--detach", selected)
                metadata = fixtures.ValidateOutcomeTests._metadata(9, "docs/aspnetcore-pr-42-abcd")
                metadata.update(title="Editorial title", body="Editorial body", labels=[])
                metadata["head"]["sha"] = "a" * 40
                pr = pull_request()
                if event_name == "workflow_dispatch":
                    event = {"inputs": {"source_repository": "dotnet/aspnetcore", "pr_number": "42"}}
                else:
                    pr["head"] = {"repo": {"full_name": head}, "sha": git("rev-parse", "main")}
                    pr["merge_commit_sha"] = workflow_sha
                    event = {"action": "closed", "pull_request": pr}
                (root / "event.json").write_text(json.dumps(event), encoding="utf-8")
                responses = {
                    "/repos/dotnet/aspnetcore/pulls/42": pr,
                    "/repos/dotnet/AspNetCore.Docs/pulls?state=open&per_page=100": [[metadata]],
                    "/repos/dotnet/AspNetCore.Docs/pulls/9": metadata,
                }
                (root / "responses.json").write_text(json.dumps(responses), encoding="utf-8")
                invocation = command
                for name, value in {
                    "GITHUB_EVENT_PATH": str(root / "event.json"), "GITHUB_EVENT_NAME": event_name,
                    "GITHUB_REPOSITORY": "dotnet/aspnetcore", "CONTEXT_DIR": str(root / "context"),
                    "DOCS_BOT_APP_SLUG": "aspnetcore-docs-bot",
                }.items():
                    invocation = invocation.replace("${" + name + "}", value)
                result = subprocess.run(
                    [sys.executable, "-c", runner, *shlex.split(invocation)[1:]],
                    cwd=root, capture_output=True, text=True,
                    env={**os.environ, "GITHUB_OUTPUT": str(root / "output"),
                         "GITHUB_STEP_SUMMARY": str(root / "summary"), "DOCS_GITHUB_TOKEN": "fixture"},
                )
                self.assertEqual(0, result.returncode, result.stderr)
                outputs = dict(line.split("=", 1) for line in (root / "output").read_text().splitlines())
                self.assertEqual("false", outputs["analyze"])
                self.assertEqual("42", outputs["source_pr_number"])
                self.assertIn("https://github.com/dotnet/AspNetCore.Docs/pull/9", (root / "summary").read_text())

    def test_cli_defaults_to_skip_and_refresh_publishes_only_the_trusted_target(self):
        cases = [("cli", mode, None) for mode in (None, "skip", "refresh")]
        cases += [("workflow_dispatch", mode, None) for mode in (None, "skip", "refresh")]
        cases += [("pull_request_target", None, head) for head in ("contributor/aspnetcore", "dotnet/aspnetcore")]
        for event_name, mode, head in cases:
            with self.subTest(event_name=event_name, mode=mode, head=head), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                metadata = fixtures.ValidateOutcomeTests._metadata(9, "docs/aspnetcore-pr-42-abcd")
                metadata.update(title="Editorial title", body="Editorial body", labels=[])
                metadata["head"]["sha"] = "a" * 40
                requests = []

                def api(command, **kwargs):
                    self.assertEqual(["gh", "api", "--method", "GET"], command[:4])
                    endpoint = command[-1]
                    requests.append(endpoint)
                    if endpoint == "/repos/dotnet/aspnetcore/pulls/42":
                        result = pull_request()
                    elif endpoint == "/repos/dotnet/AspNetCore.Docs/pulls?state=open&per_page=100":
                        result = [[metadata]]
                        self.assertEqual("docs-fixture", kwargs["env"]["GH_TOKEN"])
                    elif endpoint == "/repos/dotnet/AspNetCore.Docs/pulls/9":
                        result = metadata
                    elif endpoint == "/repos/dotnet/aspnetcore/pulls/42/files?per_page=100":
                        self.assertEqual("refresh", mode)
                        result = [[changed_file("src/Foo.cs")]]
                    else:
                        self.fail(f"Unexpected API request: {command}")
                    return subprocess.CompletedProcess(command, 0, json.dumps(result), "")

                arguments = [
                    "prepare_run.py", "--source-repository", "dotnet/aspnetcore",
                    "--source-pr-number", "42", "--output-directory", str(root),
                    "--allowed-author", "aspnetcore-docs-bot[bot]",
                ]
                if mode is not None:
                    arguments.extend(["--existing-draft", mode])
                if event_name != "cli":
                    if event_name == "workflow_dispatch":
                        inputs = {"source_repository": "dotnet/aspnetcore", "pr_number": "42"}
                        if mode is not None:
                            inputs["existing_draft"] = mode
                        event = {"inputs": inputs}
                    else:
                        pr = pull_request()
                        pr["head"] = {"repo": {"full_name": head}}
                        event = {"action": "closed", "pull_request": pr,
                                 "inputs": {"pr_number": "999", "existing_draft": "refresh"}}
                    event_path = root / "event.json"
                    event_path.write_text(json.dumps(event), encoding="utf-8")
                    arguments = [
                        "prepare_run.py", "--event-path", str(event_path), "--event-name", event_name,
                        "--workflow-repository", "dotnet/aspnetcore", "--output-directory", str(root),
                        "--allowed-author", "aspnetcore-docs-bot[bot]",
                    ]
                with patch.object(sys, "argv", arguments), patch.dict(os.environ, {
                    "GITHUB_OUTPUT": str(root / "output"), "GITHUB_STEP_SUMMARY": str(root / "summary"),
                    "DOCS_GITHUB_TOKEN": "docs-fixture",
                }), patch("prepare_context.subprocess.run", side_effect=api):
                    prepare_run.main()
                outputs = dict(line.split("=", 1) for line in (root / "output").read_text().splitlines())
                self.assertEqual("true" if mode == "refresh" else "false", outputs["analyze"])
                self.assertEqual("9" if mode == "refresh" else "", outputs["docs_pr_number"])
                self.assertEqual("a" * 40 if mode == "refresh" else "", outputs["head_sha"])
                self.assertEqual("dotnet/aspnetcore", outputs["source_repository"])
                self.assertEqual("42", outputs["source_pr_number"])
                self.assertEqual(mode == "refresh", (root / "files.json").exists())
                self.assertEqual(4 if mode == "refresh" else 3, len(requests))

    def test_excluded_events_are_rejected_before_any_api_or_analysis(self):
        cases = [
            ("pull_request_target", "dotnet/aspnetcore", {"action": "closed", "pull_request": {}}),
            ("push", "dotnet/aspnetcore", {}),
            ("workflow_dispatch", "other/aspnetcore", {"inputs": {"pr_number": "42"}}),
        ]
        for action, merged, base, repository, number in (
            ("opened", True, "main", "dotnet/aspnetcore", 42),
            ("closed", False, "main", "dotnet/aspnetcore", 42),
            ("closed", True, "release/11.0", "dotnet/aspnetcore", 42),
            ("closed", True, "main", "other/aspnetcore", 42),
            ("closed", True, "main", "dotnet/aspnetcore", 0),
            ("closed", True, "main", "dotnet/aspnetcore", "42"),
            ("closed", True, "main", "dotnet/aspnetcore", True),
        ):
            pr = pull_request()
            pr.update(merged=merged, number=number)
            pr["base"] = {"ref": base, "repo": {"full_name": repository}}
            cases.append(("pull_request_target", "dotnet/aspnetcore", {"action": action, "pull_request": pr}))
        for event_name, repository, event in cases:
            with self.subTest(event_name=event_name, repository=repository, event=event):
                with tempfile.TemporaryDirectory() as directory:
                    event_path = Path(directory) / "event.json"
                    event_path.write_text(json.dumps(event), encoding="utf-8")
                    arguments = [
                        "prepare_run.py", "--event-path", str(event_path), "--event-name", event_name,
                        "--workflow-repository", repository, "--output-directory", directory,
                        "--allowed-author", "aspnetcore-docs-bot[bot]",
                    ]
                    with patch.object(sys, "argv", arguments), patch("prepare_run.prepare_run") as preparation:
                        with self.assertRaises(ValueError):
                            prepare_run.main()
                        preparation.assert_not_called()

    def test_generated_dependencies_and_push_configuration_consume_trusted_outputs(self):
        workflow = (Path(__file__).parents[1] / "pr-docs-check.lock.yml").read_text(encoding="utf-8")
        jobs = dict(re.findall(r"^  ([a-z_]+):\n(.*?)(?=^  [a-z_]+:\n|\Z)", workflow, re.M | re.S))
        for name, required in (
            ("agent", ("activation", "docs_context", "pat_pool")),
            ("safe_outputs", ("agent", "detection", "docs_context")),
        ):
            with self.subTest(job=name):
                needs = jobs[name].split("    needs:\n", 1)[1].split("    if:", 1)[0]
                self.assertEqual(set(required) | ({"activation"} if name == "safe_outputs" else set()),
                                 set(re.findall(r"      - ([a-z_]+)", needs)))
        self.assertIn("needs.docs_context.outputs.analyze == 'true'", jobs["agent"])
        for name in ("detection", "safe_outputs", "notify_source_pr"):
            self.assertIn("needs.agent.result != 'skipped'", jobs[name].split("    runs-on:", 1)[0])
        self.assertIn('GH_AW_ALLOWED_BOTS: "dotnet-policy-service[bot]"', jobs["pre_activation"])
        self.assertIn('GH_AW_REQUIRED_ROLES: "admin,maintainer,write"', jobs["pre_activation"])
        self.assertIn("github.event.pull_request.merged == true", jobs["pre_activation"])
        self.assertIn("needs.pre_activation.outputs.activated == 'true'", jobs["docs_context"])
        self.assertIn("    needs: pre_activation\n", jobs["pat_pool"])
        self.assertIn("--event-path", jobs["docs_context"])
        self.assertIn("needs.docs_context.outputs.source_pr_number", jobs["safe_outputs"])
        for name, key in (("agent", "GH_AW_SAFE_OUTPUTS_CONFIG"), ("safe_outputs", "GH_AW_SAFE_OUTPUTS_HANDLER_CONFIG")):
            encoded = re.search(rf"^\s+{key}: (.+)$", jobs[name], re.M)[1]
            config = json.loads(json.loads(encoded))
            push = config["push_to_pull_request_branch"]
            self.assertEqual(
                "${{ needs.docs_context.outputs.docs_pr_number }}",
                push["target"],
            )
            self.assertEqual("error", push["if_no_changes"])
            self.assertNotIn("title_prefix", push)
            self.assertNotIn("required_labels", push)
            self.assertNotIn("update_pull_request", config)

    def test_direct_trigger_and_manual_inputs_keep_separate_execution_boundaries(self):
        root = Path(__file__).parents[1]
        workflow = (root / "pr-docs-check.md").read_text(encoding="utf-8")
        self.assertFalse((root / "pr-docs-check-merged.yml").exists())
        self.assertRegex(workflow, r"pull_request_target:\s+types: \[closed\]\s+branches: \[main\]")
        self.assertIn("github.event.pull_request.merged == true", workflow)
        self.assertIn("acknowledge-risk: true", workflow)
        self.assertRegex(workflow, r"allowed-checkouts:\s+- repository: dotnet/AspNetCore.Docs\s+ref: main")
        self.assertRegex(workflow, r"checkout:\s+- repository: dotnet/AspNetCore.Docs\s+ref: main")
        self.assertNotIn('fetch: ["*"]', workflow)
        self.assertNotIn("actions: write", workflow)
        self.assertNotIn('bots: ["github-actions[bot]"]', workflow)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("default: skip", workflow)
        self.assertIn("- refresh", workflow)
        self.assertIn("github.event.pull_request.number || inputs.pr_number", workflow)
        self.assertEqual(2, workflow.count("ref: main"))
        self.assertEqual(3, workflow.count(
            "ref: ${{ github.event_name == 'workflow_dispatch' && github.workflow_sha || '' }}",
        ))
        self.assertNotIn("github.event.inputs.", workflow)


if __name__ == "__main__":
    unittest.main()
