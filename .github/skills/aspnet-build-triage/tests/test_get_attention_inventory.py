#!/usr/bin/env python3
"""Offline tests for get_attention_inventory.py. Run: python3 tests/test_get_attention_inventory.py"""

from __future__ import annotations

import sys
import unittest
import unittest.mock
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "scripts"))

import get_attention_inventory as inv  # noqa: E402
import check_investigation_plugins as plugins  # noqa: E402
import retry_ci  # noqa: E402
import schedule_report  # noqa: E402
import send_report  # noqa: E402
import servicing_schedule as schedule  # noqa: E402
import workflow_failures  # noqa: E402

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
                dict(base, alertId=1), dict(base, alertId=5, severity="critical"), dict(base, alertId=7, type="legal"),
                dict(base, alertId=6),
            ],
        }

        def azure_get(path):
            if path in responses:
                return {"count": len(responses[path]), "value": responses[path]}
            if path in {"Alerts/1", "Alerts/5", "Alerts/6"}:
                return {"component": {"type": {"Alerts/1": "nuGet", "Alerts/5": "npm",
                                               "Alerts/6": "Maven"}[path]},
                        "actionItems": "Upgrade Example from 1.0 to 1.1 to fix the vulnerability."}
            raise ValueError("not available")

        with patch.object(cg, "fetch_support_policy"), \
             patch.object(cg, "supported_release_branches", return_value=["release/11.0", "release/10.0"]), \
             patch.object(cg, "fetch_version_details", return_value=set()), \
             patch.object(cg, "azure_get", side_effect=azure_get) as api:
            result = inv.collect(["component-governance"], NOW)

        self.assertEqual([(i["base"], i["id"]) for i in result["items"]],
                         [("main", 1), ("release/11.0", 5), ("release/11.0", 1)])
        self.assertEqual(sum(call.args == ("Alerts/1",) for call in api.call_args_list), 1)
        self.assertIn("update-npm-dependencies", result["items"][1]["nextAction"])
        self.assertIn("release/10.0", result["coverage"]["component-governance"]["errors"])
        output = inv.render_markdown(result)
        self.assertIn("Incomplete Component Governance results", output)
        self.assertNotIn("### `release/10.0`", output)
        self.assertIn(cg.MIRROR_PIPELINE, output)
        self.assertIn("Upgrade Example to at least 1.1", output)
        self.assertIn(inv.ADMIN_ELEVATION_URL, output)


class RemediationTests(unittest.TestCase):
    def test_keeps_highest_upgrade_target(self):
        cg = inv.component_governance
        hints = [f"Upgrade A.B from 10.0.0 to 10.0.{n} to fix the vulnerability." for n in (8, 13, 10)]
        self.assertEqual(cg.summarize_remediations(hints), "Upgrade A.B to at least 10.0.13")


class DotnetDotnetManagedTests(unittest.TestCase):
    def test_sdk_packs_and_exact_version_details_matches_are_managed(self):
        cg = inv.component_governance
        deps = cg.version_details_dependencies(
            b'<Dependencies><ProductDependencies><Dependency Name="System.Security.Cryptography.Pkcs" '
            b'Version="11.0.1" /></ProductDependencies></Dependencies>')
        self.assertTrue(cg.managed_by_dotnet_dotnet("Microsoft.WindowsDesktop.App.Runtime.win-x64", "10.0.0", None))
        self.assertTrue(cg.managed_by_dotnet_dotnet("System.Security.Cryptography.Pkcs", "11.0.1", deps))
        self.assertFalse(cg.managed_by_dotnet_dotnet("System.Security.Cryptography.Pkcs", "8.0.1", deps))
        self.assertFalse(cg.managed_by_dotnet_dotnet("Newtonsoft.Json", "12.0.3", deps))


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



