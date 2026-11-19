#!/usr/bin/env python3
"""Collect the aspnet-build team's pull request and Component Governance inventory.

Read-only: queries GitHub, the .NET support policy and Azure DevOps.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from datetime import datetime, timezone
from typing import Any, Iterable

import component_governance
import servicing_schedule
import workflow_failures

ASPNETCORE = "dotnet/aspnetcore"
ADMIN_ELEVATION_URL = "https://repos.opensource.microsoft.com/orgs/dotnet/repos/aspnetcore"
VMR = "dotnet/dotnet"
TEAM = "dotnet/aspnet-build"
TEAM_MENTION = re.compile(r"(?<![\w/-])@dotnet/aspnet-build(?![\w-])", re.IGNORECASE)
QUARANTINE_MARKER = "gh-aw-workflow-id: test-quarantine"
SERVICING_LABELS = {"servicing-approved", "tell-mode"}

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
    "servicing": "Servicing PRs to merge",
    "dependency-updates": "Dependency updates (Maestro)",
    "inter-branch-merges": "Inter-branch merges",
    "test-quarantine": "Test quarantine / unquarantine",
    "dependabot": "Dependabot",
    "localization": "Localization",
    "vmr-pings": "dotnet/dotnet PRs pinging @dotnet/aspnet-build",
    "component-governance": "Component Governance",
    "workflow-failures": "Monitored workflow failures",
}

# Search queries per category. Results are re-validated by `matches_category`.
SEARCHES = {
    # A comma in label: means OR. `base:release/` isn't a prefix match in the search API, so the
    # release/* base is checked in `matches_category`.
    "servicing": [f"repo:{ASPNETCORE} is:pr is:open label:Servicing-approved,tell-mode"],
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

REVIEW_FIELDS = """
  latestOpinionatedReviews(first: 30) { nodes { author { login __typename } state submittedAt url } }
"""

DISCUSSION_FIELDS = """
  comments(last: 100) { totalCount nodes { author { login __typename } body createdAt url } }
  reviews(last: 50) { totalCount nodes { author { login __typename } body submittedAt url state } }
  reviewThreads(last: 30) { totalCount nodes { comments(first: 20) {
    totalCount nodes { author { login __typename } body createdAt url } } } }
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
PASSING = {"SUCCESS", "NEUTRAL", "SKIPPED"}

