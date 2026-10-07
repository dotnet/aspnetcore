#!/usr/bin/env python3

import argparse
import json
import os
import re
import subprocess
from pathlib import Path
from typing import Any, Callable, Sequence


SOURCE_REPOSITORY = "dotnet/aspnetcore"
RESTRICTED = re.compile(
    r"\b(?:vulnerabilit\w*|exploit\w*|CVE(?:-\d{4}-\d+)?|GHSA(?:-[\w-]+)?|"
    r"advisory|coordinated[- ]disclosure|MSRC|security[- ]fix(?:es)?|reported[- ]weakness)\b",
    re.IGNORECASE,
)
NON_SHIPPING = re.compile(
    r"(?:^|/)(?:tests?|testassets|samples|benchmarks|perf|obj|bin)(?:/|$)|"
    r"(?:Tests|Test|FunctionalTests|E2ETests|E2E\.Tests)/",
    re.IGNORECASE,
)
HUNK = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")
BODY_SIGNALS = (
    ("explicit_breaking", re.compile(r"\bbreaking[- ]change\b", re.IGNORECASE)),
    ("explicit_user_facing", re.compile(
        r"\b(?:user[- ]facing|new default|configuration behavior|migration (?:guidance|required))\b",
        re.IGNORECASE,
    )),
)


def source_preflight(repository: str, number: str, pr: Any) -> dict[str, Any]:
    result = {
        "schema_version": 1,
        "source_repository": repository,
        "requested_pr_number": number,
        "source_pr_number": int(number) if re.fullmatch(r"[1-9][0-9]*", number) else None,
        "status": "invalid",
        "reason": "The request does not identify an existing source pull request.",
    }
    if repository != SOURCE_REPOSITORY or result["source_pr_number"] is None or pr is None:
        return result
    if not isinstance(pr, dict) or pr.get("number") != result["source_pr_number"]:
        raise ValueError("Source PR metadata does not match the requested pull request.")
    base = pr.get("base") or {}
    if (base.get("repo") or {}).get("full_name", "").lower() != SOURCE_REPOSITORY:
        raise ValueError("Source PR metadata describes another repository.")
    metadata = "\n".join([
        pr.get("title") or "", pr.get("body") or "",
        *(label["name"] for label in pr.get("labels", []) if label.get("name")),
    ])
    if RESTRICTED.search(metadata):
        result.update(status="restricted", reason="Automated documentation processing is excluded for this change.")
    elif pr.get("merged") is not True or not pr.get("merged_at"):
        result.update(status="ineligible", reason="The source pull request is not merged.")
    elif base.get("ref") != "main":
        result.update(status="ineligible", reason="The source pull request was not merged into main.")
    else:
        result.update(status="eligible", reason="The source pull request was merged into main.")
    return result


def changed_lines(patch: str) -> list[dict[str, Any]]:
    lines = []
    old = new = 0
    in_hunk = False
    for text in patch.splitlines():
        match = HUNK.match(text)
        if match:
            old, new = int(match[1]), int(match[3])
            in_hunk = True
        elif in_hunk and text.startswith("+"):
            lines.append({"side": "new", "line": new, "text": text[1:]})
            new += 1
        elif in_hunk and text.startswith("-"):
            lines.append({"side": "old", "line": old, "text": text[1:]})
            old += 1
        elif in_hunk and text.startswith(" "):
            old += 1
            new += 1
    return lines


