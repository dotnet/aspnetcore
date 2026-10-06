#!/usr/bin/env python3

import importlib.util
import json
import os
import pathlib
import re
import subprocess
import tempfile


SCRIPT = pathlib.Path(__file__).with_name("validate_pull_request_outputs.py")
SPEC = importlib.util.spec_from_file_location(
    "validate_quarantine_pull_requests",
    SCRIPT,
)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

REPOSITORY = "dotnet/aspnetcore"
REF = "refs/heads/main"
TEST_NAME = "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse"
TEST_PATH = "src/Sample.Tests/SampleTests.cs"
TYPE_NAME = "Microsoft.AspNetCore.Tests.SampleTests"


def run(root, *args):
    return subprocess.run(
        args,
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()


def gh_aw_sanitize(value):
    value = re.sub(r'[/\\:*?"<>|]', "-", value)
    value = re.sub(r"-{2,}", "-", value)
    if value.startswith("-"):
        value = value[1:]
    if value.endswith("-"):
        value = value[:-1]
    return (value or "unknown").lower()


def source(attribute=""):
    attribute_line = f"    {attribute}\n" if attribute else ""
    return f"""namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{{
{attribute_line}    public void ReturnsExpectedResponse()
    {{
    }}
}}
"""


def theory_source(data_attribute="[InlineData(HttpProtocols.Http3)]"):
    return f"""namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{{
    [ConditionalTheory]
    {data_attribute}
    public void ReturnsExpectedResponse(HttpProtocols protocol)
    {{
    }}
}}
"""


def source_two_quarantined_methods():
    return """namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
    public void ReturnsExpectedResponse()
    {
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/1")]
    public void ReturnsAnotherResponse()
    {
    }
}
"""


def source_two_unquarantined_methods():
    return """namespace Microsoft.AspNetCore.Tests;

public class SampleTests
{
    public void ReturnsExpectedResponse()
    {
    }

    public void ReturnsAnotherResponse()
    {
    }
}
"""


def initialize_repository(root, initial_source=None):
    run(root, "git", "init", "-q")
    run(root, "git", "config", "user.email", "test@example.com")
    run(root, "git", "config", "user.name", "Test")
    project = root / "src" / "Sample.Tests"
    project.mkdir(parents=True)
    (project / "Sample.Tests.csproj").write_text(
        "<Project />\n",
        encoding="utf-8",
    )
    file_path = root / TEST_PATH
    file_path.write_text(
        source() if initial_source is None else initial_source,
        encoding="utf-8",
    )
    run(root, "git", "add", ".")
    run(root, "git", "commit", "-qm", "Initial source")
    return file_path, run(root, "git", "rev-parse", "HEAD")


def receipts(commit, record, history_targets=None):
    identity = {
        "schema_version": 1,
        "repository": REPOSITORY,
        "ref": REF,
        "commit": commit,
        "history_ref": "refs/remotes/origin/main",
        "history_commit": commit,
    }
    eligibility = {
        **identity,
        "tests": {TEST_NAME: record},
    }
    history = {
        **identity,
        "targets": history_targets or [],
    }
    return eligibility, history


def case_a_record(operating_systems=None):
    return {
        "status": "eligible",
        "originating_case": "case-a",
        "source_resolution": {
            "status": "exact",
            "path": TEST_PATH,
            "type": TYPE_NAME,
            "method": "ReturnsExpectedResponse",
        },
        "current_quarantine_state": "not-quarantined",
        "latest_quarantine_transition": "none",
        "quarantine_operating_systems": operating_systems or [
            "OperatingSystems.Linux",
            "OperatingSystems.MacOSX",
            "OperatingSystems.Windows",
        ],
    }


def case_b_record(issue=1, operating_systems=None):
    return {
        "status": "ineligible",
        "originating_case": "case-b",
        "case_b_eligible": True,
        "case_b_issue": issue,
        "source_resolution": {
            "status": "exact",
            "path": TEST_PATH,
            "type": TYPE_NAME,
            "method": "ReturnsExpectedResponse",
        },
        "current_quarantine_state": "not-quarantined",
        "latest_quarantine_transition": "removed",
        "quarantine_operating_systems": operating_systems or [
            "OperatingSystems.Linux",
            "OperatingSystems.MacOSX",
            "OperatingSystems.Windows",
        ],
    }


def data_record(case, issue=1, operating_systems=None):
    record = (
        case_a_record(operating_systems)
        if case == "case-a"
        else case_b_record(issue, operating_systems)
    )
    record["source_resolution"]["matching_inline_data"] = {
        "data": "HttpProtocols.Http3",
        "values": ["Http3"],
    }
    return record


def write_json(path, value):
    path.write_text(
        json.dumps(value, separators=(",", ":")),
        encoding="utf-8",
    )


def create_patch(
    root,
    transport,
    branch,
    updated_source,
    extra_file=None,
    rename=None,
    executable=None,
):
    file_path = root / TEST_PATH
    file_path.write_text(updated_source, encoding="utf-8")
    if extra_file:
        extra_path = root / extra_file
        extra_original = extra_path.read_text(encoding="utf-8")
        extra_path.write_text(extra_original + "// unrelated\n", encoding="utf-8")
    if rename:
        run(root, "git", "mv", rename[0], rename[1])
    if executable:
        os.chmod(root / executable, 0o755)
    run(root, "git", "add", ".")
    run(root, "git", "commit", "-qm", f"Patch for {branch}")
    patch = subprocess.run(
        ["git", "format-patch", "--stdout", "HEAD^..HEAD"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    patch_path = transport / f"aw-{gh_aw_sanitize(branch)}.patch"
    patch_path.write_text(patch, encoding="utf-8")
    run(root, "git", "reset", "--hard", "HEAD^")
    return patch_path


def create_split_patch(root, transport, branch, updated_source):
    file_path = root / TEST_PATH
    current_source = file_path.read_text(encoding="utf-8")
    file_path.write_text(
        f"using Microsoft.AspNetCore.InternalTesting;\n{current_source}",
        encoding="utf-8",
    )
    run(root, "git", "add", ".")
    run(root, "git", "commit", "-qm", f"Add using for {branch}")
    file_path.write_text(updated_source, encoding="utf-8")
    run(root, "git", "add", ".")
    run(root, "git", "commit", "-qm", f"Add quarantine for {branch}")
    patch = subprocess.run(
        ["git", "format-patch", "--stdout", "HEAD~2..HEAD"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    patch_path = transport / f"aw-{gh_aw_sanitize(branch)}.patch"
    patch_path.write_text(patch, encoding="utf-8")
    run(root, "git", "reset", "--hard", "HEAD~2")
    return patch_path


def validate(
    root,
    commit,
    eligibility,
    history,
    agent_output,
    transport,
    current_main=None,
):
    evidence = root / "evidence"
    evidence.mkdir(exist_ok=True)
    eligibility_path = evidence / "eligibility.json"
    history_path = evidence / "history.json"
    output_path = evidence / "agent-output.json"
    write_json(eligibility_path, eligibility)
    write_json(history_path, history)
    write_json(output_path, agent_output)
    return MODULE.validate_outputs(
        root,
        output_path,
        eligibility_path,
        history_path,
        transport,
        REPOSITORY,
        REF,
        commit,
        current_main=current_main or commit,
    )


def pull_request(branch):
    return {
        "type": "create_pull_request",
        "branch": branch,
        "title": "Quarantine sample",
        "body": "Associated issue",
    }


def case_a_issue():
    return {
        "type": "create_quarantine_issue",
        "temporary_id": "aw_sample",
        "test_name": TEST_NAME,
    }


def assert_rejected(callback, message):
    try:
        callback()
    except MODULE.ValidationError as error:
        assert message in str(error), error
    else:
        raise AssertionError(f"Expected validation failure containing {message!r}")


def test_theory_target_binding(data="HttpProtocols.Http3", displayed="Http3", supported=True, provider=""):
    for theory in ("Theory", "ConditionalTheory"):
        for case in ("case-a", "case-b"):
            for rewrite_row in (True, False):
                with tempfile.TemporaryDirectory() as directory:
                    root = pathlib.Path(directory)
                    initial_source = theory_source().replace(
                        "[ConditionalTheory]", f"[{theory}]"
                    ).replace("HttpProtocols.Http3", data)
                    if provider:
                        initial_source = initial_source.replace(
                            f"[InlineData({data})]", f"[InlineData({data})]\n    {provider}"
                        )
                    _, commit = initialize_repository(root, initial_source)
                    record = case_a_record() if case == "case-a" else case_b_record()
                    record["source_resolution"] = MODULE.ELIGIBILITY.resolve_source(
                        root, TEST_NAME + f"(protocol: {displayed})"
                    )
                    eligibility, history = receipts(commit, record)
                    transport = root / "transport"
                    transport.mkdir()
                    branch = "test-quarantine/theory-binding"
                    issue = "#aw_sample" if case == "case-a" else "1"
                    arguments = (
                        f'"https://github.com/dotnet/aspnetcore/issues/{issue}", '
                        'OperatingSystems.Linux | OperatingSystems.MacOSX | '
                        'OperatingSystems.Windows'
                    )
                    replacement = (
                        f"[QuarantinedTestData({arguments}, {data})]"
                        if rewrite_row
                        else f"[QuarantinedTest({arguments})]\n"
                        f"    [InlineData({data})]"
                    )
                    create_patch(
                        root, transport, branch,
                        initial_source.replace(
                            f"[InlineData({data})]", replacement
                        ),
                    )
                    outputs = {"items": [pull_request(branch)]}
                    if case == "case-a":
                        outputs["items"].insert(0, case_a_issue())

                    def check():
                        return validate(
                            root, commit, eligibility, history, outputs, transport
                        )

                    if (
                        rewrite_row == (theory == "ConditionalTheory")
                        and (supported or theory == "Theory")
                    ):
                        result = check()
                        assert result[0]["operation"] == case, result
                    else:
                        assert_rejected(
                            check,
                            "not bound to an exact eligible test"
                            if case == "case-a"
                            else "not bound to one exact eligible test",
                        )
                    print(f"PASS {case}: {theory}, data={data}, row rewrite={rewrite_row}")


def test_commented_row_patch(case="case-a", comment=" // reason [detail]", changed_comment=None):
    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        reference = "#aw_sample" if case == "case-a" else "1"
        inline = "[InlineData(HttpProtocols.Http3)]"
        quarantine = (
            f'[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/{reference}", '
            "OperatingSystems.Linux, HttpProtocols.Http3)]"
        )
        before, after = (quarantine, inline) if case == "unquarantine" else (inline, quarantine)
        _, commit = initialize_repository(root, theory_source(before + comment))
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/commented-row"
        create_patch(
            root, transport, branch,
            theory_source(after + (comment if changed_comment is None else changed_comment)),
        )
        eligibility, history = receipts(
            commit,
            data_record("case-b" if case == "case-b" else "case-a",
                        operating_systems=["OperatingSystems.Linux"]),
            [{
                "scope": "data", "path": TEST_PATH, "type": TYPE_NAME,
                "method": "ReturnsExpectedResponse", "data": "HttpProtocols.Http3",
                "issue": 1, "status": "first-quarantine",
            }] if case == "unquarantine" else None,
        )
        items = [pull_request(branch)]
        if case == "case-a":
            items.insert(0, case_a_issue())

        def check():
            return validate(root, commit, eligibility, history, {"items": items}, transport)

        if changed_comment is None:
            result = check()
            assert result[0]["operation"] == case, result
        else:
            assert_rejected(check, "preserve trailing comments")
        print(f"PASS commented row: {case}, {comment!r}, changed={changed_comment!r}")


def test_row_comment_ownership():
    replacement = (
        '[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
        'OperatingSystems.Linux, 1)]'
    )
    swapped = (
        "@@ -1 +1 @@\n-[InlineData(1)] // first\n"
        f"+{replacement} // second\n"
        "@@ -10 +10 @@\n-[InlineData(1)] // second\n"
        f"+{replacement} // first\n"
    )
    assert_rejected(
        lambda: MODULE.changed_patch_lines(swapped), "preserve trailing comments"
    )
    data = '"https://example.test/path] // text"'
    actual = MODULE.changed_patch_lines(
        f"@@ -1 +1 @@\n-[InlineData({data})] // reason [\n"
        '+[QuarantinedTestData("https://github.com/dotnet/aspnetcore/issues/1", '
        f"OperatingSystems.Linux, {data})] // reason [\n"
    )
    assert actual == [("-", None, "inline", data), ("+", "1", "data", data)], actual


def main():
    test_theory_target_binding(provider="[MsQuicSupported]")
    test_theory_target_binding(
        supported=False, provider="[MsQuicSupported]\n    [MemberData(nameof(GetRows))]"
    )
    for provider in (
        '[MemberData(nameof(GetRows))]',
        '[ClassData(typeof(Rows))]',
        '[CustomRows]',
    ):
        test_theory_target_binding(supported=False, provider=provider)
    test_row_comment_ownership()
    for case in ("case-a", "case-b", "unquarantine"):
        for comment in (" // reason", " // reason [detail]", " // unmatched [",
                        " /* reason [detail] */"):
            test_commented_row_patch(case, comment)
        for comment, changed in [
            (" // reason", ""), ("", " // new reason"), (" // reason", " // changed reason"),
        ]:
            test_commented_row_patch(case, comment, changed)
    test_theory_target_binding()
    test_theory_target_binding("true", "True")
    test_theory_target_binding("-1L", "-1")
    test_theory_target_binding('nameof(HttpProtocols.Http3)', '"Http3"', supported=False)
    assert_rejected(
        lambda: MODULE.changed_patch_lines(
            "diff --git a/src/A.cs b/src/A.cs\n"
            "--- a/src/A.cs\n"
            "+++ b/src/A.cs\n"
            "@@ -1,0 +2 @@\n"
            "+++ b/src/A.cs\n"
        ),
        "non-quarantine change",
    )
    for reference in ("1", "#aw_sample"):
        assert_rejected(
            lambda reference=reference: MODULE.changed_patch_lines(
                "diff --git a/src/A.cs b/src/A.cs\n"
                "--- a/src/A.cs\n"
                "+++ b/src/A.cs\n"
                "@@ -1 +1,4 @@\n"
                "-[InlineData(HttpProtocols.Http3)]\n"
                "+[QuarantinedTestData(\n"
                f'+    "https://github.com/dotnet/aspnetcore/issues/{reference}",\n'
                "+    OperatingSystems.Linux,\n"
                "+    HttpProtocols.Http3)]\n"
            ),
            "require one-line attributes",
        )
    assert_rejected(
        lambda: MODULE.changed_patch_lines(
            "diff --git a/src/A.cs b/src/A.cs\n"
            "--- a/src/A.cs\n"
            "+++ b/src/A.cs\n"
            "@@ -1 +1 @@\n"
            "-[MemberData(nameof(Data))]\n"
            "+[QuarantinedTestData("
            '"https://github.com/dotnet/aspnetcore/issues/1", '
            "OperatingSystems.Linux, 1)]\n"
        ),
        "non-quarantine change",
    )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        file_path, commit = initialize_repository(root)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/Case-A"
        create_patch(
            root,
            transport,
            branch,
            source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
            ),
        )
        eligibility, history = receipts(commit, case_a_record())
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {
                "items": [
                    case_a_issue(),
                    pull_request(branch),
                ],
            },
            transport,
        )
        assert result == [{
            "branch": branch,
            "operation": "case-a",
            "test": TEST_NAME,
        }], result
        inherited = case_a_record()
        inherited["source_resolution"]["type"] = (
            "Microsoft.AspNetCore.Tests.DerivedSampleTests"
        )
        inherited["source_resolution"]["declaring_type"] = TYPE_NAME
        assert MODULE.exact_source_target(TEST_NAME, inherited) == (
            "method",
            TEST_PATH,
            TYPE_NAME,
            "ReturnsExpectedResponse",
            None,
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {"items": [case_a_issue()]},
                transport,
            ),
            "cannot run without a validated Case A pull request",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root, theory_source())
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/data-case-a"
        create_patch(
            root,
            transport,
            branch,
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]'
            ),
        )
        eligibility, history = receipts(
            commit,
            data_record(
                "case-a",
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {
                "items": [
                    case_a_issue(),
                    pull_request(branch),
                ],
            },
            transport,
        )
        assert result[0]["operation"] == "case-a", result
        wrong_operating_systems, _ = receipts(
            commit,
            data_record("case-a"),
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                wrong_operating_systems,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "do not match deterministic failure evidence",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root, theory_source())
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/data-case-b"
        create_patch(
            root,
            transport,
            branch,
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/1", '
                'OperatingSystems.Windows | OperatingSystems.Linux, '
                'HttpProtocols.Http3)]'
            ),
        )
        eligibility, history = receipts(
            commit,
            data_record(
                "case-b",
                operating_systems=[
                    "OperatingSystems.Linux",
                    "OperatingSystems.Windows",
                ],
            ),
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {"items": [pull_request(branch)]},
            transport,
        )
        assert result[0]["operation"] == "case-b", result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        quarantined_row = (
            '[QuarantinedTestData('
            '"https://github.com/dotnet/aspnetcore/issues/1", '
            'OperatingSystems.Linux, HttpProtocols.Http3)]'
        )
        _, commit = initialize_repository(
            root,
            theory_source(quarantined_row),
        )
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/data-unquarantine"
        create_patch(root, transport, branch, theory_source())
        history_target = {
            "scope": "data",
            "path": TEST_PATH,
            "type": TYPE_NAME,
            "method": "ReturnsExpectedResponse",
            "data": "HttpProtocols.Http3",
            "issue": 1,
            "status": "first-quarantine",
        }
        eligibility, history = receipts(
            commit,
            data_record("case-a"),
            [history_target],
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {"items": [pull_request(branch)]},
            transport,
        )
        assert result[0]["operation"] == "unquarantine", result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root, theory_source())
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/data-mismatch"
        create_patch(
            root,
            transport,
            branch,
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample", '
                'OperatingSystems.Linux, HttpProtocols.Http2)]'
            ),
        )
        eligibility, history = receipts(
            commit,
            data_record(
                "case-a",
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "not bound to an exact eligible test",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        two_rows = theory_source(
            "[InlineData(HttpProtocols.Http3)]\n"
            "    [InlineData(HttpProtocols.Http2)]"
        )
        _, commit = initialize_repository(root, two_rows)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/extra-data-row"
        create_patch(
            root,
            transport,
            branch,
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample", '
                'OperatingSystems.Linux, HttpProtocols.Http3)]\n'
                '    [QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample", '
                'OperatingSystems.Linux, HttpProtocols.Http2)]'
            ),
        )
        eligibility, history = receipts(
            commit,
            data_record(
                "case-a",
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "must add exactly one target",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root, theory_source())
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/invalid-operating-system"
        create_patch(
            root,
            transport,
            branch,
            theory_source(
                '[QuarantinedTestData('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample", '
                'OperatingSystems.FreeBSD, HttpProtocols.Http3)]'
            ),
        )
        eligibility, history = receipts(
            commit,
            data_record(
                "case-a",
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "unique supported OperatingSystems flags",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/split-valid-patch"
        create_split_patch(
            root,
            transport,
            branch,
            "using Microsoft.AspNetCore.InternalTesting;\n"
            + source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
            ),
        )
        eligibility, history = receipts(commit, case_a_record())
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {
                "items": [
                    case_a_issue(),
                    pull_request(branch),
                ],
            },
            transport,
        )
        assert result[0]["operation"] == "case-a", result

        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {"items": [pull_request(branch)]},
                transport,
            ),
            "must match one create_quarantine_issue",
        )

        other = root / "src" / "Sample.Tests" / "Other.cs"
        other.write_text("public class Other {}\n", encoding="utf-8")
        run(root, "git", "add", ".")
        run(root, "git", "commit", "-qm", "Advance test project")
        advanced_main = run(root, "git", "rev-parse", "HEAD")
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
                current_main=advanced_main,
            ),
            "changed a validated test project",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/case-b"
        create_patch(
            root,
            transport,
            branch,
            source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/1", '
                "OperatingSystems.Linux)]"
            ),
        )
        eligibility, history = receipts(
            commit,
            case_b_record(
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {"items": [pull_request(branch)]},
            transport,
        )
        assert result[0]["operation"] == "case-b", result

        wrong_eligibility, _ = receipts(
            commit,
            case_b_record(
                issue=2,
                operating_systems=["OperatingSystems.Linux"],
            ),
        )
        assert_rejected(
            lambda: validate(
                root,
                commit,
                wrong_eligibility,
                history,
                {"items": [pull_request(branch)]},
                transport,
            ),
            "not bound to one exact eligible test",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        quarantined = source(
            '[QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1", '
            "OperatingSystems.Windows | OperatingSystems.MacOSX)]"
        )
        _, commit = initialize_repository(root, quarantined)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/unquarantine"
        create_patch(root, transport, branch, source())
        history_target = {
            "scope": "method",
            "path": TEST_PATH,
            "type": TYPE_NAME,
            "method": "ReturnsExpectedResponse",
            "issue": 1,
            "status": "first-quarantine",
        }
        eligibility, history = receipts(
            commit,
            case_a_record(),
            [history_target],
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {"items": [pull_request(branch)]},
            transport,
        )
        assert result[0]["operation"] == "unquarantine", result

        history["targets"][0]["status"] = "re-quarantined"
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {"items": [pull_request(branch)]},
                transport,
            ),
            "not an exact first quarantine",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        quarantined = source(
            '[QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/1", '
            "OperatingSystems.Linux)]"
        )
        _, commit = initialize_repository(root, quarantined)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/change-operating-system"
        create_patch(
            root,
            transport,
            branch,
            source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/1", '
                "OperatingSystems.Windows)]"
            ),
        )
        eligibility, history = receipts(commit, case_a_record())
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {"items": [pull_request(branch)]},
                transport,
            ),
            "mixes quarantine additions and removals",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(
            root,
            source_two_quarantined_methods(),
        )
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/unquarantine-group"
        create_patch(
            root,
            transport,
            branch,
            source_two_unquarantined_methods(),
        )
        eligibility, history = receipts(
            commit,
            case_a_record(),
            [
                {
                    "scope": "method",
                    "path": TEST_PATH,
                    "type": TYPE_NAME,
                    "method": "ReturnsExpectedResponse",
                    "issue": 1,
                    "status": "first-quarantine",
                },
                {
                    "scope": "method",
                    "path": TEST_PATH,
                    "type": TYPE_NAME,
                    "method": "ReturnsAnotherResponse",
                    "issue": 1,
                    "status": "first-quarantine",
                },
            ],
        )
        result = validate(
            root,
            commit,
            eligibility,
            history,
            {"items": [pull_request(branch)]},
            transport,
        )
        assert result[0]["targets"] == 2, result

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root)
        extra = root / "src" / "Sample.Tests" / "Other.cs"
        extra.write_text("public class Other {}\n", encoding="utf-8")
        run(root, "git", "add", ".")
        run(root, "git", "commit", "-qm", "Add other source")
        commit = run(root, "git", "rev-parse", "HEAD")
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/unrelated"
        create_patch(
            root,
            transport,
            branch,
            source(
                '[QuarantinedTest('
                '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
            ),
            "src/Sample.Tests/Other.cs",
        )
        eligibility, history = receipts(commit, case_a_record())
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "non-quarantine change",
        )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        hidden_source = (
            source().rsplit("\n}\n", 1)[0]
            + "\n    [Fact] public void HiddenTest() { }\n"
            + "}\n"
        )
        _, commit = initialize_repository(root, hidden_source)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/extra-attribute"
        injected_source = hidden_source.replace(
            "    public void ReturnsExpectedResponse()",
            '    [QuarantinedTest("https://github.com/dotnet/'
            'aspnetcore/issues/#aw_sample")]\n'
            "    public void ReturnsExpectedResponse()",
        ).replace(
            "    [Fact] public void HiddenTest() { }",
            '    [QuarantinedTest("https://github.com/dotnet/'
            'aspnetcore/issues/999")]\n'
            "    [Fact] public void HiddenTest() { }",
        )
        create_patch(root, transport, branch, injected_source)
        eligibility, history = receipts(commit, case_a_record())
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "attribute lines do not match its one derived target",
        )

    for operation in ("rename", "mode"):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            _, commit = initialize_repository(root)
            extra = root / "src" / "Sample.Tests" / "Production.cs"
            extra.write_text("public class Production {}\n", encoding="utf-8")
            run(root, "git", "add", ".")
            run(root, "git", "commit", "-qm", "Add production source")
            commit = run(root, "git", "rev-parse", "HEAD")
            transport = root / "transport"
            transport.mkdir()
            branch = f"test-quarantine/{operation}"
            create_patch(
                root,
                transport,
                branch,
                source(
                    '[QuarantinedTest('
                    '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
                ),
                rename=(
                    "src/Sample.Tests/Production.cs",
                    "src/Sample.Tests/Renamed.cs",
                ) if operation == "rename" else None,
                executable=(
                    "src/Sample.Tests/Production.cs"
                    if operation == "mode"
                    else None
                ),
            )
            eligibility, history = receipts(commit, case_a_record())
            expected = (
                "rename, copy, add, delete, or type change"
                if operation == "rename"
                else "changes file metadata"
            )
            assert_rejected(
                lambda: validate(
                    root,
                    commit,
                    eligibility,
                    history,
                    {
                        "items": [
                            case_a_issue(),
                            pull_request(branch),
                        ],
                    },
                    transport,
                ),
                expected,
            )

    with tempfile.TemporaryDirectory() as directory:
        root = pathlib.Path(directory)
        _, commit = initialize_repository(root)
        transport = root / "transport"
        transport.mkdir()
        branch = "test-quarantine/attribute-code"
        injected_source = source(
            '[QuarantinedTest('
            '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")]'
        ).replace(
            "    public void ReturnsExpectedResponse()",
            "    [QuarantinedTest("
            '"https://github.com/dotnet/aspnetcore/issues/#aw_sample")] '
            "public static int Injected = 1;\n"
            "    public void ReturnsExpectedResponse()",
        )
        create_patch(root, transport, branch, injected_source)
        eligibility, history = receipts(commit, case_a_record())
        assert_rejected(
            lambda: validate(
                root,
                commit,
                eligibility,
                history,
                {
                    "items": [
                        case_a_issue(),
                        pull_request(branch),
                    ],
                },
                transport,
            ),
            "non-quarantine change",
        )

    print("All quarantine pull request validator tests passed.")


if __name__ == "__main__":
    main()
