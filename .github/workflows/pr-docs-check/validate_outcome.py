#!/usr/bin/env python3

import argparse
import json
import re
from pathlib import Path
from typing import Any, Sequence


class OutcomeValidationError(ValueError):
    pass


DOCS_REPOSITORY = "dotnet/AspNetCore.Docs"
DOCS_HEAD_REPOSITORY = "dotnet/AspNetCore.Docs.Automation"
DOCS_PR_URL = re.compile(r"^https://github\.com/dotnet/AspNetCore\.Docs/pull/([1-9][0-9]*)$")


def _load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise OutcomeValidationError(f"File not found: {path}.") from error
    except json.JSONDecodeError as error:
        raise OutcomeValidationError(f"Invalid JSON in {path}: {error}.") from error


def _items(payload: Any) -> list[dict[str, Any]]:
    raw_items = payload.get("items") if isinstance(payload, dict) else None
    return [item for item in raw_items if isinstance(item, dict)] if isinstance(raw_items, list) else []


def _one_item(payload: Any, item_type: str) -> dict[str, Any]:
    matches = [item for item in _items(payload) if item.get("type") == item_type]
    if len(matches) != 1:
        raise OutcomeValidationError(f"Expected exactly one {item_type} item, found {len(matches)}.")
    return matches[0]


def _count(payload: Any, item_type: str) -> int:
    return sum(item.get("type") == item_type for item in _items(payload))


def _positive_int(value: Any, field_name: str) -> int:
    if isinstance(value, float) and value.is_integer():
        value = int(value)
    if not isinstance(value, int) or isinstance(value, bool) or value <= 0:
        raise OutcomeValidationError(f"{field_name} must be a positive integer; received {value!r}.")
    return value


def _validate_docs_pr(
    metadata: Any,
    expected_number: int,
    source_repository: str,
    source_pr_number: int,
    docs_pr_author: str,
) -> str:
    if not isinstance(metadata, dict):
        raise OutcomeValidationError("Docs PR metadata must be a JSON object.")
    number = _positive_int(metadata.get("number"), "Docs PR number")
    if number != expected_number:
        raise OutcomeValidationError(f"Docs PR metadata describes #{number}; expected #{expected_number}.")
    expected_url = f"https://github.com/{DOCS_REPOSITORY}/pull/{expected_number}"
    if metadata.get("html_url") != expected_url:
        raise OutcomeValidationError(f"Unexpected docs PR URL: {metadata.get('html_url')!r}.")
    if metadata.get("state") != "open" or metadata.get("draft") is not True:
        raise OutcomeValidationError("The documentation pull request must be open and draft.")
    base = metadata.get("base")
    base_repo = base.get("repo") if isinstance(base, dict) else None
    if (
        not isinstance(base, dict)
        or base.get("ref") != "main"
        or not isinstance(base_repo, dict)
        or str(base_repo.get("full_name", "")).lower() != DOCS_REPOSITORY.lower()
    ):
        raise OutcomeValidationError("The documentation pull request must target main in the configured docs repository.")
    head = metadata.get("head")
    head_repo = head.get("repo") if isinstance(head, dict) else None
    if not isinstance(head_repo, dict) or str(head_repo.get("full_name", "")).lower() != DOCS_HEAD_REPOSITORY.lower():
        raise OutcomeValidationError("The documentation pull request head must belong to the configured automation fork.")
    if not isinstance(head.get("ref"), str) or re.fullmatch(
        rf"docs/aspnetcore-pr-{source_pr_number}(?:-[a-f0-9]+)?",
        head["ref"],
    ) is None:
        raise OutcomeValidationError(f"Unexpected documentation branch: {head.get('ref')!r}.")
    author = metadata.get("user")
    if not docs_pr_author:
        raise OutcomeValidationError("The configured documentation pull request author is missing.")
    if not isinstance(author, dict) or author.get("login") != docs_pr_author:
        raise OutcomeValidationError("The documentation pull request must be owned by the configured automation identity.")
    title = metadata.get("title")
    if not isinstance(title, str) or not title.startswith("[docs] "):
        raise OutcomeValidationError("The documentation pull request title must start with '[docs] '.")
    labels = metadata.get("labels")
    label_names = {
        label.get("name")
        for label in labels
        if isinstance(label, dict) and isinstance(label.get("name"), str)
    } if isinstance(labels, list) else set()
    if "documentation" not in label_names:
        raise OutcomeValidationError("The documentation pull request must have the documentation label.")
    body = metadata.get("body")
    marker = f"Source: {source_repository}#{source_pr_number}"
    if not isinstance(body, str) or marker not in body.splitlines():
        raise OutcomeValidationError(f"The documentation pull request body is missing {marker!r}.")
    return expected_url


