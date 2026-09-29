import re
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pr-docs-check.md"
POOL_TOKEN_EXPRESSION = "${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}"


class AuthenticationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")

    def test_copilot_inference_uses_pat_pool(self):
        engine = self._section("engine:", "checkout:")

        self.assertIn(
            "imports:\n"
            "  - uses: shared/pat_pool.md\n"
            "    with:\n"
            "      environment: copilot-pat-pool",
            self.workflow,
        )
        self.assertIn("\nenvironment: copilot-pat-pool\n", self.workflow)
        self.assertIn(f"COPILOT_GITHUB_TOKEN: {POOL_TOKEN_EXPRESSION}", engine)
        self.assertNotIn("secrets.COPILOT_GITHUB_TOKEN", engine)

    def test_pat_pool_token_is_inference_only(self):
        repository_mutations = self.workflow.split("safe-outputs:", 1)[1]

        self.assertEqual(1, self.workflow.count(POOL_TOKEN_EXPRESSION))
        self.assertNotIn("COPILOT_GITHUB_TOKEN", repository_mutations)
        self.assertNotIn("COPILOT_PAT_", repository_mutations)

    def test_copilot_requests_write_is_absent(self):
        self.assertNotIn("copilot-requests: write", self.workflow)

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
        self.assertIn("pull-requests: write", job)
        self.assertNotIn("COPILOT_GITHUB_TOKEN", job)
        self.assertNotIn("COPILOT_PAT_", job)
        self.assertIn("github-token: ${{ github.token }}", job)

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
