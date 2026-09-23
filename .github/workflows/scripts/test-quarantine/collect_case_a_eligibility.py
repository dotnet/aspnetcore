#!/usr/bin/env python3

# Compute quarantine eligibility before the agent runs. Combine main-branch
# and merged-PR failures, resolve each exact test's source and quarantine
# history, and exclude stale, pre-cutoff-source, or test-changing-PR evidence.
# Require two distinct builds for Case A and one for Case B, and bind an
# eligible Case A test to exact build/run/result evidence. Emit
# repository/commit-bound receipts, including rejection reasons, for agent
# candidate selection and safe-output validation.
# This script does not diagnose failures, choose matchers, or create GitHub objects.

import argparse
import datetime
import hashlib
import json
import os
import pathlib
import re
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request


WORK_ITEM_SUFFIX = ".WorkItemExecution"
QUARANTINE = "QuarantinedTest"
QUARANTINE_ATTRIBUTE_PATTERN = re.compile(
    r"(?:\[|,)\s*(?:assembly\s*:\s*)?"
    r"(?:[A-Za-z_][A-Za-z0-9_]*\.)*"
    r"QuarantinedTest(?:Attribute)?\s*(?:\(|\])"
)
ASSEMBLY_QUARANTINE_PATTERN = re.compile(
    r"\[\s*assembly\s*:\s*"
    r"(?:[A-Za-z_][A-Za-z0-9_]*\.)*"
    r"QuarantinedTest(?:Attribute)?\s*(?:\(|\])"
)
QUARANTINE_ISSUE_PATTERN = re.compile(
    r"https://github\.com/dotnet/aspnetcore/issues/(?P<issue>\d+)"
)
QUARANTINE_REFERENCE_PATTERN = re.compile(
    r"https://github\.com/dotnet/aspnetcore/issues/"
    r"(?P<reference>\d+|#aw_[A-Za-z0-9_]{3,12})"
)
METHOD_PATTERN = re.compile(
    r"(?m)^[ \t]*(?:public|internal|protected|private)\s+"
    r"(?:(?:static|virtual|override|sealed|async|new|unsafe|partial|extern)\s+)*"
    r"(?:[A-Za-z_][A-Za-z0-9_?.<>\[\],]*\s+)+"
    r"(?P<method>[A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>{}]*>)?\s*\("
)
SOURCE_C_FAILURE_PATTERN = re.compile(
    r"(?m)^[ \t]*(?P<test>.+?)[ \t]+\[FAIL\][ \t]*$"
)
IDENTITY_HISTORY_CACHE = {}


def has_quarantine_attribute(text):
    return QUARANTINE_ATTRIBUTE_PATTERN.search(sanitize_csharp(text)) is not None


def parse_utc(value):
    if not value:
        return None
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))


def git(root, *args):
    return subprocess.check_output(
        ["git", "-C", str(root), *args],
        text=True,
        stderr=subprocess.DEVNULL,
    ).strip()


def git_result(root, *args):
    return subprocess.run(
        ["git", "-C", str(root), *args],
        check=False,
        capture_output=True,
        text=True,
    )


def sanitize_csharp(text):
    output = list(text)
    state = "code"
    index = 0
    while index < len(text):
        character = text[index]
        following = text[index + 1] if index + 1 < len(text) else ""
        if state == "code":
            if character == "/" and following == "/":
                output[index] = output[index + 1] = " "
                state = "line-comment"
                index += 2
                continue
            if character == "/" and following == "*":
                output[index] = output[index + 1] = " "
                state = "block-comment"
                index += 2
                continue
            if character == '"':
                output[index] = " "
                state = "string"
            elif character == "'":
                output[index] = " "
                state = "character"
        elif state == "line-comment":
            if character == "\n":
                state = "code"
            else:
                output[index] = " "
        elif state == "block-comment":
            if character == "*" and following == "/":
                output[index] = output[index + 1] = " "
                state = "code"
                index += 2
                continue
            if character != "\n":
                output[index] = " "
        elif state in ("string", "character"):
            delimiter = '"' if state == "string" else "'"
            if character == "\\":
                output[index] = " "
                if index + 1 < len(text):
                    output[index + 1] = " "
                    index += 2
                    continue
            elif character == delimiter:
                output[index] = " "
                state = "code"
            elif character != "\n":
                output[index] = " "
        index += 1
    return "".join(output)


def matching_brace(text, opening):
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return index
    return None


def declaration_ranges(clean):
    ranges = []
    pattern = re.compile(
        r"\b(?:(namespace)\s+([A-Za-z_][A-Za-z0-9_.]*)|"
        r"(?:class|struct|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*))"
        r"[^;{]*\{"
    )
    for match in pattern.finditer(clean):
        opening = clean.find("{", match.start(), match.end())
        closing = matching_brace(clean, opening)
        if closing is None:
            continue
        kind = "namespace" if match.group(1) else "type"
        name = match.group(2) or match.group(3)
        base_type = None
        if kind == "type":
            header = clean[match.start():opening]
            base_match = re.search(
                r":\s*([A-Za-z_][A-Za-z0-9_.]*)",
                re.sub(r"<[^<>]*>", "", header),
            )
            if base_match:
                base_type = base_match.group(1)
        ranges.append((opening, closing, kind, name, match.start(), base_type))
    return ranges


def attribute_block(lines, declaration_line):
    collected = []
    bracket_depth = 0
    index = declaration_line - 1
    while index >= 0:
        stripped = lines[index].strip()
        if not stripped or stripped.startswith("//"):
            if collected:
                collected.append(lines[index])
            index -= 1
            continue
        bracket_depth += stripped.count("]") - stripped.count("[")
        if stripped.startswith("[") or bracket_depth > 0:
            collected.append(lines[index])
            index -= 1
            continue
        break
    return "\n".join(reversed(collected))


def normalize_type_name(value):
    return re.sub(r"`\d+", "", value.replace("+", "."))


def full_type_name(ranges, position, file_namespace, declared_type=None):
    containing = [
        entry for entry in ranges
        if entry[0] < position < entry[1]
    ]
    namespaces = [entry[3] for entry in containing if entry[2] == "namespace"]
    types = [entry for entry in containing if entry[2] == "type"]
    types.sort(key=lambda entry: entry[0])
    parts = []
    if file_namespace:
        parts.append(file_namespace)
    parts.extend(namespaces)
    parts.extend(entry[3] for entry in types)
    if declared_type:
        parts.append(declared_type)
    return normalize_type_name(".".join(parts))


def find_project_root(root, relative_path):
    file_path = pathlib.Path(root, relative_path)
    for parent in [file_path.parent, *file_path.parents]:
        if parent == pathlib.Path(root).parent:
            break
        if any(parent.glob("*.csproj")):
            return parent
    return file_path.parent


def build_source_index(root):
    root = pathlib.Path(root)
    files = []
    type_quarantines = {}
    assembly_quarantines = {}
    assembly_quarantine_ambiguities = {}
    assembly_targets = []
    method_index = {}
    type_index = {}

    for file_path in pathlib.Path(root, "src").rglob("*.cs"):
        relative_path = str(file_path.relative_to(root)).replace(os.sep, "/")
        text = file_path.read_text(encoding="utf-8", errors="replace")
        clean = sanitize_csharp(text)
        lines = text.splitlines()
        project_root = find_project_root(root, relative_path)
        file_namespace = None
        namespace_match = re.search(
            r"(?m)^[ \t]*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;",
            clean,
        )
        if namespace_match:
            file_namespace = namespace_match.group(1)
        ranges = declaration_ranges(clean)
        files.append((
            relative_path,
            str(project_root),
            clean,
            lines,
            file_namespace,
            ranges,
        ))
        if ASSEMBLY_QUARANTINE_PATTERN.search(clean):
            project_files = list(project_root.glob("*.csproj"))
            if len(project_files) == 1:
                assembly_quarantines[str(project_root)] = True
            else:
                assembly_quarantine_ambiguities[str(project_root)] = True
            for line in lines:
                if not ASSEMBLY_QUARANTINE_PATTERN.search(
                    sanitize_csharp(line)
                ):
                    continue
                issue = QUARANTINE_ISSUE_PATTERN.search(line)
                assembly_targets.append({
                    "scope": "assembly",
                    "path": relative_path,
                    "project_root": str(project_root),
                    "issue": int(issue.group("issue")) if issue else None,
                    "quarantine_attribute": line,
                })

        for entry in ranges:
            if entry[2] != "type":
                continue
            position = entry[4]
            type_name = full_type_name(
                ranges,
                position,
                file_namespace,
                declared_type=entry[3],
            )
            declaration_line = clean.count("\n", 0, position)
            attributes = attribute_block(lines, declaration_line)
            quarantined = has_quarantine_attribute(attributes)
            if quarantined:
                type_quarantines[(str(project_root), type_name)] = True
            type_index.setdefault(type_name, []).append({
                "type": type_name,
                "base": entry[5],
                "path": relative_path,
                "project_root": str(project_root),
                "quarantined": quarantined,
                "quarantine_attribute": attributes if quarantined else "",
                "assembly_quarantined": assembly_quarantines.get(str(project_root), False),
                "assembly_quarantine_ambiguous": assembly_quarantine_ambiguities.get(
                    str(project_root),
                    False,
                ),
            })

    for declarations in type_index.values():
        for declaration in declarations:
            declaration["assembly_quarantined"] = assembly_quarantines.get(
                declaration["project_root"],
                False,
            )
            declaration["assembly_quarantine_ambiguous"] = (
                assembly_quarantine_ambiguities.get(
                    declaration["project_root"],
                    False,
                )
            )

    for relative_path, project_root, clean, lines, file_namespace, ranges in files:
        for method_match in METHOD_PATTERN.finditer(clean):
            position = method_match.start()
            type_name = full_type_name(ranges, position, file_namespace)
            method = method_match.group("method")
            method_line = clean.count("\n", 0, position)
            attributes = attribute_block(lines, method_line)
            method_index.setdefault(method, []).append({
                "path": relative_path,
                "type": type_name,
                "method": method,
                "method_line": method_line + 1,
                "project_root": project_root,
                "method_quarantined": has_quarantine_attribute(attributes),
                "quarantine_attribute": (
                    attributes if has_quarantine_attribute(attributes) else ""
                ),
                "type_quarantined": type_quarantines.get(
                    (project_root, type_name),
                    False,
                ),
                "assembly_quarantined": assembly_quarantines.get(project_root, False),
                "assembly_quarantine_ambiguous": assembly_quarantine_ambiguities.get(
                    project_root,
                    False,
                ),
            })
    return {
        "methods": method_index,
        "types": type_index,
        "assemblies": assembly_targets,
    }


