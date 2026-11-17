#!/usr/bin/env python3
"""Collect the aspnet-build team's pull request inventory.

Read-only: this script only queries GitHub through the authenticated `gh` CLI.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from datetime import datetime, timezone
from typing import Any, Iterable

ASPNETCORE = "dotnet/aspnetcore"
ADMIN_ELEVATION_URL = "https://repos.opensource.microsoft.com/orgs/dotnet/repos/aspnetcore"
VMR = "dotnet/dotnet"
TEAM = "dotnet/aspnet-build"
TEAM_MENTION = re.compile(r"(?<![\w/-])@dotnet/aspnet-build(?![\w-])", re.IGNORECASE)
QUARANTINE_MARKER = "gh-aw-workflow-id: test-quarantine"

# User accounts (not GitHub Apps) that post automated content.
AUTOMATION_LOGINS = {
    "dotnet-maestro-bot",
    "dotnet-bot",
    "dotnet-policy-service",
    "msftbot",
    "azure-pipelines",
    "github-actions",
    "copilot-pull-request-reviewer",
}

CATEGORIES = {
    "dependency-updates": "Dependency updates (Maestro)",
    "inter-branch-merges": "Inter-branch merges",
    "test-quarantine": "Test quarantine / unquarantine",
    "dependabot": "Dependabot",
    "localization": "Localization",
    "vmr-pings": "dotnet/dotnet PRs pinging @dotnet/aspnet-build",
}

# Search queries per category. Results are re-validated by `matches_category`.
SEARCHES = {
    "dependency-updates": [f"repo:{ASPNETCORE} is:pr is:open author:app/dotnet-maestro"],
    "inter-branch-merges": [
        f'repo:{ASPNETCORE} is:pr is:open author:app/github-actions "Merge branch" in:title',
        f"repo:{ASPNETCORE} is:pr is:open author:dotnet-maestro-bot",
    ],
    "test-quarantine": [f"repo:{ASPNETCORE} is:pr is:open author:app/github-actions test-quarantine in:title"],
    "dependabot": [f"repo:{ASPNETCORE} is:pr is:open author:app/dependabot"],
    "localization": [f"repo:{ASPNETCORE} is:pr is:open author:app/dotnet-oneloc-localization"],
    # `team:` only finds PRs where the team is a requested reviewer. Text search also finds
    # mention-only PRs; both result sets are filtered later for human-authored mentions.
    "vmr-pings": [
        f'repo:{VMR} is:pr is:open "aspnet-build" in:title,body,comments',
        f"repo:{VMR} is:pr is:open team:{TEAM}",
    ],
}

PR_FIELDS = """
  number title url isDraft createdAt updatedAt body
  baseRefName headRefName mergeable mergeStateStatus reviewDecision
  repository { nameWithOwner }
  author { login __typename }
  labels(first: 20) { nodes { name } }
  commits(last: 1) { nodes { commit { committedDate statusCheckRollup {
    state
    contexts(first: 100) { totalCount nodes {
      __typename
      ... on CheckRun { name status conclusion detailsUrl }
      ... on StatusContext { context state targetUrl }
    } }
  } } } }
"""

DISCUSSION_FIELDS = """
  comments(last: 100) { nodes { author { login __typename } body createdAt url } }
  reviews(last: 50) { nodes { author { login __typename } body submittedAt url state } }
  reviewThreads(last: 30) { nodes { comments(first: 20) { nodes { author { login __typename } body createdAt url } } } }
