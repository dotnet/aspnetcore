#!/usr/bin/env python3
"""Verify that a quarantine issue body carries an intact Known Issue payload.

This is the deterministic counterpart to the payload the collector renders. It is
self-contained on purpose: everything it needs is inside the issue body, so it can be run
against a freshly created issue, an old one, or a body captured in a test — without
needing the originating workflow run's data.

What it proves:
  * the `kbe-signature` marker is present and well-formed
  * there is exactly one fenced ```json block, and it parses
  * the blob has the Arcade-required shape and our required settings
  * `ErrorMessage` still hashes to the value recorded in the marker, i.e. the agent
    copied it rather than paraphrasing, re-wrapping, or truncating it
  * the body does not contain two `## Error Message` headings

What it does NOT prove: that the signature actually matches the cited build. That is a
live Azure DevOps question and is deliberately out of scope here (it is the next
milestone). Nothing in this file makes a network call.

Usage:
    verify_kbe_payload.py <file-with-issue-body> [...]
    cat body.md | verify_kbe_payload.py -

Exit codes: 0 = every body verified, 1 = at least one body failed.
"""

import hashlib
import json
import re
import sys

MARKER = re.compile(
    r"<!--\s*kbe-signature:\s*v1\s+build=(?P<build>\d+)\s+"
    r"captured=(?P<captured>\S+)\s+sha256_12=(?P<hash>[0-9a-f]{12})\s*-->"
)
JSON_FENCE = re.compile(r"```json\s*\n(.*?)\n\s*```", re.S)
REQUIRED_HEADING = "### Known Issue Error Message"


def verify(body, label="body"):
    """Return (ok, notes). `ok` is False only for a payload that is present but broken."""
    notes = []

    marker = MARKER.search(body)
    has_heading = REQUIRED_HEADING in body

    if not marker and not has_heading:
        # A body with no payload at all is a legitimate outcome: the collector fails
        # closed whenever it cannot derive a trustworthy signature. Absence is not an
        # error, so report it and move on.
        return True, [f"{label}: no Known Issue payload present (nothing to verify)"]

    if not marker:
        return False, [f"{label}: has the '{REQUIRED_HEADING}' heading but no kbe-signature marker"]
    if not has_heading:
        return False, [f"{label}: has a kbe-signature marker but not the '{REQUIRED_HEADING}' heading"]

    fences = JSON_FENCE.findall(body)
    if len(fences) != 1:
        return False, [f"{label}: expected exactly 1 fenced json block, found {len(fences)}"]

    try:
        blob = json.loads(fences[0])
    except json.JSONDecodeError as ex:
        return False, [f"{label}: fenced json does not parse ({ex})"]

    if not isinstance(blob, dict):
        return False, [f"{label}: fenced json is {type(blob).__name__}, expected an object"]

    ok = True
    sig = blob.get("ErrorMessage")
    if not isinstance(sig, str) or not sig:
        return False, [f"{label}: ErrorMessage is missing or not a non-empty string"]

    actual = hashlib.sha256(sig.encode("utf-8")).hexdigest()[:12]
    if actual != marker.group("hash"):
        ok = False
        notes.append(
            f"{label}: ErrorMessage was modified after capture — marker records "
            f"{marker.group('hash')}, body hashes to {actual}. The signature is a "
            f"String.Contains matcher, so any edit breaks it."
        )

    if "ErrorPattern" in blob:
        ok = False
        notes.append(f"{label}: blob mixes ErrorMessage with ErrorPattern; Arcade treats these as alternatives")
    if blob.get("BuildRetry") is not False:
        ok = False
        notes.append(f"{label}: BuildRetry must be false, found {blob.get('BuildRetry')!r}")
    if blob.get("ExcludeConsoleLog") is not True:
        ok = False
        notes.append(f"{label}: ExcludeConsoleLog must be true, found {blob.get('ExcludeConsoleLog')!r}")

    if body.count("## Error Message") > 1:
        ok = False
        notes.append(f"{label}: body contains more than one '## Error Message' heading")

    build = f"buildId={marker.group('build')}"
    if build not in body:
        ok = False
        notes.append(f"{label}: marker cites build {marker.group('build')} but no matching Build link is present")

    if "Leg Name:" not in body:
        ok = False
        notes.append(f"{label}: payload is missing the 'Leg Name:' line")

    if ok:
        notes.append(f"{label}: payload verified (build {marker.group('build')}, signature {actual})")
    return ok, notes


def main(argv):
    if not argv:
        print(__doc__)
        return 2
    failed = False
    for path in argv:
        body = sys.stdin.read() if path == "-" else open(path, encoding="utf-8").read()
        ok, notes = verify(body, label=path)
        for n in notes:
            print(("  ok  " if ok else "FAIL  ") + n)
        failed = failed or not ok
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
