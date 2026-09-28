import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.md"


class WorkflowPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")

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

    def test_turn_cap_is_not_raised_as_the_fix(self):
        self.assertIn("\nmax-turns: 100\n", self.workflow)


if __name__ == "__main__":
    unittest.main()
