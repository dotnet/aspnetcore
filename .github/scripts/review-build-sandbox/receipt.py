"""Bind optional execution evidence to this run without exposing target logs to AI."""

import hashlib
import json
import math
import os
from pathlib import Path
import re
import sys

from contract import PROFILES, commands

ACCEPTANCE = (
    "container_gate", "nonzero_tests", "pinned_sdk", "allow_and_deny",
    "no_credentials_or_control_paths", "exact_head_and_tree", "disk_reserve", "memory_reserve",
)
FILES = {
    "awf.log", "awf-config.json", "probes.json", "phases.json", "bootstrap.log",
    "sdk.log", "restore.log", "build.log", "test.log", "tests.xml", "failure.txt",
}


def unavailable(target, reason, classification="unavailable"):
    return {
        "schemaVersion": 1, "headSha": target["head"], "mergeBaseSha": target["mergeBase"],
        "baseTipSha": target["baseTip"], "available": False, "classification": classification,
        "reason": reason, "supportingEvidenceOnly": True,
    }


def normalize(report, target, identity, helper_hashes):
    result = unavailable(target, "Execution evidence did not match the frozen source and current run.")
    expected = {
        "schema": 2, "repository": identity["repository"],
        "workflow_repository": identity["repository"], "pull_request": identity["pr"],
        "head_sha": target["head"], "base_tip": target["baseTip"], "merge_base": target["mergeBase"],
        "workflow_sha": identity["workflow_sha"], "implementation_sha": identity["workflow_sha"],
        "run_id": identity["run_id"], "run_attempt": identity["run_attempt"],
        **helper_hashes,
    }
    if not isinstance(report, dict) or any(
        type(report.get(key)) is not type(value) or report[key] != value
        for key, value in expected.items()
    ):
        return result
    if report.get("status") == "unsupported":
        return unavailable(target, "No supported bounded managed-test profile matched this change.", "unsupported")
    if report.get("status") != "passed":
        return unavailable(target, "Sandboxed execution did not complete successfully; see the retained receipt.")
    profile = report.get("profile")
    if profile not in PROFILES:
        return unavailable(target, "Execution evidence selected an unknown profile.")
    acceptance = report.get("acceptance")
    if not isinstance(acceptance, dict) or not all(acceptance.get(key) is True for key in ACCEPTANCE):
        return unavailable(target, "Execution evidence did not pass every required boundary and execution check.")
    if not isinstance(report.get("sdk"), str) or not re.fullmatch(
        r"[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9.-]+)?", report["sdk"]
    ):
        return unavailable(target, "Execution evidence has no valid target SDK.")
    tests = report.get("tests")
    if not isinstance(tests, dict) or any(
        type(tests.get(key)) is not int or not 0 <= tests[key] <= 100000
        for key in ("executed", "passed", "failed", "skipped")
    ):
        return unavailable(target, "Execution evidence has invalid test counts.")
    if tests["executed"] == 0 or tests["passed"] != tests["executed"] or tests["failed"] != 0:
        return unavailable(target, "Execution evidence has no successful nonzero executed-test result.")
    planned = commands(profile)
    phases = report.get("phases")
    if not isinstance(phases, list) or len(phases) != len(planned):
        return unavailable(target, "Execution evidence has incomplete phase records.")
    for phase, (name, command) in zip(phases, planned.items()):
        if (not isinstance(phase, dict) or phase.get("name") != name or phase.get("command") != command
                or type(phase.get("exit")) is not int or phase["exit"] != 0
                or type(phase.get("seconds")) not in (int, float)
                or not math.isfinite(phase["seconds"]) or not 0 <= phase["seconds"] <= 1800):
            return unavailable(target, "Execution evidence has unexpected commands, timings, or process results.")
    uncovered = report.get("uncovered_changed_paths")
    if not isinstance(uncovered, list) or len(uncovered) > 1000 or any(not isinstance(p, str) for p in uncovered):
        return unavailable(target, "Execution evidence has an invalid profile boundary.")
    return {
        **result, "available": True, "classification": "head-green",
        "reason": "The selected existing project passed at the frozen head; no baseline or authored repro ran.",
        "profile": profile, "sdk": report["sdk"],
        "counts": {key: tests[key] for key in ("executed", "passed", "failed", "skipped")},
        "phases": [{key: phase[key] for key in ("name", "command", "exit", "seconds")} for phase in phases],
        "uncoveredChangedPathCount": len(uncovered),
    }