def build_outcome(
    payload: Any,
    source_repository: str,
    source_pr_number: int,
    created_pr_url: str,
    docs_pr_metadata: Any | None,
    expected_existing_draft: Any | None = None,
    docs_pr_author: str = "",
) -> dict[str, Any]:
    notification = _one_item(payload, "notify_source_pr")
    notification_source_pr_number = _positive_int(
        notification.get("source_pr_number"),
        "Notification source_pr_number",
    )
    if notification_source_pr_number != source_pr_number:
        raise OutcomeValidationError(
            f"Notification targeted PR {notification.get('source_pr_number')!r}; expected {source_pr_number}."
        )

    result = str(notification.get("result") or "")
    action = str(notification.get("docs_pr_action") or "")
    confidence = notification.get("docs_needed_confidence")
    if result not in {"drafted", "skipped", "draft_failed", "restricted"}:
        raise OutcomeValidationError(f"Unsupported result: {result!r}.")
    if action not in {"none", "created", "updated"}:
        raise OutcomeValidationError(f"Unsupported docs_pr_action: {action!r}.")
    if isinstance(confidence, float) and confidence.is_integer():
        confidence = int(confidence)
    if not isinstance(confidence, int) or isinstance(confidence, bool) or not 0 <= confidence <= 100:
        raise OutcomeValidationError(f"Confidence must be an integer from 0 through 100; received {confidence!r}.")

    summary = str(notification.get("summary") or "").strip()
    if not summary or len(summary) > 2000:
        raise OutcomeValidationError("Summary must contain between 1 and 2000 characters.")

    surfaces: list[dict[str, Any]] = []
    for key, name in (
        ("conceptual", "Conceptual article"),
        ("migration", "Migration guidance"),
        ("breaking_change", "Breaking change"),
    ):
        required = notification.get(f"{key}_required")
        reason = str(notification.get(f"{key}_reason") or "").strip()
        if not isinstance(required, bool):
            raise OutcomeValidationError(f"{name} required flag must be a boolean.")
        if not reason:
            raise OutcomeValidationError(f"{name} reason must not be empty.")
        surfaces.append({"name": name, "required": required, "reason": reason})

    create_count = _count(payload, "create_pull_request")
    push_count = _count(payload, "push_to_pull_request_branch")
    update_count = _count(payload, "update_pull_request")
    code_output_count = create_count + push_count + update_count
    created_pr_url = created_pr_url.strip()
    _validate_expected_draft_contract(
        result,
        action,
        notification,
        create_count,
        push_count,
        update_count,
        expected_existing_draft,
    )

    canonical = {
        "allow_comment": True,
        "diagnostic": "",
        "render_kind": result,
        "source_pr_number": source_pr_number,
        "summary": summary,
        "confidence": confidence,
        "surfaces": surfaces,
        "docs_pr_action": action,
        "docs_pr_number": None,
        "docs_pr_url": "",
    }

    if result == "restricted":
        if confidence != 0 or action != "none" or code_output_count != 0 or any(surface["required"] for surface in surfaces):
            raise OutcomeValidationError("Restricted outcomes cannot request documentation or code-writing outputs.")
        canonical["summary"] = "Automated documentation processing is excluded for this change."
        canonical["surfaces"] = [
            {"name": surface["name"], "required": False, "reason": "Automated processing is excluded."}
            for surface in surfaces
        ]
        return canonical

    if result == "skipped":
        if confidence >= 60 or action != "none" or code_output_count != 0 or created_pr_url:
            raise OutcomeValidationError("Skipped outcomes require confidence below 60 and no docs PR operation.")
        return canonical

    if result == "draft_failed":
        if confidence < 60 or action != "none" or code_output_count != 0 or created_pr_url:
            raise OutcomeValidationError("draft_failed requires confidence of at least 60 and no docs PR operation.")
        return canonical

    if confidence < 60:
        raise OutcomeValidationError("Drafted outcomes require confidence of at least 60.")

    if action == "created":
        if create_count != 1 or push_count != 0 or update_count != 0:
            raise OutcomeValidationError("Creating a draft requires one create_pull_request and no update outputs.")
        if not created_pr_url:
            canonical["render_kind"] = "drafted_missing_pr"
            canonical["diagnostic"] = "The agent requested a docs PR, but safe outputs did not create one."
            return canonical
        match = DOCS_PR_URL.fullmatch(created_pr_url)
        if match is None:
            raise OutcomeValidationError(f"Unexpected created docs PR URL: {created_pr_url}.")
        number = int(match.group(1))
    elif action == "updated":
        if create_count != 0 or push_count != 1 or update_count != 1:
            raise OutcomeValidationError(
                "Updating a draft requires one push_to_pull_request_branch, one update_pull_request, and no create output."
            )
        number = _positive_int(notification.get("existing_docs_pr_number"), "existing_docs_pr_number")
        for item_type in ("push_to_pull_request_branch", "update_pull_request"):
            item = _one_item(payload, item_type)
            if item.get("pull_request_number") != number:
                raise OutcomeValidationError(f"{item_type} targeted {item.get('pull_request_number')!r}; expected {number}.")
        if created_pr_url:
            raise OutcomeValidationError("Updating an existing draft must not report a newly created PR URL.")
    else:
        raise OutcomeValidationError("Drafted outcomes must use docs_pr_action created or updated.")

    canonical["docs_pr_number"] = number
    canonical["docs_pr_url"] = _validate_docs_pr(
        docs_pr_metadata,
        number,
        source_repository,
        source_pr_number,
        docs_pr_author,
    )
    return canonical


