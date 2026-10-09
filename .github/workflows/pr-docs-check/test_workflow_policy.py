import json
import re
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.md"
COMPILED_WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.lock.yml"


class WorkflowPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")
        cls.compiled_workflow = COMPILED_WORKFLOW.read_text(encoding="utf-8")

    def test_directory_values_do_not_require_optional_surfaces(self):
        self.assertIn(
            "These directory values specify placement only when the corresponding documentation "
            "surface is independently required",
            self.workflow,
        )
        self.assertIn(
            "Release-note content is optional, not a fourth documentation obligation.",
            self.workflow,
        )
        self.assertIn(
            "Do not create a new future-version release-note entry point, directory, or `includes` "
            "hierarchy merely because `release_notes_directory` resolves to that path.",
            self.workflow,
        )
        self.assertIn(
            "`aspnetcore/release-notes/aspnetcore-<major>.md` entry point and "
            "`aspnetcore/release-notes/aspnetcore-<major>/includes/` hierarchy",
            self.workflow,
        )
        self.assertIn(
            "When that structure is absent, skip the optional release-note surface",
            self.workflow,
        )

    def test_denied_tools_are_not_retried_through_alternatives(self):
        self.assertIn(
            "If a command or tool call is denied by policy, treat that denial as final for the "
            "attempted operation.",
            self.workflow,
        )
        self.assertIn(
            "Do not retry the operation through alternate binaries, shell constructions, encoded "
            "commands, installers, or indirect equivalents.",
            self.workflow,
        )
        self.assertIn(
            'emit `notify_source_pr` with `result: "draft_failed"` and '
            '`docs_pr_action: "none"` instead of looping',
            self.workflow,
        )

    def test_agent_bash_allowlist_remains_narrow(self):
        tools = self.workflow.split("tools:", 1)[1].split("network:", 1)[0]

        self.assertIn("bash: [cat, find, git, grep, head, jq, mkdir, sed]", tools)
        self.assertNotIn("bash: '*'", tools)
        self.assertNotIn("bash: [\"*\"]", tools)
        for command in ("install", "touch", "cp", "python3", "curl", "base64", "rm"):
            self.assertNotRegex(tools, rf"\b{command}\b")

    def test_workflow_recovery_policy(self):
        self.assertIn("\nmax-turns: 50\n", self.workflow)
        self.assertIn("start finalizing by invocation 35", self.workflow)

        create_pull_request = self.workflow.split("  create-pull-request:", 1)[1].split(
            "  push-to-pull-request-branch:", 1
        )[0]

        self.assertIn("    preserve-branch-name: true", create_pull_request)
        self.assertIn("    recreate-ref: true", create_pull_request)
        self.assertIn('\\"preserve_branch_name\\":true', self.compiled_workflow)
        self.assertIn('\\"recreate_ref\\":true', self.compiled_workflow)

    def test_cached_context_feeds_decisions_without_reducing_context_access(self):
        self.assertIn("pr-docs-check/pr.json", self.workflow)
        self.assertIn("pr-docs-check/signals.json", self.workflow)
        self.assertIn("pr-docs-check/files.json", self.workflow)
        self.assertIn("Use the deterministic signals as an evidence checklist", self.workflow)
        self.assertIn("not as an automatic docs requirement", self.workflow)
        self.assertIn("both review and issue comments when needed", self.workflow)
        self.assertIn("review and issue comments only when they contain information needed", self.workflow)
        self.assertIn("A score below 60 means no documentation PR may be created.", self.workflow)
        for floor in (
            "public API added or materially changed: at least 70",
            "project template or generated application behavior changed: at least 70",
            "default, convention, or configuration behavior changed: at least 75",
            "explicit breaking-change evidence: at least 80",
        ):
            self.assertIn(floor, self.workflow)
        self.assertIn("a bug fix that merely restores behavior already documented accurately", self.workflow)

    def test_manual_main_gate_precedes_version_resolution(self):
        trigger = self.workflow.split("on:", 1)[1].split("permissions:", 1)[0]
        self.assertIn("workflow_dispatch:", trigger)
        self.assertNotIn("pull_request:", trigger)
        preparation = self.workflow.split("- name: Resolve source version and existing docs draft", 1)[1]
        self.assertLess(preparation.index("prepare_run.py"), preparation.index("source-branches.json"))
        self.assertLess(preparation.index('!= "eligible"'), preparation.index("resolve_target_version.py"))
        self.assertEqual(2, self.workflow.count('--source-preflight "${RUNNER_TEMP}'))
        self.assertEqual(2, self.compiled_workflow.count('--source-preflight "${RUNNER_TEMP}'))
        self.assertIn("GH_AW_MAX_TURNS: 50", self.compiled_workflow)
        self.assertIn('\\"maxRuns\\":50', self.compiled_workflow)

    def test_author_is_body_only_and_updates_preserve_description(self):
        attribution = self.workflow.split("- name: Notify source author on docs pull request", 1)[1].split(
            "pre-agent-steps:", 1,
        )[0]
        self.assertIn("github.rest.pulls.update", attribution)
        self.assertNotIn("github.rest.issues", attribution)
        self.assertNotIn("assignees:", self.workflow)
        self.assertNotIn("reviewers:", self.workflow)
        self.assertIn("Do not emit `update_pull_request` or edit the title or body.", self.workflow)
        self.assertNotIn("  update-pull-request:", self.workflow)
        self.assertNotIn('\\"required_title_prefix\\"', self.compiled_workflow)

    def test_compiler_and_setup_use_the_same_published_release(self):
        metadata = json.loads(self.compiled_workflow.splitlines()[0].removeprefix("# gh-aw-metadata: "))
        self.assertEqual("v0.91.6", metadata["compiler_version"])
        self.assertTrue(metadata["strict"])
        setup = re.findall(r"uses: github/gh-aw-actions/setup@([a-f0-9]{40}) # (v[0-9.]+)", self.compiled_workflow)
        self.assertEqual(6, len(setup))
        self.assertEqual({("13f59f7032b1e9a26305dada85d6b99fa2a5e649", "v0.91.6")}, set(setup))
        self.assertNotIn("source-303b402", self.compiled_workflow)
        self.assertNotIn("github/gh-aw/actions/setup@", self.compiled_workflow)
        cache = json.loads((WORKFLOW.parents[1] / "aw" / "actions-lock.json").read_text(encoding="utf-8"))
        self.assertEqual(setup[0][0], cache["entries"]["github/gh-aw-actions/setup@v0.91.6"]["sha"])


if __name__ == "__main__":
    unittest.main()
