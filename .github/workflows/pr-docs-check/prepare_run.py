import argparse
import json
import os
import re
from pathlib import Path
from typing import Any, Callable

from find_existing_draft import find_existing_draft
from prepare_context import github_api, prepare_analysis, prepare_context, write_json
from validate_outcome import _validate_docs_pr, _validate_docs_pr_topology


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
    parser.add_argument("--source-repository", required=True)
    parser.add_argument("--source-pr-number", required=True)
    parser.add_argument("--output-directory", required=True, type=Path)
    parser.add_argument("--existing-draft", choices=("skip", "refresh"), default="skip")
    parser.add_argument("--allowed-author", required=True)
    args = parser.parse_args()
    result = prepare_run(
        args.source_repository, args.source_pr_number, args.output_directory,
        args.existing_draft, args.allowed_author,
    )
    with Path(os.environ["GITHUB_OUTPUT"]).open("a", encoding="utf-8") as output:
        output.write(f"analyze={str(result['analyze']).lower()}\nhead_sha={result['head_sha']}\n")
        output.write(f"docs_pr_number={result['docs_pr_number']}\n")
    if result["summary"]:
        with Path(os.environ["GITHUB_STEP_SUMMARY"]).open("a", encoding="utf-8") as summary:
            summary.write(result["summary"])


if __name__ == "__main__":
    main()