ACTION_ORDER = ["investigate-ci", "retry-ci", "resolve-conflicts", "ready-to-merge", "needs-approval",
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


def search_prs(query: str, extra_fields: str = "") -> list[dict[str, Any]]:
    gql = SEARCH_QUERY % (PR_FIELDS, extra_fields)
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
    if category == "servicing":
        return pr["baseRefName"].startswith("release/") and bool(servicing_labels(pr))
    return True


def servicing_labels(pr: dict[str, Any]) -> list[str]:
    return [l["name"] for l in pr["labels"]["nodes"] if l["name"].lower() in SERVICING_LABELS]


def approvers(pr: dict[str, Any]) -> tuple[list[str], list[str]]:
    """Humans other than the author whose latest review approves, or requests changes."""
    author = (pr.get("author") or {}).get("login")
    approved, changes = [], []
    for review in (pr.get("latestOpinionatedReviews") or {}).get("nodes") or []:
        if not is_human(review["author"]) or review["author"]["login"] == author:
            continue
        if review["state"] == "APPROVED":
            approved.append(review["author"]["login"])
        elif review["state"] == "CHANGES_REQUESTED":
            changes.append(review["author"]["login"])
    return sorted(approved), sorted(changes)


def required_check_states(pr: dict[str, Any], required: list[str] | None) -> list[dict[str, Any]] | None:
    """State of each branch-protection required check; None when the required list is unknown."""
    if required is None:
        return None
    commits = (pr.get("commits") or {}).get("nodes") or []
    rollup = commits[0]["commit"].get("statusCheckRollup") if commits else None
    contexts = rollup["contexts"]["nodes"] if rollup else []
    states = []
    for name in required:
        results, url = [], None
        for ctx in contexts:
            if (ctx.get("name") or ctx.get("context")) != name:
                continue
            if ctx["__typename"] == "CheckRun":
                results.append(ctx["conclusion"] if ctx["status"] == "COMPLETED" else "PENDING")
                url = ctx.get("detailsUrl")
            else:
                results.append(ctx["state"])
                url = ctx.get("targetUrl")
        if not results:
            state = "MISSING"
        elif any(r in FAILING for r in results):
            state = "FAILING"
        elif all(r in PASSING for r in results):
            state = "PASSING"
        else:
            state = "PENDING"
        states.append({"name": name, "state": state, "url": url})
    return states


def servicing_next_action(pr: dict[str, Any], checks: dict[str, Any],
                          required: list[dict[str, Any]] | None = None) -> str:
    """Servicing PRs are merged on green CI plus another dev's approval; failures are just retried.

    CI is green only when no check is failing or pending and every branch-protection required
    check has reported success. If the required checks couldn't be read, CI can't be called green.
    GitHub's reviewDecision must also be APPROVED, so approvals made stale by later pushes don't count.
    """
    approved, changes = approvers(pr)
    failing = bool(checks["failing"]) or any(r["state"] == "FAILING" for r in required or [])
    pending = bool(checks["pending"]) or any(r["state"] in ("PENDING", "MISSING") for r in required or [])
    if pr["isDraft"]:
        return "draft"
    if pr["mergeable"] == "CONFLICTING":
        return "resolve-conflicts"
    if failing:
        return "retry-ci"
    if pending:
        return "wait-for-ci"
    if changes:
        return "address-feedback"
    if not approved or pr.get("reviewDecision") != "APPROVED":
        return "needs-approval"
    # BLOCKED is expected: elevating to admin bypasses branch protection, and checks and approvals
    # are verified above. States that elevation doesn't resolve still need a look.
    if required is None or pr["mergeable"] != "MERGEABLE" or pr["mergeStateStatus"] in ("BEHIND", "DIRTY", "UNKNOWN"):
        return "check-merge-state"
    return "ready-to-merge"


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


def discussion_truncated(pr: dict[str, Any]) -> bool:
    """True when the GraphQL search returned only part of a PR's comments, reviews or threads."""
    def partial(conn: dict[str, Any] | None) -> bool:
        return bool(conn) and conn.get("totalCount", 0) > len(conn.get("nodes") or [])
    threads = pr.get("reviewThreads") or {}
    return (partial(pr.get("comments")) or partial(pr.get("reviews")) or partial(threads)
            or any(partial(t.get("comments")) for t in threads.get("nodes") or []))


def rest_list(path: str) -> list[dict[str, Any]]:
    result = subprocess.run(["gh", "api", "--paginate", path, "--jq", ".[]"], capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"gh api {path} failed: {result.stderr.strip()[:300]}")
    return [json.loads(line) for line in result.stdout.splitlines() if line.strip()]


def full_discussion(pr: dict[str, Any]) -> dict[str, Any]:
    """Replace a truncated GraphQL discussion with every comment, review and review comment (REST)."""
    base = f"repos/{pr['repository']['nameWithOwner']}"
    number = pr["number"]

    def author(user: dict[str, Any] | None) -> dict[str, Any]:
        user = user or {}
        return {"login": user.get("login", ""), "__typename": "Bot" if user.get("type") == "Bot" else "User"}

    comments = [{"author": author(c["user"]), "body": c["body"], "createdAt": c["created_at"], "url": c["html_url"]}
                for c in rest_list(f"{base}/issues/{number}/comments?per_page=100")]
    reviews = [{"author": author(r["user"]), "body": r["body"], "submittedAt": r.get("submitted_at"),
                "url": r["html_url"], "state": r["state"]}
               for r in rest_list(f"{base}/pulls/{number}/reviews?per_page=100")]
    review_comments = [{"author": author(c["user"]), "body": c["body"], "createdAt": c["created_at"],
                        "url": c["html_url"]}
                       for c in rest_list(f"{base}/pulls/{number}/comments?per_page=100")]
    return {**pr, "comments": {"nodes": comments}, "reviews": {"nodes": reviews},
            "reviewThreads": {"nodes": [{"comments": {"nodes": review_comments}}]}}


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


def build_item(category: str, pr: dict[str, Any], now: datetime,
               required_checks: dict[str, list[str] | None] | None = None) -> dict[str, Any]:
    checks = summarize_checks(pr)
    required = None
    if category == "servicing":
        required = required_check_states(pr, (required_checks or {}).get(pr["baseRefName"]))
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
        "nextAction": servicing_next_action(pr, checks, required) if category == "servicing" else next_action(pr, checks),
        "requiresAdminElevation": requires_admin_elevation(pr),
    }
    if category == "vmr-pings":
        if discussion_truncated(pr):
            pr = full_discussion(pr)
        item["ping"] = find_human_pings(pr)
    if category == "servicing":
        approved, changes = approvers(pr)
        item.update(servicingLabels=servicing_labels(pr), approvedBy=approved, changesRequestedBy=changes,
                    requiredChecks=required)
    return item


