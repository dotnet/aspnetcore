#!/usr/bin/env python3

import datetime
import json
import pathlib
import tempfile
import unittest
import urllib.parse
from unittest import mock

import collect_unquarantine_eligibility as MODULE
import test_collect_case_a_eligibility as FIXTURES
import test_validate_pull_request_outputs as PATCHES


NOW = datetime.datetime(2026, 10, 28, tzinfo=datetime.timezone.utc)
NAME = FIXTURES.TEST_NAME


class Provider:
    def __init__(self, results=None, builds=None):
        self.results = results if results is not None else {
            1: [(NAME, "Passed")], 2: [(NAME, "Passed")],
        }
        self.builds = builds if builds is not None else {84: [1, 2], 87: []}
        self.calls = []
        self.fail_build = None

    def __call__(self, url):
        self.calls.append(url)
        query = urllib.parse.parse_qs(urllib.parse.urlparse(url).query)
        page = int(query.get("continuationToken", ["0"])[0])
        if "/build/builds?" in url:
            pipeline = int(query["definitions"][0])
            if query["branchName"] != ["refs/heads/main"]:
                raise AssertionError("The collector must request main builds")
            values = [{
                "id": build, "definition": {"id": pipeline},
                "sourceBranch": "refs/heads/main", "status": "completed",
            } for build in self.builds[pipeline]]
        else:
            build = int(query["buildId"][0])
            if build == self.fail_build:
                raise OSError("result page unavailable")
            values = [
                {"automatedTestName": name, "outcome": outcome}
                for name, outcome in self.results.get(build, [])
            ]
        headers = {"x-ms-continuationtoken": str(page + 1)} if page + 1 < len(values) else {}
        return {"value": values[page:page + 1]}, headers


class UnquarantineTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = pathlib.Path(self.directory.name)
        FIXTURES.initialize_repository(self.root)
        self.path = self.root / FIXTURES.TEST_PATH
        self.path.write_text(FIXTURES.source(), encoding="utf-8")
        FIXTURES.commit(self.root, "Initial test", "2026-01-01T00:00:00Z")
        self.set_source(FIXTURES.source(FIXTURES.QUARANTINE_ATTRIBUTE))

    def set_source(self, source, timestamp="2026-07-01T00:00:00Z"):
        self.path.write_text(source, encoding="utf-8")
        FIXTURES.commit(self.root, "Quarantine", timestamp)
        self.history = MODULE.SOURCE.collect_requarantine_history(
            self.root, "HEAD", "dotnet/aspnetcore", "refs/heads/main", "a" * 40,
        )

    def collect(self, provider=None, closed=None, blocked=None, requarantine_prs=None):
        encoded = json.dumps(self.history).encode()
        return MODULE.collect(
            self.root, self.history, encoded, closed or [], blocked or [], requarantine_prs or [],
            NOW, provider or Provider(),
        )

    def fresh_source(self, baseline, quarantined, relative_path=FIXTURES.TEST_PATH):
        self.root = self.root / "fresh"
        self.root.mkdir()
        FIXTURES.initialize_git_repository(self.root)
        self.path = self.root / relative_path
        self.path.parent.mkdir(parents=True)
        (self.path.parent / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        self.path.write_text(baseline, encoding="utf-8")
        FIXTURES.commit(self.root, "Initial source", "2026-01-01T00:00:00Z")
        self.set_source(quarantined)

    def test_paginated_collection_finishes_before_candidates(self):
        provider = Provider({1: [(NAME, "Passed"), (NAME, "NotExecuted")],
                             2: [(NAME, "Passed")]})
        receipt = self.collect(provider)
        self.assertTrue(receipt["complete"])
        self.assertEqual(set(receipt["pipelines"]), {"84", "87"})
        self.assertEqual(receipt["pipelines"]["84"]["build_count"], 2)
        self.assertEqual(len(receipt["targets"]), 1, receipt)
        evidence = receipt["targets"][0]
        self.assertEqual(evidence["introduced_utc"], "2026-07-01T00:00:00Z")
        self.assertEqual(evidence["pipelines"]["84"]["passed"], 2)
        self.assertTrue(any("continuationToken=" in url for url in provider.calls))

    def test_both_pipelines_are_collected_even_without_candidates(self):
        self.history["targets"][0]["status"] = "re-quarantined"
        receipt = self.collect()
        self.assertEqual(set(receipt["pipelines"]), {"84", "87"})
        self.assertEqual(receipt["targets"], [])

    def test_missing_page_aborts_instead_of_emitting_empty_candidates(self):
        provider = Provider()
        provider.fail_build = 2
        with self.assertRaisesRegex(OSError, "page unavailable"):
            self.collect(provider)

    def test_bad_pages_and_repeated_continuation_fail_closed(self):
        for data, headers in (({}, {}), ({"value": []}, {"x-ms-continuationtoken": "same"})):
            with self.subTest(data=data, headers=headers):
                with self.assertRaises(ValueError):
                    list(MODULE.pages("https://example.test", {}, lambda _: (data, headers)))

    def test_skips_failures_age_and_appearance_fail_closed(self):
        for label, results, builds in (
            ("skips", {1: [(NAME, "NotExecuted")], 2: [(NAME, "NotExecuted")]}, None),
            ("failure", {1: [(NAME, "Passed")], 2: [(NAME, "Failed")]}, None),
            ("sparse", {1: [(NAME, "Passed")]}, {84: [1, 2, 3], 87: []}),
        ):
            with self.subTest(label=label):
                self.assertEqual(self.collect(Provider(results, builds))["targets"], [])
        self.history["targets"][0]["introduced_utc"] = "2026-10-01T00:00:00Z"
        self.assertEqual(self.collect()["targets"], [])

    def test_pipeline_denominators_are_separate(self):
        receipt = self.collect(Provider(builds={84: [1, 2], 87: list(range(3, 10))}))
        self.assertEqual(len(receipt["targets"]), 1, receipt)
        provider = Provider({1: [(NAME, "Passed")], 2: [(NAME, "Failed")]},
                            {84: [1], 87: [2]})
        self.assertEqual(self.collect(provider)["targets"], [])

    def test_unrelated_class_cannot_supply_run_evidence(self):
        unrelated = NAME.replace("SampleTests", "UnrelatedTests")
        self.assertEqual(self.collect(Provider({1: [(unrelated, "Passed")]}))["targets"], [])

    def test_method_uses_all_rows_but_data_target_only_its_row(self):
        self.set_source(FIXTURES.theory_source(
            "[InlineData(HttpProtocols.Http3)]", "[InlineData(HttpProtocols.Http2)]",
            method_quarantine=FIXTURES.QUARANTINE_ATTRIBUTE,
        ))
        http3 = FIXTURES.THEORY_TEST_NAME
        http2 = http3.replace("Http3", "Http2")
        provider = Provider({1: [(http3, "Passed"), (http2, "Failed")],
                             2: [(http3, "Passed")]})
        self.assertEqual(self.collect(provider)["targets"], [])
        self.root = self.root / "data-target"
        self.root.mkdir()
        FIXTURES.initialize_repository(self.root)
        self.path = self.root / FIXTURES.TEST_PATH
        self.path.write_text(FIXTURES.theory_source(
            "[InlineData(HttpProtocols.Http3)]", "[InlineData(HttpProtocols.Http2)]",
        ), encoding="utf-8")
        FIXTURES.commit(self.root, "Initial theory rows", "2026-01-01T00:00:00Z")
        self.set_source(FIXTURES.theory_source(
            '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
            'OperatingSystems.Linux, HttpProtocols.Http3)]',
            "[InlineData(HttpProtocols.Http2)]",
        ))
        self.assertEqual(len(self.collect(provider)["targets"]), 1)

    def test_human_suppression_and_issue_blocklist(self):
        for pr in (
            {"title": NAME, "trusted_closed": True, "closed_at": "2026-10-01T00:00:00Z"},
            {"title": NAME, "unquarantine_label_added_at": "2026-10-15T00:00:00Z"},
        ):
            self.assertEqual(self.collect(closed=[pr])["targets"], [])
        self.assertEqual(self.collect(blocked=[1])["targets"], [])
        receipt = self.collect(closed=[{
            "title": NAME, "quarantine_label_added_at": "2026-10-15T00:00:00Z",
        }])
        self.assertEqual(len(receipt["targets"]), 1)
        for signals in (
            {"trusted_closed": True, "closed_at": "2026-10-01T00:00:00Z"},
            {"unquarantine_label_added_at": "2026-10-15T00:00:00Z"},
        ):
            with self.subTest(signals=signals):
                receipt = self.collect(closed=[{
                    "title": NAME.replace("SampleTests", "UnrelatedTests"), **signals,
                }])
                self.assertEqual(len(receipt["targets"]), 1, receipt)

    def test_type_and_assembly_require_every_test_method(self):
        baseline = FIXTURES.class_source(
            "Microsoft.AspNetCore.Tests", "SampleTests",
            members=["[Fact]\n" + FIXTURES.method_member("One"),
                     '[Trait("Category", "Sample"), Fact]\n' + FIXTURES.method_member("Two")],
        )
        for scope in ("type", "assembly"):
            with self.subTest(scope=scope):
                quarantined = (
                    baseline.replace("public class", FIXTURES.QUARANTINE_ATTRIBUTE + "\npublic class")
                    if scope == "type"
                    else FIXTURES.QUARANTINE_ATTRIBUTE.replace("[QuarantinedTest", "[assembly: QuarantinedTest")
                    + "\n" + baseline
                )
                self.fresh_source(baseline, quarantined)
                one = NAME.rsplit(".", 1)[0] + ".One"
                two = NAME.rsplit(".", 1)[0] + ".Two"
                self.assertEqual(self.collect(Provider({1: [(one, "Passed")], 2: [(one, "Passed")]}))["targets"], [])
                receipt = self.collect(Provider({
                    1: [(one, "Passed"), (two, "Passed")],
                    2: [(one, "Passed"), (two, "Passed")],
                }))
                self.assertEqual(len(receipt["targets"]), 1, receipt)

    def test_inherited_test_overrides_use_effective_class_and_assembly_methods(self):
        for scope in ("type", "assembly"):
            for kind in ("Fact", "Theory"):
                with self.subTest(scope=scope, kind=kind):
                    attribute = "[Fact]" if kind == "Fact" else "[ConditionalTheory]\n    [InlineData(true)]"
                    parameters = "" if kind == "Fact" else "bool value"
                    baseline = f"""namespace Microsoft.AspNetCore.Tests;

public abstract class BaseTests
{{
    {attribute}
    public virtual void Check({parameters})
    {{
    }}
}}

public class SampleTests : BaseTests
{{
    public override void Check({parameters})
    {{
    }}

    [Fact]
    public void Other()
    {{
    }}
}}
"""
                    quarantined = (
                        baseline.replace("public class SampleTests", FIXTURES.QUARANTINE_ATTRIBUTE + "\npublic class SampleTests")
                        if scope == "type"
                        else FIXTURES.QUARANTINE_ATTRIBUTE.replace("[QuarantinedTest", "[assembly: QuarantinedTest")
                        + "\n" + baseline
                    )
                    self.fresh_source(baseline, quarantined)
                    check = NAME.rsplit(".", 1)[0] + ".Check"
                    if kind == "Theory":
                        check += "(value: True)"
                    other = NAME.rsplit(".", 1)[0] + ".Other"
                    receipt = self.collect(Provider({
                        1: [(check, "Passed"), (other, "Passed")],
                        2: [(check, "Passed"), (other, "Passed")],
                    }))
                    self.assertEqual(len(receipt["targets"]), 1, receipt)
                    for results in (
                        {1: [(other, "Passed")], 2: [(other, "Passed")]},
                        {1: [(check, "Failed"), (other, "Passed")],
                         2: [(check, "Passed"), (other, "Passed")]},
                    ):
                        self.assertEqual(self.collect(Provider(results))["targets"], [])

    def test_data_row_suppression_distinguishes_method_wide_and_other_rows(self):
        baseline = FIXTURES.theory_source(
            "[InlineData(HttpProtocols.Http3)]", "[InlineData(HttpProtocols.Http2)]",
        )
        quarantined = baseline.replace(
            "[InlineData(HttpProtocols.Http3)]",
            '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
            'OperatingSystems.Linux, HttpProtocols.Http3)]',
        )
        self.fresh_source(baseline, quarantined)
        row = FIXTURES.THEORY_TEST_NAME
        provider = Provider({1: [(row, "Passed")], 2: [(row, "Passed")]})
        self.assertEqual(len(self.collect(provider)["targets"]), 1)
        for signals in (
            {"trusted_closed": True, "closed_at": "2026-10-01T00:00:00Z"},
            {"unquarantine_label_added_at": "2026-10-15T00:00:00Z"},
        ):
            for title, expected in (
                (NAME, 0), (NAME + "()", 0), (row, 0), (row.replace("Http3", "Http2"), 1),
                (NAME.replace("SampleTests", "UnrelatedTests"), 1),
            ):
                with self.subTest(signals=signals, title=title):
                    receipt = self.collect(provider, closed=[{"title": title, **signals}])
                    self.assertEqual(len(receipt["targets"]), expected, receipt)

    def test_inherited_method_suppression_matches_declaring_identity(self):
        baseline = """namespace Microsoft.AspNetCore.Tests;

public class BaseTests
{
    [Fact]
    public void ReturnsExpectedResponse()
    {
    }
}

public class SampleTests : BaseTests
{
}
"""
        quarantined = baseline.replace(
            "[Fact]", FIXTURES.QUARANTINE_ATTRIBUTE + "\n    [Fact]",
        )
        self.fresh_source(baseline, quarantined)
        self.assertEqual(len(self.collect()["targets"]), 1)
        for signals in (
            {"trusted_closed": True, "closed_at": "2026-10-01T00:00:00Z"},
            {"unquarantine_label_added_at": "2026-10-15T00:00:00Z"},
        ):
            for title, expected in (
                (NAME.replace("SampleTests", "BaseTests"), 0),
                (NAME.replace("SampleTests", "UnrelatedTests"), 1),
            ):
                with self.subTest(signals=signals, title=title):
                    receipt = self.collect(closed=[{"title": title, **signals}])
                    self.assertEqual(len(receipt["targets"]), expected, receipt)

    def test_inherited_data_row_suppression_keeps_declaring_rows_distinct(self):
        baseline = FIXTURES.theory_source(
            "[InlineData(HttpProtocols.Http3)]", "[InlineData(HttpProtocols.Http2)]",
        ).replace("SampleTests", "BaseTests")
        baseline += "\npublic class SampleTests : BaseTests\n{\n}\n"
        quarantined = baseline.replace(
            "[InlineData(HttpProtocols.Http3)]",
            '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
            'OperatingSystems.Linux, HttpProtocols.Http3)]',
        )
        self.fresh_source(baseline, quarantined)
        row = FIXTURES.THEORY_TEST_NAME
        provider = Provider({1: [(row, "Passed")], 2: [(row, "Passed")]})
        self.assertEqual(len(self.collect(provider)["targets"]), 1)
        for signals in (
            {"trusted_closed": True, "closed_at": "2026-10-01T00:00:00Z"},
            {"unquarantine_label_added_at": "2026-10-15T00:00:00Z"},
        ):
            for title, expected in (
                (NAME.replace("SampleTests", "BaseTests"), 0),
                (row.replace("SampleTests", "BaseTests"), 0),
                (row.replace("SampleTests", "BaseTests").replace("Http3", "Http2"), 1),
            ):
                with self.subTest(signals=signals, title=title):
                    receipt = self.collect(provider, closed=[{"title": title, **signals}])
                    self.assertEqual(len(receipt["targets"]), expected, receipt)

    def test_iis_variants_cannot_hide_a_failure_or_merge_unrelated_classes(self):
        namespace = "Microsoft.AspNetCore.Server.IIS.FunctionalTests"
        baseline = FIXTURES.source().replace("Microsoft.AspNetCore.Tests", namespace)
        self.fresh_source(
            baseline, baseline.replace("public void", FIXTURES.QUARANTINE_ATTRIBUTE + "\n    public void"),
            "src/Servers/IIS/IIS/test/Common.LongTests/SampleTests.cs",
        )
        normal = NAME.replace("Microsoft.AspNetCore.Tests", namespace)
        variant = normal.replace(".IIS.FunctionalTests", ".IIS.NewHandler.FunctionalTests")
        receipt = self.collect(Provider({1: [(normal, "Passed"), (variant, "NotExecuted")],
                                         2: [(normal, "Passed"), (variant, "Passed")]}))
        self.assertEqual(len(receipt["targets"]), 1, receipt)
        receipt = self.collect(Provider({1: [(normal, "Passed"), (variant, "Failed")],
                                         2: [(normal, "Passed")]}))
        self.assertEqual(receipt["targets"], [])
        unrelated = normal.replace(".SampleTests.", ".UnrelatedTests.")
        receipt = self.collect(Provider({1: [(unrelated, "Passed")], 2: [(unrelated, "Passed")]}))
        self.assertEqual(receipt["targets"], [])

    def test_case_b_collector_does_not_authorize_an_unverified_issue(self):
        self.path.write_text(FIXTURES.source(), encoding="utf-8")
        FIXTURES.commit(self.root, "Unquarantine", "2026-07-02T00:00:00Z")
        for verified in (False, True):
            with self.subTest(verified=verified):
                receipt = FIXTURES.collect(
                    self.root, FIXTURES.evidence(),
                    issue_exists_provider=lambda _: verified,
                )
                record = receipt["tests"][NAME]
                self.assertEqual(record["originating_case"], "case-b")
                self.assertIs(record["case_b_issue_verified"], verified)
                self.assertIs(record["case_b_eligible"], verified)

    def test_requarantine_pr_does_not_block_an_unrelated_issue_in_the_same_file(self):
        for issue, truncated, expected_count in ((2, False, 1), (1, False, 0), (2, True, 0)):
            with self.subTest(issue=issue, truncated=truncated):
                receipt = self.collect(requarantine_prs=[{
                    "quarantine_entries": [{
                        "filename": FIXTURES.TEST_PATH,
                        "patch_truncated": truncated,
                        "added_lines": [
                            FIXTURES.QUARANTINE_ATTRIBUTE.replace("/issues/1", f"/issues/{issue}")
                        ],
                    }],
                }])
                self.assertEqual(len(receipt["targets"]), expected_count, receipt)

    def test_validator_rejects_missing_ineligible_and_unbound_evidence(self):
        target = self.collect()["targets"][0]
        key = PATCHES.MODULE.target_key(target)
        removed = {key: {**target, "reference": "1"}}
        PATCHES.MODULE.validate_removals(removed, {key: target}, {key: target})
        for evidence in ({}, {key: {**target, "eligible": False}},
                         {key: {**target, "issue": 2}}):
            with self.assertRaisesRegex(PATCHES.MODULE.ValidationError, "Part 2 eligibility"):
                PATCHES.MODULE.validate_removals(removed, {key: target}, evidence)


class TrackingIssueTests(unittest.TestCase):
    def test_open_and_closed_issues_are_verified_but_prs_are_not(self):
        for state, extra, expected in (
            ("open", {}, True), ("closed", {}, True),
            ("open", {"pull_request": {}}, False), ("unknown", {}, False),
        ):
            with self.subTest(state=state, extra=extra):
                response = FIXTURES.FakeResponse({
                    "number": 68223, "state": state, **extra,
                    "url": "https://api.github.com/repos/dotnet/aspnetcore/issues/68223",
                })
                with mock.patch.object(MODULE.SOURCE.urllib.request, "urlopen",
                                       return_value=response) as urlopen:
                    self.assertIs(MODULE.SOURCE.github_issue_exists(
                        "dotnet/aspnetcore", 68223, "test-token"), expected)
                self.assertEqual(urlopen.call_args.args[0].full_url,
                                 "https://api.github.com/repos/dotnet/aspnetcore/issues/68223")

    def test_missing_issue_is_not_verified_and_transient_errors_are_retried(self):
        error = MODULE.urllib.error.HTTPError("url", 404, "missing", {}, None)
        with mock.patch.object(MODULE.SOURCE.urllib.request, "urlopen", side_effect=error):
            self.assertFalse(MODULE.SOURCE.github_issue_exists("dotnet/aspnetcore", 1, "token"))
        error.close()
        with mock.patch.object(MODULE.SOURCE.time, "sleep"), mock.patch.object(
            MODULE.SOURCE.urllib.request, "urlopen", side_effect=OSError("unavailable"),
        ) as urlopen:
            with self.assertRaisesRegex(OSError, "unavailable"):
                MODULE.SOURCE.github_issue_exists("dotnet/aspnetcore", 1, "token")
        self.assertEqual(urlopen.call_count, 3)

    def test_validator_requires_case_b_verification(self):
        record = PATCHES.case_b_record()
        target = {"scope": "method", "path": PATCHES.TEST_PATH, "type": PATCHES.TYPE_NAME,
                  "method": "ReturnsExpectedResponse", "reference": "1",
                  "operating_systems": record["quarantine_operating_systems"]}
        for verified in (False, None):
            with self.subTest(verified=verified):
                record["case_b_issue_verified"] = verified
                with self.assertRaisesRegex(PATCHES.MODULE.ValidationError, "Case B addition"):
                    PATCHES.MODULE.validate_addition(target, {"tests": {NAME: record}}, [])


if __name__ == "__main__":
    unittest.main()
