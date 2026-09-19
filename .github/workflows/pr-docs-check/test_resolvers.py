import unittest

from find_existing_draft import find_existing_draft
from resolve_target_version import VersionResolutionError, resolve_target_version


POLICY = {
    "schemaVersion": 1,
    "mainVersion": {"value": "12.0", "description": "main"},
    "releaseBranchPattern": {
        "value": r"^release/(?P<version>[0-9]+\.0)$",
        "description": "release",
    },
    "milestoneVersionPattern": {
        "value": r"^(?P<version>[0-9]+\.0)(?:\b|[- ].*)",
        "description": "milestone",
    },
    "docsMonikerPrefix": {"value": "aspnetcore-", "description": "moniker"},
    "docsBaseBranch": {"value": "main", "description": "docs base"},
}


class ResolveTargetVersionTests(unittest.TestCase):
    def test_main_uses_verified_configured_version(self):
        result = resolve_target_version(
            POLICY,
            {"base": {"ref": "main"}, "milestone": {"title": "12.0-preview1"}},
            ["release/10.0", "release/11.0", "other"],
        )

        self.assertEqual("12.0", result["target_version"])
        self.assertEqual(">= aspnetcore-12.0", result["docs_moniker_range"])
        self.assertEqual("aspnetcore/migration/110-to-120", result["migration_directory"])
        self.assertTrue(result["milestone_corroborated"])

    def test_release_branch_resolves_exact_version(self):
        result = resolve_target_version(
            POLICY,
            {"base": {"ref": "release/11.0"}, "milestone": None},
            ["release/10.0", "release/11.0"],
        )

        self.assertEqual("11.0", result["target_version"])
        self.assertEqual("source_release_branch", result["resolution"])

    def test_main_policy_must_match_release_branches(self):
        with self.assertRaisesRegex(VersionResolutionError, "expected 13.0"):
            resolve_target_version(
                POLICY,
                {"base": {"ref": "main"}, "milestone": None},
                ["release/12.0"],
            )

    def test_milestone_mismatch_fails(self):
        with self.assertRaisesRegex(VersionResolutionError, "resolves to 11.0"):
            resolve_target_version(
                POLICY,
                {"base": {"ref": "main"}, "milestone": {"title": "11.0"}},
                ["release/11.0"],
            )


class FindExistingDraftTests(unittest.TestCase):
    def test_selects_most_recent_matching_draft(self):
        pulls = [
            self._pull(1, "2026-01-01T00:00:00Z", "docs/aspnetcore-pr-42-abcd"),
            self._pull(2, "2026-02-01T00:00:00Z", "docs/aspnetcore-pr-42"),
        ]

        result = find_existing_draft(
            pulls,
            "dotnet/aspnetcore",
            42,
            "dotnet/AspNetCore.Docs",
            "aspnetcore-docs-bot[bot]",
        )

        self.assertTrue(result["found"])
        self.assertEqual(2, result["selected"]["number"])
        self.assertEqual([1], [pull["number"] for pull in result["other_matching_pull_requests"]])

    def test_ignores_untrusted_or_non_draft_pull_requests(self):
        pulls = [
            self._pull(1, "2026-01-01T00:00:00Z", "docs/aspnetcore-pr-42", draft=False),
            self._pull(2, "2026-01-01T00:00:00Z", "docs/aspnetcore-pr-42", head_repo="someone/docs"),
        ]

        result = find_existing_draft(
            pulls,
            "dotnet/aspnetcore",
            42,
            "dotnet/AspNetCore.Docs",
            "aspnetcore-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertTrue(result["blocked"])

    def test_ignores_branch_for_another_source_pr(self):
        result = find_existing_draft(
            [self._pull(1, "2026-01-01T00:00:00Z", "docs/aspnetcore-pr-99")],
            "dotnet/aspnetcore",
            42,
            "dotnet/AspNetCore.Docs",
            "aspnetcore-docs-bot[bot]",
        )

        self.assertFalse(result["found"])
        self.assertFalse(result["blocked"])

    @staticmethod
    def _pull(
        number,
        updated_at,
        head_ref,
        *,
        draft=True,
        head_repo="dotnet/AspNetCore.Docs",
    ):
        return {
            "number": number,
            "html_url": f"https://github.com/dotnet/AspNetCore.Docs/pull/{number}",
            "state": "open",
            "draft": draft,
            "updated_at": updated_at,
            "body": "Source: dotnet/aspnetcore#42\n\nDetails",
            "base": {"ref": "main"},
            "head": {"ref": head_ref, "repo": {"full_name": head_repo}},
            "user": {"login": "aspnetcore-docs-bot[bot]"},
            "labels": [{"name": "documentation"}],
        }


if __name__ == "__main__":
    unittest.main()
