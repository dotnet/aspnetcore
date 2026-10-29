#!/usr/bin/env python3

import argparse
import collections
import datetime
import hashlib
import json
import os
import pathlib
import re
import time
import urllib.error
import urllib.parse
import urllib.request

from collect_requarantine_history import MODULE as SOURCE


ADO = "https://dev.azure.com/dnceng-public/public/_apis"
VSTMR = "https://vstmr.dev.azure.com/dnceng-public/public/_apis"
PIPELINES = (84, 87)
IIS_PREFIX = re.compile(
    r"^Microsoft\.AspNetCore\.Server\.IIS\."
    r"(?:(?:IISExpress|NewHandler|NewShim)\.)?FunctionalTests\."
)


def fetch(url):
    for attempt in range(3):
        try:
            request = urllib.request.Request(
                url, headers={"User-Agent": "aspnetcore-test-quarantine"}
            )
            with urllib.request.urlopen(request, timeout=120) as response:
                return json.load(response), response.headers
        except urllib.error.HTTPError as error:
            if attempt == 2 or error.code < 500 and error.code != 429:
                raise
        except OSError:
            if attempt == 2:
                raise
        time.sleep(2 * (attempt + 1))


def pages(endpoint, parameters, provider):
    token = None
    seen = set()
    while True:
        query = dict(parameters)
        if token:
            query["continuationToken"] = token
        data, headers = provider(f"{endpoint}?{urllib.parse.urlencode(query)}")
        if not isinstance(data, dict) or not isinstance(data.get("value"), list):
            raise ValueError("Azure DevOps returned an invalid result page")
        yield from data["value"]
        token = headers.get("x-ms-continuationtoken") or data.get("continuationToken")
        if not token:
            break
        if token in seen:
            raise ValueError("Azure DevOps repeated a continuation token")
        seen.add(token)


def collect_results(now, provider=fetch):
    pipelines = {}
    start = now - datetime.timedelta(days=30)
    for pipeline in PIPELINES:
        builds = list(pages(f"{ADO}/build/builds", {
            "definitions": pipeline,
            "branchName": "refs/heads/main",
            "statusFilter": "completed",
            "minTime": start.isoformat(),
            "$top": 100,
            "api-version": "7.1",
        }, provider))
        build_ids = set()
        tests = {}
        for build in builds:
            build_id = build["id"]
            if build_id in build_ids:
                raise ValueError("Azure DevOps returned a duplicate build")
            if (
                build.get("sourceBranch") != "refs/heads/main"
                or build.get("status") != "completed"
                or build.get("definition", {}).get("id") != pipeline
            ):
                raise ValueError("Azure DevOps returned a build outside the query")
            build_ids.add(build_id)
            for result in pages(f"{VSTMR}/testresults/resultsbyBuild", {
                "buildId": build_id,
                "$top": 10000,
                "api-version": "7.1-preview.1",
            }, provider):
                name = result.get("automatedTestName")
                if not isinstance(name, str) or not name:
                    raise ValueError("A test result lacks its exact automatedTestName")
                if name.endswith(".WorkItemExecution"):
                    continue
                counts = tests.setdefault(name, {
                    "passed": 0, "failed": 0, "other": 0, "builds": set(),
                })
                outcome = result.get("outcome")
                if not isinstance(outcome, str):
                    raise ValueError("A test result lacks its outcome")
                key = {"Passed": "passed", "Failed": "failed"}.get(outcome, "other")
                counts[key] += 1
                counts["builds"].add(build_id)
        pipelines[str(pipeline)] = {"build_count": len(build_ids), "tests": tests}
    return pipelines


def target_key(target):
    return tuple(target.get(key) for key in ("scope", "path", "type", "method", "data"))


def resolve(root, name, index):
    source = SOURCE.resolve_source(root, name, index)
    if source["status"] == "missing" and IIS_PREFIX.match(name):
        test_name, _ = SOURCE.split_test_name(name)
        runtime_type = test_name.rsplit(".", 1)[0]
        suffix = IIS_PREFIX.sub("", runtime_type)
        candidates = [
            type_name for type_name in index["types"]
            if IIS_PREFIX.match(type_name)
            and IIS_PREFIX.sub("", type_name) == suffix
        ]
        if len(candidates) == 1:
            source = SOURCE.resolve_source(
                root, candidates[0] + name[len(runtime_type):], index
            )
            if not source.get("path", "").startswith("src/Servers/IIS/"):
                return {"status": "ambiguous"}
    return source