def _validate_expected_draft_contract(
    result: str,
    action: str,
    notification: dict[str, Any],
    create_count: int,
    push_count: int,
    update_count: int,
    expected_existing_draft: Any | None,
) -> None:
    if expected_existing_draft is None:
        return
    if not isinstance(expected_existing_draft, dict):
        raise OutcomeValidationError("Expected existing draft data must be a JSON object.")

    found = expected_existing_draft.get("found")
    blocked = expected_existing_draft.get("blocked")
    if not isinstance(found, bool) or not isinstance(blocked, bool):
        raise OutcomeValidationError("Expected existing draft data must contain boolean found and blocked fields.")
    if found and blocked:
        raise OutcomeValidationError("Expected existing draft data cannot be both found and blocked.")

    code_output_count = create_count + push_count + update_count
    if blocked and code_output_count:
        raise OutcomeValidationError("A matching non-draft docs PR exists and must not be modified or replaced.")
    if result != "drafted":
        return
    if blocked:
        raise OutcomeValidationError("A drafted outcome is not allowed while a matching non-draft docs PR exists.")
    if found:
        selected = expected_existing_draft.get("selected")
        expected_number = _positive_int(
            selected.get("number") if isinstance(selected, dict) else None,
            "Selected existing docs PR number",
        )
        if action != "updated":
            raise OutcomeValidationError(
                f"Existing docs PR #{expected_number} must be updated instead of creating a duplicate."
            )
        actual_number = _positive_int(
            notification.get("existing_docs_pr_number"),
            "existing_docs_pr_number",
        )
        if actual_number != expected_number:
            raise OutcomeValidationError(
                f"Existing docs PR #{expected_number} must be updated instead of creating a duplicate."
            )
    elif action == "updated":
        raise OutcomeValidationError("The agent attempted to update a docs PR when no trusted draft was found.")