class WorkflowFailureTests(unittest.TestCase):
    LOG = "\n".join([
        "2026-11-15T10:00:00.0000000Z  * [new branch]  fix/error-handling -> origin/fix/error-handling",
        "2026-11-15T10:00:01.0000000Z GH_AW_AGENT_OUTPUT: /tmp/out.json",
        "2026-11-15T10:00:02.0000000Z FATAL: Case B addition is not bound to one exact eligible test",
        "2026-11-15T10:00:03.0000000Z ##[error]Process completed with exit code 1.",
    ])

    def run_collect(self, runs):
        def gh_api(path, raw=False, args=()):
            if path == "user":
                return {"login": "dev"}
            if path == "graphql":
                return {"data": {"search": {"nodes": [
                    {"number": 7, "title": "fix", "url": "pr/7", "author": {"login": "dev"},
                     "files": {"nodes": [{"path": ".github/workflows/scripts/test-quarantine/validate.py"}]}},
                    {"number": 8, "title": "other", "url": "pr/8", "author": {"login": "x"},
                     "files": {"nodes": [{"path": "src/Foo.cs"}]}}]}}}
            if path.startswith(f"repos/{workflow_failures.REPO}/commits?"):
                return [{"sha": "abc"}] if "test-quarantine.md" in path else []
            if path.endswith("/pulls"):
                return [{"number": 5, "title": "merged fix", "html_url": "pr/5", "user": {"login": "x"},
                         "merged_at": "2026-11-16T00:00:00Z"},
                        {"number": 4, "title": "older", "html_url": "pr/4", "user": {"login": "x"},
                         "merged_at": "2026-11-10T00:00:00Z"}]
            if path.endswith("/logs"):
                return self.LOG
            if "actions/workflows?" in path:
                return {"workflows": [{"id": 1, "path": ".github/workflows/test-quarantine.lock.yml"},
                                      {"id": 2, "path": "dynamic/dependabot/dependabot-updates"}]}
            if "/runs/" in path and path.split("?")[0].endswith("/jobs"):
                return {"jobs": [{"id": 9, "name": "safe_outputs", "conclusion": "failure", "html_url": "job",
                                  "steps": [{"name": "Validate", "conclusion": "failure"}]}]}
            return {"workflow_runs": runs if "/workflows/1/" in path else []}

        with unittest.mock.patch.object(workflow_failures, "gh_api", gh_api):
            return workflow_failures.collect(NOW)

    @staticmethod
    def make_run(run_id, conclusion, title="Daily Test Quarantine Management", branch="main", created="2026-11-15T10:00:00Z"):
        return {"id": run_id, "conclusion": conclusion, "display_title": title, "head_branch": branch,
                "created_at": created, "html_url": f"run/{run_id}", "event": "schedule"}

    def test_evidence_keeps_errors_and_drops_noise(self):
        self.assertEqual(workflow_failures.failure_evidence(self.LOG), [
            "FATAL: Case B addition is not bound to one exact eligible test",
            "##[error]Process completed with exit code 1.",
        ])

    def test_only_failed_runs_are_reported_with_later_success(self):
        items, coverage = self.run_collect([
            self.make_run(3, "success", created="2026-11-16T10:00:00Z"),
            self.make_run(2, "failure"),
            self.make_run(1, "success", created="2026-11-14T10:00:00Z"),
        ])
        self.assertEqual([i["runId"] for i in items], [2])
        self.assertEqual(items[0]["laterSuccessUrl"], "run/3")
        self.assertEqual(items[0]["failedJobs"][0]["failedSteps"], ["Validate"])
        self.assertEqual([c["runs"] for c in coverage["checked"]], [3, 0])
        self.assertEqual([pr["number"] for pr in items[0]["mergedFixCandidates"]], [5])
        self.assertEqual([(pr["number"], pr["authoredByUser"]) for pr in items[0]["openFixCandidates"]], [(7, True)])

    def test_later_success_must_match_target(self):
        items, _ = self.run_collect([
            self.make_run(1, "failure", title="nuget in /. - Update #1"),
            self.make_run(2, "success", title="npm_and_yarn in /. - Update #2", created="2026-11-16T10:00:00Z"),
            self.make_run(3, "success", title="nuget in /. - Update #3", branch="release/10.0", created="2026-11-16T11:00:00Z"),
        ])
        self.assertIsNone(items[0]["laterSuccessUrl"])


