#!/usr/bin/env python3

import argparse
import datetime
import hashlib
import json
import os
import pathlib
import re
import subprocess
import urllib.request


WORK_ITEM_SUFFIX = ".WorkItemExecution"
QUARANTINE = "QuarantinedTest"
ASSEMBLY_QUARANTINE_PATTERN = re.compile(
    r"\[\s*assembly\s*:\s*QuarantinedTest\b"
)
METHOD_PATTERN = re.compile(
    r"(?m)^[ \t]*(?:public|internal|protected|private)\s+"
    r"(?:(?:static|virtual|override|sealed|async|new|unsafe|partial|extern)\s+)*"
    r"(?:[A-Za-z_][A-Za-z0-9_?.<>\[\],]*\s+)+"
    r"(?P<method>[A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>{}]*>)?\s*\("
)


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
            quarantined = QUARANTINE in attribute_block(lines, declaration_line)
            if quarantined:
                type_quarantines[(str(project_root), type_name)] = True
            type_index.setdefault(type_name, []).append({
                "type": type_name,
                "base": entry[5],
                "path": relative_path,
                "project_root": str(project_root),
                "quarantined": quarantined,
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
            method_index.setdefault(method, []).append({
                "path": relative_path,
                "type": type_name,
                "method": method,
                "method_line": method_line + 1,
                "project_root": project_root,
                "method_quarantined": QUARANTINE in attribute_block(lines, method_line),
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
    }


def resolve_source(root, test_name, source_index=None):
    method = test_name.rsplit(".", 1)[-1]
    expected_type = normalize_type_name(test_name.rsplit(".", 1)[0])
    source_index = source_index or build_source_index(root)
    matches = [
        entry for entry in source_index["methods"].get(method, [])
        if entry["type"] == expected_type
    ]
    runner_types = []
    if not matches:
        current_type = expected_type
        visited = set()
        while current_type not in visited:
            visited.add(current_type)
            declarations = source_index["types"].get(current_type, [])
            if len(declarations) != 1 or not declarations[0].get("base"):
                break
            runner_types.append(declarations[0])
            base_name = normalize_type_name(declarations[0]["base"])
            namespace = current_type.rsplit(".", 1)[0]
            candidates = [
                name for name in source_index["types"]
                if name == f"{namespace}.{base_name}" or name.endswith(f".{base_name}")
            ]
            if len(candidates) != 1:
                break
            current_type = candidates[0]
            matches = [
                entry for entry in source_index["methods"].get(method, [])
                if entry["type"] == current_type
            ]
            if matches:
                break
    if len(matches) != 1:
        return {
            "status": "missing" if not matches else "ambiguous",
            "matches": matches[:5],
        }
    result = dict(matches[0])
    result["status"] = "exact"
    result["declaring_type"] = result["type"]
    result["type"] = expected_type
    result["type_quarantined"] = (
        result["type_quarantined"]
        or any(entry["quarantined"] for entry in runner_types)
    )
    assembly_declaration = runner_types[0] if runner_types else result
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
        "path": entry["path"],
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
    return result


def historical_assembly_state(root, project_root, commit, state_cache):
    cache_key = (project_root, commit)
    if cache_key in state_cache:
        return state_cache[cache_key]
    if commit is None:
        return {"status": "exact", "quarantined": False}

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

    state = {
        "status": "exact",
        "quarantined": bool(assembly_files),
        "paths": assembly_files,
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
            "scope": "assembly",
        })
    return {"status": "exact", "events": events}


def historical_project_source_index(
    root,
    project_root,
    commit,
    source_cache,
    content_cache,
):
    cache_key = (commit, project_root)
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
    methods = {}
    for relative_path in paths:
        if not relative_path.endswith(".cs"):
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
        clean = sanitize_csharp(content_cache[content_key])
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
            types.add(full_type_name(
                ranges,
                entry[4],
                file_namespace,
                declared_type=entry[3],
            ))
        for match in METHOD_PATTERN.finditer(clean):
            key = (
                full_type_name(ranges, match.start(), file_namespace),
                match.group("method"),
            )
            methods[key] = methods.get(key, 0) + 1

    result = {
        "status": "exact",
        "types": types,
        "methods": methods,
    }
    source_cache[cache_key] = result
    return result


def source_location_status(
    root,
    location,
    commit,
    source_cache,
    content_cache,
):
    relative_project_root = str(
        pathlib.Path(location["project_root"]).relative_to(root)
    ).replace(os.sep, "/")
    source_index = historical_project_source_index(
        root,
        relative_project_root,
        commit,
        source_cache,
        content_cache,
    )
    if source_index["status"] != "exact":
        return "ambiguous"
    if location["method"] is None:
        return "exact" if location["type"] in source_index["types"] else "missing"
    matches = source_index["methods"].get(
        (location["type"], location["method"]),
        0,
    )
    if matches > 1:
        return "ambiguous"
    return "exact" if matches == 1 else "missing"


def assembly_quarantine_transition(
    root,
    project_root,
    locations,
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
        location_statuses = [
            source_location_status(
                root,
                location,
                applicable_commit,
                source_cache,
                content_cache,
            )
            for location in locations
        ]
        if "ambiguous" in location_statuses:
            return {
                "status": "ambiguous",
                "commit": event["commit"],
                "utc": event["utc"],
            }
        if all(status == "exact" for status in location_statuses):
            return {
                key: value
                for key, value in event.items()
                if key != "parent"
            }
    return {"status": "none"}


def quarantine_transition(root, relative_path, method, type_name, history_ref):
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
        return {"status": "ambiguous"}
    if not history:
        return {"status": "none"}
    commits = re.split(r"(?=@@COMMIT@@)", history)
    for commit in commits:
        if not commit.startswith("@@COMMIT@@"):
            continue
        header, *patch_lines = commit.splitlines()
        metadata = header.removeprefix("@@COMMIT@@").split("\t", 1)
        sha = metadata[0]
        timestamp = metadata[1] if len(metadata) > 1 else None
        patch = "\n".join(patch_lines)
        relevant_hunks = []
        ambiguous = False
        type_short_name = type_name.rsplit(".", 1)[-1]
        for hunk in re.split(r"(?=^@@)", patch, flags=re.MULTILINE):
            hunk_lines = hunk.splitlines()
            changed = [
                (index, line) for index, line in enumerate(hunk_lines)
                if line.startswith(("+", "-")) and not line.startswith(("+++", "---"))
                and QUARANTINE in line
            ]
            if not changed:
                continue
            hunk_relevant = False
            hunk_ambiguous = False
            for change_index, changed_line in changed:
                if ASSEMBLY_QUARANTINE_PATTERN.search(changed_line):
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
                    hunk_relevant = True
                elif re.search(
                    rf"\b(?:class|struct|record)\s+{re.escape(type_short_name)}\b",
                    target,
                ):
                    hunk_relevant = True
                elif re.search(
                    r"\b(?:class|struct|record)\s+[A-Za-z_][A-Za-z0-9_]*\b|"
                    r"\b[A-Za-z_][A-Za-z0-9_]*\s*(?:<[^>{}]*>)?\s*\(",
                    target,
                ):
                    continue
                else:
                    hunk_ambiguous = True
            if hunk_relevant:
                relevant_hunks.append(hunk)
            elif hunk_ambiguous:
                ambiguous = True
        if not relevant_hunks:
            if ambiguous:
                return {"status": "ambiguous", "commit": sha, "utc": timestamp}
            continue
        relevant = "\n".join(relevant_hunks).splitlines()
        if any(line.startswith("-") and QUARANTINE in line for line in relevant):
            return {"status": "removed", "commit": sha, "utc": timestamp}
        if any(line.startswith("+") and QUARANTINE in line for line in relevant):
            return {"status": "added", "commit": sha, "utc": timestamp}
        return {"status": "ambiguous", "commit": sha, "utc": timestamp}
    return {"status": "none"}


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


def collect(
    part1,
    part1_bytes,
    root,
    closed_prs,
    repository,
    ref,
    commit,
    pr_files_provider=None,
    history_ref="HEAD",
):
    root = pathlib.Path(root)
    token = os.environ.get("GH_TOKEN", "")
    pr_files_provider = pr_files_provider or (
        lambda pr: github_pr_files(repository, pr, token)
    )
    source_index = build_source_index(root)
    history_commit = git(root, "rev-parse", "--verify", history_ref)
    builds = part1.get("builds", {})
    source_a = part1.get("source_a", {})
    source_b = part1.get("source_b", {})
    test_names = sorted(set(source_a) | set(source_b))
    receipts = {}
    pr_files_cache = {}
    assembly_history_cache = {}
    historical_source_cache = {}
    historical_content_cache = {}

    for test_name in test_names:
        reasons = []
        record_a = source_a.get(test_name)
        record_b = source_b.get(test_name)
        raw_builds = sorted({
            *([] if not record_a else record_a.get("builds", [])),
            *([] if not record_b else record_b.get("builds", [])),
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
            "eligible_failure_builds": [],
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
        transitions = [
            quarantine_transition(
                root,
                location["path"],
                location["method"],
                location["type"],
                history_ref,
            )
            for location in source["history_locations"]
        ]
        transitions.append(assembly_quarantine_transition(
            root,
            source["assembly_project_root"],
            source["assembly_history_locations"],
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
            transition = max(
                changed_transitions,
                key=lambda item: parse_utc(item["utc"]),
                default={"status": "none"},
            )
        receipt["latest_quarantine_transition"] = transition["status"]
        if transition["status"] == "ambiguous":
            reasons.append("quarantine-history-ambiguous")
            continue
        if quarantined:
            receipt["status"] = "ineligible"
            receipt["originating_case"] = "already-quarantined"
            reasons.append("currently-quarantined")
            continue
        if transition["status"] == "removed":
            receipt["cutoff"] = {
                "utc": transition["utc"],
                "reason": "latest-quarantine-transition",
                "commit": transition["commit"],
            }
            receipt["status"] = "ineligible"
            receipt["originating_case"] = "case-b"
            reasons.append("latest-quarantine-transition-removed")
            continue
        if transition["status"] != "none":
            reasons.append(f"quarantine-history-{transition['status']}")
            continue
        receipt["originating_case"] = "case-a"

        cutoffs = [
            latest_file_change(root, location["path"], history_ref)
            for location in source["history_locations"]
        ]
        prior_attempt = latest_closed_attempt(closed_prs, test_name)
        if prior_attempt:
            cutoffs.append(prior_attempt)
        receipt["cutoff"] = max(cutoffs, key=lambda item: parse_utc(item["utc"]))
        cutoff_utc = parse_utc(receipt["cutoff"]["utc"])

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
            if record_b and build_id in record_b.get("builds", []):
                pr_number = metadata.get("pr")
                if not isinstance(pr_number, int):
                    receipt["excluded_builds"].append({
                        "build": build_id,
                        "reason": "source-b-pr-unavailable",
                    })
                    continue
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
        if record_a is not None and record_a.get("is_consistent_regression") is not False:
            receipt["status"] = "ineligible"
            reasons.append("consistent-regression-or-unproven")
            continue
        if len(included) < 2:
            receipt["status"] = "ineligible"
            reasons.append("fewer-than-two-post-cutoff-failures")
            continue

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
        receipt["status"] = "eligible"

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
    for record in receipt["tests"].values():
        counts[record["status"]] = counts.get(record["status"], 0) + 1
    print(f"Case A eligibility receipts: {counts}")


if __name__ == "__main__":
    main()