def verify_files(report, directory):
    hashes = report.get("file_sha256")
    if not isinstance(hashes, dict) or any(name not in FILES for name in hashes):
        raise ValueError("Unexpected evidence file inventory.")
    if report.get("status") == "passed" and not FILES - {"failure.txt"} <= hashes.keys():
        raise ValueError("Required execution files are missing.")
    for name, expected in hashes.items():
        path = directory / name
        if (not isinstance(expected, str) or not re.fullmatch(r"[a-f0-9]{64}", expected)
                or path.is_symlink() or not path.is_file() or path.stat().st_size > 8 * 1024 * 1024
                or hashlib.sha256(path.read_bytes()).hexdigest() != expected):
            raise ValueError("Execution evidence file hash or bounds failed.")


def public_summary(result, run_url):
    if not re.fullmatch(
        r"https://github\.com/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+/actions/runs/[1-9][0-9]*", run_url
    ):
        raise ValueError("Invalid trusted execution run URL.")
    lines = [
        "Execution: " + result["classification"],
        "Frozen trees: head " + result["headSha"] + "; merge base " + result["mergeBaseSha"]
        + "; base tip " + result["baseTipSha"] + ".",
    ]
    if result["available"]:
        counts = result["counts"]
        lines.extend([
            "Profile: " + result["profile"]["id"] + "; project " + result["profile"]["project"] + ".",
            "Target SDK: " + result["sdk"] + ".",
            f"Recorded rows: head {counts['passed']} passed, 0 failed, {counts['skipped']} skipped.",
            "No baseline, product reversion, browser workflow, or newly authored repro was executed.",
            "Changed paths outside this profile: " + str(result["uncoveredChangedPathCount"]) + ".",
            "Recorded commands and full results: review-execution artifact at " + run_url,
        ])
    else:
        lines.extend([
            "Unavailable reason (JSON string): " + json.dumps(result["reason"], ensure_ascii=True),
            "Execution report/logs, if recorded: review-execution artifact at " + run_url,
        ])
    lines.append("Supporting evidence only; source findings are not execution-verified.")
    return "\n".join(lines)


def main():
    report_path, manifest_path, output_path = map(Path, sys.argv[1:])
    target = json.loads(manifest_path.read_text(encoding="utf-8"))["target"]
    identity = {
        "repository": os.environ["GITHUB_REPOSITORY"], "pr": os.environ["REVIEW_PR"],
        "workflow_sha": os.environ["GITHUB_WORKFLOW_SHA"], "run_id": os.environ["GITHUB_RUN_ID"],
        "run_attempt": os.environ["GITHUB_RUN_ATTEMPT"],
    }
    helper_hashes = {
        key + "_sha256": hashlib.sha256(Path(__file__).with_name(filename).read_bytes()).hexdigest()
        for key, filename in (("launcher", "host.py"), ("guest", "guest.py"), ("contract", "contract.py"))
    }
    try:
        if report_path.is_symlink() or report_path.stat().st_size > 256 * 1024:
            raise ValueError("Execution receipt is oversized or linked.")
        report = json.loads(report_path.read_text(encoding="utf-8"))
        verify_files(report, report_path.parent)
        result = normalize(report, target, identity, helper_hashes)
    except FileNotFoundError:
        result = unavailable(target, "The execution receipt or a required evidence file is missing.")
    except (OSError, ValueError, KeyError, TypeError, AttributeError, RecursionError):
        result = unavailable(target, "The execution receipt is unreadable, malformed, or failed file verification.")
    if not result["available"]:
        print("Execution evidence unavailable: " + result["reason"], file=sys.stderr)
    result["publicSummary"] = public_summary(result, os.environ["REVIEW_EXECUTION_RUN_URL"])
    output_path.write_text(json.dumps(result, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    output_path.with_name("execution-public-summary.txt").write_text(result["publicSummary"], encoding="utf-8")


if __name__ == "__main__":
    main()