"""

SEARCH_QUERY = """
query($q: String!, $after: String) {
  search(query: $q, type: ISSUE, first: 25, after: $after) {
    pageInfo { hasNextPage endCursor }
    nodes { ... on PullRequest { %s %s } }
  }
}
"""

FAILING = {"FAILURE", "ERROR", "TIMED_OUT", "CANCELLED", "ACTION_REQUIRED", "STARTUP_FAILURE"}
# Derived checks that only reflect other checks' results; never a root cause.
IGNORED_CHECKS = {"Maestro auto-merge - All Checks Successful"}
PENDING = {"PENDING", "EXPECTED", "QUEUED", "IN_PROGRESS", "WAITING", "REQUESTED"}

ACTION_ORDER = ["investigate-ci", "resolve-conflicts", "ready-to-merge", "needs-approval",
                "address-feedback", "check-merge-state", "wait-for-ci", "draft"]


def run_graphql(query: str, variables: dict[str, Any]) -> dict[str, Any]:
    args = ["gh", "api", "graphql", "-f", f"query={query}"]
    for key, value in variables.items():
        if value is not None:
            args += ["-f", f"{key}={value}"]
    result = subprocess.run(args, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"gh api graphql failed: {result.stderr.strip()}")
    payload = json.loads(result.stdout)
    if payload.get("errors"):
        raise RuntimeError(f"GraphQL errors: {payload['errors']}")
    return payload["data"]


def search_prs(query: str, with_discussion: bool) -> list[dict[str, Any]]:
    gql = SEARCH_QUERY % (PR_FIELDS, DISCUSSION_FIELDS if with_discussion else "")
    nodes: list[dict[str, Any]] = []
    after = None
    while True:
        data = run_graphql(gql, {"q": query, "after": after})["search"]
        nodes += [n for n in data["nodes"] if n]
        if not data["pageInfo"]["hasNextPage"]:
            return nodes
        after = data["pageInfo"]["endCursor"]


def is_human(author: dict[str, Any] | None) -> bool:
    if not author:
        return False
    login = author.get("login", "")
    if author.get("__typename") == "Bot" or login.endswith("[bot]"):
        return False
    return login.lower() not in AUTOMATION_LOGINS


def matches_category(category: str, pr: dict[str, Any]) -> bool:
    if category == "inter-branch-merges":
        return pr["headRefName"].startswith("merge/") and "merge branch" in pr["title"].lower()
    if category == "test-quarantine":
        return QUARANTINE_MARKER in (pr.get("body") or "")
    return True


def subcategory(category: str, pr: dict[str, Any]) -> str | None:
    title, head = pr["title"], pr["headRefName"]
    if category == "dependency-updates":
        return "codeflow" if "Source code updates from" in title else "dependency-update"
    if category == "test-quarantine":
        return "unquarantine" if "unquarantine" in title.lower() else "quarantine"
    if category == "dependabot":
        match = re.match(r"dependabot/([^/]+)/", head)
        return match.group(1) if match else None
    return None


def summarize_checks(pr: dict[str, Any]) -> dict[str, Any]:
    commits = (pr.get("commits") or {}).get("nodes") or []
    rollup = commits[0]["commit"].get("statusCheckRollup") if commits else None
    if not rollup:
        return {"state": "NONE", "failing": [], "pending": []}
    failing, pending = [], []
    for ctx in rollup["contexts"]["nodes"]:
        if (ctx.get("name") or ctx.get("context")) in IGNORED_CHECKS:
            continue
        if ctx["__typename"] == "CheckRun":
            if ctx["status"] != "COMPLETED":
                pending.append(ctx["name"])
            elif ctx["conclusion"] in FAILING:
                failing.append({"name": ctx["name"], "url": ctx.get("detailsUrl")})
        elif ctx["state"] in FAILING:
            failing.append({"name": ctx["context"], "url": ctx.get("targetUrl")})
        elif ctx["state"] in PENDING:
            pending.append(ctx["context"])
    if rollup["contexts"].get("totalCount", 0) > len(rollup["contexts"]["nodes"]):
        # Fall back to the rollup state for checks beyond the first page.
        if not failing and rollup["state"] in FAILING:
            failing.append({"name": "(unlisted checks; rollup failing)", "url": None})
        elif not pending and rollup["state"] in PENDING:
            pending.append("(unlisted checks; rollup pending)")
    return {"state": rollup["state"], "failing": failing, "pending": pending}


def next_action(pr: dict[str, Any], checks: dict[str, Any]) -> str:
    """Deterministic hint for what the person on build duty should do next."""
    if pr["isDraft"]:
        return "draft"
    if pr["mergeable"] == "CONFLICTING":
        return "resolve-conflicts"
    if checks["failing"]:
        return "investigate-ci"
    if checks["pending"]:
        return "wait-for-ci"
    if pr["reviewDecision"] == "CHANGES_REQUESTED":
        return "address-feedback"
    if pr["reviewDecision"] == "REVIEW_REQUIRED":
        return "needs-approval"
    if pr["mergeStateStatus"] in ("CLEAN", "HAS_HOOKS", "UNSTABLE"):
        return "ready-to-merge"
    return "check-merge-state"


def iter_discussion(pr: dict[str, Any]) -> Iterable[dict[str, Any]]:
    yield {"author": pr.get("author"), "body": pr.get("body") or "", "at": pr["createdAt"], "url": pr["url"]}
    for c in (pr.get("comments") or {}).get("nodes") or []:
        yield {"author": c["author"], "body": c["body"], "at": c["createdAt"], "url": c["url"]}
    for r in (pr.get("reviews") or {}).get("nodes") or []:
        if r.get("submittedAt"):
            yield {"author": r["author"], "body": r["body"], "at": r["submittedAt"], "url": r["url"]}
    for thread in (pr.get("reviewThreads") or {}).get("nodes") or []:
        for c in thread["comments"]["nodes"]:
            yield {"author": c["author"], "body": c["body"], "at": c["createdAt"], "url": c["url"]}


def find_human_pings(pr: dict[str, Any]) -> dict[str, Any] | None:
    """Return the latest human @dotnet/aspnet-build mention and any human replies after it."""
    events = sorted(iter_discussion(pr), key=lambda e: e["at"])
    pings = [e for e in events if is_human(e["author"]) and TEAM_MENTION.search(e["body"] or "")]
    if not pings:
        return None
    latest = pings[-1]
    pinger = latest["author"]["login"]
    replies = [
        e for e in events
        if e["at"] > latest["at"] and is_human(e["author"]) and e["author"]["login"] != pinger
    ]
    return {
        "pingedBy": pinger,
        "pingedAt": latest["at"],
        "pingUrl": latest["url"],
        "pingCount": len(pings),
        "repliedAfterPing": [{"by": r["author"]["login"], "at": r["at"], "url": r["url"]} for r in replies],
    }


def age_days(timestamp: str, now: datetime) -> int:
    return (now - datetime.fromisoformat(timestamp.replace("Z", "+00:00"))).days


def requires_admin_elevation(pr: dict[str, Any]) -> bool:
    """Changing aspnetcore release/* PRs requires temporarily elevating to repo admin."""
    return pr["repository"]["nameWithOwner"] == ASPNETCORE and pr["baseRefName"].startswith("release/")


def build_item(category: str, pr: dict[str, Any], now: datetime) -> dict[str, Any]:
    checks = summarize_checks(pr)
    item = {
        "category": category,
        "subcategory": subcategory(category, pr),
        "repo": pr["repository"]["nameWithOwner"],
        "number": pr["number"],
        "title": pr["title"],
        "url": pr["url"],
        "author": (pr.get("author") or {}).get("login"),
        "base": pr["baseRefName"],
        "head": pr["headRefName"],
        "isDraft": pr["isDraft"],
        "ageDays": age_days(pr["createdAt"], now),
        "updatedAt": pr["updatedAt"],
        "mergeable": pr["mergeable"],
        "mergeState": pr["mergeStateStatus"],
        "reviewDecision": pr["reviewDecision"],
        "labels": [label["name"] for label in pr["labels"]["nodes"]],
        "checks": checks,
        "nextAction": next_action(pr, checks),
        "requiresAdminElevation": requires_admin_elevation(pr),
    }
    if category == "vmr-pings":
        item["ping"] = find_human_pings(pr)
    return item


def classify(category: str, prs: Iterable[dict[str, Any]], now: datetime,
             seen: set[tuple[str, int]], excluded: dict[str, int]) -> list[dict[str, Any]]:
    items = []
    for pr in prs:
        key = (pr["repository"]["nameWithOwner"], pr["number"])
        if key in seen or not matches_category(category, pr):
            continue
        seen.add(key)
        item = build_item(category, pr, now)
        if category == "vmr-pings" and item["ping"] is None:
            excluded["vmr-pings-no-human-mention"] = excluded.get("vmr-pings-no-human-mention", 0) + 1
            continue
        items.append(item)
    return items


def collect(categories: list[str], now: datetime) -> dict[str, Any]:
    items: list[dict[str, Any]] = []
    seen: set[tuple[str, int]] = set()
    excluded: dict[str, int] = {}
    for category in categories:
        for query in SEARCHES[category]:
            prs = search_prs(query, with_discussion=category == "vmr-pings")
            items += classify(category, prs, now, seen, excluded)
    return {"generatedAt": now.isoformat(timespec="seconds"), "categories": categories,
            "excluded": excluded, "items": items}


def render_markdown(result: dict[str, Any]) -> str:
    lines = [f"# aspnet-build attention inventory ({result['generatedAt']})", ""]
    for category in result["categories"]:
        items = [i for i in result["items"] if i["category"] == category]
        lines.append(f"## {CATEGORIES[category]} ({len(items)})")
        lines.append("")
        if not items:
            lines += ["_Nothing open._", ""]
            continue
        items.sort(key=lambda i: (ACTION_ORDER.index(i["nextAction"]), -i["ageDays"]))
        lines += ["| PR | Base | Age | Next action |", "|:--|:--|--:|:--|"]
        for i in items:
            ref = f"#{i['number']}" if i["repo"] == ASPNETCORE else f"{i['repo']}#{i['number']}"
            kind = f" ({i['subcategory']})" if i["subcategory"] else ""
            lines.append(f"| [{ref}]({i['url']}){kind} | {i['base']} | {i['ageDays']}d | {i['nextAction']} |")
        lines.append("")
        for i in items:
            details = [i["title"][:100]]
            if i["checks"]["failing"]:
                names = [f["name"] for f in i["checks"]["failing"]]
                details.append("failing: " + ", ".join(names[:5]) + (" …" if len(names) > 5 else ""))
            if i.get("ping"):
                p = i["ping"]
                repliers = sorted({r["by"] for r in p["repliedAfterPing"]})
                replied = "replied by " + ", ".join(repliers) if repliers else "no reply yet"
                details.append(f"pinged by @{p['pingedBy']} on {p['pingedAt'][:10]} ({replied})")
            if i["requiresAdminElevation"]:
                details.append("release branch: elevate to admin before acting")
            ref = f"#{i['number']}" if i["repo"] == ASPNETCORE else f"{i['repo']}#{i['number']}"
            lines.append(f"- {ref}: " + " — ".join(details))
        lines.append("")
    if any(i["requiresAdminElevation"] for i in result["items"]):
        lines += [f"Before acting on a `release/*` PR, temporarily elevate to administrator at {ADMIN_ELEVATION_URL}.", ""]
    if result["excluded"]:
        lines.append("Excluded: " + ", ".join(f"{k}={v}" for k, v in result["excluded"].items()))
    return "\n".join(lines).rstrip() + "\n"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--category", action="append", choices=list(CATEGORIES),
                        help="Limit to one or more categories (repeatable; default: all).")
    parser.add_argument("--format", choices=["markdown", "json"], default="markdown")
    args = parser.parse_args(argv)
    result = collect(args.category or list(CATEGORIES), datetime.now(timezone.utc))
    print(json.dumps(result, indent=2) if args.format == "json" else render_markdown(result), end="")
    return 0


if __name__ == "__main__":
    sys.exit(main())
