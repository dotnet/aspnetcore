import argparse
import json
import os
import re
from pathlib import Path
from typing import Any, Callable

from find_existing_draft import find_existing_draft
from prepare_context import github_api, prepare_analysis, prepare_context, write_json
from validate_outcome import _validate_docs_pr


def prepare_run(
    repository: str, number: str, directory: Path, existing_draft: str, author: str,
    source_api: Callable[[str], Any] = github_api,
    docs_api: Callable[[str], Any] | None = None,
) -> dict[str, Any]:
    if existing_draft not in {"skip", "refresh"}:
        raise ValueError("Existing draft mode must be skip or refresh.")
    gate = prepare_context(repository, number, directory, source_api, metadata_only=True)
    result = {"analyze": True, "head_sha": "", "summary": ""}
    if gate["status"] != "eligible":
        return result
    if docs_api is None:
        token = os.environ["DOCS_GITHUB_TOKEN"]
        if not token:
            raise ValueError("The documentation lookup token is missing.")
        docs_api = lambda endpoint: github_api(endpoint, token)
    pages = docs_api("/repos/dotnet/AspNetCore.Docs/pulls?state=open&base=main&per_page=100")
    draft = find_existing_draft(
        pages, repository, int(number), "dotnet/AspNetCore.Docs",
        "dotnet/AspNetCore.Docs.Automation", author,
    )
    if draft["found"]:
        selected = draft["selected"]
        metadata = docs_api(f"/repos/dotnet/AspNetCore.Docs/pulls/{selected['number']}")
        url = _validate_docs_pr(metadata, selected["number"], repository, int(number), author)
        if metadata["head"]["ref"] != selected["head_ref"]:
            raise ValueError("Existing docs draft head changed during lookup.")
        sha = metadata["head"].get("sha")
        if not isinstance(sha, str) or not re.fullmatch(r"[a-f0-9]{40}", sha):
            raise ValueError("Existing docs draft has an invalid head SHA.")
        selected["head_sha"] = sha
        result["head_sha"] = sha
        if existing_draft == "skip":
            result.update(
                analyze=False,
                summary=f"Skipped analysis: trusted documentation draft already exists: [{url}]({url}).\n",
            )
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
    if result["summary"]:
        with Path(os.environ["GITHUB_STEP_SUMMARY"]).open("a", encoding="utf-8") as summary:
            summary.write(result["summary"])


if __name__ == "__main__":
    main()