class ServicingTests(unittest.TestCase):
    def servicing_pr(self, contexts=(), reviews=(), labels=("Servicing-approved",), base="release/11.0", **kw):
        pr = make_pr(base=base, contexts=contexts, **kw)
        pr["labels"]["nodes"] = [{"name": n} for n in labels]
        pr["latestOpinionatedReviews"] = {"nodes": [
            {"author": a, "state": st, "submittedAt": "2026-11-16T00:00:00Z", "url": "u"} for a, st in reviews]}
        return pr

    def action(self, pr, required=("aspnetcore-ci",)):
        states = inv.required_check_states(pr, None if required is None else list(required))
        return inv.servicing_next_action(pr, inv.summarize_checks(pr), states)

    def test_requires_release_base_and_label(self):
        self.assertTrue(inv.matches_category("servicing", self.servicing_pr(labels=("tell-mode",))))
        self.assertFalse(inv.matches_category("servicing", self.servicing_pr(base="main")))
        self.assertFalse(inv.matches_category("servicing", self.servicing_pr(labels=("servicing",))))

    def test_merge_needs_all_checks_green_and_another_devs_approval(self):
        green = [check("aspnetcore-ci"), check("Maestro auto-merge - All Checks Successful", "FAILURE")]
        self.assertEqual(self.action(self.servicing_pr(green, [(OTHER, "APPROVED")])), "ready-to-merge")
        # The author's own approval and bot approvals don't count.
        self.assertEqual(self.action(self.servicing_pr(green, [(HUMAN, "APPROVED"), (MAESTRO, "APPROVED")])),
                         "needs-approval")
        self.assertEqual(self.action(self.servicing_pr(green, [(OTHER, "CHANGES_REQUESTED")])), "address-feedback")
        # An approval made stale by a later push leaves GitHub's review decision unapproved.
        self.assertEqual(self.action(self.servicing_pr(green, [(OTHER, "APPROVED")], review="REVIEW_REQUIRED")),
                         "needs-approval")
        # Unknown required checks mean CI can't be confirmed green.
        self.assertEqual(self.action(self.servicing_pr([], [(OTHER, "APPROVED")]), required=None), "check-merge-state")

    def test_any_failing_check_is_retried_and_missing_required_one_is_awaited(self):
        approved = [(OTHER, "APPROVED")]
        failing = [check("aspnetcore-ci", "FAILURE"), check("aspnetcore-ci (Test: macOS)", "FAILURE")]
        self.assertEqual(self.action(self.servicing_pr(failing, approved)), "retry-ci")
        # CI must be fully green: a failing non-required check still blocks merging.
        self.assertEqual(self.action(self.servicing_pr([check("aspnetcore-ci"), check("agent", "FAILURE")], approved)),
                         "retry-ci")
        self.assertEqual(self.action(self.servicing_pr([check("aspnetcore-ci"), check("other", None, "IN_PROGRESS")],
                                                       approved)), "wait-for-ci")
        self.assertEqual(self.action(self.servicing_pr([check("other")], approved)), "wait-for-ci")
        self.assertEqual(self.action(self.servicing_pr([check("aspnetcore-ci", "NEUTRAL")], approved)), "ready-to-merge")


