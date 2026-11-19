"""Collect failed runs of monitored GitHub Actions workflows and extract failure evidence."""

import json
import re
import subprocess
from datetime import datetime, timedelta

REPO = "dotnet/aspnetcore"
LOOKBACK_DAYS = 7
# Workflow ID or file name -> what the workflow is supposed to accomplish.
MONITORED = {
    "test-quarantine.lock.yml": {
        "name": "Daily Test Quarantine Management",
        "url": f"https://github.com/{REPO}/actions/workflows/test-quarantine.lock.yml",
        "paths": [".github/workflows/test-quarantine.md", ".github/workflows/test-quarantine.lock.yml",
                  ".github/workflows/scripts/test-quarantine/"],
        "keyword": "quarantine",
    },
    "dynamic/dependabot/dependabot-updates": {
        "name": "Dependabot Updates",
        "url": f"https://github.com/{REPO}/actions/workflows/dependabot/dependabot-updates",
        "paths": [".github/dependabot.yml"],
        "keyword": "dependabot",
    },
}
FAILED = {"failure", "timed_out", "startup_failure"}
EVIDENCE = re.compile(
    r"##\[error\]|::error|^(FATAL|ERROR|Error|error)\b|^Traceback|^\w+(Error|Exception):|"
    r"permission denied|rejected|not permitted|Resource not accessible", re.IGNORECASE)
MAX_EVIDENCE_LINES = 12


def gh_api(path, raw=False, args=()):
    try:
        result = subprocess.run(["gh", "api", path, *args], check=True, capture_output=True, text=True, timeout=60)
    except FileNotFoundError:
        raise ValueError("gh CLI is required for workflow monitoring") from None
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as error:
        raise ValueError(f"GitHub request failed: {path}") from error
    return result.stdout if raw else json.loads(result.stdout)


def workflow_id(path):
    for workflow in gh_api(f"repos/{REPO}/actions/workflows?per_page=100")["workflows"]:
        if workflow["path"].endswith(path):
            return workflow["id"]
    raise ValueError(f"Workflow not found: {path}")


def failure_evidence(log):
    """Return the most useful error lines from a job log, without timestamps or ANSI codes."""
    picked = []
    for line in log.splitlines():
        line = re.sub(r"\x1b\[[0-9;]*m", "", re.sub(r"^\S+Z ", "", line)).strip()
        if line.startswith("* [new branch]") or not EVIDENCE.search(line):
            continue
        if line not in picked:
            picked.append(line)
    return [line[:300] for line in picked[-MAX_EVIDENCE_LINES:]]


def failed_jobs(run_id):
    jobs = gh_api(f"repos/{REPO}/actions/runs/{run_id}/jobs?per_page=100")["jobs"]
    result = []
    for job in jobs:
        if job["conclusion"] not in FAILED:
            continue
        try:
            evidence = failure_evidence(gh_api(f"repos/{REPO}/actions/jobs/{job['id']}/logs", raw=True))
        except ValueError as error:
            evidence = [f"(log unavailable: {error})"]
        result.append({
            "name": job["name"], "url": job["html_url"],
            "failedSteps": [s["name"] for s in job.get("steps", []) if s["conclusion"] in FAILED],
            "evidence": evidence,
        })
    return result


def pr_summary(pr, user):
    author = (pr.get("author") or pr.get("user") or {}).get("login")
    return {"number": pr["number"], "title": pr["title"], "url": f"https://github.com/{REPO}/pull/{pr['number']}",
            "author": author, "authoredByUser": bool(user) and author == user,
            "mergedAt": pr.get("mergedAt") or pr.get("merged_at")}


def touches(paths, files):
    return any(f == p or (p.endswith("/") and f.startswith(p)) for f in files for p in paths)


def merged_fix_candidates(info, since, user):
    """Merged PRs that changed the workflow's files on main after `since`."""
    found = {}
    for path in info["paths"]:
        for commit in gh_api(f"repos/{REPO}/commits?sha=main&path={path}&since={since}&per_page=100"):
            for pr in gh_api(f"repos/{REPO}/commits/{commit['sha']}/pulls"):
                if pr.get("merged_at"):
                    found[pr["number"]] = pr_summary(pr, user)
    return list(found.values())


OPEN_PRS_QUERY = """query($q: String!) { search(query: $q, type: ISSUE, first: 50) { nodes { ... on PullRequest {
  number title url author { login } files(first: 100) { nodes { path } } } } } }"""


def open_fix_candidates(info, user):
    """Open PRs mentioning the workflow that change its files."""
    data = gh_api("graphql", args=["-f", f"query={OPEN_PRS_QUERY}",
                                   "-f", f"q=repo:{REPO} is:pr is:open {info['keyword']}"])
    return [pr_summary(pr, user) for pr in data["data"]["search"]["nodes"]
            if pr and touches(info["paths"], [f["path"] for f in pr["files"]["nodes"]])]


