#!/usr/bin/env python3

import argparse
import json
import re
from pathlib import Path
from typing import Any, Sequence


class VersionResolutionError(ValueError):
    pass


def _load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise VersionResolutionError(f"File not found: {path}.") from error
    except json.JSONDecodeError as error:
        raise VersionResolutionError(f"Invalid JSON in {path}: {error}.") from error


def _policy_value(policy: dict[str, Any], name: str) -> str:
    entry = policy.get(name)
    value = entry.get("value") if isinstance(entry, dict) else None
    description = entry.get("description") if isinstance(entry, dict) else None
    if not isinstance(value, str) or not value:
        raise VersionResolutionError(f"Policy field {name}.value must be a non-empty string.")
    if not isinstance(description, str) or not description:
        raise VersionResolutionError(f"Policy field {name}.description must be a non-empty string.")
    return value


def _parse_version(value: str, field_name: str) -> tuple[int, int]:
    match = re.fullmatch(r"([0-9]+)\.([0-9]+)", value)
    if match is None:
        raise VersionResolutionError(f"{field_name} must be a major.minor version; received {value!r}.")
    return int(match.group(1)), int(match.group(2))


def _extract_branch_names(payload: Any) -> list[str]:
    if not isinstance(payload, list):
        raise VersionResolutionError("Release branch payload must be a JSON array.")

    names: list[str] = []
    for entry in payload:
        if isinstance(entry, str):
            name = entry
        elif isinstance(entry, dict) and isinstance(entry.get("name"), str):
            name = entry["name"]
        else:
            raise VersionResolutionError("Every release branch entry must be a string or an object with a name.")
        names.append(name)
    return names


def resolve_target_version(
    policy: dict[str, Any],
    pull_request: dict[str, Any],
    release_branches: Any,
) -> dict[str, Any]:
    if policy.get("schemaVersion") != 1:
        raise VersionResolutionError(f"Unsupported version policy schema: {policy.get('schemaVersion')}.")

    main_version = _policy_value(policy, "mainVersion")
    release_pattern = re.compile(_policy_value(policy, "releaseBranchPattern"))
    milestone_pattern = re.compile(_policy_value(policy, "milestoneVersionPattern"), re.IGNORECASE)
    moniker_prefix = _policy_value(policy, "docsMonikerPrefix")
    docs_base_branch = _policy_value(policy, "docsBaseBranch")

    base = pull_request.get("base")
    base_ref = base.get("ref") if isinstance(base, dict) else None
    if not isinstance(base_ref, str) or not base_ref:
        raise VersionResolutionError("Pull request base.ref must be a non-empty string.")

    observed_versions: list[tuple[int, int]] = []
    observed_release_branches: list[str] = []
    for branch_name in _extract_branch_names(release_branches):
        match = release_pattern.fullmatch(branch_name)
        if match is None:
            continue
        version = match.groupdict().get("version")
        if not version:
            raise VersionResolutionError("releaseBranchPattern must define a named version group.")
        observed_versions.append(_parse_version(version, f"Version from {branch_name}"))
        observed_release_branches.append(branch_name)

    if not observed_versions:
        raise VersionResolutionError("No release branches matched the configured releaseBranchPattern.")

    highest_release = max(observed_versions)
    configured_main = _parse_version(main_version, "mainVersion")
    expected_main = (highest_release[0] + 1, 0)
    if configured_main != expected_main:
        raise VersionResolutionError(
            f"Configured mainVersion {main_version} disagrees with the highest release branch "
            f"{highest_release[0]}.{highest_release[1]}; expected {expected_main[0]}.{expected_main[1]}."
        )

    if base_ref == "main":
        resolved = configured_main
        resolution = "configured_main_verified_by_release_branches"
    else:
        base_match = release_pattern.fullmatch(base_ref)
        if base_match is None:
            raise VersionResolutionError(f"Unsupported pull request base branch: {base_ref}.")
        version = base_match.groupdict().get("version")
        if not version:
            raise VersionResolutionError("releaseBranchPattern must define a named version group.")
        resolved = _parse_version(version, "Pull request base version")
        resolution = "source_release_branch"

    milestone = pull_request.get("milestone")
    milestone_title = milestone.get("title") if isinstance(milestone, dict) else None
    milestone_version: tuple[int, int] | None = None
    if isinstance(milestone_title, str) and milestone_title:
        milestone_match = milestone_pattern.match(milestone_title)
        if milestone_match is not None:
            version = milestone_match.groupdict().get("version")
            if not version:
                raise VersionResolutionError("milestoneVersionPattern must define a named version group.")
            milestone_version = _parse_version(version, "Milestone version")
            if milestone_version != resolved:
                raise VersionResolutionError(
                    f"Milestone {milestone_title!r} resolves to {version}, but base branch {base_ref!r} "
                    f"resolves to {resolved[0]}.{resolved[1]}."
                )

    version_text = f"{resolved[0]}.{resolved[1]}"
    previous_version = f"{resolved[0] - 1}.0"
    return {
        "schema_version": 1,
        "source_pr_base": base_ref,
        "target_version": version_text,
        "previous_version": previous_version,
        "docs_moniker": f"{moniker_prefix}{version_text}",
        "docs_moniker_range": f">= {moniker_prefix}{version_text}",
        "docs_base_branch": docs_base_branch,
        "migration_directory": f"aspnetcore/migration/{previous_version.replace('.', '')}-to-{version_text.replace('.', '')}",
        "breaking_changes_directory": f"aspnetcore/breaking-changes/{resolved[0]}",
        "release_notes_directory": f"aspnetcore/release-notes/aspnetcore-{resolved[0]}",
        "resolution": resolution,
        "configured_main_version": main_version,
        "highest_observed_release_version": f"{highest_release[0]}.{highest_release[1]}",
        "observed_release_branches": sorted(observed_release_branches),
        "milestone": milestone_title,
        "milestone_corroborated": milestone_version is not None,
    }


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--policy", required=True, type=Path)
    parser.add_argument("--pull-request", required=True, type=Path)
    parser.add_argument("--release-branches", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args(argv)

    try:
        result = resolve_target_version(
            _load_json(args.policy),
            _load_json(args.pull_request),
            _load_json(args.release_branches),
        )
    except VersionResolutionError as error:
        print(f"::error::{error}")
        return 1

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