def matches(target, source):
    if source.get("status") != "exact":
        return False
    scope = target["scope"]
    if scope == "assembly":
        return target["project_root"] == source["assembly_project_root"]
    if scope == "type":
        return any(
            item["type"] == target["type"]
            and item["project_root"] == target["project_root"]
            for item in source["type_history_locations"]
        )
    if (
        target["path"] != source["path"]
        or target["type"] != source["declaring_type"]
        or target["method"] != source["method"]
    ):
        return False
    if scope == "data":
        row = source.get("data_quarantine")
        return isinstance(row, dict) and row["data"] == target["data"]
    return scope == "method"


def expected_methods(root, target, index):
    if target["scope"] in ("method", "data"):
        return {(target["type"], target["method"])}
    if target["scope"] == "type":
        runners = {target["type"]}
    else:
        runners = {
            name for name, declarations in index["types"].items()
            if any(item["project_root"] == target["project_root"] for item in declarations)
            and not any(
                item["project_root"] == target["project_root"] and item["is_abstract"]
                for item in declarations
            )
        }
    methods = set()
    for runner in runners:
        owners = set()
        current_type = runner
        proven = True
        while current_type not in owners:
            owners.add(current_type)
            logical = SOURCE.logical_type(index, current_type, target["project_root"])
            if logical["status"] != "exact":
                proven = False
                break
            if not logical["base"]:
                break
            base = SOURCE.resolve_base_type(
                index, current_type, logical["base"], target["project_root"],
            )
            if base["status"] != "exact":
                proven = False
                break
            current_type = base["type"]
        else:
            proven = False
        names = {
            name for name, declarations in index["methods"].items()
            if any(
                item["project_root"] == target["project_root"]
                and item["is_test"] and item["type"] in owners
                for item in declarations
            )
        }
        if not names:
            continue
        if not proven:
            return set()
        for name in names:
            source = SOURCE.resolve_source(root, f"{runner}.{name}", index)
            if source["status"] == "unmatched-data-row" and len(source["matches"]) == 1:
                # Enumerating a theory's method does not select a parameter row.
                source = source["matches"][0]
            elif source["status"] != "exact":
                return set()
            methods.add((source.get("declaring_type") or source["type"], source["method"]))
    return methods


def suppressed(target, names, closed_prs, now):
    identifiers = set(names)
    method_patterns = []
    if target["scope"] in ("method", "data"):
        declaring_method = f"{target['type']}.{target['method']}"
        methods = {SOURCE.split_test_name(name)[0] for name in names} | {declaring_method}
        if target["scope"] == "data":
            identifiers.update(
                declaring_method + name[len(SOURCE.split_test_name(name)[0]):]
                for name in names
            )
        for method in methods:
            pattern = rf"(?<![\w.]){re.escape(method)}(?![\w.])"
            if target["scope"] == "data":
                pattern += r"(?:\s*\(\s*\))?(?!\s*\()"
            method_patterns.append(pattern)
    if target["scope"] == "type":
        identifiers.add(target["type"])
    for pr in closed_prs:
        text = f"{pr.get('title', '')}\n{pr.get('body', '')}"
        if (
            not any(identifier in text for identifier in identifiers)
            and not any(re.search(pattern, text) for pattern in method_patterns)
        ):
            continue
        if pr.get("trusted_closed") is True and pr.get("closed_at"):
            return True
        timestamp = pr.get("unquarantine_label_added_at")
        if timestamp and now < SOURCE.parse_utc(timestamp) + datetime.timedelta(days=30):
            return True
    return False


