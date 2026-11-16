#!/usr/bin/env python3

import argparse
import importlib.util
import json
import pathlib


ELIGIBILITY_SCRIPT = pathlib.Path(__file__).with_name(
    "collect_case_a_eligibility.py"
)
SPEC = importlib.util.spec_from_file_location(
    "test_quarantine_eligibility",
    ELIGIBILITY_SCRIPT,
)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--repo-root", default=".")
    parser.add_argument(
        "--history-ref",
        default="refs/remotes/origin/main",
    )
    parser.add_argument("--repository", required=True)
    parser.add_argument("--ref", required=True)
    parser.add_argument("--commit", required=True)
    args = parser.parse_args()

    receipt = MODULE.collect_requarantine_history(
        args.repo_root,
        args.history_ref,
        args.repository,
        args.ref,
        args.commit,
    )
    pathlib.Path(args.output).write_text(
        json.dumps(receipt, separators=(",", ":"), sort_keys=True),
        encoding="utf-8",
    )
    counts = {}
    for target in receipt["targets"]:
        status = target["status"]
        counts[status] = counts.get(status, 0) + 1
    print(f"Current quarantine history: {counts}")


if __name__ == "__main__":
    main()
