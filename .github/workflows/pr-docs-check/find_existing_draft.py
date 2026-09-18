#!/usr/bin/env python3

import argparse
import json
import re
from pathlib import Path
from typing import Any, Sequence


class DraftResolutionError(ValueError):
    pass


def find_existing_draft(
    pulls: Any,
    source_repository: str,
    source_pr_number: int,
    docs_repository: str,
    allowed_author: str,
) -> dict[str, Any]:
    if not isinstance(pulls, list):
        raise DraftResolutionError("Pull request payload must be a JSON array.")
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", source_repository):
        raise DraftResolutionError(f"Invalid source repository: {source_repository}.")
    if not isinstance(source_pr_number, int) or isinstance(source_pr_number, bool) or source_pr_number <= 0:
        raise DraftResolutionError(f"Invalid source PR number: {source_pr_number}.")

    marker = f"Source: {source_repository}#{source_pr_number}"
    matches: list[dict[str, Any]] = []
    expected_branch = re.compile(
        rf"docs/aspnetcore-pr-{source_pr_number}(?:-[a-f0-9]+)?"
    )
    for pull in pulls:
        if not isinstance(pull, dict):
            continue
        body = pull.get("body")
        base = pull.get("base")
        head = pull.get("head")
        head_repo = head.get("repo") if isinstance(head, dict) else None
        labels = pull.get("labels")
        label_names = {
            label.get("name")
            for label in labels
            if isinstance(label, dict) and isinstance(label.get("name"), str)
        } if isinstance(labels, list) else set()
        if (
            pull.get("state") == "open"
            and isinstance(body, str)
            and marker in body.splitlines()
            and isinstance(base, dict)
            and base.get("ref") == "main"
            and isinstance(head, dict)
            and isinstance(head_repo, dict)
            and head_repo.get("full_name", "").lower() == docs_repository.lower()
            and isinstance(head.get("ref"), str)
            and expected_branch.fullmatch(head["ref"])
            and "documentation" in label_names
            and isinstance(pull.get("user"), dict)
            and pull["user"].get("login") == allowed_author
        ):
            matches.append(pull)

    matches.sort(key=lambda pull: (str(pull.get("updated_at") or ""), int(pull.get("number") or 0)), reverse=True)
    blocked = next((pull for pull in matches if pull.get("draft") is not True), None)
    drafts = [pull for pull in matches if pull.get("draft") is True]
    selected = drafts[0] if drafts and blocked is None else None
    return {
        "schema_version": 1,
        "source": marker,
        "found": selected is not None,
        "blocked": blocked is not None,
        "blocked_reason": "matching_pull_request_is_not_draft" if blocked is not None else None,
        "blocked_pull_request": None if blocked is None else {
            "number": blocked["number"],
            "url": blocked["html_url"],
            "head_ref": blocked["head"]["ref"],
        },
        "selected": None if selected is None else {
            "number": selected["number"],
            "url": selected["html_url"],
            "head_ref": selected["head"]["ref"],
            "base_ref": selected["base"]["ref"],
        },
        "other_matching_pull_requests": [
            {"number": pull["number"], "url": pull["html_url"]}
            for pull in drafts[1:]
        ],
    }


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--pull-requests", required=True, type=Path)
    parser.add_argument("--source-repository", required=True)
    parser.add_argument("--source-pr-number", required=True, type=int)
    parser.add_argument("--docs-repository", required=True)
    parser.add_argument("--allowed-author", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args(argv)

    try:
        pulls = json.loads(args.pull_requests.read_text(encoding="utf-8"))
        result = find_existing_draft(
            pulls,
            args.source_repository,
            args.source_pr_number,
            args.docs_repository,
            args.allowed_author,
        )
    except (FileNotFoundError, json.JSONDecodeError, DraftResolutionError) as error:
        print(f"::error::{error}")
        return 1

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