def logical_type(source_index, type_name, project_root=None):
    declarations = source_index["types"].get(type_name, [])
    if project_root is not None:
        declarations = [
            entry for entry in declarations
            if entry["project_root"] == project_root
        ]
    if not declarations:
        return {"status": "missing"}

    projects = {entry["project_root"] for entry in declarations}
    if len(projects) != 1:
        return {"status": "ambiguous"}
    project_root = next(iter(projects))
    bases = {
        normalize_type_name(entry["base"])
        for entry in declarations
        if entry.get("base")
    }
    if len(bases) > 1:
        return {"status": "ambiguous"}

    base = next(iter(bases), None)
    base_declaration = None
    if base:
        base_declaration = min(
            (
                entry for entry in declarations
                if entry.get("base")
                and normalize_type_name(entry["base"]) == base
            ),
            key=lambda entry: entry["path"],
        )
    return {
        "status": "exact",
        "type": type_name,
        "project_root": project_root,
        "declarations": declarations,
        "base": base,
        "base_declaration": base_declaration,
        "quarantined": any(entry["quarantined"] for entry in declarations),
        "assembly_quarantined": declarations[0]["assembly_quarantined"],
        "assembly_quarantine_ambiguous": declarations[0][
            "assembly_quarantine_ambiguous"
        ],
    }


def resolve_base_type(source_index, current_type, base_name, project_root):
    return resolve_base_type_name(
        (
            name
            for name, declarations in source_index["types"].items()
            if any(entry["project_root"] == project_root for entry in declarations)
        ),
        current_type,
        base_name,
    )


def resolve_base_type_name(type_names, current_type, base_name):
    namespace = current_type.rsplit(".", 1)[0] if "." in current_type else ""
    qualified_base = f"{namespace}.{base_name}" if namespace else base_name
    candidates = {
        name
        for name in type_names
        if (
            name == base_name
            or name == qualified_base
            or name.endswith(f".{base_name}")
        )
    }
    if not candidates:
        return {"status": "missing"}
    if len(candidates) != 1:
        return {"status": "ambiguous"}
    return {
        "status": "exact",
        "type": next(iter(candidates)),
    }


def resolve_source(root, test_name, source_index=None):
    method = test_name.rsplit(".", 1)[-1]
    expected_type = normalize_type_name(test_name.rsplit(".", 1)[0])
    source_index = source_index or build_source_index(root)
    expected_runner = logical_type(source_index, expected_type)
    if expected_runner["status"] != "exact":
        return {"status": expected_runner["status"], "matches": []}
    project_root = expected_runner["project_root"]
    matches = [
        entry for entry in source_index["methods"].get(method, [])
        if entry["type"] == expected_type and entry["project_root"] == project_root
    ]
    runner_types = []
    resolution_status = None
    if not matches:
        current_type = expected_type
        visited = set()
        while current_type not in visited:
            visited.add(current_type)
            runner_type = logical_type(source_index, current_type, project_root)
            if runner_type["status"] == "ambiguous":
                resolution_status = "ambiguous"
                break
            if runner_type["status"] != "exact" or not runner_type["base"]:
                break
            project_root = runner_type["project_root"]
            runner_types.append(runner_type)
            base_type = resolve_base_type(
                source_index,
                current_type,
                runner_type["base"],
                runner_type["project_root"],
            )
            if base_type["status"] == "ambiguous":
                resolution_status = "ambiguous"
                break
            if base_type["status"] != "exact":
                break
            current_type = base_type["type"]
            matches = [
                entry for entry in source_index["methods"].get(method, [])
                if (
                    entry["type"] == current_type
                    and entry["project_root"] == runner_type["project_root"]
                )
            ]
            if matches:
                break
    if resolution_status == "ambiguous" or len(matches) != 1:
        return {
            "status": (
                "ambiguous"
                if resolution_status == "ambiguous" or matches
                else "missing"
            ),
            "matches": matches[:5],
        }

    declaring_type = logical_type(
        source_index,
        matches[0]["type"],
        matches[0]["project_root"],
    )
    if declaring_type["status"] != "exact":
        return {
            "status": declaring_type["status"],
            "matches": matches[:5],
        }

    result = dict(matches[0])
    result["status"] = "exact"
    result["declaring_type"] = result["type"]
    result["type"] = expected_type
    result["type_quarantined"] = (
        declaring_type["quarantined"]
        or any(entry["quarantined"] for entry in runner_types)
    )
    assembly_declaration = runner_types[0] if runner_types else declaring_type
    result["assembly_quarantined"] = assembly_declaration["assembly_quarantined"]
    result["assembly_quarantine_ambiguous"] = assembly_declaration[
        "assembly_quarantine_ambiguous"
    ]
    result["assembly_project_root"] = assembly_declaration["project_root"]
    locations = [{
        "path": result["path"],
        "type": result["declaring_type"],
        "method": result["method"],
        "project_root": result["project_root"],
    }]
    locations.extend({
        "path": entry["base_declaration"]["path"],
        "type": entry["type"],
        "method": None,
        "project_root": entry["project_root"],
    } for entry in runner_types)
    result["history_locations"] = list({
        (entry["path"], entry["type"], entry["method"]): entry
        for entry in locations
    }.values())
    assembly_locations = [locations[0]]
    if runner_types:
        assembly_locations.append(locations[1])
    result["assembly_history_locations"] = list({
        (entry["path"], entry["type"], entry["method"]): entry
        for entry in assembly_locations
    }.values())
    type_locations = [{
        "type": declaring_type["type"],
        "project_root": declaring_type["project_root"],
    }]
    type_locations.extend({
        "type": entry["type"],
        "project_root": entry["project_root"],
    } for entry in runner_types)
    result["type_history_locations"] = list({
        (entry["project_root"], entry["type"]): entry
        for entry in type_locations
    }.values())
    return result


def historical_assembly_state(root, project_root, commit, state_cache):
    cache_key = (project_root, commit)
    if cache_key in state_cache:
        return state_cache[cache_key]
    if commit is None:
        return {"status": "exact", "quarantined": False, "issue": None}

    tree = git_result(
        root,
        "ls-tree",
        "-r",
        "--name-only",
        commit,
        "--",
        project_root,
    )
    if tree.returncode != 0:
        result = {"status": "ambiguous"}
        state_cache[cache_key] = result
        return result
    paths = tree.stdout.splitlines()
    project_directories = {}
    for path in paths:
        if path.endswith(".csproj"):
            directory = str(pathlib.PurePosixPath(path).parent)
            project_directories.setdefault(directory, []).append(path)

    matches = git_result(
        root,
        "grep",
        "-l",
        "-F",
        QUARANTINE,
        commit,
        "--",
        project_root,
    )
    if matches.returncode not in (0, 1):
        return {"status": "ambiguous"}

    assembly_files = []
    assembly_issues = set()
    for match in matches.stdout.splitlines():
        relative_path = match.split(":", 1)[-1]
        if not relative_path.endswith(".cs"):
            continue
        content = git_result(root, "show", f"{commit}:{relative_path}")
        if content.returncode != 0:
            return {"status": "ambiguous"}
        if not ASSEMBLY_QUARANTINE_PATTERN.search(sanitize_csharp(content.stdout)):
            continue

        directory = pathlib.PurePosixPath(relative_path).parent
        associated_directory = None
        while True:
            directory_string = str(directory)
            if directory_string in project_directories:
                associated_directory = directory_string
                break
            if directory_string in ("", "."):
                break
            directory = directory.parent
        if associated_directory is None:
            return {"status": "ambiguous"}
        if associated_directory != project_root:
            continue
        if len(project_directories[associated_directory]) != 1:
            return {"status": "ambiguous"}
        assembly_files.append(relative_path)
        for source_line in content.stdout.splitlines():
            if not ASSEMBLY_QUARANTINE_PATTERN.search(
                sanitize_csharp(source_line)
            ):
                continue
            for issue_match in QUARANTINE_ISSUE_PATTERN.finditer(source_line):
                assembly_issues.add(int(issue_match.group("issue")))

    state = {
        "status": "exact",
        "quarantined": bool(assembly_files),
        "paths": assembly_files,
        "issue": (
            next(iter(assembly_issues))
            if len(assembly_issues) == 1
            else None
        ),
    }
    state_cache[cache_key] = state
    return state