def collect(root, history, history_bytes, closed_prs, blocked_issues, requarantine_prs,
            now, provider=fetch):
    root = pathlib.Path(root)
    if SOURCE.git(root, "rev-parse", "HEAD") != history["history_commit"]:
        raise ValueError("Source checkout differs from the history receipt")
    index = SOURCE.build_source_index(root)
    current = {target_key(target): target for target in SOURCE.current_quarantine_targets(index)}
    pipelines = collect_results(now, provider)
    resolved = {
        name: resolve(root, name, index)
        for pipeline in pipelines.values() for name in pipeline["tests"]
    }
    candidates = []
    exclusions = collections.Counter()
    for historical in history["targets"]:
        target = current.get(target_key(historical))
        if target is None:
            raise ValueError("History receipt contains a missing current target")
        reasons = []
        introduced = historical.get("introduced_utc")
        if (
            historical["status"] != "first-quarantine"
            or not isinstance(historical.get("issue"), int)
            or not introduced
            or now - SOURCE.parse_utc(introduced) < datetime.timedelta(days=60)
        ):
            exclusions["history-or-age-unproven"] += 1
            continue
        if historical["issue"] in blocked_issues:
            reasons.append("re-quarantine-issue")
        if any(
            entry["filename"] == target["path"]
            and (
                entry["patch_truncated"]
                or any(
                    SOURCE.quarantine_issue(line) == historical["issue"]
                    for line in entry["added_lines"]
                )
            )
            for pr in requarantine_prs for entry in pr["quarantine_entries"]
        ):
            # Added attribute lines do not prove their owner. Block a shared issue
            # conservatively; only a truncated diff can implicate the whole file.
            reasons.append("re-quarantine-file")
        if "src/SignalR/server/Specification.Tests/" in target["path"]:
            reasons.append("excluded-source")
        expected = expected_methods(root, target, index)
        names = [name for name, source in resolved.items() if matches(target, source)]
        if suppressed(target, names, closed_prs, now):
            reasons.append("human-suppression")
        if any("AlwaysTestTests.SuccessfulTests.GuaranteedQuarantinedTest" in name for name in names):
            reasons.append("always-quarantined")
        if any(
            source["status"] != "exact"
            and SOURCE.split_test_name(name)[0].rsplit(".", 1)[-1] in {
                method for _, method in expected
            }
            for name, source in resolved.items()
        ):
            reasons.append("unresolved-test-source")
        evidence = {}
        for pipeline_id, pipeline in pipelines.items():
            groups = {}
            for name in names:
                counts = pipeline["tests"].get(name)
                if counts is None:
                    continue
                source = resolved[name]
                method = (source["declaring_type"], source["method"])
                group = groups.setdefault(method, {
                    "passed": 0, "failed": 0, "other": 0, "builds": set(),
                })
                for key in ("passed", "failed", "other"):
                    group[key] += counts[key]
                group["builds"].update(counts["builds"])
            if not groups:
                continue
            if any(group["failed"] for group in groups.values()):
                reasons.append("failed-results")
            healthy = bool(expected) and expected == set(groups) and all(
                group["passed"] > 0
                and group["failed"] == 0
                and len(group["builds"]) * 100 >= pipeline["build_count"] * 66
                for group in groups.values()
            )
            if not healthy:
                reasons.append("insufficient-run-evidence")
            evidence[pipeline_id] = {
                "passed": sum(group["passed"] for group in groups.values()),
                "failed": sum(group["failed"] for group in groups.values()),
                "other": sum(group["other"] for group in groups.values()),
                "methods": len(groups),
                "build_count": pipeline["build_count"],
            }
        if not evidence:
            reasons.append("no-run-evidence")
        if reasons:
            exclusions.update(set(reasons))
            continue
        candidates.append({**historical, "eligible": True, "pipelines": evidence})
    return {
        **{key: history[key] for key in (
            "schema_version", "repository", "ref", "commit", "history_ref", "history_commit",
        )},
        "history_sha256": hashlib.sha256(history_bytes).hexdigest(),
        "complete": True,
        "generated_utc": now.isoformat(),
        "window_start": (now - datetime.timedelta(days=30)).isoformat(),
        "pipelines": {
            key: {"build_count": value["build_count"], "test_count": len(value["tests"])}
            for key, value in pipelines.items()
        },
        "excluded": dict(exclusions),
        "targets": candidates,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--history", required=True)
    parser.add_argument("--closed-prs", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()
    history_bytes = pathlib.Path(args.history).read_bytes()
    receipt = collect(
        args.repo_root, json.loads(history_bytes), history_bytes,
        json.loads(pathlib.Path(args.closed_prs).read_text(encoding="utf-8")),
        json.loads(os.environ["REQUARANTINE_ISSUES"]),
        json.loads(os.environ["REQUARANTINE_PRS"]),
        datetime.datetime.now(datetime.timezone.utc),
    )
    compact = json.dumps(receipt, separators=(",", ":"), sort_keys=True)
    if len(compact.encode("utf-8")) > 120000:
        raise ValueError("Unquarantine evidence exceeds the safe job-output limit")
    pathlib.Path(args.output).write_text(compact, encoding="utf-8")
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write(f"unquarantine_eligibility={compact}\n")
    print(f"Part 2 complete: {len(receipt['targets'])} eligible targets; {receipt['pipelines']}")


if __name__ == "__main__":
    main()
