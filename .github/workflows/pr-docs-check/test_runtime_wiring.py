import json
import os
import re
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
    def test_cli_defaults_to_skip_and_refresh_publishes_only_the_trusted_target(self):
        for mode in (None, "skip", "refresh"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as directory:
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
                with patch.object(sys, "argv", arguments), patch.dict(os.environ, {
                    "GITHUB_OUTPUT": str(root / "output"), "GITHUB_STEP_SUMMARY": str(root / "summary"),
                    "DOCS_GITHUB_TOKEN": "docs-fixture",
                }), patch("prepare_context.subprocess.run", side_effect=api):
                    prepare_run.main()
                outputs = dict(line.split("=", 1) for line in (root / "output").read_text().splitlines())
                self.assertEqual("true" if mode == "refresh" else "false", outputs["analyze"])
                self.assertEqual("9" if mode == "refresh" else "", outputs["docs_pr_number"])
                self.assertEqual("a" * 40 if mode == "refresh" else "", outputs["head_sha"])
                self.assertEqual(mode == "refresh", (root / "files.json").exists())
                self.assertEqual(4 if mode == "refresh" else 3, len(requests))

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
        self.assertIn('GH_AW_ALLOWED_BOTS: "github-actions[bot]"', jobs["pre_activation"])
        for name, key in (("agent", "GH_AW_SAFE_OUTPUTS_CONFIG"), ("safe_outputs", "GH_AW_SAFE_OUTPUTS_HANDLER_CONFIG")):
            encoded = re.search(rf"^\s+{key}: (.+)$", jobs[name], re.M)[1]
            config = json.loads(json.loads(encoded))
            push = config["push_to_pull_request_branch"]
            self.assertEqual(
                "${{ needs.docs_context.outputs.docs_pr_number }}" if name == "safe_outputs" else "",
                push["target"],
            )
            self.assertEqual("error", push["if_no_changes"])
            self.assertNotIn("title_prefix", push)
            self.assertNotIn("required_labels", push)
            self.assertNotIn("update_pull_request", config)

    def test_native_trigger_and_manual_inputs_keep_separate_execution_boundaries(self):
        root = Path(__file__).parents[1]
        dispatcher = (root / "pr-docs-check-merged.yml").read_text(encoding="utf-8")
        workflow = (root / "pr-docs-check.md").read_text(encoding="utf-8")
        self.assertRegex(dispatcher, r"pull_request_target:\s+types: \[closed\]\s+branches: \[main\]")
        self.assertIn("github.event.pull_request.merged == true", dispatcher)
        self.assertIn("permissions: {}", dispatcher)
        self.assertIn("actions: write", dispatcher)
        self.assertNotIn("checkout", dispatcher)
        self.assertNotIn("secrets.", dispatcher)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("default: skip", workflow)
        self.assertIn("- refresh", workflow)
        self.assertIn('group: "pr-docs-check-${{ inputs.source_repository }}-${{ inputs.pr_number }}"', workflow)
        self.assertEqual(3, workflow.count("ref: main"))
        self.assertNotIn("pull_request_target:", workflow)


if __name__ == "__main__":
    unittest.main()