def assembly_quarantine_history(root, project_root, history_ref):
    history = git_result(
        root,
        "log",
        "--first-parent",
        "--format=%H%x09%P%x09%cI",
        "-G",
        QUARANTINE,
        history_ref,
        "--",
        project_root,
    )
    if history.returncode != 0:
        return {"status": "ambiguous"}

    events = []
    state_cache = {}
    for line in history.stdout.splitlines():
        sha, parent_values, timestamp = line.split("\t", 2)
        parent = parent_values.split()[0] if parent_values else None
        current_state = historical_assembly_state(
            root,
            project_root,
            sha,
            state_cache,
        )
        parent_state = historical_assembly_state(
            root,
            project_root,
            parent,
            state_cache,
        )
        if (
            current_state["status"] != "exact"
            or parent_state["status"] != "exact"
        ):
            return {"status": "ambiguous", "commit": sha, "utc": timestamp}
        if current_state["quarantined"] == parent_state["quarantined"]:
            continue
        events.append({
            "status": "added" if current_state["quarantined"] else "removed",
            "commit": sha,
            "parent": parent,
            "utc": timestamp,
            "issue": (
                current_state["issue"]
                if current_state["quarantined"]
                else parent_state["issue"]
            ),
            "scope": "assembly",
        })
    return {"status": "exact", "events": events}


def historical_project_source_index(
    root,
    project_root,
    commit,
    source_cache,
    content_cache,
    *,
    quarantine_only=False,
):
    cache_key = (commit, project_root, quarantine_only)
    if cache_key in source_cache:
        return source_cache[cache_key]

    tree = git_result(
        root,
        "ls-tree",
        "-r",
        "--name-only",
        commit,
        "--",
        project_root,
    )
    if tree.returncode != 0:
        return {"status": "ambiguous"}
    paths = tree.stdout.splitlines()
    quarantine_paths = None
    if quarantine_only:
        matches = git_result(
            root,
            "grep",
            "-l",
            "-F",
            QUARANTINE,
            commit,
            "--",
            project_root,
        )
        if matches.returncode not in (0, 1):
            return {"status": "ambiguous"}
        quarantine_paths = {
            match.split(":", 1)[-1]
            for match in matches.stdout.splitlines()
        }
    project_directories = {}
    for path in paths:
        if path.endswith(".csproj"):
            directory = str(pathlib.PurePosixPath(path).parent)
            project_directories.setdefault(directory, []).append(path)
    if len(project_directories.get(project_root, [])) > 1:
        result = {"status": "ambiguous"}
        source_cache[cache_key] = result
        return result

    types = set()
    bases = {}
    type_quarantines = set()
    type_quarantine_issues = {}
    methods = {}
    method_quarantines = set()
    method_quarantine_issues = {}
    for relative_path in paths:
        if not relative_path.endswith(".cs"):
            continue
        if quarantine_paths is not None and relative_path not in quarantine_paths:
            continue
        directory = pathlib.PurePosixPath(relative_path).parent
        while str(directory) not in project_directories:
            if str(directory) in ("", "."):
                directory = None
                break
            directory = directory.parent
        if directory is None or str(directory) != project_root:
            continue

        content_key = (commit, relative_path)
        if content_key not in content_cache:
            content = git_result(root, "show", f"{commit}:{relative_path}")
            if content.returncode != 0:
                result = {"status": "ambiguous"}
                source_cache[cache_key] = result
                return result
            content_cache[content_key] = content.stdout
        text = content_cache[content_key]
        clean = sanitize_csharp(text)
        lines = text.splitlines()
        file_namespace = None
        namespace_match = re.search(
            r"(?m)^[ \t]*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;",
            clean,
        )
        if namespace_match:
            file_namespace = namespace_match.group(1)
        ranges = declaration_ranges(clean)
        for entry in ranges:
            if entry[2] != "type":
                continue
            type_name = full_type_name(
                ranges,
                entry[4],
                file_namespace,
                declared_type=entry[3],
            )
            types.add(type_name)
            type_bases = bases.setdefault(type_name, set())
            if entry[5]:
                type_bases.add(normalize_type_name(entry[5]))
            declaration_line = clean.count("\n", 0, entry[4])
            attributes = attribute_block(lines, declaration_line)
            if has_quarantine_attribute(attributes):
                type_quarantines.add(type_name)
                issues = {
                    int(match.group("issue"))
                    for match in QUARANTINE_ISSUE_PATTERN.finditer(attributes)
                }
                issue = next(iter(issues)) if len(issues) == 1 else None
                if (
                    type_name in type_quarantine_issues
                    and type_quarantine_issues[type_name] != issue
                ):
                    type_quarantine_issues[type_name] = None
                else:
                    type_quarantine_issues[type_name] = issue
        for match in METHOD_PATTERN.finditer(clean):
            key = (
                full_type_name(ranges, match.start(), file_namespace),
                match.group("method"),
            )
            methods[key] = methods.get(key, 0) + 1
            declaration_line = clean.count("\n", 0, match.start())
            attributes = attribute_block(lines, declaration_line)
            if has_quarantine_attribute(attributes):
                method_quarantines.add(key)
                issues = {
                    int(match.group("issue"))
                    for match in QUARANTINE_ISSUE_PATTERN.finditer(attributes)
                }
                issue = next(iter(issues)) if len(issues) == 1 else None
                if (
                    key in method_quarantine_issues
                    and method_quarantine_issues[key] != issue
                ):
                    method_quarantine_issues[key] = None
                else:
                    method_quarantine_issues[key] = issue

    result = {
        "status": "exact",
        "types": types,
        "bases": bases,
        "type_quarantines": type_quarantines,
        "type_quarantine_issues": type_quarantine_issues,
        "methods": methods,
        "method_quarantines": method_quarantines,
        "method_quarantine_issues": method_quarantine_issues,
    }
    source_cache[cache_key] = result
    return result


def historical_test_source(
    root,
    project_root,
    test_name,
    commit,
    source_cache,
    content_cache,
):
    source_index = historical_project_source_index(
        root,
        project_root,
        commit,
        source_cache,
        content_cache,
    )
    if source_index["status"] != "exact":
        return {"status": "ambiguous"}

    runner_type, method = test_name.rsplit(".", 1)
    current_type = normalize_type_name(runner_type)
    if current_type not in source_index["types"]:
        return {"status": "missing"}
    visited = set()
    while current_type not in visited:
        visited.add(current_type)
        bases = source_index["bases"].get(current_type, set())
        if len(bases) > 1:
            return {"status": "ambiguous"}
        matches = source_index["methods"].get((current_type, method), 0)
        if matches > 1:
            return {"status": "ambiguous"}
        if matches == 1:
            return {
                "status": "exact",
                "declaring_type": current_type,
                "types": visited,
            }
        if not bases:
            return {"status": "missing"}
        base_type = resolve_base_type_name(
            source_index["types"],
            current_type,
            next(iter(bases)),
        )
        if base_type["status"] != "exact":
            # An unresolved declared base may still provide the method.
            return {"status": "ambiguous"}
        current_type = base_type["type"]
    return {"status": "ambiguous"}