class ServicingScheduleTests(unittest.TestCase):
    DECEMBER = """
| Activity | ET in Days | Start | End | Days | Notes |
| Code-complete (all versions, 6,8,10,11) | 1 | Nov 16 | Nov 16 | Mon | 4 PM PT cut-off. |
| 146350 | [x](y) | CVE | Code complete is September 14. |
"""
    JANUARY = "| Code-complete  | 1 | Dec 14 | Dec 14 | Mon | 4 PM PT cut-off. |"
    NOVEMBER = """
| .NET interim Code Complete (tell mode) | 1 | 12-Oct |  | Mon | 4:00 PM Pacific |
| **.NET Final Code Complete** (ask mode) | 1 | **19-Oct** | **19-Oct** | Mon | 4:00 PM Pacific |
| .NET 6.0 Code Complete | 1 | 21-Oct | 21-Oct | Wed | 4:00 PM Pacific |
"""
    APRIL = """
| Code-complete (8.0, 9.0) | 1 | 12-Mar | 12-Mar | Thu | | 11AM PT cut-off |
| Code-Complete 10.0 | 1 | 24-Mar | 24-Mar | Tue | | 10.0.6 | 20 |11AM cutoff
"""

    def cutoff(self, content, year, month, major):
        return schedule.code_complete_cutoff(content, year, month, major).isoformat()

    def test_parses_each_page_layout(self):
        self.assertEqual(self.cutoff(self.DECEMBER, 2026, 12, 10), "2026-11-16T16:00:00-08:00")
        self.assertEqual(self.cutoff(self.JANUARY, 2027, 1, 11), "2026-12-14T16:00:00-08:00")
        # Generic final code complete applies; the 6.0-only row doesn't.
        self.assertEqual(self.cutoff(self.NOVEMBER, 2026, 11, 10), "2026-10-19T16:00:00-07:00")
        self.assertEqual(self.cutoff(self.APRIL, 2026, 4, 10), "2026-03-24T11:00:00-07:00")
        self.assertEqual(self.cutoff(self.APRIL, 2026, 4, 9), "2026-03-12T11:00:00-07:00")

    def test_merge_after_code_complete_rolls_to_next_release(self):
        pages = {"November 2026": self.NOVEMBER, "December 2026": self.DECEMBER, "January 2027": self.JANUARY}
        fetch = lambda title: {"id": 1, "content": pages[title]} if title in pages else None  # noqa: E731
        result = schedule.target_release(10, NOW.replace(hour=21), fetch)
        self.assertEqual(result["release"], "January 2027")
        before = datetime(2026, 11, 16, 23, 59, tzinfo=timezone.utc)  # 3:59 PM PT
        self.assertEqual(schedule.target_release(10, before, fetch)["release"], "December 2026")
        late = datetime(2026, 12, 20, tzinfo=timezone.utc)
        self.assertIsNone(schedule.target_release(10, late, fetch)["release"])


class RetryCiTests(unittest.TestCase):
    URL = "https://dev.azure.com/dnceng-public/proj/_build/results?buildId={}"

    def test_each_failed_azdo_build_is_retried_once(self):
        checks = [
            {"name": "aspnetcore-ci", "conclusion": "FAILURE", "detailsUrl": self.URL.format(1)},
            {"name": "aspnetcore-ci (Test: macOS)", "conclusion": "FAILURE", "detailsUrl": self.URL.format(1) + "&view=logs"},
            {"name": "aspnetcore-quarantined-pr", "conclusion": "FAILURE", "detailsUrl": self.URL.format(2)},
            {"name": "aspnetcore-components-e2e", "conclusion": "SUCCESS", "detailsUrl": self.URL.format(3)},
            {"name": "Build Analysis", "conclusion": "FAILURE", "detailsUrl": "https://github.com/x"},
        ]
        builds, other = retry_ci.failed_builds(checks)
        self.assertEqual(sorted(builds), ["1", "2"])
        self.assertEqual(other, ["Build Analysis"])


class InvestigationPluginTests(unittest.TestCase):
    def test_reports_missing_marketplaces_and_disabled_plugins(self):
        listing = "Installed plugins:\n  \u2022 dotnet-dnceng@dotnet-arcade-skills (v0.1.0)\n" \
                  "  \u2022 dotnet-helix@dotnet-arcade-skills (v0.1.0) [disabled]\n"
        markets = "Registered marketplaces:\n  \u2022 dotnet-arcade-skills (GitHub: dotnet/arcade-skills)\n"
        result = plugins.check(plugins.parse_plugins(listing), plugins.parse_marketplaces(markets))
        self.assertEqual(result["missingMarketplaces"], ["dotnet-agent-skills"])
        self.assertEqual(result["disabledPlugins"], ["dotnet-helix@dotnet-arcade-skills"])
        self.assertNotIn("dotnet-dnceng@dotnet-arcade-skills", result["missingPlugins"])
        self.assertIn("copilot plugin marketplace add dotnet/skills", result["commands"])
        self.assertIn("copilot plugin enable dotnet-helix@dotnet-arcade-skills", result["commands"])
        self.assertFalse(result["ready"])


