#!/usr/bin/env python3
"""Retry the failed Azure DevOps CI builds of a dotnet/aspnetcore PR.

Dry run by default: prints what would be retried. With --execute, re-runs only the failed jobs of
each failed build (Azure DevOps "Rerun failed jobs"). Don't use this to investigate failures.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from typing import Any

REPO = "dotnet/aspnetcore"
AZDO_RESOURCE = "499b84ac-1321-427f-aa17-267ca6975798"
BUILD_URL = re.compile(r"https://dev\.azure\.com/([^/]+)/([^/]+)/_build/results\?buildId=(\d+)")
FAILING = {"FAILURE", "ERROR", "TIMED_OUT", "CANCELLED", "ACTION_REQUIRED", "STARTUP_FAILURE"}
RETRYABLE_RESULTS = {"failed", "partiallySucceeded", "canceled"}


def run(args: list[str]) -> str:
    result = subprocess.run(args, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"{' '.join(args[:3])} failed: {result.stderr.strip()[:500]}")
    return result.stdout


def required_checks(branch: str) -> list[str] | None:
    try:
        out = run(["gh", "api", f"repos/{REPO}/branches/{branch}",
                   "--jq", "[.protection.required_status_checks.contexts // [] | .[]]"])
    except RuntimeError:
        return None
    return json.loads(out or "null")


def check_name(check: dict[str, Any]) -> str:
    return check.get("name") or check.get("context") or ""


def is_failing(check: dict[str, Any]) -> bool:
    return (check.get("conclusion") or check.get("state") or "").upper() in FAILING


def failed_builds(checks: list[dict[str, Any]], required: list[str] | None) -> tuple[dict[str, dict[str, Any]], list[str]]:
    """Failed Azure DevOps builds keyed by build ID, plus failing checks that aren't AzDO builds.

    When `required` is given, only builds for those pipelines (and their per-job checks, named
    "<pipeline> (<job>)") are considered.
    """
    builds: dict[str, dict[str, Any]] = {}
    other = []
    for check in checks:
        if not is_failing(check):
            continue
        name = check_name(check)
        pipeline = name.split(" (", 1)[0]
        if required is not None and pipeline not in required:
            continue
        match = BUILD_URL.match(check.get("detailsUrl") or check.get("targetUrl") or "")
        if not match:
            other.append(name)
            continue
        org, project, build_id = match.groups()
        builds.setdefault(build_id, {"org": org, "project": project, "buildId": build_id, "pipeline": pipeline})
    return builds, other


def build_api(build: dict[str, Any], query: str = "") -> str:
    return (f"https://dev.azure.com/{build['org']}/{build['project']}/_apis/build/builds/"
            f"{build['buildId']}?api-version=7.1{query}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("pr", type=int, help="dotnet/aspnetcore PR number")
    parser.add_argument("--all-checks", action="store_true",
                        help="Also retry failed builds that aren't required by branch protection.")
    parser.add_argument("--execute", action="store_true", help="Actually retry the builds.")
    args = parser.parse_args(argv)

    pr = json.loads(run(["gh", "pr", "view", str(args.pr), "-R", REPO,
                         "--json", "number,url,baseRefName,statusCheckRollup"]))
    required = None if args.all_checks else required_checks(pr["baseRefName"])
    builds, other = failed_builds(pr["statusCheckRollup"], required)
    scope = "all failing checks" if required is None else "required checks: " + ", ".join(required)
    print(f"{REPO}#{pr['number']} ({pr['baseRefName']}), {scope}")
    if pr["baseRefName"].startswith("release/"):
        print("Reminder: elevate to administrator at "
              "https://repos.opensource.microsoft.com/orgs/dotnet/repos/aspnetcore before acting on this PR.")
    if not builds:
        print("No failed Azure DevOps builds to retry.")
    status = 0
    for build in builds.values():
        info = json.loads(run(["az", "rest", "--method", "get", "--url", build_api(build),
                               "--resource", AZDO_RESOURCE, "-o", "json"]))
        link = info.get("_links", {}).get("web", {}).get("href", build_api(build))
        label = f"{build['pipeline']} build {build['buildId']} ({info.get('status')}/{info.get('result')}): {link}"
        if info.get("status") != "completed" or info.get("result") not in RETRYABLE_RESULTS:
            print(f"skip   {label}")
            continue
        if not args.execute:
            print(f"would retry failed jobs in {label}")
            continue
        try:
            run(["az", "rest", "--method", "patch", "--url", build_api(build, "&retry=true"),
                 "--resource", AZDO_RESOURCE, "--body", "{}", "-o", "none"])
            print(f"retried failed jobs in {label}")
        except RuntimeError as error:
            status = 1
            print(f"FAILED to retry {label}: {error}\n"
                  f"  Fallback: comment `/azp run {build['pipeline']}` on the PR (reruns the whole pipeline).")
    if other:
        print("Failing checks that aren't Azure DevOps builds (not retried): " + ", ".join(sorted(set(other))))
    return status


if __name__ == "__main__":
    sys.exit(main())
