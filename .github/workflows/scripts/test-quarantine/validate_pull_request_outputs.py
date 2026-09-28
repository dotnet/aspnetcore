#!/usr/bin/env python3

import argparse
import collections
import importlib.util
import json
import os
import pathlib
import re
import subprocess
import tempfile


SCRIPT_DIRECTORY = pathlib.Path(__file__).parent
ELIGIBILITY_SCRIPT = SCRIPT_DIRECTORY / "collect_case_a_eligibility.py"
SPEC = importlib.util.spec_from_file_location(
    "test_quarantine_eligibility",
    ELIGIBILITY_SCRIPT,
)
ELIGIBILITY = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(ELIGIBILITY)

TEMPORARY_ID = re.compile(r"^aw_[A-Za-z0-9_]{3,12}$")
SHA = re.compile(r"^[0-9a-f]{40}$")
USING_LINE = "using Microsoft.AspNetCore.InternalTesting;"
ATTRIBUTE_LINE = re.compile(
    r"^\[\s*(?:assembly\s*:\s*)?"
    r"(?:[A-Za-z_][A-Za-z0-9_]*\.)*"
    r"QuarantinedTest(?:Attribute)?\s*\(\s*"
    r'"https://github\.com/dotnet/aspnetcore/issues/'
    r'(?P<reference>\d+|#aw_[A-Za-z0-9_]{3,12})"\s*\)\s*\]$'
)


class ValidationError(ValueError):
    pass


def run(root, *args, check=True):
    return subprocess.run(
        args,
        cwd=root,
        check=check,
        capture_output=True,
        text=True,
    )


