import re
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.md"


class AuthenticationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")

    def test_source_access_uses_workflow_token(self):
        tools = self._section("tools:", "network:")

        self.assertIn("github-token: ${{ secrets.GITHUB_TOKEN }}", tools)
        self.assertNotIn("github-app:", tools)
        self.assertIn("GH_TOKEN: ${{ github.token }}", self.workflow)
        self.assertIn("github-token: ${{ github.token }}", self._step("Publish trusted source outcome"))

    def test_docs_app_tokens_do_not_request_source_repository(self):
        for match in re.finditer(r"repositories:(?: \S+|\s*\n(?: {8,}\S+\n)+)", self.workflow):
            repositories = match.group(0)
            self.assertNotRegex(repositories, r"(?im)^\s*aspnetcore\s*$")

    def test_docs_operations_keep_app_authentication(self):
        self.assertIn("repository: dotnet/AspNetCore.Docs", self.workflow)
        self.assertIn("repositories: [\"AspNetCore.Docs\"]", self.workflow)
        self.assertIn("repositories: [\"AspNetCore.Docs.Automation\"]", self.workflow)
        self.assertIn("DOCS_GITHUB_TOKEN: ${{ steps.docs-bot-token.outputs.token }}", self.workflow)
        self.assertIn(
            'GH_TOKEN="${DOCS_GITHUB_TOKEN}" gh api',
            self.workflow,
        )
        self.assertIn(
            "github-token: ${{ steps.docs-bot-token.outputs.token }}",
            self._step("Notify source author on docs pull request"),
        )

    def test_source_notification_has_least_privilege_write_permissions(self):
        job = self._section("notify-source-pr:", "pre-agent-steps:")

        self.assertIn("contents: read", job)
        self.assertIn("issues: write", job)
        self.assertIn("pull-requests: read", job)

    def _section(self, start, end):
        _, section = self.workflow.split(start, 1)
        section, _ = section.split(end, 1)
        return section

    def _step(self, name):
        marker = f"- name: {name}"
        _, step = self.workflow.split(marker, 1)
        next_step = re.search(r"^\s*- name: ", step, re.MULTILINE)
        return step[: next_step.start()] if next_step else step


if __name__ == "__main__":
    unittest.main()
