#!/usr/bin/env python3
"""
validate-repro.py — Validates a repro JSON file for dotnet/aspnetcore.

Usage:
    python validate-repro.py <path-to-json>

Exit codes:
    0 = valid
    1 = schema/content violations
    2 = file error / unreadable
"""

import json
import sys
from pathlib import Path

VALID_CONCLUSIONS = {
    "reproduced", "not-reproduced", "needs-platform",
    "needs-hardware", "partial", "inconclusive"
}
VALID_LAYERS = {"setup", "csharp", "hosting", "middleware", "http", "deployment"}
VALID_STEP_RESULTS = {"success", "failure", "wrong-output", "skip"}
VALID_VERSION_RESULTS = {"reproduced", "not-reproduced", "error", "not-tested"}
VALID_PROJ_TYPES = {
    "webapi", "mvc", "razorpages", "blazor-wasm", "blazor-server", "blazor-ssr",
    "grpc", "console", "docker", "test", "existing", "simulation"
}
VALID_ARCH = {"x64", "arm64", "x86", "wasm"}
VALID_SUGGESTED_ACTIONS = {
    "needs-investigation", "close-as-fixed", "close-as-by-design", "close-with-docs",
    "close-as-duplicate", "convert-to-discussion", "request-info", "keep-open"
}


def validate(data: dict) -> list[str]:
    errors = []

    # Required top-level
    for field in ("meta", "conclusion", "notes", "reproductionSteps", "environment"):
        if field not in data:
            errors.append(f"Missing required field: '{field}'")

    # meta
    if meta := data.get("meta"):
        if meta.get("repo") != "dotnet/aspnetcore":
            errors.append(f"meta.repo must be 'dotnet/aspnetcore' (got '{meta.get('repo')}')")
        if not isinstance(meta.get("number"), int) or meta.get("number", 0) <= 0:
            errors.append("meta.number must be a positive integer")
        if not meta.get("analyzedAt"):
            errors.append("meta.analyzedAt is required")

    # conclusion
    conclusion = data.get("conclusion")
    if conclusion:
        if conclusion not in VALID_CONCLUSIONS:
            errors.append(f"conclusion '{conclusion}' not in: {', '.join(sorted(VALID_CONCLUSIONS))}")
        # Conditional required fields based on conclusion
        needs_output    = {"reproduced", "not-reproduced"}
        needs_blockers  = {"needs-platform", "needs-hardware", "partial", "inconclusive"}
        if conclusion in needs_output:
            if "output" not in data:
                errors.append(f"'output' is required when conclusion is '{conclusion}'")
            if "versionResults" not in data:
                errors.append(f"'versionResults' is required when conclusion is '{conclusion}'")
        elif conclusion in needs_blockers:
            blockers = data.get("blockers")
            if not blockers or len(blockers) == 0:
                errors.append(f"'blockers' (non-empty) is required when conclusion is '{conclusion}'")

    # notes
    if notes := data.get("notes"):
        if len(notes) < 10:
            errors.append("notes is too short (minimum 10 characters)")

    # reproductionSteps
    if steps := data.get("reproductionSteps"):
        if not isinstance(steps, list) or len(steps) == 0:
            errors.append("reproductionSteps must be a non-empty array")
        else:
            for i, step in enumerate(steps):
                if not step.get("stepNumber"):
                    errors.append(f"reproductionSteps[{i}]: missing stepNumber")
                if not step.get("description"):
                    errors.append(f"reproductionSteps[{i}]: missing description")
                if (layer := step.get("layer")) and layer not in VALID_LAYERS:
                    errors.append(f"reproductionSteps[{i}]: layer '{layer}' not in: {', '.join(sorted(VALID_LAYERS))}")
                if (result := step.get("result")) and result not in VALID_STEP_RESULTS:
                    errors.append(f"reproductionSteps[{i}]: result '{result}' not in: {', '.join(sorted(VALID_STEP_RESULTS))}")

    # versionResults
    if vrs := data.get("versionResults"):
        for i, vr in enumerate(vrs):
            if not vr.get("version"):
                errors.append(f"versionResults[{i}]: missing version")
            if (result := vr.get("result")) and result not in VALID_VERSION_RESULTS:
                errors.append(f"versionResults[{i}]: result '{result}' not in: {', '.join(sorted(VALID_VERSION_RESULTS))}")

    # reproProject
    if rp := data.get("reproProject"):
        if (pt := rp.get("type")) and pt not in VALID_PROJ_TYPES:
            errors.append(f"reproProject.type '{pt}' not in: {', '.join(sorted(VALID_PROJ_TYPES))}")

    # environment
    if env := data.get("environment"):
        for field in ("os", "arch", "dotnetVersion", "aspnetcoreVersion"):
            if not env.get(field):
                errors.append(f"environment.{field} is required")
        if (arch := env.get("arch")) and arch not in VALID_ARCH:
            errors.append(f"environment.arch '{arch}' not in: {', '.join(sorted(VALID_ARCH))}")

    # output
    if output := data.get("output"):
        if act := output.get("actionability"):
            if (sa := act.get("suggestedAction")) and sa not in VALID_SUGGESTED_ACTIONS:
                errors.append(f"output.actionability.suggestedAction '{sa}' not in: {', '.join(sorted(VALID_SUGGESTED_ACTIONS))}")
            if (conf := act.get("confidence")) is not None:
                if not isinstance(conf, (int, float)) or not (0 <= conf <= 1):
                    errors.append("output.actionability.confidence must be between 0 and 1")

    return errors


def main():
    if len(sys.argv) < 2:
        print("Usage: python validate-repro.py <path-to-json>", file=sys.stderr)
        sys.exit(2)

    path = Path(sys.argv[1])
    if not path.exists():
        print(f"File not found: {path}", file=sys.stderr)
        sys.exit(2)

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        print(f"Cannot parse JSON: {exc}", file=sys.stderr)
        sys.exit(2)

    errors = validate(data)

    if errors:
        print(f"VALIDATION FAILED — {len(errors)} error(s):", file=sys.stderr)
        for e in errors:
            print(f"  ✗ {e}", file=sys.stderr)
        sys.exit(1)

    meta = data.get("meta", {})
    steps = data.get("reproductionSteps", [])
    vrs = data.get("versionResults", [])
    print(f"VALIDATION PASSED — {path}")
    print(f"  conclusion:    {data.get('conclusion')}")
    print(f"  issue:         {meta.get('repo')}#{meta.get('number')}")
    print(f"  steps:         {len(steps)}")
    if vrs:
        print(f"  versions:      {len(vrs)}")
    sys.exit(0)


if __name__ == "__main__":
    main()