def load_json(path):
    try:
        return json.loads(pathlib.Path(path).read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise ValidationError(f"Required file is missing: {path}") from error
    except json.JSONDecodeError as error:
        raise ValidationError(f"Invalid JSON in {path}: {error}") from error


def items(payload, item_type):
    raw_items = payload.get("items") if isinstance(payload, dict) else None
    if not isinstance(raw_items, list):
        raise ValidationError("Agent output must contain an items array")
    return [
        item for item in raw_items
        if isinstance(item, dict) and item.get("type") == item_type
    ]


def sanitize_branch(branch):
    value = re.sub(r'[/\\:*?"<>|]', "-", branch)
    value = re.sub(r"-{2,}", "-", value)
    if value.startswith("-"):
        value = value[1:]
    if value.endswith("-"):
        value = value[:-1]
    return (value or "unknown").lower()


def transport_path(branch, transport_directory, repository):
    sanitized_branch = sanitize_branch(branch)
    sanitized_repository = sanitize_branch(repository)
    candidates = [
        pathlib.Path(
            transport_directory,
            f"aw-{sanitized_repository}-{sanitized_branch}.patch",
        ),
        pathlib.Path(
            transport_directory,
            f"aw-{sanitized_branch}.patch",
        ),
    ]
    matches = [path for path in candidates if path.is_file()]
    if len(matches) != 1:
        raise ValidationError(
            f"Expected one authoritative patch for branch {branch!r}; "
            f"found {len(matches)}"
        )
    bundle_matches = [
        path.with_suffix(".bundle")
        for path in candidates
        if path.with_suffix(".bundle").is_file()
    ]
    if bundle_matches:
        raise ValidationError(
            f"Bundle transport is not allowed for validated branch {branch!r}"
        )
    return matches[0]


def changed_patch_lines(patch):
    changed = []
    attributes = []
    in_hunk = False
    for line in patch.splitlines():
        if line.startswith("diff --git "):
            in_hunk = False
            continue
        if not in_hunk and line.startswith(("--- ", "+++ ")):
            continue
        if line.startswith("@@"):
            in_hunk = True
            continue
        if not in_hunk:
            continue
        if line.startswith("\\ No newline at end of file"):
            continue
        if not line.startswith(("+", "-")):
            continue
        content = line[1:].strip()
        changed.append((line[0], content))
    for operation, content in changed:
        attribute = ATTRIBUTE_LINE.fullmatch(content)
        if attribute:
            attributes.append((operation, attribute.group("reference")))
            continue
        if operation == "+" and content == USING_LINE:
            continue
        raise ValidationError(
            f"Patch contains a non-quarantine change: {operation}{content}"
        )
    return attributes


def target_key(target):
    return (
        target["scope"],
        target["path"],
        target.get("type"),
        target.get("method"),
    )


def target_map(root, source_index):
    result = {}
    for target in ELIGIBILITY.current_quarantine_targets(source_index):
        project_root = pathlib.Path(target["project_root"])
        if project_root.is_absolute():
            target["project_root"] = str(
                project_root.relative_to(root)
            ).replace(os.sep, "/")
        key = target_key(target)
        if key in result:
            raise ValidationError(f"Duplicate current quarantine target: {key}")
        result[key] = target
    return result


def validate_receipts(eligibility, history, repository, ref, commit):
    for name, receipt in (
        ("eligibility", eligibility),
        ("requarantine history", history),
    ):
        if (
            receipt.get("schema_version") != 1
            or receipt.get("repository") != repository
            or receipt.get("ref") != ref
            or receipt.get("commit") != commit
            or receipt.get("history_ref") != "refs/remotes/origin/main"
            or not SHA.fullmatch(str(receipt.get("history_commit", "")))
        ):
            raise ValidationError(
                f"Deterministic {name} receipt identity does not match this run"
            )
    if eligibility["history_commit"] != history["history_commit"]:
        raise ValidationError(
            "Eligibility and requarantine history receipts disagree on main"
        )
    return eligibility["history_commit"]


def exact_source_target(test_name, record):
    source = record.get("source_resolution")
    if not isinstance(source, dict) or source.get("status") != "exact":
        return None
    return (
        "method",
        source.get("path"),
        source.get("declaring_type") or source.get("type"),
        source.get("method"),
    )


def validate_addition(target, eligibility, issue_items):
    reference = target["reference"]
    key = target_key(target)
    tests = eligibility.get("tests")
    if not isinstance(tests, dict):
        raise ValidationError("Eligibility receipt lacks test records")

    if not isinstance(reference, str):
        raise ValidationError(
            f"Quarantine target lacks one supported issue reference: {key}"
        )
    if reference.startswith("#"):
        temporary_id = reference[1:]
        if not TEMPORARY_ID.fullmatch(temporary_id):
            raise ValidationError(
                f"Invalid temporary issue reference for quarantine target: {reference}"
            )
        matches = [
            item for item in issue_items
            if item.get("temporary_id") == temporary_id
        ]
        if len(matches) != 1:
            raise ValidationError(
                f"Case A target must match one create_quarantine_issue item: {key}"
            )
        test_name = matches[0].get("test_name")
        record = tests.get(test_name)
        if (
            not isinstance(record, dict)
            or record.get("status") != "eligible"
            or record.get("originating_case") != "case-a"
            or record.get("current_quarantine_state") != "not-quarantined"
            or record.get("latest_quarantine_transition") != "none"
            or exact_source_target(test_name, record) != key
        ):
            raise ValidationError(
                f"Case A addition is not bound to an exact eligible test: {key}"
            )
        return ("case-a", test_name)

    if not reference.isdigit():
        raise ValidationError(f"Unsupported quarantine issue reference: {reference}")
    issue = int(reference)
    matches = [
        test_name
        for test_name, record in tests.items()
        if (
            isinstance(record, dict)
            and record.get("originating_case") == "case-b"
            and record.get("case_b_eligible") is True
            and record.get("case_b_issue") == issue
            and record.get("current_quarantine_state") == "not-quarantined"
            and record.get("latest_quarantine_transition") == "removed"
            and exact_source_target(test_name, record) == key
        )
    ]
    if len(matches) != 1:
        raise ValidationError(
            f"Case B addition is not bound to one exact eligible test: {key}"
        )
    return ("case-b", matches[0])


def history_target_map(history):
    result = {}
    targets = history.get("targets")
    if not isinstance(targets, list):
        raise ValidationError("Requarantine history receipt lacks targets")
    for target in targets:
        if not isinstance(target, dict):
            raise ValidationError("Invalid requarantine history target")
        key = target_key(target)
        if key in result:
            raise ValidationError(f"Duplicate requarantine history target: {key}")
        result[key] = target
    return result


def validate_removals(removed, history_targets):
    issues = set()
    for key, target in removed.items():
        reference = target["reference"]
        if not isinstance(reference, str) or not reference.isdigit():
            raise ValidationError(
                f"Unquarantine target lacks a numeric issue reference: {key}"
            )
        history_target = history_targets.get(key)
        issue = int(reference)
        if (
            not isinstance(history_target, dict)
            or history_target.get("status") != "first-quarantine"
            or history_target.get("issue") != issue
        ):
            raise ValidationError(
                f"Unquarantine target is not an exact first quarantine: {key}"
            )
        issues.add(issue)
    if len(issues) != 1:
        raise ValidationError(
            "One unquarantine pull request must contain targets for one issue"
        )


def changed_project_roots(root, base_commit, current_main, project_roots):
    if current_main == base_commit:
        return set()
    result = run(
        root,
        "git",
        "diff",
        "--name-only",
        f"{base_commit}..{current_main}",
    )
    changed = set(result.stdout.splitlines())
    return {
        project_root
        for project_root in project_roots
        if any(
            path == project_root or path.startswith(f"{project_root}/")
            for path in changed
        )
    }


def validate_commit_changes(root, base_commit, head_commit, branch):
    commits = run(
        root,
        "git",
        "rev-list",
        "--reverse",
        f"{base_commit}..{head_commit}",
    ).stdout.splitlines()
    if not commits:
        raise ValidationError(
            f"Patch for branch {branch!r} does not contain any commits"
        )
    paths = set()
    attribute_changes = []
    parent = base_commit
    for commit in commits:
        status_output = run(
            root,
            "git",
            "diff",
            "--name-status",
            "--no-renames",
            "-z",
            parent,
            commit,
        ).stdout
        status_parts = status_output.split("\0")
        if status_parts and status_parts[-1] == "":
            status_parts.pop()
        if len(status_parts) % 2 != 0:
            raise ValidationError(
                f"Unable to parse changed paths for {branch!r}"
            )
        status_entries = list(zip(
            status_parts[0::2],
            status_parts[1::2],
        ))
        if not status_entries or any(
            status != "M" for status, _ in status_entries
        ):
            raise ValidationError(
                f"Patch for branch {branch!r} contains a rename, copy, "
                "add, delete, or type change"
            )
        paths.update(path for _, path in status_entries)
        summary = run(
            root,
            "git",
            "diff",
            "--summary",
            parent,
            commit,
        ).stdout.strip()
        if summary:
            raise ValidationError(
                f"Patch for branch {branch!r} changes file metadata: {summary}"
            )
        commit_diff = run(
            root,
            "git",
            "diff",
            "--unified=0",
            "--no-renames",
            parent,
            commit,
        ).stdout
        attribute_changes.extend(changed_patch_lines(commit_diff))
        parent = commit
    if not attribute_changes:
        raise ValidationError("Patch does not change a QuarantinedTest attribute")
    return paths, attribute_changes


def validate_outputs(
    repo_root,
    agent_output_path,
    eligibility_path,
    history_path,
    transport_directory,
    repository,
    ref,
    commit,
    current_main=None,
):
    repo_root = pathlib.Path(repo_root).resolve()
    payload = load_json(agent_output_path)
    eligibility = load_json(eligibility_path)
    history = load_json(history_path)
    history_commit = validate_receipts(
        eligibility,
        history,
        repository,
        ref,
        commit,
    )
    pull_requests = items(payload, "create_pull_request")
    issue_items = items(payload, "create_quarantine_issue")
    if not pull_requests:
        if issue_items:
            raise ValidationError(
                "create_quarantine_issue cannot run without a validated "
                "Case A pull request"
            )
        return []
    history_targets = history_target_map(history)

    branches = [item.get("branch") for item in pull_requests]
    if any(not isinstance(branch, str) or not branch for branch in branches):
        raise ValidationError("Every create_pull_request item must name its branch")
    if len(set(branches)) != len(branches):
        raise ValidationError("Each create_pull_request item must use a unique branch")
    sanitized_branches = [sanitize_branch(branch) for branch in branches]
    if len(set(sanitized_branches)) != len(sanitized_branches):
        raise ValidationError(
            "create_pull_request branch names collide after gh-aw sanitization"
        )

    patches = []
    for item in pull_requests:
        patch_path = transport_path(
            item["branch"],
            transport_directory,
            repository,
        )
        patch = patch_path.read_text(encoding="utf-8")
        if not patch.strip():
            raise ValidationError(
                f"Patch for branch {item['branch']!r} is empty"
            )
        patches.append((item, patch_path))

    if current_main is None:
        run(repo_root, "git", "fetch", "--no-tags", "origin", "main")
        current_main = run(
            repo_root,
            "git",
            "rev-parse",
            "refs/remotes/origin/main",
        ).stdout.strip()
    if not SHA.fullmatch(current_main):
        raise ValidationError("Unable to resolve the current origin/main commit")

    summaries = []
    claimed_targets = set()
    claimed_issue_ids = set()
    with tempfile.TemporaryDirectory() as directory:
        worktree = pathlib.Path(directory, "worktree")
        run(
            repo_root,
            "git",
            "worktree",
            "add",
            "--detach",
            str(worktree),
            history_commit,
        )
        try:
            run(
                worktree,
                "git",
                "config",
                "user.name",
                "test-quarantine-validator",
            )
            run(
                worktree,
                "git",
                "config",
                "user.email",
                "test-quarantine-validator@users.noreply.github.com",
            )
            before = target_map(
                worktree,
                ELIGIBILITY.build_source_index(worktree),
            )
            for item, patch_path in patches:
                run(worktree, "git", "reset", "--hard", history_commit)
                run(worktree, "git", "clean", "-fd")
                applied = run(
                    worktree,
                    "git",
                    "am",
                    "--3way",
                    str(patch_path),
                    check=False,
                )
                if applied.returncode != 0:
                    run(
                        worktree,
                        "git",
                        "am",
                        "--abort",
                        check=False,
                    )
                    raise ValidationError(
                        f"Patch for branch {item['branch']!r} does not apply "
                        "to the deterministic main snapshot: "
                        f"{applied.stderr.strip()}"
                    )
                patch_head = run(
                    worktree,
                    "git",
                    "rev-parse",
                    "HEAD",
                ).stdout
                paths, attribute_changes = validate_commit_changes(
                    worktree,
                    history_commit,
                    patch_head.strip(),
                    item["branch"],
                )
                after = target_map(
                    worktree,
                    ELIGIBILITY.build_source_index(worktree),
                )
                removed = {
                    key: target for key, target in before.items()
                    if key not in after or after[key]["reference"] != target["reference"]
                }
                added = {
                    key: target for key, target in after.items()
                    if key not in before or before[key]["reference"] != target["reference"]
                }
                if removed and added:
                    raise ValidationError(
                        f"Branch {item['branch']!r} mixes quarantine additions "
                        "and removals or changes an issue reference"
                    )
                if not removed and not added:
                    raise ValidationError(
                        f"Branch {item['branch']!r} has no derived quarantine change"
                    )
                targets = set(removed) | set(added)
                if claimed_targets.intersection(targets):
                    raise ValidationError(
                        f"Multiple pull requests claim the same quarantine target: "
                        f"{claimed_targets.intersection(targets)}"
                    )
                claimed_targets.update(targets)

                project_roots = {
                    (removed.get(key) or added[key])["project_root"]
                    for key in targets
                }
                stale_roots = changed_project_roots(
                    repo_root,
                    history_commit,
                    current_main,
                    project_roots,
                )
                if stale_roots:
                    raise ValidationError(
                        "origin/main changed a validated test project after "
                        f"evidence collection: {sorted(stale_roots)}"
                    )
                target_paths = {
                    (removed.get(key) or added[key])["path"]
                    for key in targets
                }
                if not paths.issubset(target_paths):
                    raise ValidationError(
                        f"Patch changes files outside its quarantine targets: "
                        f"{sorted(paths - target_paths)}"
                    )

                if added:
                    if len(added) != 1:
                        raise ValidationError(
                            "A quarantine pull request must add exactly one target"
                        )
                    case, test_name = validate_addition(
                        next(iter(added.values())),
                        eligibility,
                        issue_items,
                    )
                    reference = next(iter(added.values()))["reference"]
                    if attribute_changes != [("+", reference)]:
                        raise ValidationError(
                            "Quarantine pull request attribute lines do not "
                            "match its one derived target"
                        )
                    if case == "case-a":
                        claimed_issue_ids.add(reference[1:])
                    summaries.append({
                        "branch": item["branch"],
                        "operation": case,
                        "test": test_name,
                    })
                else:
                    expected_attributes = collections.Counter(
                        ("-", target["reference"])
                        for target in removed.values()
                    )
                    if collections.Counter(attribute_changes) != expected_attributes:
                        raise ValidationError(
                            "Unquarantine pull request attribute lines do not "
                            "match its derived targets"
                        )
                    validate_removals(removed, history_targets)
                    summaries.append({
                        "branch": item["branch"],
                        "operation": "unquarantine",
                        "targets": len(removed),
                    })
        finally:
            run(
                repo_root,
                "git",
                "worktree",
                "remove",
                "--force",
                str(worktree),
                check=False,
            )
    requested_issue_ids = {
        item.get("temporary_id")
        for item in issue_items
        if isinstance(item.get("temporary_id"), str)
    }
    if (
        len(requested_issue_ids) != len(issue_items)
        or any(
            not TEMPORARY_ID.fullmatch(temporary_id)
            for temporary_id in requested_issue_ids
        )
    ):
        raise ValidationError(
            "create_quarantine_issue items must have unique valid temporary IDs"
        )
    if requested_issue_ids != claimed_issue_ids:
        raise ValidationError(
            "Every create_quarantine_issue item must be bound to exactly one "
            "validated Case A pull request"
        )
    return summaries


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--agent-output", required=True)
    parser.add_argument("--evidence-directory", required=True)
    parser.add_argument("--transport-directory", default="/tmp/gh-aw")
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--commit", required=True)
    args = parser.parse_args()

    evidence_directory = pathlib.Path(args.evidence_directory)
    try:
        summaries = validate_outputs(
            args.repo_root,
            args.agent_output,
            evidence_directory / "test-quarantine-case-a-eligibility.json",
            evidence_directory / "test-quarantine-requarantine-history.json",
            args.transport_directory,
            args.repository,
            args.ref,
            args.commit,
        )
    except ValidationError as error:
        raise SystemExit(f"FATAL: {error}") from error
    print(json.dumps(summaries, separators=(",", ":"), sort_keys=True))


if __name__ == "__main__":
    main()
