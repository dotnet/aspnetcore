#!/usr/bin/env python3

import ast
import datetime
import http.client
import importlib.util
import io
import json
import os
import pathlib
import subprocess
import tempfile
import textwrap
import urllib.parse
from unittest import mock


SCRIPT = pathlib.Path(__file__).with_name("collect_case_a_eligibility.py")
SPEC = importlib.util.spec_from_file_location("case_a_eligibility", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

TEST_NAME = "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse"
THEORY_TEST_NAME = (
    "Microsoft.AspNetCore.Tests.SampleTests."
    "ReturnsExpectedResponse(protocol: Http3)"
)
TEST_PATH = "src/Sample.Tests/SampleTests.cs"
DERIVED_TEST_NAME = (
    "Microsoft.AspNetCore.Server.Tests."
    "DerivedTests.ReturnsExpectedResponse"
)
QUARANTINE_ATTRIBUTE = '[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]'
RUNNER_TEST_NAME = "Sample.Runner.ReturnsExpectedResponse"
RUNNER_USINGS = "using Xunit;\nusing Microsoft.AspNetCore.InternalTesting;"


def run(root, *args, env=None):
    subprocess.run(
        args,
        cwd=root,
        env={**os.environ, **(env or {})},
        check=True,
        capture_output=True,
        text=True,
    )


def commit(root, message, timestamp):
    run(root, "git", "add", ".")
    run(
        root,
        "git",
        "commit",
        "-m",
        message,
        env={
            "GIT_AUTHOR_DATE": timestamp,
            "GIT_COMMITTER_DATE": timestamp,
        },
    )


def source(quarantine=""):
    return f"""namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{{
    {quarantine}
    public void ReturnsExpectedResponse()
    {{
    }}
}}
"""


def theory_source(*data_attributes, method_quarantine=""):
    attributes = "\n".join(
        f"    {attribute}"
        for attribute in data_attributes
    )
    if method_quarantine:
        attributes = (
            f"    {method_quarantine}\n{attributes}"
            if attributes
            else f"    {method_quarantine}"
        )
    return f"""namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{{
    [ConditionalTheory]
{attributes}
    public void ReturnsExpectedResponse(HttpProtocols protocol)
    {{
    }}
}}
"""


def class_quarantined_source():
    return """namespace Microsoft.AspNetCore.Tests;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
public class SampleTests
{
    public void ReturnsExpectedResponse()
    {
    }
}
"""


def method_member(name="ReturnsExpectedResponse", quarantine=""):
    prefix = f"{quarantine}\n" if quarantine else ""
    return f"""{prefix}public void {name}()
{{
}}
"""


def class_source(
    namespace,
    type_name,
    *,
    partial=False,
    base=None,
    type_quarantine="",
    members=None,
    usings="",
):
    declaration = f"public{' partial' if partial else ''} class {type_name}"
    if base:
        declaration = f"{declaration} : {base}"

    member_items = [members] if isinstance(members, str) else list(members or [])
    member_text = "\n\n".join(
        textwrap.dedent(item).strip("\n")
        for item in member_items
        if item
    )
    if member_text:
        member_text = f"{textwrap.indent(member_text, '    ')}\n"

    using_block = f"{textwrap.dedent(usings).strip()}\n\n" if usings else ""
    attribute_block = f"{type_quarantine}\n" if type_quarantine else ""

    return (
        f"{using_block}namespace {namespace};\n\n"
        f"{attribute_block}{declaration}\n"
        "{\n"
        f"{member_text}"
        "}\n"
    )


def evidence(regression=False, builds=(101, 102), test_name=TEST_NAME):
    type_name, method_name = test_name.rsplit(".", 1)
    metadata = {
        str(build): {
            "def": 83,
            "startedUtc": f"2026-08-{15 + index:02d}T10:00:00Z",
            "finishedUtc": f"2026-08-{15 + index:02d}T10:10:00Z",
            "sourceVersion": str(build),
            "pr": None,
        }
        for index, build in enumerate(builds)
    }
    return {
        "generated_utc": "2026-08-17T00:00:00Z",
        "builds": metadata,
        "source_a": {
            test_name: {
                "count": len(builds),
                "assembly": "Sample.Tests--net11.0",
                "builds": list(builds),
                "evidence_build": builds[-1],
                "run_id": 2001,
                "result_id": 3001,
                "leg": "Sample.Tests--net11.0",
                "queues": {str(build): ["ubuntu.2404.amd64.open"] for build in builds},
                "error": "stable-marker-123",
                "stack": f"at {type_name.rsplit('.', 1)[-1]}.{method_name}()",
                "is_consistent_regression": regression,
            },
        },
        "source_b": {},
        "source_c": [],
        "source_c_truncated": False,
    }


def source_b_evidence(builds=(101, 102), test_name=TEST_NAME):
    data = evidence(builds=builds, test_name=test_name)
    data["source_b"] = data.pop("source_a")
    for metadata in data["builds"].values():
        metadata["pr"] = 42
    return data


def collect(
    root,
    data,
    pr_files_provider=lambda _: set(),
    commit_contains_provider=lambda _ancestor, _descendant: True,
    module=None,
):
    module = module or MODULE
    serialized = json.dumps(data, separators=(",", ":")).encode()
    return module.collect(
        data,
        serialized,
        root,
        [],
        "dotnet/aspnetcore",
        "refs/heads/main",
        subprocess.check_output(["git", "-C", root, "rev-parse", "HEAD"], text=True).strip(),
        pr_files_provider=pr_files_provider,
        commit_contains_provider=commit_contains_provider,
    )


def record(receipt, test_name=TEST_NAME):
    return receipt["tests"][test_name]


def collect_result(
    root,
    data,
    *,
    test_name=TEST_NAME,
    pr_files_provider=lambda _: set(),
    commit_contains_provider=lambda _ancestor, _descendant: True,
    module=None,
):
    return record(
        collect(
            root,
            data,
            pr_files_provider=pr_files_provider,
            commit_contains_provider=commit_contains_provider,
            module=module,
        ),
        test_name,
    )


def assert_case_b(result, removal_commit):
    assert result["status"] == "ineligible", result
    assert result["originating_case"] == "case-b", result
    assert result["case_b_issue"] == 1, result
    assert result["current_quarantine_state"] == "not-quarantined", result
    assert result["latest_quarantine_transition"] == "removed", result
    assert result["cutoff"]["commit"] == removal_commit, result
    assert result["cutoff"]["reason"] == "latest-quarantine-transition", result


def assert_already_quarantined(result):
    assert result["status"] == "ineligible", result
    assert result["originating_case"] == "already-quarantined", result
    assert result["current_quarantine_state"] == "quarantined", result


def test_row_targets_require_conditional_theory():
    for attribute, row_target in [
        ("[Theory]", False),
        ("[TheoryAttribute]", False),
        ("[ConditionalTheory]", True),
        ("[ConditionalTheory(Skip = \"temporary\")]", True),
        ("[Microsoft.AspNetCore.InternalTesting.ConditionalTheoryAttribute]", True),
        ("[global::Microsoft.AspNetCore.InternalTesting.ConditionalTheoryAttribute()]", True),
        ("[ConditionalTheory, Trait(\"Category\", \"Sample\")]", True),
    ]:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            file_path.write_text(
                theory_source("[InlineData(HttpProtocols.Http3)]").replace(
                    "[ConditionalTheory]", attribute
                ),
                encoding="utf-8",
            )
            commit(root, "Add theory", "2026-08-01T00:00:00Z")
            result = collect_result(
                root, evidence(test_name=THEORY_TEST_NAME),
                test_name=THEORY_TEST_NAME,
            )
            assert result["status"] == "eligible", (attribute, result)
            assert bool(result["source_resolution"]["matching_inline_data"]) == (
                row_target
            ), (attribute, result)
            print(f"PASS row target: {attribute}")

    for attribute in ("Theory", "ConditionalTheory"):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            file_path.write_text(
                theory_source(
                    "[InlineData(HttpProtocols.Http3)]",
                    "[InlineData(HttpProtocols.Http3)]",
                ).replace("[ConditionalTheory]", f"[{attribute}]"),
                encoding="utf-8",
            )
            resolved = MODULE.resolve_source(root, THEORY_TEST_NAME)
            assert resolved["status"] == (
                "exact" if attribute == "Theory" else "ambiguous"
            ), (attribute, resolved)


def part1_functions():
    workflow = (SCRIPT.parents[2] / "test-quarantine.md").read_text(encoding="utf-8")
    step = workflow.split("    - name: Aggregate Part 1 failures\n", 1)[1]
    script = textwrap.dedent(
        step.split("        python3 << 'SCRIPT'\n", 1)[1].split(
            "\n        SCRIPT", 1
        )[0]
    )
    tree = ast.parse(script)
    tree.body = [
        node for node in tree.body
        if isinstance(node, ast.FunctionDef)
    ]
    namespace = {
        "json": json, "WI_SUFFIX": ".WorkItemExecution", "OCC_CAP": 2,
        "OS_DETAIL_BUDGET": 1000, "ERROR_CAP": 1200, "STACK_CAP": 900,
        "VSTMR": "https://example.invalid", "scrub_secrets": lambda text: text,
        "HELIX": "https://helix.dot.net/api/2019-06-17",
        "urllib": urllib, "http": http, "sys": mock.Mock(),
    }
    exec(compile(tree, "test-quarantine.md", "exec"), namespace)
    return namespace


def test_workflow_platform_evidence():
    namespace = part1_functions()
    linux = "ubuntu.2404.amd64.open"
    windows = "windows.amd64.vs2026.open"
    scenarios = [
        ("two Linux builds", {101: [linux], 102: [linux]}, ["Linux"]),
        ("three Linux builds", {101: [linux], 102: [linux], 103: [linux]}, ["Linux"]),
        ("mixed builds", {101: [linux], 102: [windows]}, ["Linux", "Windows"]),
        ("mixed same build", {101: [linux, windows], 102: [linux]}, ["Linux", "Windows"]),
        ("unknown second queue", {101: [linux, ""], 102: [linux]}, ["Linux", "MacOSX", "Windows"]),
        ("detail unavailable", {101: [linux], 102: [None]}, ["Linux", "MacOSX", "Windows"]),
        ("missing identity", {101: [linux], 102: ["missing-id"]}, ["Linux", "MacOSX", "Windows"]),
        ("missing first identity", {101: ["missing-id"], 102: [linux]}, ["Linux", "MacOSX", "Windows"]),
        ("queue unavailable", {101: [linux], 102: ["fetch-error"]}, ["Linux", "MacOSX", "Windows"]),
        ("unsupported queue", {101: [linux], 102: ["unknown.amd64.open"]}, ["Linux", "MacOSX", "Windows"]),
    ]
    for scenario, builds, expected in scenarios:
        results = {}
        details = {}
        for build, legs in builds.items():
            results[build] = []
            for index, leg in enumerate(legs):
                result_id = build * 10 + index
                results[build].append({
                    "automatedTestName": TEST_NAME,
                    "runId": build, "id": result_id if leg != "missing-id" else None,
                })
                details[result_id] = leg
            results[build].append(dict(results[build][0]))

        def fetch(url):
            if "/jobs/" in url:
                result_id = int(url.rsplit("-", 1)[1])
                if details[result_id] == "fetch-error":
                    raise OSError("fixture queue unavailable")
                return {"QueueId": details[result_id]}, {}
            result_id = int(url.split("/results/")[1].split("?")[0])
            leg = details[result_id]
            if leg is None:
                raise OSError("fixture detail unavailable")
            return {"comment": json.dumps({
                "HelixJobId": f"job-{result_id}", "HelixWorkItemName": "Sample.Tests--net11.0",
            })}, {}

        namespace["failed_results"] = lambda build: iter(results[build])
        namespace["fetch"] = mock.Mock(side_effect=fetch)
        aggregated = namespace["enrich"](namespace["aggregate"](list(builds)), {})
        actual = aggregated[TEST_NAME]
        assert actual["count"] == len(builds), (scenario, actual)
        assert namespace["fetch"].call_count == sum(
            leg != "missing-id" for leg in details.values()
        ) + sum(
            leg not in (None, "missing-id") for leg in details.values()
        ), (scenario, namespace["fetch"].call_count)
        if scenario == "missing first identity":
            assert actual["evidence_build"] == 102, actual
        for source in ("source_a", "source_b"):
            with tempfile.TemporaryDirectory() as directory:
                root = pathlib.Path(directory)
                _, file_path = initialize_repository(root)
                commit(root, "Add test", "2026-08-01T00:00:00Z")
                data = evidence(builds=tuple(builds))
                data["source_a"] = {}
                data[source] = {TEST_NAME: {
                    **actual, "is_consistent_regression": False,
                }}
                if source == "source_b":
                    for metadata in data["builds"].values():
                        metadata["pr"] = 42
                result = collect_result(root, data)
                assert result["status"] == "eligible", (scenario, source, result)
                assert result["quarantine_operating_systems"] == [
                    f"OperatingSystems.{name}" for name in expected
                ], (scenario, source, result)
        print(f"PASS platform evidence: {scenario}")

    namespace["OS_DETAIL_BUDGET"] = 0
    namespace["failed_results"] = lambda build: iter([{
        "automatedTestName": TEST_NAME, "runId": build, "id": build,
    }])
    namespace["fetch"] = mock.Mock(side_effect=lambda url: (
        {"QueueId": linux} if "/jobs/" in url else
        {"comment": json.dumps({
            "HelixJobId": "job", "HelixWorkItemName": "Sample.Tests--net11.0",
        })}, {}
    ))
    actual = namespace["enrich"](namespace["aggregate"]([101, 102]), {})[TEST_NAME]
    assert actual["queues"] == {"101": [linux], "102": [None]}, actual
    assert actual["detail_note"] == "platform evidence detail budget exhausted"
    assert namespace["fetch"].call_count == 2
    assert MODULE.quarantine_operating_systems(actual, None, None, [101, 102]) == list(
        MODULE.OPERATING_SYSTEMS
    )
    assert MODULE.quarantine_operating_systems(actual, None, None, [101]) == [
        "OperatingSystems.Linux",
    ]

    namespace["failed_results"] = lambda build: iter([{
        "automatedTestName": "Sample.WorkItemExecution", "runId": build, "id": build,
    }])
    namespace["fetch"] = mock.Mock(return_value=({
        "comment": json.dumps({"HelixJobId": "job", "HelixWorkItemName": "Sample.Tests--net11.0"}),
    }, {}))
    work_item = namespace["enrich"](namespace["aggregate"]([101, 102, 103]), {})[
        "Sample.WorkItemExecution"
    ]
    assert work_item["count"] == 3, work_item
    assert len(work_item["probes"]) == 2, work_item
    assert namespace["fetch"].call_count == 2


def test_source_c_platform_evidence():
    for second_queue, expected in [
        ("windows.amd64.vs2026.open", ["OperatingSystems.Linux", "OperatingSystems.Windows"]),
        ("unknown", list(MODULE.OPERATING_SYSTEMS)),
    ]:
        record = MODULE.source_c_failure_records([
            {"build": 101, "workitem": "batch_1--net11.0", "queue": queue,
             "fail_blocks": f"{TEST_NAME} [FAIL]"}
            for queue in ("ubuntu.2404.amd64.open", second_queue)
        ])[TEST_NAME]
        assert MODULE.quarantine_operating_systems(None, None, record, [101]) == expected
    assert MODULE.quarantine_operating_systems(
        {"builds": [101], "queues": {"101": [None]}},
        {"builds": [101], "queues": {"101": ["ubuntu.2404.amd64.open"]}},
        None, [101],
    ) == list(MODULE.OPERATING_SYSTEMS)
    for record in [
        {"builds": [101], "legs": {"101": ["Windows_Test"]},
         "evidence_build": 101, "leg": "Linux_Test"},
        MODULE.source_c_failure_records([{
            "build": 101, "job": "Linux", "workitem": "Windows",
            "fail_blocks": f"{TEST_NAME} [FAIL]",
        }])[TEST_NAME],
    ]:
        assert MODULE.quarantine_operating_systems(record, None, None, [101]) == list(
            MODULE.OPERATING_SYSTEMS
        ), record


def test_helix_queue_cache():
    for response, expected, warning in [
        ({"QueueId": "ubuntu.2404.amd64.open"}, "ubuntu.2404.amd64.open", False),
        ({"QueueId": "unknown.amd64.open"}, "unknown.amd64.open", False),
        ({}, None, True),
        ({"QueueId": ""}, None, True),
        ({"QueueId": ["ubuntu.2404.amd64.open"]}, None, True),
        ([], None, True),
        (OSError("fixture queue unavailable"), None, True),
        (ValueError("fixture invalid JSON"), None, True),
        (http.client.IncompleteRead(b"partial"), None, True),
        (http.client.BadStatusLine("fixture invalid status"), None, True),
    ]:
        namespace = part1_functions()
        namespace["fetch"] = mock.Mock(
            side_effect=response if isinstance(response, Exception) else None,
            return_value=(response, {}),
        )
        cache = {}
        for _ in range(2):
            assert namespace["helix_queue"]("job-id", cache) == expected
        namespace["fetch"].assert_called_once_with(
            "https://helix.dot.net/api/2019-06-17/jobs/job-id"
        )
        assert namespace["sys"].stderr.write.call_count == int(warning)
        assert namespace["helix_queue"](None, cache) is None
        assert namespace["fetch"].call_count == 1


def test_production_queue_evidence(
    queue="ubuntu.2404.amd64.open",
    expected="Linux",
    sources=("source_a", "source_b", "source_c"),
):
    for source in sources:
        namespace = part1_functions()
        builds = [
            {"id": build, "startTime": f"2026-08-{day}T10:00:00Z",
             "sourceVersion": str(build), "definition": {"id": 83},
             "sourceBranch": "refs/pull/42/merge" if source == "source_b" else "refs/heads/main"}
            for build, day in ((101, 15), (102, 16))
        ]
        workitem = "batch_1--net11.0"
        # Shape verified against Helix job 1c70e76a-1985-4b12-943c-1cb84e4c9499.
        job_id = "1c70e76a-1985-4b12-943c-1cb84e4c9499"
        calls = []

        def fetch(url):
            calls.append(url)
            if url == f"{namespace['HELIX']}/jobs/{job_id}":
                return {"Name": job_id, "QueueId": queue}, {}
            assert "/testresults/runs/" in url, url
            result_job = (
                "earlier-job-with-no-fail-blocks"
                if source == "source_c" and "/runs/102/" in url
                else job_id
            )
            return {"comment": json.dumps({
                "HelixJobId": result_job, "HelixWorkItemName": workitem,
            })}, {}

        namespace.update({
            "DEFS": [83],
            "os": mock.Mock(environ={
                "SOURCE_B_BUILD_IDS": "[101,102]" if source == "source_b" else "",
            }),
            "datetime": datetime,
            "SOURCE_C_DOWNLOAD_BUDGET": 10000,
            "SOURCE_C_GLOBAL_CAP": 10000, "WORKITEM_CAP": 10000,
            "list_failed_builds": lambda *_args, **_kwargs: (
                [] if source == "source_b" else builds
            ),
            "builds_by_ids": lambda _ids: builds,
            "list_completed_builds": lambda *_args, **_kwargs: [],
            "mark_intermittency": lambda agg, *_args: [
                entry.update(is_consistent_regression=False) for entry in agg.values()
            ],
            "failed_results": lambda build: iter([{
                "automatedTestName": (
                    workitem + ".WorkItemExecution" if source == "source_c" else TEST_NAME
                ),
                "runId": build, "id": build,
            }]),
            "fetch": fetch,
            "helix_console_blocks": lambda job, _wi: (
                [f"{TEST_NAME} [FAIL]"] if job == job_id else [], 100
            ),
            "emit": lambda out: json.dumps(out),
        })
        data = json.loads(namespace["main"]())
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            initialize_repository(root)
            commit(root, "Add test", "2026-08-01T00:00:00Z")
            result = collect_result(root, data)
            assert result["eligible_failure_builds"], result
            assert result["status"] == (
                "ineligible" if source == "source_c" else "eligible"
            ), result
            if source == "source_c":
                assert result["eligible_failure_builds"] == [101], result
            assert result["quarantine_operating_systems"] == [
                f"OperatingSystems.{expected}",
            ], (queue, source, result)
        assert calls.count(f"{namespace['HELIX']}/jobs/{job_id}") == 1, calls
        print(f"PASS production queue: {source}, {queue}")


def test_theory_data_quarantine_support():
    assert MODULE.split_test_name(THEORY_TEST_NAME) == (
        TEST_NAME,
        ("Http3",),
    )
    assert MODULE.data_values(
        'HttpProtocols.Http3, 42, "value,with,commas"'
    ) == (
        "Http3",
        "42",
        '"value,with,commas"',
    )
    multiline_attributes = MODULE.quarantine_attributes(
        """[QuarantinedTest(
    "https://github.com/dotnet/aspnetcore/issues/1",
    OperatingSystems.Linux)]
[QuarantinedTestData(
    "https://github.com/dotnet/aspnetcore/issues/1",
    OperatingSystems.Linux,
    HttpProtocols.Http3)]
[InlineData(
    HttpProtocols.Http2)]"""
    )
    assert len(multiline_attributes["method"]) == 1, multiline_attributes
    assert multiline_attributes["data"][0]["values"] == (
        "Http3",
    ), multiline_attributes
    assert multiline_attributes["inline"][0]["values"] == (
        "Http2",
    ), multiline_attributes
    combined_attributes = MODULE.quarantine_attributes(
        """[Fact, QuarantinedTest(
    "https://github.com/dotnet/aspnetcore/issues/1",
    OperatingSystems.Linux)]
[ConditionalTheory, QuarantinedTestData(
    "https://github.com/dotnet/aspnetcore/issues/1",
    OperatingSystems.Linux,
    HttpProtocols.Http3)]
[Theory, InlineData(HttpProtocols.Http2)]"""
    )
    assert len(combined_attributes["method"]) == 1, combined_attributes
    assert combined_attributes["data"][0]["values"] == (
        "Http3",
    ), combined_attributes
    assert combined_attributes["inline"][0]["values"] == (
        "Http2",
    ), combined_attributes

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.write_text(
            theory_source(
                "[InlineData(HttpProtocols.Http2)]",
                "[InlineData(HttpProtocols.Http3)]",
            ),
            encoding="utf-8",
        )
        commit(root, "Add theory rows", "2026-08-01T00:00:00Z")

        resolved = MODULE.resolve_source(root, THEORY_TEST_NAME)
        assert resolved["status"] == "exact", resolved
        assert resolved["matching_inline_data"]["data"] == (
            "HttpProtocols.Http3"
        ), resolved
        assert resolved["data_quarantine"] is None, resolved

        eligible = collect_result(
            root,
            evidence(test_name=THEORY_TEST_NAME),
            test_name=THEORY_TEST_NAME,
        )
        assert eligible["status"] == "eligible", eligible
        assert eligible["originating_case"] == "case-a", eligible
        assert eligible["source_resolution"]["matching_inline_data"][
            "data"
        ] == "HttpProtocols.Http3", eligible

        file_path.write_text(
            theory_source(
                "[InlineData(HttpProtocols.Http2)]",
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/1", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]',
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine one theory row", "2026-08-02T00:00:00Z")
        row_quarantined = collect_result(
            root,
            evidence(test_name=THEORY_TEST_NAME),
            test_name=THEORY_TEST_NAME,
        )
        assert_already_quarantined(row_quarantined)
        assert row_quarantined["source_resolution"]["data_quarantine"][
            "data"
        ] == "HttpProtocols.Http3", row_quarantined

        file_path.write_text(
            theory_source(
                "[InlineData(HttpProtocols.Http2)]",
                "[InlineData(HttpProtocols.Http3)]",
            ),
            encoding="utf-8",
        )
        commit(root, "Unquarantine one theory row", "2026-08-05T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")
        row_case_b = collect_result(
            root,
            evidence(test_name=THEORY_TEST_NAME),
            test_name=THEORY_TEST_NAME,
        )
        assert_case_b(row_case_b, removal_commit)

        file_path.write_text(
            theory_source(
                "[InlineData(HttpProtocols.Http2)]",
                "[InlineData(HttpProtocols.Http3)]",
                method_quarantine=(
                    '[QuarantinedTest('
                    '"https://github.com/dotnet/aspnetcore/issues/1", '
                    "OperatingSystems.Linux)]"
                ),
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine theory method on Linux", "2026-08-06T00:00:00Z")
        method_quarantined = collect_result(
            root,
            evidence(test_name=THEORY_TEST_NAME),
            test_name=THEORY_TEST_NAME,
        )
        assert_already_quarantined(method_quarantined)
        assert method_quarantined["source_resolution"][
            "method_quarantined"
        ], method_quarantined

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(
            theory_source(
                "[InlineData(HttpProtocols.Http3)]",
                "[InlineData(HttpProtocols.Http3)]",
            ),
            encoding="utf-8",
        )
        commit(root, "Add ambiguous theory rows", "2026-08-01T00:00:00Z")
        ambiguous = MODULE.resolve_source(root, THEORY_TEST_NAME)
        assert ambiguous["status"] == "ambiguous", ambiguous

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(
            theory_source("[InlineData(HttpProtocols.Http3)]"),
            encoding="utf-8",
        )
        commit(root, "Add theory row", "2026-08-01T00:00:00Z")
        file_path.write_text(
            theory_source(
                '[QuarantinedTestData(\n'
                '        "https://github.com/dotnet/aspnetcore/issues/1",\n'
                "        OperatingSystems.Linux,\n"
                "        Microsoft.AspNetCore.Server.Kestrel.Core."
                "HttpProtocols.Http3)]"
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine theory row", "2026-08-02T00:00:00Z")
        file_path.write_text(
            theory_source("[InlineData(HttpProtocols.Http3)]"),
            encoding="utf-8",
        )
        commit(root, "Unquarantine theory row", "2026-08-03T00:00:00Z")
        file_path.write_text(
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/1", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]'
            ),
            encoding="utf-8",
        )
        commit(root, "Re-quarantine theory row", "2026-08-04T00:00:00Z")
        history = MODULE.collect_requarantine_history(root, "HEAD")
        assert history["targets"] == [{
            "scope": "data",
            "path": TEST_PATH,
            "type": "Microsoft.AspNetCore.Tests.SampleTests",
            "method": "ReturnsExpectedResponse",
            "data": "HttpProtocols.Http3",
            "issue": 1,
            "status": "re-quarantined",
        }], history


def test_inline_literal_resolution(raw="true", rendered="True"):
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(theory_source(f"[InlineData({raw})]"), encoding="utf-8")
        commit(root, "Add inline constant", "2026-08-01T00:00:00Z")
        name = TEST_NAME + f"(protocol: {rendered})"
        result = collect_result(root, evidence(test_name=name), test_name=name)
        assert result["status"] == "eligible", result
        row = result["source_resolution"]["matching_inline_data"]
        assert row is not None and row["data"] == raw, result
        print(f"PASS inline literal: {raw} -> {rendered}")


def test_unmatched_inline_row_fails_closed(cases=None):
    for raw, name in cases or [
        ("nameof(HttpProtocols.Http3)", TEST_NAME + '(protocol: "Http3")'),
        ("1.0f", TEST_NAME + "(protocol: 1)"),
        ("HttpProtocols.Http3", TEST_NAME),
        ("HttpProtocols.Http3", TEST_NAME + "(protocol: Unknown)"),
        ("HttpProtocols.Http3", TEST_NAME + "(protocol: Http3"),
    ]:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            file_path.write_text(theory_source(f"[InlineData({raw})]"), encoding="utf-8")
            commit(root, "Add unmatched row", "2026-08-01T00:00:00Z")
            result = collect_result(root, evidence(test_name=name), test_name=name)
            assert result["status"] == "unproven", (raw, name, result)
            assert "source-unmatched-data-row" in result["reasons"], result


def test_mixed_row_providers(provider='[MemberData(nameof(GetRows))]'):
    for row in (
        "[InlineData(HttpProtocols.Http3)]",
        '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
        'OperatingSystems.Linux, HttpProtocols.Http3)]',
    ):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            file_path.write_text(theory_source(row, provider), encoding="utf-8")
            commit(root, "Add mixed row providers", "2026-08-01T00:00:00Z")
            result = collect_result(
                root, evidence(test_name=THEORY_TEST_NAME), test_name=THEORY_TEST_NAME
            )
            assert result["status"] == "unproven", result
            assert "source-ambiguous" in result["reasons"], result
            print(f"PASS mixed row providers: {row}, {provider}")


def test_non_data_condition_rows(condition="[MsQuicSupported]"):
    for quarantined in (False, True):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            row = (
                '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]'
                if quarantined else "[InlineData(HttpProtocols.Http3)]"
            )
            file_path.write_text(theory_source(condition, row), encoding="utf-8")
            commit(root, "Add conditioned row", "2026-08-01T00:00:00Z")
            result = collect_result(
                root, evidence(test_name=THEORY_TEST_NAME), test_name=THEORY_TEST_NAME
            )
            if quarantined:
                assert_already_quarantined(result)
            else:
                assert result["status"] == "eligible", result
                assert result["source_resolution"]["matching_inline_data"] is not None, result
            file_path.write_text(
                theory_source(condition, row, "[MemberData(nameof(GetRows))]"),
                encoding="utf-8",
            )
            assert MODULE.resolve_source(root, THEORY_TEST_NAME)["status"] == "ambiguous"
            print(f"PASS non-data condition: {condition}, quarantined={quarantined}")


def test_kestrel_condition_rows():
    kestrel_source = SCRIPT.parents[4] / (
        "src/Servers/Kestrel/test/Interop.FunctionalTests/Http3/Http3RequestTests.cs"
    )
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(kestrel_source.read_text(encoding="utf-8"), encoding="utf-8")
        for protocol, row_key in (("Http3", "data_quarantine"), ("Http2", "matching_inline_data")):
            result = MODULE.resolve_source(
                root,
                "Interop.FunctionalTests.Http3.Http3RequestTests."
                f"POST_ClientCancellationBidirectional_RequestAbortRaised(protocol: {protocol})",
            )
            assert result["status"] == "exact", result
            assert result[row_key]["data"] == f"HttpProtocols.{protocol}", result
            print(f"PASS Kestrel source row: {protocol}")


def test_commented_row_attributes():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(
            theory_source("[InlineData(HttpProtocols.Http3)] // reason [detail]")
            .replace("[ConditionalTheory]", "[ConditionalTheory] // condition"),
            encoding="utf-8",
        )
        commit(root, "Add commented row", "2026-08-01T00:00:00Z")
        result = collect_result(
            root, evidence(test_name=THEORY_TEST_NAME), test_name=THEORY_TEST_NAME
        )
        assert result["status"] == "eligible", result
        assert result["source_resolution"]["matching_inline_data"] is not None, result
        quarantine = (
            '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
            'OperatingSystems.Linux, HttpProtocols.Http3)] // reason'
        )
        file_path.write_text(theory_source(quarantine), encoding="utf-8")
        commit(root, "Quarantine commented row", "2026-08-02T00:00:00Z")
        result = collect_result(
            root, evidence(test_name=THEORY_TEST_NAME), test_name=THEORY_TEST_NAME
        )
        assert_already_quarantined(result)


def test_renamed_row_history(cases=None):
    for old, new in cases or [
        ("ReturnsExpectedResponse", "RenamedResponse"),
        ("SampleTests", "RenamedTests"),
        ("Microsoft.AspNetCore.Tests", "Microsoft.AspNetCore.Renamed"),
    ]:
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, file_path = initialize_repository(root)
            inline = theory_source("[InlineData(HttpProtocols.Http3)]")
            quarantined = theory_source(
                '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]'
            )
            for day, text in enumerate((inline, quarantined, inline), 1):
                file_path.write_text(text, encoding="utf-8")
                commit(root, "Change row quarantine", f"2026-08-0{day}T00:00:00Z")
            file_path.write_text(inline.replace(old, new), encoding="utf-8")
            commit(root, "Rename unquarantined row owner", "2026-08-04T00:00:00Z")
            file_path.write_text(quarantined.replace(old, new), encoding="utf-8")
            commit(root, "Re-quarantine renamed row", "2026-08-05T00:00:00Z")
            history = MODULE.collect_requarantine_history(root, "HEAD")
            assert len(history["targets"]) == 1, history
            assert history["targets"][0]["status"] == "ambiguous", (old, history)


def test_build_source_ancestry():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        ancestor = run_output(root, "git", "rev-parse", "HEAD")
        (project / "Other.cs").write_text(
            "namespace Microsoft.AspNetCore.Tests;\npublic class Other {}\n",
            encoding="utf-8",
        )
        commit(root, "Add unrelated source", "2026-08-02T00:00:00Z")
        descendant = run_output(root, "git", "rev-parse", "HEAD")

        assert MODULE.commit_contains(
            root,
            "dotnet/aspnetcore",
            ancestor,
            descendant,
            "",
        )
        assert not MODULE.commit_contains(
            root,
            "dotnet/aspnetcore",
            descendant,
            ancestor,
            "",
        )

        file_path.write_text(
            source(QUARANTINE_ATTRIBUTE),
            encoding="utf-8",
        )
        commit(root, "Quarantine test", "2026-08-03T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-04T00:00:00Z")
        unquarantine_commit = run_output(root, "git", "rev-parse", "HEAD")
        file_path.write_text(
            source() + "\n// Fix the test after unquarantining it.\n",
            encoding="utf-8",
        )
        commit(root, "Fix test after unquarantine", "2026-08-10T00:00:00Z")
        fix_commit = run_output(root, "git", "rev-parse", "HEAD")

        ancestry_calls = []
        stale = collect_result(
            root,
            source_b_evidence(builds=(101,)),
            commit_contains_provider=lambda cutoff, source_version: (
                ancestry_calls.append((cutoff, source_version))
                or cutoff == unquarantine_commit
            ),
        )
        assert stale["originating_case"] == "case-b", stale
        assert stale["case_b_eligible"] is False, stale
        assert stale["eligible_failure_builds"] == [], stale
        assert stale["required_ancestor"] == fix_commit, stale
        assert ancestry_calls == [(fix_commit, "101")], ancestry_calls
        assert {
            item["reason"] for item in stale["excluded_builds"]
        } == {"source-version-before-cutoff"}, stale

        current = collect_result(
            root,
            source_b_evidence(builds=(101,)),
            commit_contains_provider=lambda _cutoff, _source: True,
        )
        assert current["originating_case"] == "case-b", current
        assert current["case_b_eligible"] is True, current
        assert current["eligible_failure_builds"] == [101], current
        assert current["ancestry_verified_builds"] == [101], current

        unavailable = collect_result(
            root,
            source_b_evidence(builds=(101,)),
            commit_contains_provider=lambda _cutoff, _source: (_ for _ in ()).throw(
                ValueError("missing commit"),
            ),
        )
        assert unavailable["case_b_eligible"] is False, unavailable
        assert {
            item["reason"] for item in unavailable["excluded_builds"]
        } == {"source-version-ancestry-unavailable"}, unavailable

        missing_version = source_b_evidence(builds=(101,))
        missing_version["builds"]["101"]["sourceVersion"] = None
        missing = collect_result(root, missing_version)
        assert missing["case_b_eligible"] is False, missing
        assert {
            item["reason"] for item in missing["excluded_builds"]
        } == {"missing-source-version"}, missing

        source_c = source_b_evidence(builds=(101,))
        source_c["source_a"] = {}
        source_c["source_b"] = {}
        source_c["source_c"] = [{
            "build": 101,
            "workitem": "Sample.Tests.WorkItemExecution",
            "fail_block_count": 1,
            "fail_blocks": f"{TEST_NAME} [FAIL]\nFailure details",
        }]
        source_c_result = collect_result(root, source_c)
        assert source_c_result["originating_case"] == "case-b", source_c_result
        assert source_c_result["case_b_eligible"] is True, source_c_result
        assert source_c_result["eligible_failure_builds"] == [101], source_c_result
        changed_source_c_result = collect_result(
            root,
            source_c,
            pr_files_provider=lambda _: {TEST_PATH},
        )
        assert changed_source_c_result["case_b_eligible"] is False, (
            changed_source_c_result
        )
        assert changed_source_c_result["excluded_builds"][-1] == {
            "build": 101,
            "reason": "source-b-pr-changed-test-file",
        }, changed_source_c_result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, _ = initialize_repository(root)
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        case_a = evidence()
        case_a_result = collect_result(
            root,
            case_a,
            commit_contains_provider=lambda _cutoff, source_version: (
                source_version == "102"
            ),
        )
        assert case_a_result["originating_case"] == "case-a", case_a_result
        assert case_a_result["status"] == "ineligible", case_a_result
        assert case_a_result["eligible_failure_builds"] == [102], case_a_result
        assert "fewer-than-two-post-cutoff-failures" in case_a_result["reasons"]

        source_c_case_a = source_b_evidence()
        source_c_case_a["source_a"] = {}
        source_c_case_a["source_b"] = {}
        source_c_case_a["source_c"] = [
            {
                "build": build,
                "workitem": "Sample.Tests.WorkItemExecution",
                "fail_block_count": 1,
                "fail_blocks": f"{TEST_NAME} [FAIL]\nFailure details",
            }
            for build in (101, 102)
        ]
        source_c_case_a_result = collect_result(root, source_c_case_a)
        assert source_c_case_a_result["originating_case"] == "case-a", (
            source_c_case_a_result
        )
        assert source_c_case_a_result["status"] == "eligible", (
            source_c_case_a_result
        )
        assert source_c_case_a_result["evidence"] is None, source_c_case_a_result
        assert "eligible-evidence-identity-unavailable" in (
            source_c_case_a_result["reasons"]
        )


def test_history_cutoff_uses_first_parent_order():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-03T00:00:00Z")
        unquarantine_commit = run_output(root, "git", "rev-parse", "HEAD")
        file_path.write_text(
            source().replace(
                "    public void ReturnsExpectedResponse()\n    {\n    }",
                "    public void ReturnsExpectedResponse()\n"
                "    {\n"
                "        // Later source edit\n"
                "    }",
            ),
            encoding="utf-8",
        )
        commit(root, "Edit test after unquarantine", "2026-08-02T00:00:00Z")
        source_edit_commit = run_output(root, "git", "rev-parse", "HEAD")

        item = evidence()
        item["builds"]["101"]["sourceVersion"] = unquarantine_commit
        item["builds"]["102"]["sourceVersion"] = source_edit_commit
        result = collect_result(
            root,
            item,
            commit_contains_provider=lambda ancestor, descendant: (
                MODULE.commit_contains(
                    root,
                    "dotnet/aspnetcore",
                    ancestor,
                    descendant,
                    "",
                )
            ),
        )

        assert result["required_ancestor"] == source_edit_commit, result
        assert result["eligible_failure_builds"] == [102], result
        assert result["excluded_builds"] == [{
            "build": 101,
            "reason": "source-version-before-cutoff",
        }], result


def test_requarantine_history():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine test", "2026-08-02T00:00:00Z")

        first = MODULE.collect_requarantine_history(root, "HEAD")
        assert first["targets"] == [{
            "scope": "method",
            "path": TEST_PATH,
            "type": "Microsoft.AspNetCore.Tests.SampleTests",
            "method": "ReturnsExpectedResponse",
            "issue": 1,
            "status": "first-quarantine",
        }], first

        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-03T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Re-quarantine test", "2026-08-04T00:00:00Z")

        requarantined = MODULE.collect_requarantine_history(root, "HEAD")
        assert requarantined["targets"][0]["status"] == "re-quarantined", (
            requarantined
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-07-31T00:00:00Z")
        file_path.write_text(
            source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine with temporary issue", "2026-08-01T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Resolve quarantine issue", "2026-08-02T00:00:00Z")

        resolved = MODULE.collect_requarantine_history(root, "HEAD")
        assert resolved["targets"][0]["status"] == "first-quarantine", resolved

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Add class and assembly quarantines", "2026-08-01T00:00:00Z")

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert {
            (target["scope"], target["issue"], target["status"])
            for target in targets
        } == {
            ("type", 1, "first-quarantine"),
            ("assembly", 2, "first-quarantine"),
        }, targets

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add partial test type", "2026-07-31T00:00:00Z")
        file_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
public partial class SampleTests
{
    public void ReturnsExpectedResponse()
    {
    }
}
""",
            encoding="utf-8",
        )
        partial_path = project / "SampleTests.Partial.cs"
        partial_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

public partial class SampleTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Quarantine partial type", "2026-08-01T00:00:00Z")
        file_path.write_text(
            file_path.read_text(encoding="utf-8").replace(
                QUARANTINE_ATTRIBUTE + "\n",
                "",
            ),
            encoding="utf-8",
        )
        partial_path.write_text(
            partial_path.read_text(encoding="utf-8").replace(
                "public partial class",
                f"{QUARANTINE_ATTRIBUTE}\npublic partial class",
            ),
            encoding="utf-8",
        )
        commit(root, "Move quarantine across partial type", "2026-08-02T00:00:00Z")

        moved = MODULE.collect_requarantine_history(root, "HEAD")
        assert moved["targets"][0]["status"] == "first-quarantine", moved

        partial_path.write_text(
            partial_path.read_text(encoding="utf-8").replace(
                QUARANTINE_ATTRIBUTE + "\n",
                "",
            ),
            encoding="utf-8",
        )
        commit(root, "Unquarantine partial type", "2026-08-03T00:00:00Z")
        partial_path.write_text(
            partial_path.read_text(encoding="utf-8").replace(
                "public partial class",
                f"{QUARANTINE_ATTRIBUTE}\npublic partial class",
            ),
            encoding="utf-8",
        )
        commit(root, "Re-quarantine partial type", "2026-08-04T00:00:00Z")

        requarantined_type = MODULE.collect_requarantine_history(root, "HEAD")
        assert requarantined_type["targets"][0]["status"] == "re-quarantined", (
            requarantined_type
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, _ = initialize_repository(root)
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly", "2026-08-01T00:00:00Z")
        assembly_info.write_text("", encoding="utf-8")
        commit(root, "Unquarantine assembly", "2026-08-02T00:00:00Z")
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Re-quarantine assembly", "2026-08-03T00:00:00Z")
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Update assembly issue", "2026-08-04T00:00:00Z")

        requarantined_assembly = MODULE.collect_requarantine_history(
            root,
            "HEAD",
        )
        assert requarantined_assembly["targets"][0]["issue"] == 2, (
            requarantined_assembly
        )
        assert (
            requarantined_assembly["targets"][0]["status"]
            == "re-quarantined"
        ), requarantined_assembly

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        moved_path = project / "SampleTests.Moved.cs"
        moved_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

public partial class SampleTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine method", "2026-08-02T00:00:00Z")
        file_path.unlink()
        moved_path.write_text(
            source(QUARANTINE_ATTRIBUTE).replace(
                "public class SampleTests",
                "public partial class SampleTests",
            ),
            encoding="utf-8",
        )
        commit(root, "Move and re-quarantine method", "2026-08-03T00:00:00Z")

        moved_method = MODULE.collect_requarantine_history(root, "HEAD")
        assert moved_method["targets"][0]["path"].endswith(
            "SampleTests.Moved.cs"
        ), moved_method
        assert moved_method["targets"][0]["status"] == "re-quarantined", (
            moved_method
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine before project move", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine before project move", "2026-08-02T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Re-quarantine before project move", "2026-08-03T00:00:00Z")
        run(root, "git", "mv", "src/Sample.Tests", "src/Renamed.Tests")
        commit(root, "Move test project", "2026-08-04T00:00:00Z")

        moved_project = MODULE.collect_requarantine_history(root, "HEAD")
        assert moved_project["targets"][0]["status"] == "ambiguous", moved_project

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine before project move", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine before project move", "2026-08-02T00:00:00Z")
        run(root, "git", "mv", "src/Sample.Tests", "src/Renamed.Tests")
        commit(root, "Move unquarantined test project", "2026-08-03T00:00:00Z")

        moved_case_b = collect_result(root, evidence())
        assert moved_case_b["status"] == "unproven", moved_case_b
        assert moved_case_b["originating_case"] == "unknown", moved_case_b
        assert moved_case_b["reasons"] == ["project-history-incomplete"], (
            moved_case_b
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        destination = root / "src" / "Destination.Tests"
        destination.mkdir()
        (destination / "Destination.Tests.csproj").write_text(
            "<Project />\n",
            encoding="utf-8",
        )
        commit(root, "Add destination project", "2026-08-01T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine in source project", "2026-08-02T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine in source project", "2026-08-03T00:00:00Z")
        moved_path = destination / file_path.name
        run(root, "git", "mv", file_path, moved_path)
        moved_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(
            root,
            "Move across projects and re-quarantine",
            "2026-08-04T00:00:00Z",
        )

        cross_project = MODULE.collect_requarantine_history(root, "HEAD")
        assert cross_project["targets"][0]["status"] == "ambiguous", cross_project

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(root, "Quarantine type", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine type", "2026-08-02T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Re-quarantine method", "2026-08-03T00:00:00Z")

        type_to_method = MODULE.collect_requarantine_history(root, "HEAD")
        assert type_to_method["targets"][0]["status"] == "re-quarantined", (
            type_to_method
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly", "2026-08-01T00:00:00Z")
        assembly_info.write_text("", encoding="utf-8")
        commit(root, "Unquarantine assembly", "2026-08-02T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Re-quarantine method", "2026-08-03T00:00:00Z")

        assembly_to_method = MODULE.collect_requarantine_history(root, "HEAD")
        assert assembly_to_method["targets"][0]["status"] == "re-quarantined", (
            assembly_to_method
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine method", "2026-08-02T00:00:00Z")
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(root, "Re-quarantine type", "2026-08-03T00:00:00Z")

        method_to_type = MODULE.collect_requarantine_history(root, "HEAD")
        assert method_to_type["targets"][0]["status"] == "re-quarantined", (
            method_to_type
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine method", "2026-08-02T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Re-quarantine assembly", "2026-08-03T00:00:00Z")

        method_to_assembly = MODULE.collect_requarantine_history(root, "HEAD")
        assert (
            method_to_assembly["targets"][0]["status"]
            == "re-quarantined"
        ), method_to_assembly

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly", "2026-08-01T00:00:00Z")
        assembly_info.write_text("", encoding="utf-8")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(
            root,
            "Replace assembly quarantine with method quarantine",
            "2026-08-02T00:00:00Z",
        )
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Re-quarantine assembly", "2026-08-03T00:00:00Z")

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assembly_target = next(
            target for target in targets
            if target["scope"] == "assembly"
        )
        assert assembly_target["status"] == "re-quarantined", targets

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{
}
""",
            encoding="utf-8",
        )
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly before test exists", "2026-08-01T00:00:00Z")
        assembly_info.write_text("", encoding="utf-8")
        commit(root, "Unquarantine assembly", "2026-08-02T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Add test after assembly unquarantine", "2026-08-03T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine new method", "2026-08-04T00:00:00Z")

        later_method = MODULE.collect_requarantine_history(root, "HEAD")
        assert later_method["targets"][0]["status"] == "first-quarantine", (
            later_method
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine method", "2026-08-02T00:00:00Z")
        file_path.write_text(
            source(QUARANTINE_ATTRIBUTE).replace(
                "SampleTests",
                "RenamedTests",
            ),
            encoding="utf-8",
        )
        commit(root, "Rename type and re-quarantine", "2026-08-03T00:00:00Z")

        renamed_type = MODULE.collect_requarantine_history(root, "HEAD")
        assert renamed_type["targets"][0]["status"] == "ambiguous", renamed_type

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine method", "2026-08-02T00:00:00Z")
        file_path.write_text(
            source(QUARANTINE_ATTRIBUTE).replace(
                "ReturnsExpectedResponse",
                "ReturnsExpectedResponseV2",
            ),
            encoding="utf-8",
        )
        commit(root, "Rename method and re-quarantine", "2026-08-03T00:00:00Z")

        renamed_method = MODULE.collect_requarantine_history(root, "HEAD")
        assert renamed_method["targets"][0]["status"] == "ambiguous", (
            renamed_method
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-02T00:00:00Z")
        file_path.write_text(
            source().replace("SampleTests", "RenamedTests"),
            encoding="utf-8",
        )
        commit(root, "Rename unquarantined type", "2026-08-03T00:00:00Z")

        renamed_name = (
            "Microsoft.AspNetCore.Tests."
            "RenamedTests.ReturnsExpectedResponse"
        )
        renamed_evidence = evidence()
        renamed_evidence["source_a"][renamed_name] = (
            renamed_evidence["source_a"].pop(TEST_NAME)
        )
        renamed_result = collect_result(
            root,
            renamed_evidence,
            test_name=renamed_name,
        )
        assert renamed_result["status"] == "unproven", renamed_result
        assert renamed_result["originating_case"] == "unknown", renamed_result
        assert renamed_result["reasons"] == [
            "target-identity-rename-ambiguous"
        ], renamed_result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-02T00:00:00Z")
        file_path.write_text(
            source().replace(
                "Microsoft.AspNetCore.Tests",
                "Microsoft.AspNetCore.RenamedTests",
            ),
            encoding="utf-8",
        )
        commit(root, "Rename test namespace", "2026-08-03T00:00:00Z")

        renamed_name = (
            "Microsoft.AspNetCore.RenamedTests."
            "SampleTests.ReturnsExpectedResponse"
        )
        renamed_evidence = evidence()
        renamed_evidence["source_a"][renamed_name] = (
            renamed_evidence["source_a"].pop(TEST_NAME)
        )
        renamed_result = collect_result(
            root,
            renamed_evidence,
            test_name=renamed_name,
        )
        assert renamed_result["status"] == "unproven", renamed_result
        assert renamed_result["originating_case"] == "unknown", renamed_result
        assert renamed_result["reasons"] == [
            "target-identity-rename-ambiguous"
        ], renamed_result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)

        def multi_namespace_source(namespace, attribute=""):
            return f"""namespace Stable
{{
    public class Other
    {{
    }}
}}

namespace {namespace}
{{
    public class SampleTests
    {{
        {attribute}
        public void ReturnsExpectedResponse()
        {{
        }}
    }}
}}
"""

        file_path.write_text(
            multi_namespace_source("Microsoft.AspNetCore.Tests"),
            encoding="utf-8",
        )
        commit(root, "Add multi-namespace test", "2026-07-31T00:00:00Z")
        file_path.write_text(
            multi_namespace_source(
                "Microsoft.AspNetCore.Tests",
                QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(
            multi_namespace_source("Microsoft.AspNetCore.Tests"),
            encoding="utf-8",
        )
        commit(root, "Unquarantine test", "2026-08-02T00:00:00Z")
        file_path.write_text(
            multi_namespace_source("Microsoft.AspNetCore.RenamedTests"),
            encoding="utf-8",
        )
        commit(root, "Rename second namespace", "2026-08-03T00:00:00Z")

        renamed_name = (
            "Microsoft.AspNetCore.RenamedTests."
            "SampleTests.ReturnsExpectedResponse"
        )
        renamed_evidence = evidence()
        renamed_evidence["source_a"][renamed_name] = (
            renamed_evidence["source_a"].pop(TEST_NAME)
        )
        renamed_result = collect_result(
            root,
            renamed_evidence,
            test_name=renamed_name,
        )
        assert renamed_result["status"] == "unproven", renamed_result
        assert renamed_result["reasons"] == [
            "target-identity-rename-ambiguous"
        ], renamed_result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)

        def nested_namespace_source(inner_namespace, attribute=""):
            return f"""namespace Outer
{{
    namespace {inner_namespace}
    {{
        public class SampleTests
        {{
            {attribute}
            public void ReturnsExpectedResponse()
            {{
            }}
        }}
    }}
}}
"""

        file_path.write_text(
            nested_namespace_source("Old"),
            encoding="utf-8",
        )
        commit(root, "Add nested namespace test", "2026-07-31T00:00:00Z")
        file_path.write_text(
            nested_namespace_source("Old", QUARANTINE_ATTRIBUTE),
            encoding="utf-8",
        )
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(
            nested_namespace_source("Old"),
            encoding="utf-8",
        )
        commit(root, "Unquarantine test", "2026-08-02T00:00:00Z")
        file_path.write_text(
            nested_namespace_source("New"),
            encoding="utf-8",
        )
        commit(root, "Rename nested namespace", "2026-08-03T00:00:00Z")

        renamed_name = "Outer.New.SampleTests.ReturnsExpectedResponse"
        renamed_evidence = evidence()
        renamed_evidence["source_a"][renamed_name] = (
            renamed_evidence["source_a"].pop(TEST_NAME)
        )
        renamed_result = collect_result(
            root,
            renamed_evidence,
            test_name=renamed_name,
        )
        assert renamed_result["status"] == "unproven", renamed_result
        assert renamed_result["reasons"] == [
            "target-identity-rename-ambiguous"
        ], renamed_result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)

        def moved_type_source(target_namespace, attribute=""):
            def target_body(namespace):
                if namespace != target_namespace:
                    return ""
                return f"""
    public class SampleTests
    {{
        {attribute}
        public void ReturnsExpectedResponse()
        {{
        }}
    }}
"""

            return f"""namespace Old
{{{target_body("Old")}
}}

namespace New
{{{target_body("New")}
}}
"""

        file_path.write_text(moved_type_source("Old"), encoding="utf-8")
        commit(root, "Add test in old namespace", "2026-07-31T00:00:00Z")
        file_path.write_text(
            moved_type_source("Old", QUARANTINE_ATTRIBUTE),
            encoding="utf-8",
        )
        commit(root, "Quarantine test", "2026-08-01T00:00:00Z")
        file_path.write_text(moved_type_source("Old"), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-02T00:00:00Z")
        file_path.write_text(moved_type_source("New"), encoding="utf-8")
        commit(root, "Move type between namespaces", "2026-08-03T00:00:00Z")

        moved_name = "New.SampleTests.ReturnsExpectedResponse"
        moved_evidence = evidence()
        moved_evidence["source_a"][moved_name] = (
            moved_evidence["source_a"].pop(TEST_NAME)
        )
        moved_result = collect_result(
            root,
            moved_evidence,
            test_name=moved_name,
        )
        assert moved_result["status"] == "unproven", moved_result
        assert moved_result["reasons"] == [
            "target-identity-rename-ambiguous"
        ], moved_result


def test_assembly_history_ignores_child_removal_after_own_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly", "2026-08-02T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(
            root,
            "Remove redundant method quarantine",
            "2026-08-03T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assembly_target = next(
            target for target in targets
            if target["scope"] == "assembly"
        )
        assert assembly_target["status"] == "first-quarantine", targets


def test_assembly_history_ignores_child_churn_during_own_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine assembly", "2026-08-01T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Add redundant method quarantine", "2026-08-02T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        commit(
            root,
            "Remove redundant method quarantine",
            "2026-08-03T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "assembly", targets
        assert targets[0]["status"] == "first-quarantine", targets


def test_method_history_ignores_assembly_churn_during_own_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(root, "Add redundant assembly quarantine", "2026-08-02T00:00:00Z")
        assembly_info.write_text("", encoding="utf-8")
        commit(
            root,
            "Remove redundant assembly quarantine",
            "2026-08-03T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "method", targets
        assert targets[0]["status"] == "first-quarantine", targets


def test_method_history_ignores_type_churn_during_own_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                type_quarantine=QUARANTINE_ATTRIBUTE,
                members=method_member(quarantine=QUARANTINE_ATTRIBUTE),
            ),
            encoding="utf-8",
        )
        commit(root, "Add redundant type quarantine", "2026-08-02T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(
            root,
            "Remove redundant type quarantine",
            "2026-08-03T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "method", targets
        assert targets[0]["status"] == "first-quarantine", targets


def test_type_history_ignores_method_churn_during_own_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(root, "Quarantine type", "2026-08-01T00:00:00Z")
        file_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                type_quarantine=QUARANTINE_ATTRIBUTE,
                members=method_member(quarantine=QUARANTINE_ATTRIBUTE),
            ),
            encoding="utf-8",
        )
        commit(root, "Add redundant method quarantine", "2026-08-02T00:00:00Z")
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(
            root,
            "Remove redundant method quarantine",
            "2026-08-03T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "type", targets
        assert targets[0]["status"] == "first-quarantine", targets


def test_same_commit_method_to_assembly_replacement_is_not_first_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(root, "Quarantine method", "2026-08-01T00:00:00Z")
        file_path.write_text(source(), encoding="utf-8")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/2")]\n',
            encoding="utf-8",
        )
        commit(
            root,
            "Replace method quarantine with assembly quarantine",
            "2026-08-02T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "assembly", targets
        assert targets[0]["status"] == "ambiguous", targets


def test_same_commit_type_to_method_replacement_is_not_first_quarantine():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, file_path = initialize_repository(root)
        commit(root, "Add test project", "2026-07-31T00:00:00Z")
        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(root, "Quarantine type", "2026-08-01T00:00:00Z")
        file_path.write_text(source(QUARANTINE_ATTRIBUTE), encoding="utf-8")
        commit(
            root,
            "Replace type quarantine with method quarantine",
            "2026-08-02T00:00:00Z",
        )

        targets = MODULE.collect_requarantine_history(root, "HEAD")["targets"]
        assert targets[0]["scope"] == "method", targets
        assert targets[0]["status"] == "ambiguous", targets


def test_exact_target_transition_in_shared_hunk():
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
    public void Alpha()
    {
    }

    public void Beta()
    {
    }
}
""",
            encoding="utf-8",
        )
        commit(root, "Quarantine Alpha", "2026-08-01T00:00:00Z")
        file_path.write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{
    public void Alpha()
    {
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/2")]
    public void Beta()
    {
    }
}
""",
            encoding="utf-8",
        )
        commit(root, "Move quarantine to Beta", "2026-08-02T00:00:00Z")

        alpha = MODULE.quarantine_transitions(
            root,
            str(file_path.relative_to(root)),
            "Alpha",
            "Microsoft.AspNetCore.Tests.SampleTests",
            "HEAD",
        )
        beta = MODULE.quarantine_transitions(
            root,
            str(file_path.relative_to(root)),
            "Beta",
            "Microsoft.AspNetCore.Tests.SampleTests",
            "HEAD",
        )
        assert [event["status"] for event in alpha] == ["removed", "added"], alpha
        assert [event["status"] for event in beta] == ["added"], beta


class FakeResponse(io.BytesIO):
    def __init__(self, payload, link=""):
        super().__init__(json.dumps(payload).encode())
        self.headers = {"Link": link}

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self.close()


def test_github_pr_files():
    try:
        MODULE.github_pr_files("dotnet/aspnetcore", 42, "")
    except ValueError as error:
        assert str(error) == "A GitHub token is required to inspect pull request files"
    else:
        raise AssertionError("github_pr_files must reject a missing token")

    responses = [
        FakeResponse(
            [{
                "filename": "src/New.cs",
                "previous_filename": "src/Old.cs",
            }],
            '<https://api.github.com/next-page>; rel="next"',
        ),
        FakeResponse([{"filename": "src/Other.cs"}]),
    ]
    requests = []

    def urlopen(request, timeout):
        requests.append((request, timeout))
        return responses.pop(0)

    with mock.patch.object(MODULE.urllib.request, "urlopen", side_effect=urlopen):
        files = MODULE.github_pr_files("dotnet/aspnetcore", 42, "token-123")

    assert files == {"src/New.cs", "src/Old.cs", "src/Other.cs"}
    assert [request.full_url for request, _ in requests] == [
        "https://api.github.com/repos/dotnet/aspnetcore/pulls/42/files?per_page=100",
        "https://api.github.com/next-page",
    ]
    assert all(timeout == 30 for _, timeout in requests)
    for request, _ in requests:
        assert request.get_method() == "GET"
        assert request.get_header("Authorization") == "Bearer token-123"
        assert request.get_header("Accept") == "application/vnd.github+json"
        assert request.get_header("User-agent") == "aspnetcore-test-quarantine"

    with mock.patch.object(
        MODULE.urllib.request,
        "urlopen",
        side_effect=OSError("network unavailable"),
    ) as failing_urlopen:
        try:
            MODULE.github_pr_files("dotnet/aspnetcore", 42, "token-123")
        except OSError as error:
            assert str(error) == "network unavailable"
        else:
            raise AssertionError("github_pr_files must fail when a page cannot be read")
    failing_urlopen.assert_called_once()


def test_github_commit_contains():
    try:
        MODULE.github_commit_contains("dotnet/aspnetcore", "a", "b", "")
    except ValueError as error:
        assert str(error) == "A GitHub token is required to compare build commits"
    else:
        raise AssertionError("github_commit_contains must reject a missing token")

    for status, expected in [
        ("ahead", True),
        ("identical", True),
        ("behind", False),
        ("diverged", False),
    ]:
        with mock.patch.object(
            MODULE.urllib.request,
            "urlopen",
            return_value=FakeResponse({"status": status}),
        ):
            assert MODULE.github_commit_contains(
                "dotnet/aspnetcore",
                "ancestor",
                "descendant",
                "token-123",
            ) is expected

    not_found = MODULE.urllib.error.HTTPError(
        "https://api.github.com/compare",
        404,
        "Not Found",
        {},
        io.BytesIO(),
    )
    with mock.patch.object(
        MODULE.urllib.request,
        "urlopen",
        side_effect=not_found,
    ):
        try:
            MODULE.github_commit_contains(
                "dotnet/aspnetcore",
                "ancestor",
                "descendant",
                "token-123",
            )
        except ValueError as error:
            assert "could not compare ancestor to descendant" in str(error)
        else:
            raise AssertionError("404 comparisons must be unavailable")

    transient = MODULE.urllib.error.HTTPError(
        "https://api.github.com/compare",
        500,
        "Server Error",
        {},
        io.BytesIO(),
    )
    with (
        mock.patch.object(
            MODULE.urllib.request,
            "urlopen",
            side_effect=[transient, FakeResponse({"status": "ahead"})],
        ) as urlopen_mock,
        mock.patch.object(MODULE.time, "sleep") as sleep_mock,
    ):
        assert MODULE.github_commit_contains(
            "dotnet/aspnetcore",
            "ancestor",
            "descendant",
            "token-123",
        )
    assert urlopen_mock.call_count == 2
    sleep_mock.assert_called_once_with(2)


def test_workflow_runner_temp():
    workflow = (SCRIPT.parents[2] / "test-quarantine.md").read_text(encoding="utf-8")
    for step_name in [
        "Aggregate Part 1 failures",
        "Collect deterministic current quarantine history",
        "Collect deterministic quarantine eligibility",
    ]:
        step = workflow.split(f"    - name: {step_name}\n", 1)[1].split("\n    - name:", 1)[0]
        script = textwrap.dedent(step.split("      run: |\n", 1)[1])
        # Stop at the first Python invocation so the preflight cannot collect live evidence.
        command = "python3() { printf 'Python invoked\\n' >&2; return 97; }\n" + script
        with tempfile.TemporaryDirectory() as directory:
            for name, runner_temp in [("missing", None), ("empty", ""), ("set", directory)]:
                env = dict(os.environ)
                env.pop("RUNNER_TEMP", None)
                if runner_temp is not None:
                    env["RUNNER_TEMP"] = runner_temp
                result = subprocess.run(
                    ["/bin/bash", "--noprofile", "--norc", "-e", "-c", command],
                    env=env,
                    stdin=subprocess.DEVNULL,
                    capture_output=True,
                    text=True,
                    timeout=10,
                )
                scenario = f"{step_name}: RUNNER_TEMP {name}"
                if runner_temp:
                    assert result.returncode == 97, (scenario, result.stderr)
                    assert "Python invoked" in result.stderr, scenario
                else:
                    assert result.returncode != 0, scenario
                    assert "RUNNER_TEMP must be set" in result.stderr, (scenario, result.stderr)
                    assert "Python invoked" not in result.stderr, scenario


def initialize_git_repository(root):
    run(root, "git", "init", "-q")
    run(root, "git", "config", "user.email", "test@example.com")
    run(root, "git", "config", "user.name", "Test")


def create_project(root, project_name, files, project_count=1):
    project = root / "src" / project_name
    project.mkdir(parents=True)
    for index in range(project_count):
        suffix = "" if index == 0 else str(index + 1)
        (project / f"{project_name}{suffix}.csproj").write_text(
            "<Project />",
            encoding="utf-8",
        )
    for relative_path, content in files.items():
        file_path = project / relative_path
        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_text(content, encoding="utf-8")
    return project


def initialize_repository(root, project_count=1):
    initialize_git_repository(root)
    project = create_project(
        root,
        "Sample.Tests",
        {"SampleTests.cs": source()},
        project_count=project_count,
    )
    file_path = root / TEST_PATH
    return project, file_path


def runner_source(
    type_name,
    *,
    partial=False,
    base=None,
    quarantine=False,
    member=False,
    members=None,
):
    if members is None and member:
        members = [f"[Fact]\n{method_member()}"]
    return class_source(
        "Sample",
        type_name,
        partial=partial,
        base=base,
        type_quarantine=QUARANTINE_ATTRIBUTE if quarantine else "",
        members=members,
        usings=RUNNER_USINGS,
    )


def runner_result(root, *, assembly="Sample.Tests--net11.0", module=None):
    data = evidence(test_name=RUNNER_TEST_NAME)
    data["source_a"][RUNNER_TEST_NAME]["assembly"] = assembly
    return collect_result(
        root,
        data,
        test_name=RUNNER_TEST_NAME,
        module=module,
    )


def assert_case_a(result, source_path):
    assert result["status"] == "eligible", result
    assert result["originating_case"] == "case-a", result
    assert result["latest_quarantine_transition"] == "none", result
    assert result["source_resolution"]["status"] == "exact", result
    assert result["source_resolution"]["path"] == source_path, result
    assert result["eligible_failure_builds"] == [101, 102], result


def initialize_partial_base_runner_repository(root, *, runner_inherits_base):
    project = create_project(
        root,
        "Sample.Tests",
        {
            "Base.Method.cs": runner_source("Base", partial=True, member=True),
            "Base.Quarantine.cs": runner_source("Base", partial=True, quarantine=True),
            "Runner.cs": runner_source(
                "Runner",
                base="Base" if runner_inherits_base else None,
            ),
        },
    )
    commit(root, "Add quarantined base and runner", "2026-08-01T00:00:00Z")
    quarantine_path = project / "Base.Quarantine.cs"
    quarantine_path.write_text(
        runner_source("Base", partial=True),
        encoding="utf-8",
    )
    commit(root, "Remove sibling base quarantine", "2026-08-02T00:00:00Z")
    return project, run_output(root, "git", "rev-parse", "HEAD")


def initialize_multihop_runner_repository(root, *, mid_inherits_base):
    project = create_project(
        root,
        "Sample.Tests",
        {
            "Base.Method.cs": runner_source("Base", partial=True, member=True),
            "Base.Quarantine.cs": runner_source("Base", partial=True, quarantine=True),
            "Mid.cs": runner_source(
                "Mid",
                base="Base" if mid_inherits_base else None,
            ),
            "Runner.cs": runner_source("Runner", base="Mid"),
        },
    )
    commit(root, "Add quarantined base and multi-hop runner", "2026-08-01T00:00:00Z")
    quarantine_path = project / "Base.Quarantine.cs"
    quarantine_path.write_text(
        runner_source("Base", partial=True),
        encoding="utf-8",
    )
    commit(root, "Remove sibling base quarantine", "2026-08-02T00:00:00Z")
    return project, run_output(root, "git", "rev-parse", "HEAD")


def initialize_mid_quarantine_runner_repository(root, *, runner_base):
    project = create_project(
        root,
        "Sample.Tests",
        {
            "Base.cs": runner_source("Base", member=True),
            "Mid.cs": runner_source("Mid", base="Base", quarantine=True),
            "Runner.cs": runner_source("Runner", base=runner_base),
        },
    )
    commit(root, "Add quarantined Mid runner chain", "2026-08-01T00:00:00Z")
    return project


def test_assembly_quarantine_history():
    for delete_file in (False, True):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            project, _ = initialize_repository(root)
            commit(root, "Add test", "2026-08-01T00:00:00Z")
            assembly_info = project / "AssemblyInfo.cs"
            assembly_info.write_text(
                '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
                encoding="utf-8",
            )
            commit(root, "Quarantine test assembly", "2026-08-02T00:00:00Z")
            if delete_file:
                assembly_info.unlink()
                message = "Delete assembly quarantine file"
            else:
                assembly_info.write_text(
                    "using Microsoft.AspNetCore.Testing;\n",
                    encoding="utf-8",
                )
                message = "Remove assembly quarantine"
            commit(root, message, "2026-08-03T00:00:00Z")
            removal_commit = run_output(
                root,
                "git",
                "rev-parse",
                "HEAD",
            )

            result = record(collect(root, evidence()))
            assert result["status"] == "ineligible", result
            assert result["originating_case"] == "case-b"
            assert result["latest_quarantine_transition"] == "removed"
            assert result["cutoff"]["commit"] == removal_commit
            assert result["cutoff"]["reason"] == "latest-quarantine-transition"

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_repository(root)
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        state_cache = {}
        invalid_state = MODULE.historical_assembly_state(
            root,
            "src/Sample.Tests",
            "not-a-commit",
            state_cache,
        )
        assert invalid_state["status"] == "ambiguous"
        assert state_cache[("src/Sample.Tests", "not-a-commit")] == invalid_state

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine test assembly", "2026-08-02T00:00:00Z")
        assembly_info.write_text(
            "using Microsoft.AspNetCore.Testing;\n",
            encoding="utf-8",
        )
        commit(root, "Remove assembly quarantine", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")
        renamed_path = project / "RenamedSampleTests.cs"
        run(root, "git", "mv", file_path, renamed_path)
        commit(root, "Rename test file", "2026-08-04T00:00:00Z")
        rename_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = record(collect(root, evidence()))
        assert result["status"] == "ineligible", result
        assert result["originating_case"] == "case-b"
        assert result["source_resolution"]["path"] == (
            "src/Sample.Tests/RenamedSampleTests.cs"
        )
        assert result["latest_quarantine_transition"] == "removed"
        assert result["cutoff"]["commit"] == rename_commit
        assert result["cutoff"]["reason"] == "latest-test-file-change", result
        assert removal_commit != rename_commit

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, file_path = initialize_repository(root)
        file_path.unlink()
        (project / "BaseTests.cs").write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class BaseTests
{
    public void ReturnsExpectedResponse()
    {
    }
}
""",
            encoding="utf-8",
        )
        derived_path = project / "DerivedTests.cs"
        derived_path.write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

public class DerivedTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Add inherited test", "2026-08-01T00:00:00Z")
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine test assembly", "2026-08-02T00:00:00Z")
        assembly_info.write_text(
            "using Microsoft.AspNetCore.Testing;\n",
            encoding="utf-8",
        )
        commit(root, "Remove assembly quarantine", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")
        (project / "IntermediateTests.cs").write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class IntermediateTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        derived_path.write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

public class DerivedTests : IntermediateTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Add intermediate test runner", "2026-08-04T00:00:00Z")
        runner_commit = run_output(root, "git", "rev-parse", "HEAD")

        test_name = (
            "Microsoft.AspNetCore.Server.Tests."
            "DerivedTests.ReturnsExpectedResponse"
        )
        inherited_evidence = evidence()
        inherited_evidence["source_a"][test_name] = (
            inherited_evidence["source_a"].pop(TEST_NAME)
        )
        result = collect(root, inherited_evidence)["tests"][test_name]
        assert result["status"] == "ineligible", result
        assert result["originating_case"] == "case-b"
        assert result["latest_quarantine_transition"] == "removed"
        assert result["cutoff"]["commit"] == runner_commit
        assert removal_commit != runner_commit

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        project, _ = initialize_repository(root, project_count=2)
        assembly_info = project / "AssemblyInfo.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Add ambiguously associated quarantine", "2026-08-01T00:00:00Z")
        assembly_info.write_text(
            "using Microsoft.AspNetCore.Testing;\n",
            encoding="utf-8",
        )
        commit(root, "Remove ambiguous quarantine", "2026-08-02T00:00:00Z")

        result = record(collect(root, evidence()))
        assert result["status"] == "unproven", result
        assert result["latest_quarantine_transition"] == "unknown"
        assert result["reasons"] == ["project-history-incomplete"]


def test_partial_sibling_type_quarantine_is_already_quarantined(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    type_quarantine=QUARANTINE_ATTRIBUTE,
                ),
            },
        )
        commit(root, "Add partially quarantined test", "2026-08-01T00:00:00Z")

        result = collect_result(root, evidence(), module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert_already_quarantined(result)


def test_partial_sibling_type_quarantine_removal_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                ),
            },
        )
        commit(root, "Add partial test", "2026-08-01T00:00:00Z")

        sibling_path = root / "src/Sample.Tests/SampleTests.Partial.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                type_quarantine=QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine sibling partial type", "2026-08-02T00:00:00Z")

        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
            ),
            encoding="utf-8",
        )
        commit(root, "Remove sibling partial quarantine", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = collect_result(root, evidence(), module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert_case_b(result, removal_commit)


def test_partial_sibling_type_quarantine_removal_after_rename_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                ),
            },
        )
        commit(root, "Add partial test", "2026-08-01T00:00:00Z")

        original_path = root / "src/Sample.Tests/SampleTests.Partial.cs"
        original_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                type_quarantine=QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine sibling partial type", "2026-08-02T00:00:00Z")

        renamed_path = root / "src/Sample.Tests/RenamedSampleTests.Partial.cs"
        run(root, "git", "mv", original_path, renamed_path)
        commit(root, "Rename sibling partial quarantine file", "2026-08-03T00:00:00Z")

        renamed_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
            ),
            encoding="utf-8",
        )
        commit(root, "Remove renamed sibling partial quarantine", "2026-08-04T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = collect_result(root, evidence(), module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert_case_b(result, removal_commit)


def test_partial_sibling_type_quarantine_removal_by_deletion_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                ),
            },
        )
        commit(root, "Add partial test", "2026-08-01T00:00:00Z")

        sibling_path = root / "src/Sample.Tests/SampleTests.Partial.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                type_quarantine=QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine sibling partial type", "2026-08-02T00:00:00Z")

        sibling_path.unlink()
        commit(root, "Delete sibling partial quarantine file", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = collect_result(root, evidence(), module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert_case_b(result, removal_commit)


def test_partial_other_method_unquarantine_does_not_make_test_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Other.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member(
                        "ReturnsOtherResponse",
                        quarantine=QUARANTINE_ATTRIBUTE,
                    )],
                ),
            },
        )
        commit(root, "Add unrelated quarantined sibling method", "2026-08-01T00:00:00Z")

        sibling_path = root / "src/Sample.Tests/SampleTests.Other.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                members=[method_member("ReturnsOtherResponse")],
            ),
            encoding="utf-8",
        )
        commit(root, "Unquarantine unrelated sibling method", "2026-08-02T00:00:00Z")

        result = collect_result(root, evidence(), module=module)
        assert result["status"] == "eligible", result
        assert result["originating_case"] == "case-a", result
        assert result["latest_quarantine_transition"] == "none", result
        assert result["eligible_failure_builds"] == [101, 102], result


def test_partial_inherited_runner_sibling_type_removal_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
                "DerivedTests.Base.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    partial=True,
                    base="BaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
                "DerivedTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    partial=True,
                ),
            },
        )
        commit(root, "Add partial inherited runner", "2026-08-01T00:00:00Z")

        sibling_path = root / "src/Sample.Tests/DerivedTests.Partial.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Server.Tests",
                "DerivedTests",
                partial=True,
                type_quarantine=QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine sibling runner partial type", "2026-08-02T00:00:00Z")

        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Server.Tests",
                "DerivedTests",
                partial=True,
            ),
            encoding="utf-8",
        )
        commit(root, "Remove sibling runner partial quarantine", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["type"] == (
            "Microsoft.AspNetCore.Server.Tests.DerivedTests"
        ), result
        assert result["source_resolution"]["declaring_type"] == (
            "Microsoft.AspNetCore.Tests.BaseTests"
        ), result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/BaseTests.cs", result
        assert_case_b(result, removal_commit)


def test_partial_declaring_type_sibling_quarantine_removal_is_case_b_for_inherited_test(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.Method.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    partial=True,
                    members=[method_member()],
                ),
                "BaseTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    partial=True,
                ),
                "DerivedTests.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    base="BaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
            },
        )
        commit(root, "Add inherited test with partial declaring type", "2026-08-01T00:00:00Z")

        sibling_path = root / "src/Sample.Tests/BaseTests.Partial.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "BaseTests",
                partial=True,
                type_quarantine=QUARANTINE_ATTRIBUTE,
            ),
            encoding="utf-8",
        )
        commit(root, "Quarantine declaring type in sibling partial", "2026-08-02T00:00:00Z")

        current = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert current["source_resolution"]["status"] == "exact", current
        assert_already_quarantined(current)

        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "BaseTests",
                partial=True,
            ),
            encoding="utf-8",
        )
        commit(root, "Remove declaring type sibling partial quarantine", "2026-08-03T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["source_resolution"]["status"] == "exact", result
        assert_case_b(result, removal_commit)


def test_same_full_type_in_multiple_projects_fails_closed(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
                "DerivedTests.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    base="BaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
            },
        )
        create_project(
            root,
            "Other.Tests",
            {
                "AlternativeBaseTests.cs": class_source(
                    "Contoso.Tests",
                    "AlternativeBaseTests",
                    members=[method_member()],
                ),
                "DerivedTests.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    base="AlternativeBaseTests",
                    usings="using Contoso.Tests;",
                ),
            },
        )
        commit(root, "Add conflicting inherited runners", "2026-08-01T00:00:00Z")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["status"] == "unproven", result
        assert result["source_resolution"]["status"] == "ambiguous", result
        assert "source-ambiguous" in result["reasons"], result


def test_other_project_same_name_type_does_not_contaminate_inherited_resolution(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
                "DerivedTests.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    base="BaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
            },
        )
        create_project(
            root,
            "Other.Tests",
            {
                "BaseTests.cs": class_source(
                    "Contoso.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
            },
        )
        commit(root, "Add same-name base type in another project", "2026-08-01T00:00:00Z")

        resolved = module.resolve_source(root, DERIVED_TEST_NAME)
        assert resolved["status"] == "exact", resolved
        assert resolved["type"] == "Microsoft.AspNetCore.Server.Tests.DerivedTests", resolved
        assert resolved["declaring_type"] == "Microsoft.AspNetCore.Tests.BaseTests", resolved
        assert resolved["path"] == "src/Sample.Tests/BaseTests.cs", resolved


def test_other_project_same_full_intermediate_type_does_not_contaminate_multihop_inherited_resolution(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
                "MidTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "MidTests",
                    base="BaseTests",
                    members=["// Intermediate runner without its own method body."],
                ),
                "DerivedTests.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    base="MidTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
            },
        )
        create_project(
            root,
            "Other.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member("ReturnsOtherProjectResponse")],
                ),
                "MidTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "MidTests",
                    base="BaseTests",
                    members=["// Same full intermediate type in another project."],
                ),
            },
        )
        commit(root, "Add multi-hop inherited test with duplicate intermediate type", "2026-08-01T00:00:00Z")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["status"] == "eligible", result
        assert result["originating_case"] == "case-a", result
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/BaseTests.cs", result
        assert {
            entry["path"] for entry in result["source_resolution"]["history_locations"]
        } == {
            "src/Sample.Tests/BaseTests.cs",
            "src/Sample.Tests/MidTests.cs",
            "src/Sample.Tests/DerivedTests.cs",
        }, result


def test_partial_type_quarantine_removal_only_applies_to_methods_present_at_removal(module=None):
    module = module or MODULE
    added_later_test_name = (
        "Microsoft.AspNetCore.Tests.SampleTests.AddedLaterStaysEligible"
    )
    existing_test_name = (
        "Microsoft.AspNetCore.Tests.SampleTests.RemovedEarlierStaysCaseB"
    )
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.Existing.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member("RemovedEarlierStaysCaseB")],
                ),
                "SampleTests.Quarantine.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    type_quarantine=QUARANTINE_ATTRIBUTE,
                ),
            },
        )
        commit(root, "Add quarantined partial type with existing method", "2026-08-01T00:00:00Z")

        quarantine_path = root / "src/Sample.Tests/SampleTests.Quarantine.cs"
        quarantine_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
            ),
            encoding="utf-8",
        )
        commit(root, "Remove sibling partial type quarantine", "2026-08-02T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        (root / "src/Sample.Tests/SampleTests.Added.cs").write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                members=[method_member("AddedLaterStaysEligible")],
            ),
            encoding="utf-8",
        )
        commit(root, "Add later partial test method", "2026-08-03T00:00:00Z")

        data = evidence(test_name=added_later_test_name)
        data["source_a"][existing_test_name] = dict(
            data["source_a"][added_later_test_name],
            run_id=2002,
            result_id=3002,
        )
        receipt = collect(root, data, module=module)["tests"]

        added_later = receipt[added_later_test_name]
        assert added_later["status"] == "eligible", added_later
        assert added_later["originating_case"] == "case-a", added_later
        assert added_later["latest_quarantine_transition"] == "none", added_later
        assert added_later["eligible_failure_builds"] == [101, 102], added_later

        existing = receipt[existing_test_name]
        assert_case_b(existing, removal_commit)


def test_inherited_runner_added_after_declaring_type_unquarantine_stays_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    type_quarantine=QUARANTINE_ATTRIBUTE,
                    members=[method_member()],
                ),
            },
        )
        commit(root, "Add quarantined declaring base type", "2026-08-01T00:00:00Z")

        base_path = root / "src/Sample.Tests/BaseTests.cs"
        base_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "BaseTests",
                members=[method_member()],
            ),
            encoding="utf-8",
        )
        commit(root, "Remove declaring base type quarantine", "2026-08-02T00:00:00Z")

        (root / "src/Sample.Tests/DerivedTests.cs").write_text(
            class_source(
                "Microsoft.AspNetCore.Server.Tests",
                "DerivedTests",
                base="BaseTests",
                usings="using Microsoft.AspNetCore.Tests;",
            ),
            encoding="utf-8",
        )
        commit(root, "Add inherited runner after declaring type unquarantine", "2026-08-03T00:00:00Z")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["status"] == "eligible", result
        assert result["originating_case"] == "case-a", result
        assert result["latest_quarantine_transition"] == "none", result
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == (
            "Microsoft.AspNetCore.Tests.BaseTests"
        ), result


def test_runner_inherits_base_after_partial_type_unquarantine_stays_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project, _ = initialize_partial_base_runner_repository(
            root,
            runner_inherits_base=False,
        )

        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Runner now inherits Base", "2026-08-03T00:00:00Z")

        result = runner_result(root, module=module)
        assert_case_a(result, "src/Sample.Tests/Base.Method.cs")
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result


def test_runner_already_inherits_base_before_partial_type_unquarantine_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        _, removal_commit = initialize_partial_base_runner_repository(
            root,
            runner_inherits_base=True,
        )

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/Base.Method.cs", result
        assert_case_b(result, removal_commit)


def test_duplicate_runner_identity_with_inherited_assembly_fails_closed(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "A.Tests",
            {
                "Runner.cs": runner_source("Runner", member=True),
            },
        )
        create_project(
            root,
            "B.Tests",
            {
                "Base.cs": runner_source("Base", member=True, quarantine=True),
                "Runner.cs": runner_source("Runner", base="Base"),
            },
        )
        commit(root, "Add duplicate runner identities", "2026-08-01T00:00:00Z")

        result = runner_result(root, assembly="B.Tests--net11.0", module=module)
        assert result["status"] == "unproven", result
        assert result["source_resolution"]["status"] == "ambiguous", result
        assert "source-ambiguous" in result["reasons"], result


def test_unique_direct_runner_identity_stays_exact_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "A.Tests",
            {
                "Runner.cs": runner_source("Runner", member=True),
            },
        )
        commit(root, "Add unique direct runner", "2026-08-01T00:00:00Z")

        result = runner_result(root, assembly="A.Tests--net11.0", module=module)
        assert_case_a(result, "src/A.Tests/Runner.cs")
        assert result["source_resolution"]["declaring_type"] == "Sample.Runner", result


def test_multihop_runner_reaches_base_after_partial_type_unquarantine_stays_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project, _ = initialize_multihop_runner_repository(
            root,
            mid_inherits_base=False,
        )

        (project / "Mid.cs").write_text(
            runner_source("Mid", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Mid now inherits Base", "2026-08-03T00:00:00Z")

        result = runner_result(root, module=module)
        assert_case_a(result, "src/Sample.Tests/Base.Method.cs")
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert {
            entry["path"] for entry in result["source_resolution"]["history_locations"]
        } == {
            "src/Sample.Tests/Base.Method.cs",
            "src/Sample.Tests/Mid.cs",
            "src/Sample.Tests/Runner.cs",
        }, result


def test_multihop_runner_already_reaches_base_before_partial_type_unquarantine_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        _, removal_commit = initialize_multihop_runner_repository(
            root,
            mid_inherits_base=True,
        )

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/Base.Method.cs", result
        assert_case_b(result, removal_commit)


def test_runner_switches_to_mid_after_base_unquarantine_stays_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project, removal_commit = initialize_partial_base_runner_repository(
            root,
            runner_inherits_base=True,
        )

        (project / "Mid.cs").write_text(
            runner_source("Mid", base="Base"),
            encoding="utf-8",
        )
        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Mid"),
            encoding="utf-8",
        )
        commit(root, "Introduce Mid and retarget Runner", "2026-08-03T00:00:00Z")
        retarget_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/Base.Method.cs", result
        assert result["originating_case"] == "case-b", result
        assert result["cutoff"]["commit"] == retarget_commit, result
        assert result["cutoff"]["reason"] == "latest-test-file-change", result
        assert removal_commit != retarget_commit


def test_historical_mid_unquarantine_stays_case_b_after_runner_switches_directly_to_base(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project = initialize_mid_quarantine_runner_repository(root, runner_base="Mid")

        mid_path = project / "Mid.cs"
        mid_path.write_text(
            runner_source("Mid", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Remove Mid quarantine", "2026-08-02T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Base"),
            encoding="utf-8",
        )
        mid_path.unlink()
        commit(root, "Runner now inherits Base directly", "2026-08-03T00:00:00Z")
        switch_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/Base.cs", result
        assert result["originating_case"] == "case-b", result
        assert result["cutoff"]["commit"] == switch_commit, result
        assert result["cutoff"]["reason"] == "latest-test-file-change", result
        assert removal_commit != switch_commit


def test_mid_unquarantine_before_runner_adopts_mid_stays_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project = initialize_mid_quarantine_runner_repository(root, runner_base="Base")

        (project / "Mid.cs").write_text(
            runner_source("Mid", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Remove Mid quarantine", "2026-08-02T00:00:00Z")

        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Mid"),
            encoding="utf-8",
        )
        commit(root, "Runner now inherits Mid", "2026-08-03T00:00:00Z")

        result = runner_result(root, module=module)
        assert_case_a(result, "src/Sample.Tests/Base.cs")
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result


def test_historical_conflicting_partial_runner_bases_before_base_unquarantine_fail_closed(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project = create_project(
            root,
            "Sample.Tests",
            {
                "Base.Method.cs": runner_source("Base", partial=True, member=True),
                "Base.Quarantine.cs": runner_source("Base", partial=True, quarantine=True),
                "Other.cs": runner_source("Other"),
                "Runner.Left.cs": runner_source("Runner", partial=True, base="Base"),
                "Runner.Right.cs": runner_source("Runner", partial=True, base="Other"),
            },
        )
        commit(root, "Add conflicting partial runner bases", "2026-08-01T00:00:00Z")

        (project / "Base.Quarantine.cs").write_text(
            runner_source("Base", partial=True),
            encoding="utf-8",
        )
        commit(root, "Remove base quarantine", "2026-08-02T00:00:00Z")

        (project / "Runner.Right.cs").write_text(
            runner_source("Runner", partial=True),
            encoding="utf-8",
        )
        commit(root, "Resolve conflicting partial runner bases", "2026-08-03T00:00:00Z")

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["status"] == "unproven", result
        assert result["latest_quarantine_transition"] == "ambiguous", result
        assert "quarantine-history-ambiguous" in result["reasons"], result


def test_runner_inherits_base_after_assembly_unquarantine_stays_case_a(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project = create_project(
            root,
            "Sample.Tests",
            {
                "Base.cs": runner_source("Base", member=True),
                "Runner.cs": runner_source("Runner"),
            },
        )
        commit(root, "Add runner before assembly quarantine", "2026-08-01T00:00:00Z")

        assembly_info = project / "AssemblyQuarantine.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine test assembly", "2026-08-02T00:00:00Z")

        assembly_info.unlink()
        commit(root, "Remove assembly quarantine", "2026-08-03T00:00:00Z")

        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Runner now inherits Base", "2026-08-04T00:00:00Z")

        result = runner_result(root, module=module)
        assert_case_a(result, "src/Sample.Tests/Base.cs")
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result


def test_runner_already_inherits_base_before_assembly_unquarantine_is_case_b(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        project = create_project(
            root,
            "Sample.Tests",
            {
                "Base.cs": runner_source("Base", member=True),
                "Runner.cs": runner_source("Runner"),
            },
        )
        commit(root, "Add runner before assembly quarantine", "2026-08-01T00:00:00Z")

        assembly_info = project / "AssemblyQuarantine.cs"
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine test assembly", "2026-08-02T00:00:00Z")

        (project / "Runner.cs").write_text(
            runner_source("Runner", base="Base"),
            encoding="utf-8",
        )
        commit(root, "Runner now inherits Base", "2026-08-03T00:00:00Z")

        assembly_info.unlink()
        commit(root, "Remove assembly quarantine", "2026-08-04T00:00:00Z")
        removal_commit = run_output(root, "git", "rev-parse", "HEAD")

        result = runner_result(root, module=module)
        assert result["source_resolution"]["status"] == "exact", result
        assert result["source_resolution"]["declaring_type"] == "Sample.Base", result
        assert result["source_resolution"]["path"] == "src/Sample.Tests/Base.cs", result
        assert_case_b(result, removal_commit)


def test_partial_runner_conflicting_bases_in_same_project_fail_closed(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "BaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "BaseTests",
                    members=[method_member()],
                ),
                "AlternativeBaseTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "AlternativeBaseTests",
                    members=[method_member()],
                ),
                "DerivedTests.Left.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    partial=True,
                    base="BaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
                "DerivedTests.Right.cs": class_source(
                    "Microsoft.AspNetCore.Server.Tests",
                    "DerivedTests",
                    partial=True,
                    base="AlternativeBaseTests",
                    usings="using Microsoft.AspNetCore.Tests;",
                ),
            },
        )
        commit(root, "Add partial runner with conflicting bases", "2026-08-01T00:00:00Z")

        result = collect_result(
            root,
            evidence(test_name=DERIVED_TEST_NAME),
            test_name=DERIVED_TEST_NAME,
            module=module,
        )
        assert result["status"] == "unproven", result
        assert result["source_resolution"]["status"] == "ambiguous", result
        assert "source-ambiguous" in result["reasons"], result


def test_partial_sibling_edits_do_not_expand_source_b_or_freshness_scope(module=None):
    module = module or MODULE
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        initialize_git_repository(root)
        create_project(
            root,
            "Sample.Tests",
            {
                "SampleTests.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                    members=[method_member()],
                ),
                "SampleTests.Partial.cs": class_source(
                    "Microsoft.AspNetCore.Tests",
                    "SampleTests",
                    partial=True,
                ),
            },
        )
        commit(root, "Add partial test", "2026-08-01T00:00:00Z")
        method_commit = run_output(root, "git", "rev-parse", "HEAD")

        sibling_path = root / "src/Sample.Tests/SampleTests.Partial.cs"
        sibling_path.write_text(
            class_source(
                "Microsoft.AspNetCore.Tests",
                "SampleTests",
                partial=True,
                members=["// unrelated sibling-only edit"],
            ),
            encoding="utf-8",
        )
        commit(root, "Edit unrelated sibling partial file", "2026-08-20T00:00:00Z")

        result = collect_result(
            root,
            source_b_evidence(),
            pr_files_provider=lambda _: {"src/Sample.Tests/SampleTests.Partial.cs"},
            module=module,
        )
        assert result["status"] == "eligible", result
        assert result["originating_case"] == "case-a", result
        assert result["excluded_builds"] == [], result
        assert result["eligible_failure_builds"] == [101, 102], result
        assert result["cutoff"]["reason"] == "latest-test-file-change", result
        assert result["cutoff"]["commit"] == method_commit, result


def run_output(root, *args):
    return subprocess.check_output(
        args,
        cwd=root,
        text=True,
    ).strip()


def main():
    test_kestrel_condition_rows()
    for condition in (
        "[MsQuicSupported]",
        "[Microsoft.AspNetCore.InternalTesting.MsQuicSupportedAttribute()]",
        "[global::Microsoft.AspNetCore.InternalTesting.OSSkipCondition(OperatingSystems.Windows)]",
        "[FrameworkSkipCondition(RuntimeFrameworks.Mono)]",
    ):
        test_non_data_condition_rows(condition)
    for provider in (
        '[MemberData(nameof(GetRows))]',
        '[Xunit.MemberDataAttribute(nameof(GetRows))]',
        '[ClassData(typeof(Rows))]',
        '[CustomRows]',
        '[InlineData(HttpProtocols.Http3)]',
        '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
        'OperatingSystems.Linux, HttpProtocols.Http3)]',
    ):
        test_mixed_row_providers(provider)
    for raw, rendered in [
        ("true", "True"), ("false", "False"), ("-1L", "-1"),
        ("1UL", "1"), ("0x10", "16"), ("0b10", "2"), ("1_000L", "1000"),
    ]:
        test_inline_literal_resolution(raw, rendered)
    test_unmatched_inline_row_fails_closed()
    test_commented_row_attributes()
    test_renamed_row_history()
    test_helix_queue_cache()
    for queue, expected in [
        ("ubuntu.2404.amd64.open", "Linux"),
        ("azurelinux.3.amd64.open", "Linux"),
        ("almalinux.10.amd64.open", "Linux"),
        ("fedora.44.amd64.open", "Linux"),
        ("alpine.323.amd64.open", "Linux"),
        ("debian.13.arm64.open", "Linux"),
        ("OSX.26.Arm64.Open", "MacOSX"),
        ("Windows.Amd64.VS2026.Open", "Windows"),
    ]:
        test_production_queue_evidence(queue, expected)
    test_row_targets_require_conditional_theory()
    test_workflow_platform_evidence()
    test_source_c_platform_evidence()
    test_theory_data_quarantine_support()
    test_build_source_ancestry()
    test_history_cutoff_uses_first_parent_order()
    test_requarantine_history()
    test_assembly_history_ignores_child_removal_after_own_quarantine()
    test_assembly_history_ignores_child_churn_during_own_quarantine()
    test_method_history_ignores_assembly_churn_during_own_quarantine()
    test_method_history_ignores_type_churn_during_own_quarantine()
    test_type_history_ignores_method_churn_during_own_quarantine()
    test_same_commit_method_to_assembly_replacement_is_not_first_quarantine()
    test_same_commit_type_to_method_replacement_is_not_first_quarantine()
    test_exact_target_transition_in_shared_hunk()
    test_github_commit_contains()
    test_workflow_runner_temp()
    test_github_pr_files()

    assert MODULE.github_changed_paths([
        {
            "filename": "src/New.cs",
            "previous_filename": "src/Old.cs",
        },
    ]) == {"src/New.cs", "src/Old.cs"}

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        run(root, "git", "init", "-q")
        run(root, "git", "config", "user.email", "test@example.com")
        run(root, "git", "config", "user.name", "Test")
        file_path = root / TEST_PATH
        file_path.parent.mkdir(parents=True)
        (file_path.parent / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Add test", "2026-08-01T00:00:00Z")

        eligible = record(collect(root, evidence()))
        assert eligible["status"] == "eligible", eligible
        assert eligible["originating_case"] == "case-a"
        assert eligible["eligible_failure_builds"] == [101, 102]
        assert eligible["quarantine_operating_systems"] == [
            "OperatingSystems.Linux",
        ], eligible

        one_failure = record(collect(root, evidence(builds=(101,))))
        assert one_failure["status"] == "ineligible"
        assert "fewer-than-two-post-cutoff-failures" in one_failure["reasons"]
        assert one_failure["quarantine_operating_systems"] == [
            "OperatingSystems.Linux",
        ], one_failure

        regression = record(collect(root, evidence(regression=True)))
        assert regression["status"] == "ineligible"
        assert "consistent-regression-or-unproven" in regression["reasons"]

        stale = evidence()
        for metadata in stale["builds"].values():
            metadata["startedUtc"] = "2026-07-01T00:00:00Z"
        stale_record = record(collect(root, stale))
        assert stale_record["status"] == "ineligible"
        assert "fewer-than-two-post-cutoff-failures" in stale_record["reasons"]

        file_path.write_text(class_quarantined_source(), encoding="utf-8")
        commit(root, "Quarantine test class", "2026-08-02T00:00:00Z")
        class_quarantined = record(collect(root, evidence()))
        assert class_quarantined["status"] == "ineligible"
        assert class_quarantined["originating_case"] == "already-quarantined"

        assembly_info = file_path.parent / "AssemblyInfo.cs"
        file_path.write_text(source(), encoding="utf-8")
        assembly_info.write_text(
            '[assembly: QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]\n',
            encoding="utf-8",
        )
        commit(root, "Quarantine test assembly", "2026-08-03T00:00:00Z")
        assembly_quarantined = record(collect(root, evidence()))
        assert assembly_quarantined["status"] == "ineligible"
        assert assembly_quarantined["originating_case"] == "already-quarantined"

        assembly_info.unlink()
        file_path.write_text(
            source('[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]'),
            encoding="utf-8",
        )
        commit(root, "Quarantine test", "2026-08-04T00:00:00Z")
        quarantined = record(collect(root, evidence()))
        assert quarantined["status"] == "ineligible"
        assert quarantined["originating_case"] == "already-quarantined"

        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Unquarantine test", "2026-08-05T00:00:00Z")
        method_removal_commit = run_output(root, "git", "rev-parse", "HEAD")
        case_b = record(collect(root, evidence()))
        assert case_b["status"] == "ineligible"
        assert case_b["originating_case"] == "case-b"
        assert case_b["cutoff"]["commit"] == method_removal_commit

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        run(root, "git", "init", "-q")
        run(root, "git", "config", "user.email", "test@example.com")
        run(root, "git", "config", "user.name", "Test")
        file_path = root / TEST_PATH
        file_path.parent.mkdir(parents=True)
        (file_path.parent / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        file_path.write_text(source(), encoding="utf-8")
        commit(root, "Add test", "2026-08-01T00:00:00Z")
        source_b = evidence()
        source_b["source_b"] = source_b.pop("source_a")
        for metadata in source_b["builds"].values():
            metadata["pr"] = 42
        excluded = record(collect(
            root,
            source_b,
            pr_files_provider=lambda _: {TEST_PATH},
        ))
        assert excluded["status"] == "ineligible"
        assert {
            item["reason"] for item in excluded["excluded_builds"]
        } == {"source-b-pr-changed-test-file"}

        renamed = record(collect(
            root,
            source_b,
            pr_files_provider=lambda _: {
                TEST_PATH,
                "src/Sample.Tests/RenamedSampleTests.cs",
            },
        ))
        assert renamed["status"] == "ineligible"
        assert {
            item["reason"] for item in renamed["excluded_builds"]
        } == {"source-b-pr-changed-test-file"}

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        run(root, "git", "init", "-q")
        run(root, "git", "config", "user.email", "test@example.com")
        run(root, "git", "config", "user.name", "Test")
        project = root / "src/Sample.Tests"
        project.mkdir(parents=True)
        (project / "Sample.Tests.csproj").write_text("<Project />", encoding="utf-8")
        (project / "BaseTests.cs").write_text(
            """namespace Microsoft.AspNetCore.Tests;

public class BaseTests
{
    public void ReturnsExpectedResponse()
    {
    }
}
""",
            encoding="utf-8",
        )
        (project / "DerivedTests.cs").write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

public class DerivedTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Add inherited test", "2026-08-01T00:00:00Z")
        resolved = MODULE.resolve_source(
            root,
            "Microsoft.AspNetCore.Server.Tests.DerivedTests.ReturnsExpectedResponse",
        )
        assert resolved["status"] == "exact", resolved
        assert resolved["type"] == "Microsoft.AspNetCore.Server.Tests.DerivedTests"
        assert resolved["declaring_type"] == "Microsoft.AspNetCore.Tests.BaseTests"
        assert resolved["path"] == "src/Sample.Tests/BaseTests.cs"
        assert {entry["path"] for entry in resolved["history_locations"]} == {
            "src/Sample.Tests/BaseTests.cs",
            "src/Sample.Tests/DerivedTests.cs",
        }

        derived_name = "Microsoft.AspNetCore.Server.Tests.DerivedTests.ReturnsExpectedResponse"
        derived_evidence = evidence()
        derived_evidence["source_a"][derived_name] = derived_evidence["source_a"].pop(TEST_NAME)
        (project / "DerivedTests.cs").write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

// An edit to the runner type must invalidate older failures.
public class DerivedTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Edit inherited test runner", "2026-08-20T00:00:00Z")
        inherited_stale = collect(root, derived_evidence)["tests"][derived_name]
        assert inherited_stale["status"] == "ineligible", inherited_stale
        assert "fewer-than-two-post-cutoff-failures" in inherited_stale["reasons"]

        (project / "DerivedTests.cs").write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
public class DerivedTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Quarantine inherited runner", "2026-08-21T00:00:00Z")
        (project / "DerivedTests.cs").write_text(
            """using Microsoft.AspNetCore.Tests;

namespace Microsoft.AspNetCore.Server.Tests;

public class DerivedTests : BaseTests
{
}
""",
            encoding="utf-8",
        )
        commit(root, "Unquarantine inherited runner", "2026-08-22T00:00:00Z")
        inherited_case_b = collect(root, derived_evidence)["tests"][derived_name]
        assert inherited_case_b["status"] == "ineligible", inherited_case_b
        assert inherited_case_b["originating_case"] == "case-b"

    test_assembly_quarantine_history()
    test_partial_sibling_type_quarantine_is_already_quarantined()
    test_partial_sibling_type_quarantine_removal_is_case_b()
    test_partial_sibling_type_quarantine_removal_after_rename_is_case_b()
    test_partial_sibling_type_quarantine_removal_by_deletion_is_case_b()
    test_partial_other_method_unquarantine_does_not_make_test_case_b()
    test_partial_inherited_runner_sibling_type_removal_is_case_b()
    test_partial_declaring_type_sibling_quarantine_removal_is_case_b_for_inherited_test()
    test_same_full_type_in_multiple_projects_fails_closed()
    test_other_project_same_name_type_does_not_contaminate_inherited_resolution()
    test_other_project_same_full_intermediate_type_does_not_contaminate_multihop_inherited_resolution()
    test_partial_type_quarantine_removal_only_applies_to_methods_present_at_removal()
    test_inherited_runner_added_after_declaring_type_unquarantine_stays_case_a()
    test_runner_inherits_base_after_partial_type_unquarantine_stays_case_a()
    test_runner_already_inherits_base_before_partial_type_unquarantine_is_case_b()
    test_duplicate_runner_identity_with_inherited_assembly_fails_closed()
    test_unique_direct_runner_identity_stays_exact_case_a()
    test_multihop_runner_reaches_base_after_partial_type_unquarantine_stays_case_a()
    test_multihop_runner_already_reaches_base_before_partial_type_unquarantine_is_case_b()
    test_runner_switches_to_mid_after_base_unquarantine_stays_case_b()
    test_historical_mid_unquarantine_stays_case_b_after_runner_switches_directly_to_base()
    test_mid_unquarantine_before_runner_adopts_mid_stays_case_a()
    test_historical_conflicting_partial_runner_bases_before_base_unquarantine_fail_closed()
    test_runner_inherits_base_after_assembly_unquarantine_stays_case_a()
    test_runner_already_inherits_base_before_assembly_unquarantine_is_case_b()
    test_partial_runner_conflicting_bases_in_same_project_fail_closed()
    test_partial_sibling_edits_do_not_expand_source_b_or_freshness_scope()

    print("All Case A eligibility collector tests passed.")


if __name__ == "__main__":
    main()
