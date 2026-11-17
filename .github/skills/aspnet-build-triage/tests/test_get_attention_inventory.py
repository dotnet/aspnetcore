#!/usr/bin/env python3
"""Offline tests for get_attention_inventory.py. Run: python3 tests/test_get_attention_inventory.py"""

from __future__ import annotations

import sys
import unittest
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "scripts"))

import get_attention_inventory as inv  # noqa: E402

NOW = datetime(2026, 11, 17, tzinfo=timezone.utc)
HUMAN = {"login": "dev", "__typename": "User"}
OTHER = {"login": "teammate", "__typename": "User"}
MAESTRO = {"login": "dotnet-maestro", "__typename": "Bot"}


def check(name, conclusion="SUCCESS", status="COMPLETED"):
    return {"__typename": "CheckRun", "name": name, "status": status, "conclusion": conclusion, "detailsUrl": None}


def make_pr(number=1, title="t", head="h", body="", author=HUMAN, contexts=(), rollup="SUCCESS",
            review="APPROVED", merge_state="CLEAN", mergeable="MERGEABLE", draft=False,
            repo="dotnet/aspnetcore", comments=(), base="main"):
    return {
        "number": number, "title": title, "url": f"https://github.com/{repo}/pull/{number}",
        "isDraft": draft, "createdAt": "2026-11-10T00:00:00Z", "updatedAt": "2026-11-16T00:00:00Z",
        "body": body, "baseRefName": base, "headRefName": head, "mergeable": mergeable,
        "mergeStateStatus": merge_state, "reviewDecision": review,
        "repository": {"nameWithOwner": repo}, "author": author, "labels": {"nodes": []},
        "commits": {"nodes": [{"commit": {"committedDate": "2026-11-16T00:00:00Z", "statusCheckRollup": {
            "state": rollup, "contexts": {"totalCount": len(contexts), "nodes": list(contexts)}}}}]},
        "comments": {"nodes": list(comments)}, "reviews": {"nodes": []}, "reviewThreads": {"nodes": []},
    }


def comment(author, body, at):
    return {"author": author, "body": body, "createdAt": at, "url": f"https://example/{at}"}


class NextActionTests(unittest.TestCase):
    def action(self, pr):
        return inv.next_action(pr, inv.summarize_checks(pr))

    def test_derived_maestro_check_does_not_mark_pr_as_failing(self):
        pr = make_pr(rollup="FAILURE", merge_state="UNSTABLE",
                     contexts=[check("Build Analysis"), check("Maestro auto-merge - All Checks Successful", "FAILURE")])
        self.assertEqual(self.action(pr), "ready-to-merge")

    def test_real_failure_and_conflicts(self):
        self.assertEqual(self.action(make_pr(rollup="FAILURE", contexts=[check("aspnetcore-ci", "FAILURE")])),
                         "investigate-ci")
        self.assertEqual(self.action(make_pr(mergeable="CONFLICTING", contexts=[check("x", "FAILURE")])),
                         "resolve-conflicts")

    def test_pending_and_review_states(self):
        self.assertEqual(self.action(make_pr(contexts=[check("ci", None, "IN_PROGRESS")])), "wait-for-ci")
        self.assertEqual(self.action(make_pr(review="REVIEW_REQUIRED", merge_state="BLOCKED")), "needs-approval")

    def test_truncated_contexts_fall_back_to_rollup(self):
        pr = make_pr(rollup="FAILURE", contexts=[check("a")])
        pr["commits"]["nodes"][0]["commit"]["statusCheckRollup"]["contexts"]["totalCount"] = 150
        self.assertEqual(self.action(pr), "investigate-ci")


class CategoryTests(unittest.TestCase):
    def test_quarantine_requires_workflow_marker(self):
        self.assertTrue(inv.matches_category("test-quarantine",
                                             make_pr(body="x\n<!-- gh-aw-workflow-id: test-quarantine -->")))
        self.assertFalse(inv.matches_category("test-quarantine", make_pr(title="[test-quarantine] spoof")))

    def test_inter_branch_merge_requires_merge_head(self):
        self.assertTrue(inv.matches_category("inter-branch-merges", make_pr(
            title="[automated] Merge branch 'release/11.0' => 'main'", head="merge/release/11.0-to-main")))
        self.assertFalse(inv.matches_category("inter-branch-merges", make_pr(
            title="Bump inter-branch-merge-base.yml (Merge branch)", head="dependabot/github_actions/x")))