def assembly_quarantine_transition(
    root,
    project_root,
    test_name,
    history_ref,
    history_cache,
    source_cache,
    content_cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    if relative_project_root not in history_cache:
        history_cache[relative_project_root] = assembly_quarantine_history(
            root,
            relative_project_root,
            history_ref,
        )
    history = history_cache[relative_project_root]
    if history["status"] != "exact":
        return {
            "status": "ambiguous",
            "commit": history.get("commit"),
            "utc": history.get("utc"),
        }

    for event in history["events"]:
        applicable_commit = (
            event["parent"] if event["status"] == "removed" else event["commit"]
        )
        if applicable_commit is None:
            continue
        historical_source = historical_test_source(
            root,
            relative_project_root,
            test_name,
            applicable_commit,
            source_cache,
            content_cache,
        )
        if historical_source["status"] == "ambiguous":
            return {
                "status": "ambiguous",
                "commit": event["commit"],
                "utc": event["utc"],
            }
        if historical_source["status"] == "exact":
            return {
                key: value
                for key, value in event.items()
                if key != "parent"
            }
    return {"status": "none"}


def type_quarantine_transition(
    root,
    project_root,
    test_name,
    history_ref,
    history_cache,
    source_cache,
    content_cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    if relative_project_root not in history_cache:
        history = git_result(
            root,
            "log",
            "--first-parent",
            "--format=%H%x09%P%x09%cI",
            "-G",
            QUARANTINE,
            history_ref,
            "--",
            relative_project_root,
        )
        if history.returncode != 0:
            history_cache[relative_project_root] = {
                "lines": None,
                "tests": {},
            }
        else:
            history_cache[relative_project_root] = {
                "lines": history.stdout.splitlines(),
                "tests": {},
            }
    cached_history = history_cache[relative_project_root]
    if test_name in cached_history["tests"]:
        return cached_history["tests"][test_name]
    history = cached_history["lines"]
    if history is None:
        return {"status": "ambiguous"}

    for line in history:
        sha, parent_values, timestamp = line.split("\t", 2)
        parent = parent_values.split()[0] if parent_values else None
        current_index = historical_project_source_index(
            root,
            relative_project_root,
            sha,
            source_cache,
            content_cache,
        )
        if parent is None:
            parent_index = {
                "status": "exact",
                "type_quarantines": set(),
            }
        else:
            parent_index = historical_project_source_index(
                root,
                relative_project_root,
                parent,
                source_cache,
                content_cache,
            )
        if (
            current_index["status"] != "exact"
            or parent_index["status"] != "exact"
        ):
            result = {
                "status": "ambiguous",
                "commit": sha,
                "utc": timestamp,
            }
            cached_history["tests"][test_name] = result
            return result
        current_types = current_index["type_quarantines"]
        parent_types = parent_index["type_quarantines"]
        applicable_changes = []
        for status, changed_types, applicable_commit in (
            ("added", current_types - parent_types, sha),
            ("removed", parent_types - current_types, parent),
        ):
            if not changed_types:
                continue
            historical_source = historical_test_source(
                root,
                relative_project_root,
                test_name,
                applicable_commit,
                source_cache,
                content_cache,
            )
            if historical_source["status"] == "ambiguous":
                applicable_changes.append(("ambiguous", None, None))
            elif historical_source["status"] == "exact":
                matching_types = changed_types.intersection(
                    historical_source["types"]
                )
                issues = (
                    current_index["type_quarantine_issues"]
                    if status == "added"
                    else parent_index["type_quarantine_issues"]
                )
                applicable_changes.extend(
                    (status, changed_type, issues.get(changed_type))
                    for changed_type in matching_types
                )
        if not applicable_changes:
            continue
        if (
            len(applicable_changes) != 1
            or applicable_changes[0][0] == "ambiguous"
            or applicable_changes[0][2] is None
        ):
            result = {
                "status": "ambiguous",
                "commit": sha,
                "utc": timestamp,
            }
            cached_history["tests"][test_name] = result
            return result
        result = {
            "status": applicable_changes[0][0],
            "commit": sha,
            "utc": timestamp,
            "scope": "type",
            "type": applicable_changes[0][1],
            "issue": applicable_changes[0][2],
        }
        cached_history["tests"][test_name] = result
        return result
    result = {"status": "none"}
    cached_history["tests"][test_name] = result
    return result


def quarantine_transitions(root, relative_path, method, type_name, history_ref):
    try:
        history = git(
            root,
            "log",
            "--first-parent",
            "--follow",
            "--format=@@COMMIT@@%H%x09%cI",
            "-G",
            QUARANTINE,
            "-p",
            history_ref,
            "--",
            relative_path,
        )
    except subprocess.CalledProcessError:
        return [{"status": "ambiguous"}]
    if not history:
        return []
    events = []
    commits = re.split(r"(?=@@COMMIT@@)", history)
    for commit in commits:
        if not commit.startswith("@@COMMIT@@"):
            continue
        header, *patch_lines = commit.splitlines()
        metadata = header.removeprefix("@@COMMIT@@").split("\t", 1)
        sha = metadata[0]
        timestamp = metadata[1] if len(metadata) > 1 else None
        patch = "\n".join(patch_lines)
        relevant_changes = []
        ambiguous = False
        type_short_name = type_name.rsplit(".", 1)[-1]
        for hunk in re.split(r"(?=^@@)", patch, flags=re.MULTILINE):
            hunk_lines = hunk.splitlines()
            changed = [
                (index, line) for index, line in enumerate(hunk_lines)
                if line.startswith(("+", "-")) and not line.startswith(("+++", "---"))
                and has_quarantine_attribute(line)
            ]
            if not changed:
                continue
            hunk_ambiguous = False
            for change_index, changed_line in changed:
                if ASSEMBLY_QUARANTINE_PATTERN.search(
                    sanitize_csharp(changed_line)
                ):
                    continue
                target = None
                for candidate in hunk_lines[change_index + 1:change_index + 21]:
                    content = candidate[1:] if candidate[:1] in ("+", "-", " ") else candidate
                    stripped = content.strip()
                    if (
                        not stripped
                        or stripped.startswith("//")
                        or stripped.startswith("[")
                        or stripped in ("]", ")]")
                    ):
                        continue
                    target = stripped
                    break
                if target is None:
                    hunk_ambiguous = True
                elif method and re.search(rf"\b{re.escape(method)}\s*\(", target):
                    relevant_changes.append(changed_line)
                elif not method and re.search(
                    rf"\b(?:class|struct|record)\s+{re.escape(type_short_name)}\b",
                    target,
                ):
                    relevant_changes.append(changed_line)
                elif re.search(
                    r"\b(?:class|struct|record)\s+[A-Za-z_][A-Za-z0-9_]*\b|"
                    r"\b[A-Za-z_][A-Za-z0-9_]*\s*(?:<[^>{}]*>)?\s*\(",
                    target,
                ):
                    continue
                else:
                    hunk_ambiguous = True
            if hunk_ambiguous:
                ambiguous = True
        if not relevant_changes:
            if ambiguous:
                events.append({
                    "status": "ambiguous",
                    "commit": sha,
                    "utc": timestamp,
                })
            continue
        removed = any(
            line.startswith("-") and has_quarantine_attribute(line)
            for line in relevant_changes
        )
        added = any(
            line.startswith("+") and has_quarantine_attribute(line)
            for line in relevant_changes
        )
        if removed and added:
            status = "modified"
        elif removed:
            status = "removed"
        elif added:
            status = "added"
        else:
            status = "ambiguous"
        events.append({
            "status": status,
            "commit": sha,
            "utc": timestamp,
        })
    return events


def quarantine_transition(root, relative_path, method, type_name, history_ref):
    for event in quarantine_transitions(
        root,
        relative_path,
        method,
        type_name,
        history_ref,
    ):
        if event["status"] != "modified":
            return event
    return {"status": "none"}


def target_identity_rename_detected(
    root,
    relative_path,
    type_name,
    method,
    history_ref,
):
    type_short_name = type_name.rsplit(".", 1)[-1]
    identity_cache_key = (
        str(pathlib.Path(root).resolve()),
        relative_path,
        history_ref,
    )
    if identity_cache_key not in IDENTITY_HISTORY_CACHE:
        history = git_result(
            root,
            "log",
            "--first-parent",
            "--follow",
            "--format=@@COMMIT@@%H",
            "--name-only",
            history_ref,
            "--",
            relative_path,
        )
        identities = []
        if history.returncode == 0:
            historical_locations = []
            commit = None
            for line in history.stdout.splitlines():
                if line.startswith("@@COMMIT@@"):
                    commit = line.removeprefix("@@COMMIT@@")
                elif line and commit is not None:
                    historical_locations.append((commit, line))
                    commit = None
            for commit, historical_path in historical_locations:
                content = git_result(
                    root,
                    "show",
                    f"{commit}:{historical_path}",
                )
                if content.returncode != 0:
                    continue
                clean = sanitize_csharp(content.stdout)
                file_namespace_match = re.search(
                    r"(?m)^[ \t]*namespace\s+"
                    r"([A-Za-z_][A-Za-z0-9_.]*)\s*;",
                    clean,
                )
                file_namespace = (
                    file_namespace_match.group(1)
                    if file_namespace_match else None
                )
                ranges = declaration_ranges(clean)
                methods = {}
                for match in METHOD_PATTERN.finditer(clean):
                    declaring_type = full_type_name(
                        ranges,
                        match.start(),
                        file_namespace,
                    )
                    methods.setdefault(declaring_type, set()).add(
                        match.group("method")
                    )
                identities.append({
                    "types": {
                        full_type_name(
                            ranges,
                            entry[4],
                            file_namespace,
                            declared_type=entry[3],
                        )
                        for entry in ranges
                        if entry[2] == "type"
                    },
                    "methods": methods,
                })
        else:
            identities = None
        IDENTITY_HISTORY_CACHE[identity_cache_key] = identities
    identities = IDENTITY_HISTORY_CACHE[identity_cache_key]
    if identities is None:
        return True
    if any(
        historical_type.rsplit(".", 1)[-1] == type_short_name
        and historical_type != type_name
        and (
            method is None
            or method in identity["methods"].get(historical_type, set())
        )
        for identity in identities
        for historical_type in identity["types"]
    ):
        return True

    if method is None:
        declaration_pattern = re.compile(
            r"\b(?:class|struct|record)\s+"
            r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)\b"
        )
        git_pattern = re.escape(type_short_name)
        current_name = type_short_name
    else:
        declaration_pattern = METHOD_PATTERN
        git_pattern = re.escape(method)
        current_name = method

    def declaration_name(line):
        match = declaration_pattern.search(line[1:])
        if match is None:
            return None
        return match.group("method" if method is not None else "name")

    history = git_result(
        root,
        "log",
        "--first-parent",
        "--follow",
        "--format=@@COMMIT@@%H",
        "-G",
        git_pattern,
        "-p",
        history_ref,
        "--",
        relative_path,
    )
    if history.returncode != 0:
        return True
    for commit in re.split(r"(?=@@COMMIT@@)", history.stdout):
        if not commit.startswith("@@COMMIT@@"):
            continue
        if "\ncopy from " in commit:
            continue
        changed = [
            line for line in commit.splitlines()
            if line.startswith(("+", "-"))
            and not line.startswith(("+++", "---"))
        ]
        if not any(
            line.startswith("+") and declaration_name(line) == current_name
            for line in changed
        ):
            continue
        return any(
            line.startswith("-")
            and (name := declaration_name(line)) is not None
            and name != current_name
            for line in changed
        )
    return False


def namespace_rename_detected(root, relative_path, type_name, history_ref):
    path = pathlib.Path(root, relative_path)
    try:
        current = sanitize_csharp(path.read_text(encoding="utf-8"))
    except OSError:
        return True
    file_namespace_match = re.search(
        r"(?m)^[ \t]*namespace\s+"
        r"(?P<name>[A-Za-z_][A-Za-z0-9_.]*)\s*;",
        current,
    )
    file_namespace = (
        file_namespace_match.group("name")
        if file_namespace_match else None
    )
    ranges = declaration_ranges(current)
    current_names = None
    for entry in ranges:
        if entry[2] != "type":
            continue
        declared_type = full_type_name(
            ranges,
            entry[4],
            file_namespace,
            declared_type=entry[3],
        )
        if declared_type != type_name:
            continue
        namespaces = [
            containing[3]
            for containing in ranges
            if (
                containing[2] == "namespace"
                and containing[0] < entry[4] < containing[1]
            )
        ]
        current_names = [
            part for part in [file_namespace, *namespaces] if part
        ]
        break
    if not current_names:
        return False
    namespace_pattern = re.compile(
        r"\bnamespace\s+"
        r"(?P<name>[A-Za-z_][A-Za-z0-9_.]*)\b"
    )
    for current_name in current_names:
        history = git_result(
            root,
            "log",
            "--first-parent",
            "--follow",
            "--format=@@COMMIT@@%H",
            "-G",
            re.escape(current_name),
            "-p",
            history_ref,
            "--",
            relative_path,
        )
        if history.returncode != 0:
            return True
        for commit in re.split(r"(?=@@COMMIT@@)", history.stdout):
            if not commit.startswith("@@COMMIT@@") or "\ncopy from " in commit:
                continue
            changed = [
                line for line in commit.splitlines()
                if line.startswith(("+", "-"))
                and not line.startswith(("+++", "---"))
            ]
            added = {
                match.group("name")
                for line in changed
                if line.startswith("+")
                and (match := namespace_pattern.search(line)) is not None
            }
            if current_name not in added:
                continue
            removed = {
                match.group("name")
                for line in changed
                if line.startswith("-")
                and (match := namespace_pattern.search(line)) is not None
            }
            if removed - added:
                return True
    return False


def method_quarantine_transitions(
    root,
    project_root,
    type_name,
    method,
    history_ref,
    history_cache,
    source_cache,
    content_cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    if relative_project_root not in history_cache:
        history = git_result(
            root,
            "log",
            "--first-parent",
            "--format=%H%x09%P%x09%cI",
            "-G",
            QUARANTINE,
            history_ref,
            "--",
            relative_project_root,
        )
        history_cache[relative_project_root] = {
            "lines": (
                history.stdout.splitlines()
                if history.returncode == 0 else None
            ),
            "methods": {},
        }
    cached_history = history_cache[relative_project_root]
    target = (type_name, method)
    if target in cached_history["methods"]:
        return cached_history["methods"][target]
    if cached_history["lines"] is None:
        return [{"status": "ambiguous"}]

    events = []
    for line in cached_history["lines"]:
        sha, parent_values, timestamp = line.split("\t", 2)
        parent = parent_values.split()[0] if parent_values else None
        current_index = historical_project_source_index(
            root,
            relative_project_root,
            sha,
            source_cache,
            content_cache,
            quarantine_only=True,
        )
        if parent is None:
            parent_index = {
                "status": "exact",
                "method_quarantines": set(),
                "method_quarantine_issues": {},
            }
        else:
            parent_index = historical_project_source_index(
                root,
                relative_project_root,
                parent,
                source_cache,
                content_cache,
                quarantine_only=True,
            )
        if (
            current_index["status"] != "exact"
            or parent_index["status"] != "exact"
        ):
            result = [{
                "status": "ambiguous",
                "commit": sha,
                "utc": timestamp,
            }]
            cached_history["methods"][target] = result
            return result
        current = target in current_index["method_quarantines"]
        previous = target in parent_index["method_quarantines"]
        if current == previous:
            continue
        events.append({
            "status": "added" if current else "removed",
            "commit": sha,
            "utc": timestamp,
            "scope": "method",
            "type": type_name,
            "method": method,
            "issue": (
                current_index["method_quarantine_issues"].get(target)
                if current
                else parent_index["method_quarantine_issues"].get(target)
            ),
        })
    cached_history["methods"][target] = events
    return events


def target_quarantine_transitions(
    root,
    project_root,
    scope,
    type_name,
    method,
    history_ref,
    history_cache,
    source_cache,
    content_cache,
    assembly_state_cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    if relative_project_root not in history_cache:
        history = git_result(
            root,
            "log",
            "--first-parent",
            "--format=%H%x09%P%x09%cI",
            "-G",
            QUARANTINE,
            history_ref,
            "--",
            relative_project_root,
        )
        history_cache[relative_project_root] = (
            history.stdout.splitlines()
            if history.returncode == 0 else None
        )
    history = history_cache[relative_project_root]
    if history is None:
        return [{"status": "ambiguous"}]

    def target_state(source, assembly, commit):
        if source["status"] != "exact" or assembly["status"] != "exact":
            return None
        if scope == "assembly":
            state = {
                ("type", item)
                for item in source["type_quarantines"]
            }
            state.update(
                ("method", item[0], item[1])
                for item in source["method_quarantines"]
            )
            if assembly["quarantined"]:
                state.add(("assembly",))
            return frozenset(state)
        type_quarantined = type_name in source["type_quarantines"]
        if scope == "type":
            method_quarantines = {
                ("method", item[0], item[1])
                for item in source["method_quarantines"]
                if item[0] == type_name
            }
            if method_quarantines or type_quarantined:
                state = set(method_quarantines)
                if type_quarantined:
                    state.add(("type", type_name))
                if assembly["quarantined"]:
                    state.add(("assembly",))
                return frozenset(state)
            if not assembly["quarantined"]:
                return frozenset()
            full_source = historical_project_source_index(
                root,
                relative_project_root,
                commit,
                source_cache,
                content_cache,
            )
            if full_source["status"] != "exact":
                return None
            if type_name not in full_source["types"]:
                return frozenset()
            return frozenset({("assembly",)})
        method_quarantined = (
            type_name,
            method,
        ) in source["method_quarantines"]
        if method_quarantined or type_quarantined:
            state = set()
            if method_quarantined:
                state.add(("method", type_name, method))
            if type_quarantined:
                state.add(("type", type_name))
            if assembly["quarantined"]:
                state.add(("assembly",))
            return frozenset(state)
        if not assembly["quarantined"]:
            return frozenset()
        full_source = historical_project_source_index(
            root,
            relative_project_root,
            commit,
            source_cache,
            content_cache,
        )
        if full_source["status"] != "exact":
            return None
        if (type_name, method) not in full_source["methods"]:
            return frozenset()
        return frozenset({("assembly",)})

    events = []
    for line in history:
        sha, parent_values, timestamp = line.split("\t", 2)
        parent = parent_values.split()[0] if parent_values else None
        current_source = historical_project_source_index(
            root,
            relative_project_root,
            sha,
            source_cache,
            content_cache,
            quarantine_only=True,
        )
        current_assembly = historical_assembly_state(
            root,
            relative_project_root,
            sha,
            assembly_state_cache,
        )
        if parent is None:
            parent_source = {
                "status": "exact",
                "types": set(),
                "methods": {},
                "type_quarantines": set(),
                "method_quarantines": set(),
            }
            parent_assembly = {
                "status": "exact",
                "quarantined": False,
            }
        else:
            parent_source = historical_project_source_index(
                root,
                relative_project_root,
                parent,
                source_cache,
                content_cache,
                quarantine_only=True,
            )
            parent_assembly = historical_assembly_state(
                root,
                relative_project_root,
                parent,
                assembly_state_cache,
            )
        current = target_state(current_source, current_assembly, sha)
        previous = (
            frozenset()
            if parent is None
            else target_state(parent_source, parent_assembly, parent)
        )
        if current is None or previous is None:
            return [{
                "status": "ambiguous",
                "commit": sha,
                "utc": timestamp,
            }]
        if current == previous:
            continue
        identity_missing = (
            scope != "assembly"
            and (
                type_name not in parent_source["types"]
                or (
                    scope == "method"
                    and (type_name, method) not in parent_source["methods"]
                )
            )
        )
        if current and not previous and identity_missing:
            full_parent_source = (
                {
                    "status": "exact",
                    "types": set(),
                }
                if parent is None
                else historical_project_source_index(
                    root,
                    relative_project_root,
                    parent,
                    source_cache,
                    content_cache,
                )
            )
            status = (
                "added"
                if (
                    full_parent_source["status"] == "exact"
                    and type_name in full_parent_source["types"]
                    and (
                        scope != "method"
                        or (type_name, method) in full_parent_source["methods"]
                    )
                )
                else "ambiguous"
            )
        else:
            added = current - previous
            removed = previous - current
            if added and removed:
                status = (
                    "removed"
                    if scope in ("type", "assembly")
                    else "modified"
                )
            elif added:
                status = "added"
            elif removed:
                status = "removed"
            else:
                status = "ambiguous"
        events.append({
            "status": status,
            "commit": sha,
            "utc": timestamp,
        })
    return events


def type_quarantine_transitions(
    root,
    project_root,
    type_name,
    history_ref,
    source_cache,
    content_cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    history = git_result(
        root,
        "log",
        "--first-parent",
        "--format=%H%x09%P%x09%cI",
        "-G",
        QUARANTINE,
        history_ref,
        "--",
        relative_project_root,
    )
    if history.returncode != 0:
        return [{"status": "ambiguous"}]

    events = []
    for line in history.stdout.splitlines():
        sha, parent_values, timestamp = line.split("\t", 2)
        parent = parent_values.split()[0] if parent_values else None
        current_index = historical_project_source_index(
            root,
            relative_project_root,
            sha,
            source_cache,
            content_cache,
            quarantine_only=True,
        )
        if parent is None:
            parent_index = {
                "status": "exact",
                "type_quarantines": set(),
            }
        else:
            parent_index = historical_project_source_index(
                root,
                relative_project_root,
                parent,
                source_cache,
                content_cache,
                quarantine_only=True,
            )
        if (
            current_index["status"] != "exact"
            or parent_index["status"] != "exact"
        ):
            return [{
                "status": "ambiguous",
                "commit": sha,
                "utc": timestamp,
            }]
        current = type_name in current_index["type_quarantines"]
        previous = type_name in parent_index["type_quarantines"]
        if current == previous:
            continue
        events.append({
            "status": "added" if current else "removed",
            "commit": sha,
            "utc": timestamp,
        })
    return events


def classify_current_quarantine_history(events):
    meaningful = [event for event in events if event["status"] != "modified"]
    if not meaningful or meaningful[0]["status"] != "added":
        return "ambiguous"
    for event in meaningful[1:]:
        if event["status"] == "ambiguous":
            return "ambiguous"
        if event["status"] == "removed":
            return "re-quarantined"
    return "first-quarantine"


def quarantine_issue(attribute):
    issues = {
        int(match.group("issue"))
        for match in QUARANTINE_ISSUE_PATTERN.finditer(attribute)
    }
    return next(iter(issues)) if len(issues) == 1 else None


def quarantine_reference(attribute):
    references = {
        match.group("reference")
        for match in QUARANTINE_REFERENCE_PATTERN.finditer(attribute)
    }
    return next(iter(references)) if len(references) == 1 else None


def current_quarantine_targets(source_index):
    targets = []
    seen = set()
    for declarations in source_index["methods"].values():
        for declaration in declarations:
            if not declaration["method_quarantined"]:
                continue
            key = (
                "method",
                declaration["path"],
                declaration["type"],
                declaration["method"],
            )
            if key in seen:
                continue
            seen.add(key)
            targets.append({
                "scope": "method",
                "path": declaration["path"],
                "project_root": declaration["project_root"],
                "type": declaration["type"],
                "method": declaration["method"],
                "reference": quarantine_reference(
                    declaration["quarantine_attribute"]
                ),
            })
    for declarations in source_index["types"].values():
        for declaration in declarations:
            if not declaration["quarantined"]:
                continue
            key = ("type", declaration["path"], declaration["type"], None)
            if key in seen:
                continue
            seen.add(key)
            targets.append({
                "scope": "type",
                "path": declaration["path"],
                "project_root": declaration["project_root"],
                "type": declaration["type"],
                "method": None,
                "reference": quarantine_reference(
                    declaration["quarantine_attribute"]
                ),
            })
    for assembly in source_index["assemblies"]:
        key = ("assembly", assembly["path"], None, None)
        if key in seen:
            continue
        seen.add(key)
        targets.append({
            "scope": "assembly",
            "path": assembly["path"],
            "project_root": assembly["project_root"],
            "type": None,
            "method": None,
            "reference": quarantine_reference(
                assembly["quarantine_attribute"]
            ),
        })
    return targets


def collect_requarantine_history(
    root,
    history_ref,
    repository=None,
    ref=None,
    commit=None,
):
    root = pathlib.Path(root)
    source_index = build_source_index(root)
    targets = []
    seen = set()
    project_history_cache = {}
    target_history_cache = {}
    source_cache = {}
    content_cache = {}
    assembly_state_cache = {}

    for declarations in source_index["methods"].values():
        for declaration in declarations:
            if not declaration["method_quarantined"]:
                continue
            key = (
                "method",
                declaration["path"],
                declaration["type"],
                declaration["method"],
            )
            if key in seen:
                continue
            seen.add(key)
            issue = quarantine_issue(declaration["quarantine_attribute"])
            history_complete = project_history_is_complete(
                root,
                declaration["project_root"],
                [declaration["path"]],
                history_ref,
                project_history_cache,
            )
            events = (
                target_quarantine_transitions(
                    root,
                    declaration["project_root"],
                    "method",
                    declaration["type"],
                    declaration["method"],
                    history_ref,
                    target_history_cache,
                    source_cache,
                    content_cache,
                    assembly_state_cache,
                )
                if history_complete else [{"status": "ambiguous"}]
            )
            if (
                target_identity_rename_detected(
                    root,
                    declaration["path"],
                    declaration["type"],
                    declaration["method"],
                    history_ref,
                )
                or target_identity_rename_detected(
                    root,
                    declaration["path"],
                    declaration["type"],
                    None,
                    history_ref,
                )
                or namespace_rename_detected(
                    root,
                    declaration["path"],
                    declaration["type"],
                    history_ref,
                )
            ):
                events = [{"status": "ambiguous"}]
            targets.append({
                "scope": "method",
                "path": declaration["path"],
                "type": declaration["type"],
                "method": declaration["method"],
                "issue": issue,
                "status": (
                    classify_current_quarantine_history(events)
                    if issue is not None else "ambiguous"
                ),
            })

    for declarations in source_index["types"].values():
        for declaration in declarations:
            if not declaration["quarantined"]:
                continue
            key = ("type", declaration["path"], declaration["type"])
            if key in seen:
                continue
            seen.add(key)
            issue = quarantine_issue(declaration["quarantine_attribute"])
            history_complete = project_history_is_complete(
                root,
                declaration["project_root"],
                [declaration["path"]],
                history_ref,
                project_history_cache,
            )
            events = (
                target_quarantine_transitions(
                    root,
                    declaration["project_root"],
                    "type",
                    declaration["type"],
                    None,
                    history_ref,
                    target_history_cache,
                    source_cache,
                    content_cache,
                    assembly_state_cache,
                )
                if history_complete else [{"status": "ambiguous"}]
            )
            if target_identity_rename_detected(
                root,
                declaration["path"],
                declaration["type"],
                None,
                history_ref,
            ) or namespace_rename_detected(
                root,
                declaration["path"],
                declaration["type"],
                history_ref,
            ):
                events = [{"status": "ambiguous"}]
            targets.append({
                "scope": "type",
                "path": declaration["path"],
                "type": declaration["type"],
                "method": None,
                "issue": issue,
                "status": (
                    classify_current_quarantine_history(events)
                    if issue is not None else "ambiguous"
                ),
            })

    assemblies_by_project = {}
    for assembly in source_index["assemblies"]:
        assemblies_by_project.setdefault(
            assembly["project_root"],
            [],
        ).append(assembly)
    for assembly in source_index["assemblies"]:
        key = ("assembly", assembly["path"], assembly["issue"])
        if key in seen:
            continue
        seen.add(key)
        history_complete = project_history_is_complete(
            root,
            assembly["project_root"],
            [assembly["path"]],
            history_ref,
            project_history_cache,
        )
        events = (
            target_quarantine_transitions(
                root,
                assembly["project_root"],
                "assembly",
                None,
                None,
                history_ref,
                target_history_cache,
                source_cache,
                content_cache,
                assembly_state_cache,
            )
            if history_complete else [{"status": "ambiguous"}]
        )
        targets.append({
            "scope": "assembly",
            "path": assembly["path"],
            "issue": assembly["issue"],
            "type": None,
            "method": None,
            "status": (
                classify_current_quarantine_history(events)
                if (
                    assembly["issue"] is not None
                    and len(assemblies_by_project[assembly["project_root"]]) == 1
                )
                else "ambiguous"
            ),
        })

    return {
        "schema_version": 1,
        "repository": repository,
        "ref": ref,
        "commit": commit,
        "history_ref": history_ref,
        "history_commit": git(root, "rev-parse", "--verify", history_ref),
        "targets": sorted(
            targets,
            key=lambda item: (
                item["path"],
                item["scope"],
                item.get("type") or "",
                item.get("method") or "",
                item.get("issue") or 0,
            ),
        ),
    }


def latest_file_change(root, relative_path, history_ref):
    value = git(
        root,
        "log",
        "-1",
        "--first-parent",
        "--follow",
        "--format=%H%x09%cI",
        history_ref,
        "--",
        relative_path,
    )
    sha, timestamp = value.split("\t", 1)
    return {
        "utc": timestamp,
        "reason": "latest-test-file-change",
        "commit": sha,
    }


def first_parent_order(root, history_ref):
    return {
        commit: index
        for index, commit in enumerate(
            git(root, "rev-list", "--first-parent", history_ref).splitlines()
        )
    }


def newest_history_item(items, history_order):
    return min(items, key=lambda item: history_order[item["commit"]])


def project_history_is_complete(
    root,
    project_root,
    target_paths,
    history_ref,
    cache,
):
    root = pathlib.Path(root)
    relative_project_root = str(
        pathlib.Path(project_root).relative_to(root)
    ).replace(os.sep, "/")
    cache_key = (relative_project_root, tuple(sorted(target_paths)))
    if cache_key in cache:
        return cache[cache_key]

    project_cache_key = (relative_project_root, None)
    if project_cache_key not in cache:
        project_files = list(pathlib.Path(project_root).glob("*.csproj"))
        project_metadata = None
        if len(project_files) == 1:
            project_file = str(
                project_files[0].relative_to(root)
            ).replace(os.sep, "/")
            project_history = git_result(
                root,
                "log",
                "--first-parent",
                "--follow",
                "--format=@@COMMIT@@%H",
                "--name-only",
                history_ref,
                "--",
                project_file,
            )
            project_commits = []
            historical_project_files = []
            for line in project_history.stdout.splitlines():
                if line.startswith("@@COMMIT@@"):
                    project_commits.append(line.removeprefix("@@COMMIT@@"))
                elif line:
                    historical_project_files.append(line)
            history_order = first_parent_order(root, history_ref)
            if (
                project_history.returncode == 0
                and project_commits
                and project_commits[-1] in history_order
                and not any(
                    str(pathlib.PurePosixPath(path).parent)
                    != relative_project_root
                    for path in historical_project_files
                )
            ):
                project_metadata = (project_commits[-1], history_order)
        cache[project_cache_key] = project_metadata
    project_metadata = cache[project_cache_key]
    if project_metadata is None:
        cache[cache_key] = False
        return False
    project_creation, history_order = project_metadata

    for target_path in target_paths:
        target_history = git_result(
            root,
            "log",
            "--first-parent",
            "--follow",
            "--format=@@COMMIT@@%H",
            "--name-only",
            history_ref,
            "--",
            target_path,
        )
        commits = []
        historical_target_paths = []
        for line in target_history.stdout.splitlines():
            if line.startswith("@@COMMIT@@"):
                commits.append(line.removeprefix("@@COMMIT@@"))
            elif line:
                historical_target_paths.append(line)
        if (
            target_history.returncode != 0
            or not commits
            or commits[-1] not in history_order
            or history_order[commits[-1]] > history_order[project_creation]
            or any(
                not (
                    pathlib.PurePosixPath(path)
                    == pathlib.PurePosixPath(relative_project_root)
                    or pathlib.PurePosixPath(relative_project_root)
                    in pathlib.PurePosixPath(path).parents
                )
                for path in historical_target_paths
            )
        ):
            cache[cache_key] = False
            return False

    cache[cache_key] = True
    return True


def latest_closed_attempt(closed_prs, test_name):
    candidates = []
    for pr in closed_prs:
        haystack = f"{pr.get('title', '')}\n{pr.get('body', '')}"
        if test_name not in haystack:
            continue
        timestamps = []
        if pr.get("trusted_closed") and pr.get("closed_at"):
            timestamps.append((pr["closed_at"], "trusted-closed-quarantine-attempt"))
        if pr.get("quarantine_label_added_at"):
            timestamps.append((pr["quarantine_label_added_at"], "quarantine-suppression-label"))
        for timestamp, reason in timestamps:
            candidates.append({
                "utc": timestamp,
                "reason": reason,
                "pull_request": pr.get("number"),
            })
    return max(candidates, key=lambda item: parse_utc(item["utc"]), default=None)


def github_changed_paths(items):
    files = set()
    for item in items:
        files.add(item["filename"])
        if item.get("previous_filename"):
            files.add(item["previous_filename"])
    return files


def github_pr_files(repository, pr_number, token):
    if not token:
        raise ValueError("A GitHub token is required to inspect pull request files")

    url = f"https://api.github.com/repos/{repository}/pulls/{pr_number}/files?per_page=100"
    files = set()
    while url:
        request = urllib.request.Request(
            url,
            headers={
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github+json",
                "User-Agent": "aspnetcore-test-quarantine",
            },
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            files.update(github_changed_paths(json.load(response)))
            link = response.headers.get("Link", "")
        url = None
        for part in link.split(","):
            if 'rel="next"' in part:
                url = part.split("<", 1)[1].split(">", 1)[0]
                break
    return files


def github_commit_contains(repository, ancestor, descendant, token):
    if not token:
        raise ValueError("A GitHub token is required to compare build commits")

    comparison = (
        f"https://api.github.com/repos/{repository}/compare/"
        f"{urllib.parse.quote(ancestor, safe='')}..."
        f"{urllib.parse.quote(descendant, safe='')}"
    )
    status = None
    last_error = None
    for attempt in range(3):
        request = urllib.request.Request(
            comparison,
            headers={
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github+json",
                "User-Agent": "aspnetcore-test-quarantine",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                status = json.load(response).get("status")
            break
        except urllib.error.HTTPError as error:
            if error.code in (404, 422):
                raise ValueError(
                    f"GitHub could not compare {ancestor} to {descendant}"
                ) from error
            if error.code != 403 and error.code < 500:
                raise
            last_error = error
        except OSError as error:
            last_error = error
        time.sleep(2 * (attempt + 1))
    if status is None:
        raise last_error or ValueError("GitHub comparison returned no status")
    if status in ("ahead", "identical"):
        return True
    if status in ("behind", "diverged"):
        return False
    raise ValueError(f"Unexpected GitHub comparison status: {status!r}")


def commit_contains(root, repository, ancestor, descendant, token):
    if not ancestor or not descendant:
        raise ValueError("Both ancestor and descendant commits are required")

    local = git_result(root, "merge-base", "--is-ancestor", ancestor, descendant)
    if local.returncode == 0:
        return True
    descendant_exists = git_result(
        root,
        "cat-file",
        "-e",
        f"{descendant}^{{commit}}",
    ).returncode == 0
    if local.returncode == 1 and descendant_exists:
        return False
    return github_commit_contains(repository, ancestor, descendant, token)


def source_c_failure_records(source_c):
    records = {}
    for item in source_c:
        build_id = item.get("build")
        if not isinstance(build_id, int):
            continue
        for match in SOURCE_C_FAILURE_PATTERN.finditer(item.get("fail_blocks", "")):
            test_name = match.group("test").split("(", 1)[0].strip()
            if not test_name or test_name.endswith(WORK_ITEM_SUFFIX):
                continue
            record = records.setdefault(test_name, {"builds": []})
            if build_id not in record["builds"]:
                record["builds"].append(build_id)
    return records


def collect(
    part1,
    part1_bytes,
    root,
    closed_prs,
    repository,
    ref,
    commit,
    pr_files_provider=None,
    commit_contains_provider=None,
    history_ref="HEAD",
):
    root = pathlib.Path(root)
    token = os.environ.get("GH_TOKEN", "")
    pr_files_provider = pr_files_provider or (
        lambda pr: github_pr_files(repository, pr, token)
    )
    commit_contains_provider = commit_contains_provider or (
        lambda ancestor, descendant: commit_contains(
            root,
            repository,
            ancestor,
            descendant,
            token,
        )
    )
    source_index = build_source_index(root)
    history_commit = git(root, "rev-parse", "--verify", history_ref)
    history_order = first_parent_order(root, history_ref)
    builds = part1.get("builds", {})
    source_a = part1.get("source_a", {})
    source_b = part1.get("source_b", {})
    source_c = source_c_failure_records(part1.get("source_c", []))
    test_names = sorted(set(source_a) | set(source_b) | set(source_c))
    receipts = {}
    pr_files_cache = {}
    ancestry_cache = {}
    project_history_cache = {}
    assembly_history_cache = {}
    method_history_cache = {}
    type_history_cache = {}
    historical_source_cache = {}
    historical_content_cache = {}

    for test_name in test_names:
        reasons = []
        record_a = source_a.get(test_name)
        record_b = source_b.get(test_name)
        record_c = source_c.get(test_name)
        raw_builds = sorted({
            *([] if not record_a else record_a.get("builds", [])),
            *([] if not record_b else record_b.get("builds", [])),
            *([] if not record_c else record_c.get("builds", [])),
        })
        receipt = {
            "status": "unproven",
            "originating_case": "work-item" if test_name.endswith(WORK_ITEM_SUFFIX) else "unknown",
            "source_resolution": {"status": "not-attempted"},
            "current_quarantine_state": "unknown",
            "latest_quarantine_transition": "unknown",
            "is_consistent_regression": False if not record_a else record_a.get("is_consistent_regression"),
            "raw_failure_builds": raw_builds,
            "excluded_builds": [],
            "cutoff": None,
            "required_ancestor": None,
            "ancestry_verified_builds": [],
            "eligible_failure_builds": [],
            "case_b_eligible": False,
            "case_b_issue": None,
            "evidence": None,
            "reasons": reasons,
        }
        receipts[test_name] = receipt
        if test_name.endswith(WORK_ITEM_SUFFIX):
            receipt["status"] = "ineligible"
            reasons.append("work-item-record")
            continue

        source = resolve_source(root, test_name, source_index)
        receipt["source_resolution"] = source
        if source["status"] != "exact":
            reasons.append(f"source-{source['status']}")
            continue
        if not project_history_is_complete(
            root,
            source["project_root"],
            [location["path"] for location in source["history_locations"]],
            history_ref,
            project_history_cache,
        ):
            reasons.append("project-history-incomplete")
            continue
        if any(
            (
                target_identity_rename_detected(
                    root,
                    location["path"],
                    location["type"],
                    location["method"],
                    history_ref,
                )
                or (
                    location["method"] is not None
                    and target_identity_rename_detected(
                        root,
                        location["path"],
                        location["type"],
                        None,
                        history_ref,
                    )
                )
                or namespace_rename_detected(
                    root,
                    location["path"],
                    location["type"],
                    history_ref,
                )
            )
            for location in source["history_locations"]
        ):
            reasons.append("target-identity-rename-ambiguous")
            continue
        if source["assembly_quarantine_ambiguous"]:
            reasons.append("current-assembly-association-ambiguous")
            continue

        quarantined = (
            source["method_quarantined"]
            or source["type_quarantined"]
            or source["assembly_quarantined"]
        )
        receipt["current_quarantine_state"] = (
            "quarantined" if quarantined else "not-quarantined"
        )
        transitions = []
        for location in source["history_locations"]:
            if location["method"] is None:
                continue
            events = method_quarantine_transitions(
                root,
                location["project_root"],
                location["type"],
                location["method"],
                history_ref,
                method_history_cache,
                historical_source_cache,
                historical_content_cache,
            )
            transitions.append(events[0] if events else {"status": "none"})
        transitions.append(type_quarantine_transition(
            root,
            source["assembly_project_root"],
            test_name,
            history_ref,
            type_history_cache,
            historical_source_cache,
            historical_content_cache,
        ))
        transitions.append(assembly_quarantine_transition(
            root,
            source["assembly_project_root"],
            test_name,
            history_ref,
            assembly_history_cache,
            historical_source_cache,
            historical_content_cache,
        ))
        if any(item["status"] == "ambiguous" for item in transitions):
            transition = {"status": "ambiguous"}
        else:
            changed_transitions = [
                item for item in transitions
                if item["status"] in ("added", "removed") and item.get("utc")
            ]
            transition = newest_history_item(
                changed_transitions,
                history_order,
            ) if changed_transitions else {"status": "none"}
        receipt["latest_quarantine_transition"] = transition["status"]
        if transition["status"] == "ambiguous":
            reasons.append("quarantine-history-ambiguous")
            continue
        if quarantined:
            receipt["status"] = "ineligible"
            receipt["originating_case"] = "already-quarantined"
            reasons.append("currently-quarantined")
            continue
        case_b = transition["status"] == "removed"
        if case_b:
            receipt["case_b_issue"] = transition.get("issue")
            if not isinstance(receipt["case_b_issue"], int):
                receipt["status"] = "ineligible"
                reasons.append("original-quarantine-issue-unproven")
                continue
            transition_cutoff = {
                "utc": transition["utc"],
                "reason": "latest-quarantine-transition",
                "commit": transition["commit"],
            }
            file_cutoffs = [
                latest_file_change(root, location["path"], history_ref)
                for location in source["history_locations"]
            ]
            history_cutoff = newest_history_item(
                [transition_cutoff, *file_cutoffs],
                history_order,
            )
            receipt["originating_case"] = "case-b"
            reasons.append("latest-quarantine-transition-removed")
        elif transition["status"] != "none":
            reasons.append(f"quarantine-history-{transition['status']}")
            continue
        else:
            receipt["originating_case"] = "case-a"
            history_cutoffs = [
                latest_file_change(root, location["path"], history_ref)
                for location in source["history_locations"]
            ]
            history_cutoff = newest_history_item(
                history_cutoffs,
                history_order,
            )

        cutoffs = [history_cutoff]
        prior_attempt = latest_closed_attempt(closed_prs, test_name)
        if prior_attempt:
            cutoffs.append(prior_attempt)
        receipt["cutoff"] = max(cutoffs, key=lambda item: parse_utc(item["utc"]))
        cutoff_utc = parse_utc(receipt["cutoff"]["utc"])
        required_ancestor = history_cutoff["commit"]
        receipt["required_ancestor"] = required_ancestor

        included = set()
        for build_id in raw_builds:
            metadata = builds.get(str(build_id))
            if not metadata or not parse_utc(metadata.get("startedUtc")):
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "missing-build-metadata",
                })
                continue
            if parse_utc(metadata["startedUtc"]) <= cutoff_utc:
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "not-after-cutoff",
                })
                continue
            source_version = metadata.get("sourceVersion")
            if not source_version:
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "missing-source-version",
                })
                continue
            ancestry_key = (required_ancestor, source_version)
            if ancestry_key not in ancestry_cache:
                try:
                    ancestry_cache[ancestry_key] = commit_contains_provider(
                        required_ancestor,
                        source_version,
                    )
                except Exception:
                    ancestry_cache[ancestry_key] = None
            contains_cutoff = ancestry_cache[ancestry_key]
            if contains_cutoff is None:
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "source-version-ancestry-unavailable",
                })
                continue
            if contains_cutoff is not True:
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "source-version-before-cutoff",
                })
                continue
            receipt["ancestry_verified_builds"].append(build_id)
            pr_number = metadata.get("pr")
            if (
                record_b
                and build_id in record_b.get("builds", [])
                and not isinstance(pr_number, int)
            ):
                receipt["excluded_builds"].append({
                    "build": build_id,
                    "reason": "source-b-pr-unavailable",
                })
                continue
            if isinstance(pr_number, int):
                if pr_number not in pr_files_cache:
                    try:
                        pr_files_cache[pr_number] = pr_files_provider(pr_number)
                    except Exception:
                        pr_files_cache[pr_number] = None
                changed_files = pr_files_cache[pr_number]
                if changed_files is None:
                    receipt["excluded_builds"].append({
                        "build": build_id,
                        "reason": "source-b-files-unavailable",
                    })
                    continue
                if any(
                    location["path"] in changed_files
                    for location in source["history_locations"]
                ):
                    receipt["excluded_builds"].append({
                        "build": build_id,
                        "reason": "source-b-pr-changed-test-file",
                    })
                    continue
            included.add(build_id)

        receipt["eligible_failure_builds"] = sorted(included)
        if case_b:
            receipt["status"] = "ineligible"
            if included:
                receipt["case_b_eligible"] = True
            else:
                reasons.append("no-post-cutoff-failures")
            continue
        if record_a is not None and record_a.get("is_consistent_regression") is not False:
            receipt["status"] = "ineligible"
            reasons.append("consistent-regression-or-unproven")
            continue
        if len(included) < 2:
            receipt["status"] = "ineligible"
            reasons.append("fewer-than-two-post-cutoff-failures")
            continue

        receipt["status"] = "eligible"
        evidence_records = [record for record in (record_a, record_b) if record]
        evidence_candidates = []
        for record in evidence_records:
            build_id = record.get("evidence_build")
            if (
                build_id in included
                and isinstance(record.get("run_id"), int)
                and isinstance(record.get("result_id"), int)
            ):
                evidence_candidates.append({
                    "build": build_id,
                    "run_id": record["run_id"],
                    "result_id": record["result_id"],
                    "started": parse_utc(builds[str(build_id)]["startedUtc"]),
                })
        if not evidence_candidates:
            reasons.append("eligible-evidence-identity-unavailable")
            continue
        evidence = max(evidence_candidates, key=lambda item: item["started"])
        evidence.pop("started")
        receipt["evidence"] = evidence

    return {
        "schema_version": 1,
        "part1_sha256": hashlib.sha256(part1_bytes).hexdigest(),
        "repository": repository,
        "ref": ref,
        "commit": commit,
        "history_ref": history_ref,
        "history_commit": history_commit,
        "tests": receipts,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--part1", required=True)
    parser.add_argument("--closed-prs", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--commit", required=True)
    parser.add_argument(
        "--history-ref",
        default="refs/remotes/origin/main",
    )
    args = parser.parse_args()

    part1_bytes = pathlib.Path(args.part1).read_bytes()
    part1 = json.loads(part1_bytes)
    closed_prs = json.loads(pathlib.Path(args.closed_prs).read_text(encoding="utf-8"))
    receipt = collect(
        part1,
        part1_bytes,
        args.repo_root,
        closed_prs,
        args.repository,
        args.ref,
        args.commit,
        history_ref=args.history_ref,
    )
    pathlib.Path(args.output).write_text(
        json.dumps(receipt, separators=(",", ":"), sort_keys=True),
        encoding="utf-8",
    )
    counts = {}
    exclusion_counts = {}
    for record in receipt["tests"].values():
        counts[record["status"]] = counts.get(record["status"], 0) + 1
        for excluded in record.get("excluded_builds", []):
            reason = excluded["reason"]
            exclusion_counts[reason] = exclusion_counts.get(reason, 0) + 1
    print(f"Quarantine eligibility receipts: {counts}")
    print(f"Quarantine evidence exclusions: {exclusion_counts}")


if __name__ == "__main__":
    main()