def fetch_required_checks(branch: str) -> list[str] | None:
    """Required status checks from the branch's protection, or None if they can't be read."""
    result = subprocess.run(
        ["gh", "api", f"repos/{ASPNETCORE}/branches/{branch}",
         "--jq", "[.protection.required_status_checks.contexts // [] | .[]]"],
        capture_output=True, text=True)
    if result.returncode != 0:
        return None
    return json.loads(result.stdout or "null")


def classify(category: str, prs: Iterable[dict[str, Any]], now: datetime,
             seen: set[tuple[str, int]], excluded: dict[str, int]) -> list[dict[str, Any]]:
    prs = list(prs)
    required_checks = None
    if category == "servicing":
        required_checks = {b: fetch_required_checks(b) for b in {pr["baseRefName"] for pr in prs}}
    items = []
    for pr in prs:
        key = (pr["repository"]["nameWithOwner"], pr["number"])
        if key in seen or not matches_category(category, pr):
            continue
        seen.add(key)
        item = build_item(category, pr, now, required_checks)
        if category == "vmr-pings" and item["ping"] is None:
            excluded["vmr-pings-no-human-mention"] = excluded.get("vmr-pings-no-human-mention", 0) + 1
            continue
        items.append(item)
    return items


def collect(categories: list[str], now: datetime, keep_going: bool = False) -> dict[str, Any]:
    """Collect every category. With keep_going, a failing category is recorded in `errors` instead of raising."""
    items: list[dict[str, Any]] = []
    seen: set[tuple[str, int]] = set()
    excluded: dict[str, int] = {}
    coverage: dict[str, Any] = {}
    errors: dict[str, str] = {}
    for category in categories:
        try:
            items += collect_category(category, now, seen, excluded, coverage)
        except Exception as error:  # noqa: BLE001 - reported per category
            if not keep_going:
                raise
            errors[category] = f"{type(error).__name__}: {error}"[:500]
    result = {"generatedAt": now.isoformat(timespec="seconds"), "categories": categories,
              "excluded": excluded, "items": items}
    if coverage:
        result["coverage"] = coverage
    if errors:
        result["errors"] = errors
    return result


def collect_category(category: str, now: datetime, seen: set[tuple[str, int]], excluded: dict[str, int],
                     coverage: dict[str, Any]) -> list[dict[str, Any]]:
    if category == "component-governance":
        alerts, coverage[category] = component_governance.collect()
        return alerts
    if category == "workflow-failures":
        runs, coverage[category] = workflow_failures.collect(now)
        return runs
    extra = {"vmr-pings": DISCUSSION_FIELDS, "servicing": REVIEW_FIELDS}.get(category, "")
    items: list[dict[str, Any]] = []
    for query in SEARCHES[category]:
        items += classify(category, search_prs(query, extra), now, seen, excluded)
    if category == "servicing":
        add_target_releases(items, now)
    return items