def patch_status(file: dict[str, Any]) -> str:
    patch = file.get("patch")
    if not isinstance(patch, str) or not patch:
        return "missing"
    lines = changed_lines(patch)
    if (
        sum(line["side"] == "new" for line in lines) != file["additions"]
        or sum(line["side"] == "old" for line in lines) != file["deletions"]
    ):
        return "incomplete"
    expected_old = expected_new = actual_old = actual_new = None
    for text in patch.splitlines():
        match = HUNK.match(text)
        if match:
            if expected_old is not None and (actual_old, actual_new) != (expected_old, expected_new):
                return "incomplete"
            expected_old = int(match[2]) if match[2] is not None else 1
            expected_new = int(match[4]) if match[4] is not None else 1
            actual_old = actual_new = 0
        elif expected_old is not None:
            if text.startswith((" ", "-")):
                actual_old += 1
            if text.startswith((" ", "+")):
                actual_new += 1
    if expected_old is None or (actual_old, actual_new) != (expected_old, expected_new):
        return "incomplete"
    return "available"


def compute_signals(pr: dict[str, Any], files: list[dict[str, Any]]) -> dict[str, Any]:
    evidence = []
    missing = []
    for file in files:
        filename = file["filename"]
        paths = [filename, file.get("previous_filename") or filename]
        if NON_SHIPPING.search(filename) or not (
            filename.startswith("src/") or filename in {"Directory.Build.props", "Directory.Build.targets"}
        ):
            continue
        categories = set()
        if any(re.search(r"/ref/[^/]+\.cs$|/PublicAPI\.(?:Shipped|Unshipped)\.txt$", path) for path in paths):
            categories.add("public_api")
        if any(path.startswith("src/ProjectTemplates/") and (
            "/content/" in path or "/.template.config/" in path
        ) for path in paths):
            categories.add("project_template")
        if any(re.search(r"(?:Options|Defaults|Configuration|Constants)\.cs$", path) for path in paths):
            categories.add("defaults_configuration")
        if any(re.search(r"/(?:Analyzers|Diagnostics)/|(?:DiagnosticDescriptors|DiagnosticIds)\.cs$", path) for path in paths):
            categories.add("analyzers_diagnostics")
        lines = changed_lines(file.get("patch") or "")
        for line in lines:
            if filename.endswith((".csproj", ".props", ".targets")) and re.search(
                r"<TargetFrameworks?>", line["text"],
            ):
                evidence.append({"kind": "target_framework", "file": filename, **line})
            if filename.endswith(".cs") and re.search(
                r"\[DefaultValue(?:Attribute)?\s*\(|\b(?:Default\w*|Configure\w*|"
                r"GetSection|GetValue|GetConnectionString)\s*(?:<[^>]+>)?\s*[=(]|"
                r"\bpublic\s+.*\{\s*get;\s*(?:set;|init;)\s*\}\s*=",
                line["text"],
            ):
                categories.add("defaults_configuration")
            if filename.endswith(".cs") and re.match(
                r"\s*(?:public|protected)\s+(?:(?:static|sealed|abstract|partial|readonly)\s+)*"
                r"(?:class|interface|struct|record|enum|delegate)\b",
                line["text"],
            ):
                categories.add("public_api")
        for kind in sorted(categories):
            relevant = [line for line in lines if line["text"].strip() and not line["text"].lstrip().startswith(
                ("//", "#nullable", "# Public API", "/*", "*"),
            )]
            evidence.append({
                "kind": kind, "file": filename, "basis": "candidate_requires_review",
                "lines": relevant[:8], "more_matching_lines": len(relevant) > 8,
            })
        if patch_status(file) != "available":
            missing.append({"file": filename, "patch_status": patch_status(file)})
    for field in ("title", "body"):
        for kind, pattern in BODY_SIGNALS:
            for number, text in enumerate((pr.get(field) or "").splitlines(), 1):
                if pattern.search(text):
                    evidence.append({"kind": kind, "field": field, "line": number, "text": text})
    for label in pr.get("labels", []):
        if re.fullmatch(r"breaking[- ]change", label.get("name", ""), re.IGNORECASE):
            evidence.append({"kind": "explicit_breaking", "field": "labels", "text": label["name"]})
    complete = len(files) == pr.get("changed_files")
    return {
        "schema_version": 1,
        "source_repository": SOURCE_REPOSITORY,
        "source_pr_number": pr["number"],
        "recommendation": "review_evidence" if evidence or missing or not complete else "no_signal",
        "evidence": evidence,
        "missing_patches": missing,
        "file_list_complete": complete,
        "confidence_floors_when_confirmed": {
            "public_api": 70, "project_template": 70, "defaults_configuration": 75, "explicit_breaking": 80,
        },
    }


