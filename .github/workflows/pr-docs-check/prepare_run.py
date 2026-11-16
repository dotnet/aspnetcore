import argparse
import json
import os
import re
from pathlib import Path
from typing import Any, Callable

from find_existing_draft import find_existing_draft
from prepare_context import github_api, prepare_analysis, prepare_context, write_json
from validate_outcome import _validate_docs_pr, _validate_docs_pr_topology


def resolve_request(event_name: str, event: dict[str, Any], repository: str) -> tuple[str, str, str]:
    if repository != "dotnet/aspnetcore":
        raise ValueError("Documentation checks must run in dotnet/aspnetcore.")
    if event_name == "pull_request_target":
        pr = event.get("pull_request") or {}
        base = pr.get("base") or {}
        if (
            event.get("action") != "closed" or pr.get("merged") is not True
            or base.get("ref") != "main" or (base.get("repo") or {}).get("full_name") != repository
        ):
            raise ValueError("Automatic documentation checks require a source PR merged into main.")
        number = pr.get("number")
        if type(number) is not int or number <= 0:
            raise ValueError("The merged event has an invalid source PR number.")
        return repository, str(number), "skip"
    if event_name == "workflow_dispatch":
        inputs = event.get("inputs") or {}
        source_repository = inputs.get("source_repository", repository)
        number = inputs.get("pr_number", "")
        mode = inputs.get("existing_draft", "skip")
        if source_repository != repository or not isinstance(number, str) or not re.fullmatch(r"[1-9][0-9]*", number):
            raise ValueError("The dispatch must identify a source PR in dotnet/aspnetcore.")
        if mode not in {"skip", "refresh"}:
            raise ValueError("Existing draft mode must be skip or refresh.")
        return source_repository, number, mode
    raise ValueError(f"Unsupported documentation check event: {event_name}.")


def prepare_run(
    repository: str, number: str, directory: Path, existing_draft: str, author: str,
    source_api: Callable[[str], Any] = github_api,
    docs_api: Callable[[str], Any] | None = None,
) -> dict[str, Any]:
    if existing_draft not in {"skip", "refresh"}:
        raise ValueError("Existing draft mode must be skip or refresh.")
    gate = prepare_context(repository, number, directory, source_api, metadata_only=True)
    result = {"analyze": True, "head_sha": "", "docs_pr_number": "", "summary": ""}
    if gate["status"] != "eligible":
        return result
    if docs_api is None:
        token = os.environ["DOCS_GITHUB_TOKEN"]
        if not token:
            raise ValueError("The documentation lookup token is missing.")
        docs_api = lambda endpoint: github_api(endpoint, token)
    pages = docs_api("/repos/dotnet/AspNetCore.Docs/pulls?state=open&per_page=100")
    draft = find_existing_draft(
        pages, repository, int(number), "dotnet/AspNetCore.Docs",
        "dotnet/AspNetCore.Docs.Automation", author,
    )
    if draft["found"] or draft["blocked"]:
        selected = draft["selected"] or draft["blocked_pull_request"]
        metadata = docs_api(f"/repos/dotnet/AspNetCore.Docs/pulls/{selected['number']}")
        _validate_docs_pr_topology(metadata, selected["number"], repository, int(number))
        if metadata["head"]["ref"] != selected["head_ref"]:
            raise ValueError("Existing docs draft head changed during lookup.")
        if existing_draft == "skip" or draft["blocked_reason"] == "matching_pull_request_is_not_draft":
            links = [selected, *draft["other_matching_pull_requests"]]
            reason = draft["blocked_reason"] or "existing documentation draft"
            result.update(
                analyze=False,
                summary=f"Skipped analysis ({reason}): " + ", ".join(
                    f"[{pull['url']}]({pull['url']})" for pull in links
                ) + ". No pull requests were modified.\n",
            )
        elif draft["blocked"]:
            raise ValueError(f"Documentation refresh is blocked: {draft['blocked_reason']}. No replacement is allowed.")
        else:
            _validate_docs_pr(metadata, selected["number"], repository, int(number), author)
            sha = metadata["head"].get("sha")
            if not isinstance(sha, str) or not re.fullmatch(r"[a-f0-9]{40}", sha):
                raise ValueError("Existing docs draft has an invalid head SHA.")
            selected["head_sha"] = sha
            result["head_sha"] = sha
            result["docs_pr_number"] = str(selected["number"])
    write_json(directory / "existing-draft.json", draft)
    if result["analyze"]:
        pr = json.loads((directory / "source-pr.json").read_text(encoding="utf-8"))
        prepare_analysis(repository, number, directory, pr, source_api)
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-repository")
    parser.add_argument("--source-pr-number")
    parser.add_argument("--event-path", type=Path)
    parser.add_argument("--event-name")
    parser.add_argument("--workflow-repository")
    parser.add_argument("--output-directory", required=True, type=Path)
    parser.add_argument("--existing-draft", choices=("skip", "refresh"), default="skip")
    parser.add_argument("--allowed-author", required=True)
    args = parser.parse_args()
    if args.event_path is not None:
        if args.source_repository is not None or args.source_pr_number is not None:
            parser.error("Event requests cannot also specify a source PR.")
        repository, number, mode = resolve_request(
            args.event_name, json.loads(args.event_path.read_text(encoding="utf-8")), args.workflow_repository,
        )
    else:
        if args.source_repository is None or args.source_pr_number is None:
            parser.error("Specify an event request or a source repository and PR number.")
        repository, number, mode = args.source_repository, args.source_pr_number, args.existing_draft
    result = prepare_run(
        repository, number, args.output_directory, mode, args.allowed_author,
    )
    with Path(os.environ["GITHUB_OUTPUT"]).open("a", encoding="utf-8") as output:
        output.write(f"analyze={str(result['analyze']).lower()}\nhead_sha={result['head_sha']}\n")
        output.write(f"docs_pr_number={result['docs_pr_number']}\n")
        output.write(f"source_repository={repository}\nsource_pr_number={number}\n")
    if result["summary"]:
        with Path(os.environ["GITHUB_STEP_SUMMARY"]).open("a", encoding="utf-8") as summary:
            summary.write(result["summary"])


if __name__ == "__main__":
    main()