def add_target_releases(items: list[dict[str, Any]], now: datetime) -> None:
    """Annotate servicing PRs with the monthly release they'd ship in if merged now."""
    if not items:
        return
    releases = servicing_schedule.release_for_branches([i["base"] for i in items], now)
    for item in items:
        item["shipsIn"] = releases.get(item["base"])


def render_ship_dates(items: list[dict[str, Any]]) -> list[str]:
    lines, done = [], set()
    for i in sorted(items, key=lambda i: i["base"]):
        if i["base"] in done:
            continue
        done.add(i["base"])
        ships = i.get("shipsIn") or {}
        url = ships.get("scheduleUrl", servicing_schedule.SCHEDULE_URL)
        if ships.get("release"):
            cutoff = datetime.fromisoformat(ships["codeComplete"]).strftime("%b %d %Y, %I:%M %p")
            lines.append(f"- If merged now, `{i['base']}` PRs ship in **{ships['release']}** "
                         f"(code complete {cutoff} PT, [schedule]({url})).")
        else:
            lines.append(f"- `{i['base']}`: target release unknown. {ships.get('note', '')} [Schedule]({url})")
    return lines + [""] if lines else []


def render_markdown(result: dict[str, Any]) -> str:
    lines = [f"# aspnet-build attention inventory ({result['generatedAt']})", ""]
    for category in result["categories"]:
        items = [i for i in result["items"] if i["category"] == category]
        error = result.get("errors", {}).get(category)
        if error:
            lines += [f"## {CATEGORIES[category]} (not collected)", "", f"_Couldn't collect this section: {error}_", ""]
            continue
        lines.append(f"## {CATEGORIES[category]} ({len(items)})")
        lines.append("")
        if category == "component-governance":
            lines += component_governance.render_markdown(items, result["coverage"][category])
            continue
        if category == "workflow-failures":
            lines += workflow_failures.render_markdown(items, result["coverage"][category])
            continue
        if not items:
            lines += ["_Nothing open._", ""]
            continue
        items.sort(key=lambda i: (ACTION_ORDER.index(i["nextAction"]), -i["ageDays"]))
        lines += ["| PR | Base | Age | Next action |", "|:--|:--|--:|:--|"]
        for i in items:
            ref = f"{i['repo']}#{i['number']}"
            kind = f" ({i['subcategory']})" if i["subcategory"] else ""
            lines.append(f"| [{ref}]({i['url']}){kind} | {i['base']} | {i['ageDays']}d | {i['nextAction']} |")
        lines.append("")
        for i in items:
            details = [i["title"][:100]]
            names = [f["name"] for f in i["checks"]["failing"]]
            if names:
                details.append("failing: " + ", ".join(names[:5]) + (" …" if len(names) > 5 else ""))
            if i.get("ping"):
                p = i["ping"]
                repliers = sorted({r["by"] for r in p["repliedAfterPing"]})
                replied = "replied by " + ", ".join(repliers) if repliers else "no reply yet"
                details.append(f"pinged by @{p['pingedBy']} on {p['pingedAt'][:10]} ({replied})")
            if category == "servicing":
                if i.get("requiredChecks") is not None:
                    bad = [f"{r['name']} {r['state'].lower()}" for r in i["requiredChecks"] if r["state"] != "PASSING"]
                    details.append("required checks: " + (", ".join(bad) if bad else "all passing"))
                details.append("labels: " + ", ".join(i["servicingLabels"]))
                details.append("approved by " + ", ".join(i["approvedBy"]) if i["approvedBy"] else "no approval from another dev")
                if i["changesRequestedBy"]:
                    details.append("changes requested by " + ", ".join(i["changesRequestedBy"]))
            if i["requiresAdminElevation"]:
                details.append("release branch: elevate to admin before acting")
            ref = f"{i['repo']}#{i['number']}"
            lines.append(f"- {ref}: " + " — ".join(details))
        lines.append("")
        if category == "servicing":
            lines += render_ship_dates(items)
    if any(i.get("requiresAdminElevation") for i in result["items"]):
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