class ReportTests(unittest.TestCase):
    def test_failing_category_is_reported_instead_of_aborting(self):
        with unittest.mock.patch.object(inv, "search_prs", side_effect=RuntimeError("gh: not logged in")):
            result = inv.collect(["dependabot"], NOW, keep_going=True)
            with self.assertRaises(RuntimeError):
                inv.collect(["dependabot"], NOW)
        self.assertIn("not logged in", result["errors"]["dependabot"])
        self.assertIn("Dependabot (not collected)", inv.render_markdown(result))
        summary = send_report.summarize(result)
        self.assertIn("1 section not collected", send_report.subject(result, summary))

    def test_markdown_renders_to_escaped_html(self):
        markdown = "\n".join([
            "## Servicing <PRs> (1)", "",
            "| PR | Next action |", "|:--|:--|",
            "| [dotnet/aspnetcore#1](https://github.com/dotnet/aspnetcore/pull/1) | ready-to-merge |", "",
            "- **bold** `a_b_c` see https://dev.azure.com/dnceng/internal/_componentGovernance/x_y.",
            "  - nested _note_", "",
            "_Nothing open._",
        ])
        out = send_report.markdown_to_html(markdown)
        self.assertIn("<h2>Servicing &lt;PRs&gt; (1)</h2>", out)
        self.assertIn('<a href="https://github.com/dotnet/aspnetcore/pull/1">dotnet/aspnetcore#1</a>', out)
        self.assertNotIn(":--", out)
        self.assertIn("<strong>bold</strong>", out)
        self.assertIn(">a_b_c</code>", out)
        # Underscores in URLs aren't treated as italics, and trailing punctuation isn't part of the link.
        self.assertIn('href="https://dev.azure.com/dnceng/internal/_componentGovernance/x_y"', out)
        self.assertEqual(out.count("<ul>"), 2)
        self.assertIn("<li>nested <em>note</em></li>", out)
        self.assertIn("<p><em>Nothing open.</em></p>", out)

    def test_subject_counts_only_actionable_prs(self):
        result = {"generatedAt": NOW.isoformat(), "categories": ["dependency-updates", "component-governance"],
                  "items": [{"category": "dependency-updates", "nextAction": "ready-to-merge"},
                            {"category": "dependency-updates", "nextAction": "wait-for-ci"},
                            {"category": "component-governance"}]}
        subject = send_report.subject(result, send_report.summarize(result))
        self.assertIn("1 PR to act on, 1 CG alert", subject)
        self.assertNotIn("workflow", subject)


class ScheduleTests(unittest.TestCase):
    def test_cron_expands_to_launchd_intervals(self):
        fields = schedule_report.parse_cron("30 8 * * mon-fri")
        self.assertEqual(schedule_report.launchd_intervals(fields),
                         [{"Minute": 30, "Hour": 8, "Weekday": d} for d in range(1, 6)])
        self.assertEqual(len(schedule_report.launchd_intervals(schedule_report.parse_cron("0 */4 * * *"))), 6)
        self.assertEqual(schedule_report.parse_cron("0 9 * * 7")["weekday"], [0])
        for bad in ("0 8 * *", "0 25 * * *", "0 8 1 * 1"):
            with self.assertRaises(ValueError):
                schedule_report.parse_cron(bad)

    def test_windows_supports_daily_weekly_and_hourly(self):
        sc = lambda e: schedule_report.schtasks_schedule(schedule_report.parse_cron(e))  # noqa: E731
        self.assertEqual(sc("0 8 * * *"), ["/SC", "DAILY", "/ST", "08:00"])
        self.assertEqual(sc("15 7 * * 1,3"), ["/SC", "WEEKLY", "/D", "MON,WED", "/ST", "07:15"])
        self.assertEqual(sc("0 */6 * * *"), ["/SC", "HOURLY", "/MO", "6", "/ST", "00:00"])
        # Evenly spaced hours that don't cover the whole day can't become an hourly task.
        for expression in ("0 8 1 * *", "0 1,5 * * *", "0 0,1 * * *", "0 0-12/6 * * *"):
            with self.assertRaises(ValueError):
                sc(expression)


if __name__ == "__main__":
    unittest.main()