def build_context(pr: dict[str, Any], files: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "schema_version": 1,
        "source_repository": SOURCE_REPOSITORY,
        **{key: pr.get(key) for key in (
            "number", "html_url", "title", "body", "state", "merged", "merged_at", "merge_commit_sha",
        )},
        "author": {key: (pr.get("user") or {}).get(key) for key in ("login", "type")},
        "milestone": {key: (pr.get("milestone") or {}).get(key) for key in ("title", "number")},
        "labels": [label["name"] for label in pr.get("labels", []) if label.get("name")],
        "base": {
            **{key: pr["base"].get(key) for key in ("ref", "sha")},
            "repository": (pr["base"].get("repo") or {}).get("full_name"),
        },
        "head": {
            **{key: (pr.get("head") or {}).get(key) for key in ("ref", "sha")},
            "repository": ((pr.get("head") or {}).get("repo") or {}).get("full_name"),
        },
        "changed_file_count": pr.get("changed_files"),
        "cached_file_count": len(files),
        "changed_files": [
            {
                **{key: file.get(key) for key in (
                    "filename", "previous_filename", "status", "additions", "deletions", "changes", "sha",
                )},
                "patch_status": patch_status(file),
            }
            for file in files
        ],
    }


def github_api(endpoint: str, token: str | None = None) -> Any:
    command = ["gh", "api", "--method", "GET"]
    paginated = "per_page=100" in endpoint
    if paginated:
        command.extend(["--paginate", "--slurp"])
    response = subprocess.run(
        [*command, endpoint], capture_output=True, text=True, encoding="utf-8",
        **({"env": {**os.environ, "GH_TOKEN": token}} if token else {}),
    )
    if response.returncode:
        if not paginated and "HTTP 404" in response.stderr:
            return None
        raise RuntimeError(f"GitHub API read failed: {response.stderr.strip()}")
    payload = json.loads(response.stdout)
    return [item for page in payload for item in page] if paginated else payload


def prepare_context(
    repository: str, number: str, directory: Path, api: Callable[[str], Any] = github_api,
    metadata_only: bool = False,
) -> dict[str, Any]:
    directory.mkdir(parents=True, exist_ok=True)
    pr = api(f"/repos/{repository}/pulls/{number}") if (
        repository == SOURCE_REPOSITORY and re.fullmatch(r"[1-9][0-9]*", number)
    ) else None
    gate = source_preflight(repository, number, pr)
    write_json(directory / "source-preflight.json", gate)
    write_json(directory / "existing-draft.json", {"found": False, "blocked": False})
    if gate["status"] != "eligible":
        return gate
    write_json(directory / "source-pr.json", pr)
    if metadata_only:
        return gate
    prepare_analysis(repository, number, directory, pr, api)
    return gate


def prepare_analysis(
    repository: str, number: str, directory: Path, pr: dict[str, Any],
    api: Callable[[str], Any] = github_api,
) -> None:
    files = api(f"/repos/{repository}/pulls/{number}/files?per_page=100")
    if not isinstance(files, list) or any(not isinstance(file, dict) for file in files):
        raise ValueError("Changed-file metadata must be a JSON array of objects.")
    write_json(directory / "pr.json", build_context(pr, files))
    write_json(directory / "files.json", files)
    write_json(directory / "signals.json", compute_signals(pr, files))


def write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-repository", required=True)
    parser.add_argument("--source-pr-number", required=True)
    parser.add_argument("--output-directory", required=True, type=Path)
    args = parser.parse_args(argv)
    prepare_context(args.source_repository, args.source_pr_number, args.output_directory)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