def validate_preflight(
    payload: Any,
    source_pr_number: int,
    expected_existing_draft: Any,
) -> None:
    notification = _one_item(payload, "notify_source_pr")
    notification_source_pr_number = _positive_int(
        notification.get("source_pr_number"),
        "Notification source_pr_number",
    )
    if notification_source_pr_number != source_pr_number:
        raise OutcomeValidationError(
            f"Notification targeted PR {notification_source_pr_number}; expected {source_pr_number}."
        )
    result = str(notification.get("result") or "")
    action = str(notification.get("docs_pr_action") or "")
    if result not in {"drafted", "skipped", "draft_failed", "restricted"}:
        raise OutcomeValidationError(f"Unsupported result: {result!r}.")
    if action not in {"none", "created", "updated"}:
        raise OutcomeValidationError(f"Unsupported docs_pr_action: {action!r}.")

    create_count = _count(payload, "create_pull_request")
    push_count = _count(payload, "push_to_pull_request_branch")
    update_count = _count(payload, "update_pull_request")
    _validate_expected_draft_contract(
        result,
        action,
        notification,
        create_count,
        push_count,
        update_count,
        expected_existing_draft,
    )

    code_output_count = create_count + push_count + update_count
    if result in {"restricted", "skipped", "draft_failed"} and code_output_count:
        raise OutcomeValidationError(f"{result} outcomes cannot include docs code-writing outputs.")
    if result == "drafted" and action == "created" and (create_count, push_count, update_count) != (1, 0, 0):
        raise OutcomeValidationError("Creating a draft requires exactly one create_pull_request output.")
    if result == "drafted" and action == "updated" and (create_count, push_count, update_count) != (0, 1, 1):
        raise OutcomeValidationError(
            "Updating a draft requires exactly one push_to_pull_request_branch and one update_pull_request output."
        )


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--agent-output", required=True, type=Path)
    parser.add_argument("--source-repository", required=True)
    parser.add_argument("--source-pr-number", required=True, type=int)
    parser.add_argument("--created-pr-url", default="")
    parser.add_argument("--docs-pr-metadata", type=Path)
    parser.add_argument("--docs-pr-author", default="")
    parser.add_argument("--expected-existing-draft", type=Path)
    parser.add_argument("--preflight", action="store_true")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args(argv)

    try:
        payload = _load_json(args.agent_output)
        expected_existing_draft = (
            _load_json(args.expected_existing_draft)
            if args.expected_existing_draft
            else None
        )
        if args.preflight:
            if expected_existing_draft is None:
                raise OutcomeValidationError("--expected-existing-draft is required with --preflight.")
            validate_preflight(payload, args.source_pr_number, expected_existing_draft)
            return 0
        if args.output is None:
            raise OutcomeValidationError("--output is required unless --preflight is used.")
        metadata = _load_json(args.docs_pr_metadata) if args.docs_pr_metadata else None
        outcome = build_outcome(
            payload,
            args.source_repository,
            args.source_pr_number,
            args.created_pr_url,
            metadata,
            expected_existing_draft,
            args.docs_pr_author,
        )
    except OutcomeValidationError as error:
        if args.preflight:
            print(f"::error::{error}")
            return 1
        outcome = {
            "allow_comment": False,
            "diagnostic": str(error),
            "render_kind": "invalid",
            "source_pr_number": args.source_pr_number,
        }

    if args.output is None:
        raise OutcomeValidationError("--output is required unless --preflight is used.")
    args.output.write_text(json.dumps(outcome, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