def current_user():
    try:
        return gh_api("user")["login"]
    except (ValueError, KeyError):
        return None


def workflow_runs(workflow, since, max_pages=20):
    """Every run created since `since`, following pagination."""
    runs = []
    for page in range(1, max_pages + 1):
        batch = gh_api(f"repos/{REPO}/actions/workflows/{workflow}/runs"
                       f"?created=%3E{since}&per_page=100&page={page}")["workflow_runs"]
        runs += batch
        if len(batch) < 100:
            return runs
    raise ValueError(f"More than {max_pages * 100} runs since {since}; coverage would be incomplete")


def collect(now):
    since = (now - timedelta(days=LOOKBACK_DAYS)).strftime("%Y-%m-%dT%H:%M:%SZ")
    items, errors, checked = [], {}, []
    user = current_user()
    for path, info in MONITORED.items():
        try:
            runs = workflow_runs(workflow_id(path), since)
        except ValueError as error:
            errors[info["name"]] = str(error)
            continue
        checked.append({"name": info["name"], "url": info["url"], "runs": len(runs)})
        runs.sort(key=lambda r: r["created_at"])
        failed_runs = [r for r in runs if r["conclusion"] in FAILED]
        merged, open_prs = [], []
        if failed_runs:
            try:
                merged = merged_fix_candidates(info, failed_runs[0]["created_at"], user)
                open_prs = open_fix_candidates(info, user)
            except (ValueError, KeyError, TypeError) as error:
                errors[f"{info['name']} fix PRs"] = str(error)
        for index, run in enumerate(runs):
            if run["conclusion"] not in FAILED:
                continue
            # Dependabot runs are per ecosystem/directory/branch, so only a later success for the
            # same update target counts as recovery.
            same_target = [r for r in runs[index + 1:]
                           if r["display_title"].split(" - ")[0] == run["display_title"].split(" - ")[0]
                           and r["head_branch"] == run["head_branch"]]
            later_success = next((r for r in same_target if r["conclusion"] == "success"), None)
            try:
                jobs = failed_jobs(run["id"])
            except ValueError as error:
                jobs = []
                errors[f"run {run['id']}"] = str(error)
            items.append({
                "category": "workflow-failures", "workflow": info["name"], "runId": run["id"],
                "url": run["html_url"], "title": run["display_title"], "branch": run["head_branch"],
                "event": run["event"], "conclusion": run["conclusion"], "createdAt": run["created_at"],
                "laterSuccessUrl": later_success["html_url"] if later_success else None,
                "failedJobs": jobs, "nextAction": "investigate-impact",
                "mergedFixCandidates": [pr for pr in merged if pr["mergedAt"] > run["created_at"]],
                "openFixCandidates": open_prs,
            })
    return items, {"lookbackDays": LOOKBACK_DAYS, "checked": checked, "errors": errors}


def render_markdown(items, coverage):
    def cell(value):
        return str(value).replace("|", r"\|").replace("\n", " ")

    lines = [f"Failed runs in the last {coverage['lookbackDays']} days: " + " · ".join(
        f"[{w['name']}]({w['url']}) ({w['runs']} runs)" for w in coverage["checked"]), ""]
    if coverage["errors"]:
        lines += ["**Incomplete workflow results:**"] + [f"- {k}: {cell(v)}" for k, v in coverage["errors"].items()] + [""]
    if not items:
        return lines + ["_No failed runs._", ""]
    lines += ["| Run | Workflow | Date | Failed jobs | Later success |", "|:--|:--|:--|:--|:--|"]
    for item in sorted(items, key=lambda i: i["createdAt"], reverse=True):
        jobs = ", ".join(f"{j['name']} ({', '.join(j['failedSteps']) or 'no step'})" for j in item["failedJobs"]) or "?"
        later = f"[yes]({item['laterSuccessUrl']})" if item["laterSuccessUrl"] else "no"
        lines.append(f"| [{item['runId']}]({item['url']}) | {item['workflow']} | {item['createdAt'][:10]} | "
                     f"{cell(jobs)} | {later} |")
    lines.append("")
    for item in items:
        for job in item["failedJobs"]:
            if job["evidence"]:
                lines.append(f"- {item['runId']} `{job['name']}`:")
                lines += [f"  - `{cell(line)}`" for line in job["evidence"]]
    for item in items:
        for kind, key in (("Merged", "mergedFixCandidates"), ("Open", "openFixCandidates")):
            for pr in item.get(key, []):
                mine = " (yours)" if pr["authoredByUser"] else ""
                lines.append(f"- {item['runId']} possible fix, {kind.lower()}: [{REPO}#{pr['number']}](https://github.com/{REPO}/pull/{pr['number']}) "
                             f"{cell(pr['title'])} by @{pr['author']}{mine}")
    lines += ["", "Candidate fix PRs touch the workflow's files; confirm each one addresses the failure.",
              "A later successful run doesn't mean the failed run's work was done. Investigate each failure's impact.", ""]
    return lines
