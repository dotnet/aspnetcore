#!/usr/bin/env python3

import datetime
import importlib.util
import json
import os
import pathlib
import subprocess
import tempfile


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


def main():
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
        case_b = record(collect(root, evidence()))
        assert case_b["status"] == "ineligible"
        assert case_b["originating_case"] == "case-b"

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

    print("All Case A eligibility collector tests passed.")


if __name__ == "__main__":
    main()