class ComponentGovernanceTests(unittest.TestCase):
    def test_policy_uses_only_supported_dotnet_versions(self):
        html = """
        <table><tr><td>.NET 99</td><td>Active</td></tr></table>
        <table aria-labelledby="supported-versions">
          <tr><th>Version</th><th>Support phase</th></tr>
          <tr><td><a>.NET 11</a></td><td>Active</td></tr>
          <tr><td>.NET 10</td><td>Maintenance</td></tr>
          <tr><td>.NET 9</td><td>Out of support</td></tr>
        </table>
        """
        self.assertEqual(inv.component_governance.supported_release_branches(html),
                         ["release/11.0", "release/10.0"])

    def test_inventory_preserves_branch_scope_and_reports_partial_results(self):
        from unittest.mock import patch

        cg = inv.component_governance
        base = {"alertState": "active", "isDismissed": False, "severity": "high",
                "title": "Alert", "componentName": "Example", "componentVersion": "1.0", "type": "security"}
        responses = {
            "Branches/main/AlertSummary": [
                dict(base, alertId=1), dict(base, alertId=2, alertState="fixed"),
                dict(base, alertId=3, severity="medium"), dict(base, alertId=4, isDismissed=True),
            ],
            "Branches/release%252F11.0/AlertSummary": [
                dict(base, alertId=1), dict(base, alertId=5, severity="critical", type="legal"),
                dict(base, alertId=6),
            ],
        }

        def azure_get(path):
            if path in responses:
                return {"count": len(responses[path]), "value": responses[path]}
            if path in {"Alerts/1", "Alerts/5", "Alerts/6"}:
                return {"component": {"type": {"Alerts/1": "nuGet", "Alerts/5": "npm",
                                               "Alerts/6": "Maven"}[path]}}
            raise ValueError("not available")

        with patch.object(cg, "fetch_support_policy"), \
             patch.object(cg, "supported_release_branches", return_value=["release/11.0", "release/10.0"]), \
             patch.object(cg, "azure_get", side_effect=azure_get) as api:
            result = inv.collect(["component-governance"], NOW)

        self.assertEqual([(i["base"], i["id"]) for i in result["items"]],
                         [("main", 1), ("release/11.0", 5), ("release/11.0", 1)])
        self.assertEqual(sum(call.args == ("Alerts/1",) for call in api.call_args_list), 1)
        self.assertIn("license review", result["items"][1]["nextAction"])
        self.assertIn("release/10.0", result["coverage"]["component-governance"]["errors"])
        output = inv.render_markdown(result)
        self.assertIn("Incomplete Component Governance results", output)
        self.assertNotIn("### `release/10.0`", output)
        self.assertIn(cg.MIRROR_PIPELINE, output)
        self.assertIn(inv.ADMIN_ELEVATION_URL, output)


class AdminElevationTests(unittest.TestCase):
    def test_only_aspnetcore_release_branches_require_elevation(self):
        self.assertTrue(inv.requires_admin_elevation(make_pr(base="release/10.0")))
        self.assertFalse(inv.requires_admin_elevation(make_pr(base="main")))
        self.assertFalse(inv.requires_admin_elevation(make_pr(base="release/10.0.1xx", repo="dotnet/dotnet")))

    def test_markdown_reminds_user_to_elevate(self):
        item = inv.build_item("dependency-updates", make_pr(base="release/10.0"), NOW)
        result = {"generatedAt": "now", "categories": ["dependency-updates"], "excluded": {}, "items": [item]}
        self.assertIn(inv.ADMIN_ELEVATION_URL, inv.render_markdown(result))


class PingTests(unittest.TestCase):
    def classify(self, pr):
        excluded = {}
        return inv.classify("vmr-pings", [pr], NOW, set(), excluded), excluded

    def test_bot_only_mentions_are_excluded(self):
        items, excluded = self.classify(make_pr(repo="dotnet/dotnet", author=MAESTRO, body="cc @dotnet/aspnet-build"))
        self.assertEqual(items, [])
        self.assertEqual(excluded, {"vmr-pings-no-human-mention": 1})

    def test_pr_found_by_both_vmr_searches_is_counted_once(self):
        pr = make_pr(repo="dotnet/dotnet", author=MAESTRO, body="cc @dotnet/aspnet-build")
        seen, excluded = set(), {}
        for _ in inv.SEARCHES["vmr-pings"]:
            self.assertEqual(inv.classify("vmr-pings", [pr], NOW, seen, excluded), [])
        self.assertEqual(excluded, {"vmr-pings-no-human-mention": 1})

    def test_similar_team_slug_is_not_a_ping(self):
        pr = make_pr(repo="dotnet/dotnet", comments=[comment(HUMAN, "@dotnet/aspnet-build-infra ptal", "2026-11-12T00:00:00Z")])
        self.assertIsNone(inv.find_human_pings(pr))

    def test_latest_human_ping_and_later_replies(self):
        pr = make_pr(repo="dotnet/dotnet", author=MAESTRO, comments=[
            comment(HUMAN, "@dotnet/aspnet-build can you look?", "2026-11-12T00:00:00Z"),
            comment(OTHER, "looking", "2026-11-13T00:00:00Z"),
            comment(HUMAN, "ping @Dotnet/Aspnet-Build again", "2026-11-14T00:00:00Z"),
            comment(MAESTRO, "bot noise", "2026-11-15T00:00:00Z"),
        ])
        items, _ = self.classify(pr)
        ping = items[0]["ping"]
        self.assertEqual((ping["pingedBy"], ping["pingedAt"], ping["pingCount"]), ("dev", "2026-11-14T00:00:00Z", 2))
        self.assertEqual(ping["repliedAfterPing"], [])


if __name__ == "__main__":
    unittest.main()
