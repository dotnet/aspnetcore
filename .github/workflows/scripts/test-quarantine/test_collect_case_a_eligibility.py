#!/usr/bin/env python3

import datetime
import importlib.util
import io
import json
import os
import pathlib
import subprocess
import tempfile
from unittest import mock


SCRIPT = pathlib.Path(__file__).with_name("collect_case_a_eligibility.py")
SPEC = importlib.util.spec_from_file_location("case_a_eligibility", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

TEST_NAME = "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse"
TEST_PATH = "src/Sample.Tests/SampleTests.cs"


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


def evidence(regression=False, builds=(101, 102)):
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
            TEST_NAME: {
                "count": len(builds),
                "assembly": "Sample.Tests--net11.0",
                "builds": list(builds),
                "evidence_build": builds[-1],
                "run_id": 2001,
                "result_id": 3001,
                "leg": "Linux_Test",
                "error": "stable-marker-123",
                "stack": "at SampleTests.ReturnsExpectedResponse()",
                "is_consistent_regression": regression,
            },
        },
        "source_b": {},
        "source_c": [],
        "source_c_truncated": False,
    }


def collect(root, data, pr_files_provider=lambda _: set()):
    serialized = json.dumps(data, separators=(",", ":")).encode()
    return MODULE.collect(
        data,
        serialized,
        root,
        [],
        "dotnet/aspnetcore",
        "refs/heads/main",
        subprocess.check_output(["git", "-C", root, "rev-parse", "HEAD"], text=True).strip(),
        pr_files_provider=pr_files_provider,
    )


def record(receipt):
    return receipt["tests"][TEST_NAME]


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


def initialize_repository(root, project_count=1):
    run(root, "git", "init", "-q")
    run(root, "git", "config", "user.email", "test@example.com")
    run(root, "git", "config", "user.name", "Test")
    project = root / "src/Sample.Tests"
    project.mkdir(parents=True)
    for index in range(project_count):
        suffix = "" if index == 0 else str(index + 1)
        (project / f"Sample.Tests{suffix}.csproj").write_text(
            "<Project />",
            encoding="utf-8",
        )
    file_path = root / TEST_PATH
    file_path.write_text(source(), encoding="utf-8")
    return project, file_path


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

        result = record(collect(root, evidence()))
        assert result["status"] == "ineligible", result
        assert result["originating_case"] == "case-b"
        assert result["source_resolution"]["path"] == (
            "src/Sample.Tests/RenamedSampleTests.cs"
        )
        assert result["latest_quarantine_transition"] == "removed"
        assert result["cutoff"]["commit"] == removal_commit

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
        assert result["cutoff"]["commit"] == removal_commit

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
        assert result["latest_quarantine_transition"] == "ambiguous"
        assert "quarantine-history-ambiguous" in result["reasons"]


def run_output(root, *args):
    return subprocess.check_output(
        args,
        cwd=root,
        text=True,
    ).strip()


def main():
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

        one_failure = record(collect(root, evidence(builds=(101,))))
        assert one_failure["status"] == "ineligible"
        assert "fewer-than-two-post-cutoff-failures" in one_failure["reasons"]

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

    print("All Case A eligibility collector tests passed.")


if __name__ == "__main__":
    main()
