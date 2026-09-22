import re
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).parents[1] / "pull-request-review.md"
LOCK = WORKFLOW.with_suffix(".lock.yml")


class WorkflowPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")
        cls.lock = LOCK.read_text(encoding="utf-8")
        cls.agent = cls.lock.split("\n  agent:\n", 1)[1].split("\n  conclusion:\n", 1)[0]

    def test_reviewer_turn_limit_is_doubled_without_changing_detection(self):
        self.assertIn("\nmax-turns: 400\n", self.workflow)
        self.assertIn("\n    max-turns: 20\n", self.workflow)
        self.assertIn("GH_AW_MAX_TURNS: 400\n", self.agent)

    def test_checkout_uses_event_revision_not_pr_head(self):
        self.assertIn("\ncheckout: false\n", self.workflow)
        self.assertIn("- name: Checkout reviewer criteria\n", self.agent)
        checkout = self.agent.split("- name: Checkout reviewer criteria\n", 1)[1].split(
            "\n      - ", 1
        )[0]
        self.assertRegex(checkout, r"uses: actions/checkout@[0-9a-f]{40}(?: #[^\n]*)?\n")
        for setting in (
            "ref: ${{ github.sha }}",
            "fetch-depth: 1",
            "persist-credentials: false",
        ):
            self.assertIn(setting, checkout)
        self.assertEqual(self.agent.count("uses: actions/checkout@"), 1)
        self.assertNotIn("checkout_pr_branch.cjs", self.agent)
        self.assertNotIn("restore_base_github_folders.sh", self.agent)
        self.assertLess(
            self.agent.index("- name: Checkout reviewer criteria\n"),
            self.agent.index("- name: Restore inline skills from activation artifact\n"),
        )

    def test_git_grants_cover_only_required_read_commands(self):
        commands = re.findall(r"# --allow-tool shell\((git\b[^)]*)\)", self.agent)
        self.assertCountEqual(
            commands,
            [
                "git rev-parse --show-toplevel",
                "git rev-parse HEAD",
                "git show",
            ],
        )
        self.assertNotIn("--allow-all-tools", self.agent)
        self.assertIn("\n  edit: false\n", self.workflow)
        self.assertIn("\n  cli-proxy: false\n", self.workflow)

    def test_wrapper_freezes_local_criteria_before_github_reads(self):
        self.assertIn("Before GitHub retrieval, use the skill's Step 1", self.workflow)
        local = self.workflow.index("Before GitHub retrieval, use the skill's Step 1")
        remote = self.workflow.index("Verify the GitHub head equals the trusted frozen SHA")
        self.assertLess(local, remote)
        self.assertIn("core.setOutput('reviewer_sha', context.sha);", self.workflow)
        self.assertIn("${{ needs.freeze_pr_head.outputs.reviewer_sha }}", self.workflow)
        self.assertIn("Require the resolved root and `LOCAL_SHA` to match these values", self.workflow)
        self.assertIn("git show <literal-LOCAL_SHA>:", self.workflow)
        self.assertIn("do not change directories or re-resolve `HEAD`", self.workflow)
        self.assertIn("literal `LOCAL_SHA` for criteria rereads", self.workflow)
        self.assertIn("binding target documents at `BASE_REPO`/`BASE_SHA`", self.workflow)
        self.assertNotIn("target-base guidance mode", self.workflow)
        self.assertNotIn("bundle mode", self.workflow)

    def test_failed_criteria_reads_cannot_become_successful_reviews(self):
        self.assertIn("a missing or invalid required input is `BLOCKED`, not `NO_FINDINGS`", self.workflow)
        self.assertIn("If a required read is denied or unavailable, record `BLOCKED`, call `noop`", self.workflow)
        self.assertIn("without repairing the checkout", self.workflow)
        self.assertIn("never working-tree files or remote substitutes", self.workflow)

    def test_product_review_and_publication_boundaries_are_preserved(self):
        self.assertIn("execute PR code, tests, builds, commands, or workflows", self.workflow)
        self.assertIn("Source and primary-contract evidence are not runtime proof", self.workflow)
        self.assertIn("one fresh general-purpose `task` worker per manifest row", self.workflow)
        self.assertIn("also requires `subagent-per-topic` before emitting review outputs", self.workflow)
        self.assertIn("allowed-events: [COMMENT]", self.workflow)
        self.assertEqual(
            self.workflow.count("commit-id: ${{ needs.freeze_pr_head.outputs.head_sha }}"), 2
        )


if __name__ == "__main__":
    unittest.main()
