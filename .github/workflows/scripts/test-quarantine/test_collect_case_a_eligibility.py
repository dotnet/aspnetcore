#!/usr/bin/env python3

import datetime
import importlib.util
import io
import json
import os
import pathlib
import subprocess
import tempfile
import textwrap
from unittest import mock


SCRIPT = pathlib.Path(__file__).with_name("collect_case_a_eligibility.py")
SPEC = importlib.util.spec_from_file_location("case_a_eligibility", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

TEST_NAME = "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse"
TEST_PATH = "src/Sample.Tests/SampleTests.cs"
DERIVED_TEST_NAME = (
    "Microsoft.AspNetCore.Server.Tests."
    "DerivedTests.ReturnsExpectedResponse"
)
QUARANTINE_ATTRIBUTE = '[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]'


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
                "leg": "Linux_Test",
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


def collect(root, data, pr_files_provider=lambda _: set(), module=None):
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
    )


def record(receipt, test_name=TEST_NAME):
    return receipt["tests"][test_name]


def collect_result(
    root,
    data,
    *,
    test_name=TEST_NAME,
    pr_files_provider=lambda _: set(),
    module=None,
):
    return record(
        collect(root, data, pr_files_provider=pr_files_provider, module=module),
        test_name,
    )


def assert_case_b(result, removal_commit):
    assert result["status"] == "ineligible", result
    assert result["originating_case"] == "case-b", result
    assert result["current_quarantine_state"] == "not-quarantined", result
    assert result["latest_quarantine_transition"] == "removed", result
    assert result["cutoff"]["commit"] == removal_commit, result
    assert result["cutoff"]["reason"] == "latest-quarantine-transition", result


def assert_already_quarantined(result):
    assert result["status"] == "ineligible", result
    assert result["originating_case"] == "already-quarantined", result
    assert result["current_quarantine_state"] == "quarantined", result


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


def test_workflow_runner_temp():
    workflow = (SCRIPT.parents[2] / "test-quarantine.md").read_text(encoding="utf-8")
    for step_name in ["Aggregate Part 1 failures", "Collect deterministic Case A eligibility"]:
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
    test_partial_runner_conflicting_bases_in_same_project_fail_closed()
    test_partial_sibling_edits_do_not_expand_source_b_or_freshness_scope()

    print("All Case A eligibility collector tests passed.")


if __name__ == "__main__":
    main()
