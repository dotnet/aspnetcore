#!/usr/bin/env python3
"""Offline tests for the Known Build Error signature derivation shipped in
`.github/workflows/test-quarantine.md`.

These tests do not re-implement the derivation. They extract it verbatim from the
workflow between the `kbe-signature` sentinels and execute it, so a passing run proves
the *shipped* code behaves correctly. If the sentinels move or disappear, the tests fail
loudly rather than silently testing nothing.

Run: python3 .github/workflows/scripts/test-quarantine/test_kbe_signature.py
"""

import datetime
import hashlib
import json
import pathlib
import re
import sys
import textwrap

WORKFLOW = pathlib.Path(__file__).resolve().parents[2] / "test-quarantine.md"
BEGIN = "# --- BEGIN kbe-signature"
END = "# --- END kbe-signature ---"

failures = []


def check(label, condition, detail=""):
    if condition:
        print(f"  PASS  {label}")
    else:
        print(f"  FAIL  {label}  {detail}")
        failures.append(label)


def load_shipped_module():
    """Extract the sentinel-delimited block from the workflow and exec it."""
    if not WORKFLOW.is_file():
        sys.exit(f"FATAL: cannot find workflow at {WORKFLOW}")
    text = WORKFLOW.read_text(encoding="utf-8")
    start = text.find(BEGIN)
    end = text.find(END)
    if start < 0 or end < 0 or end <= start:
        sys.exit(
            "FATAL: kbe-signature sentinels not found in test-quarantine.md. "
            "The derivation must stay wrapped in the BEGIN/END markers so this test "
            "exercises the shipped code instead of a stale copy."
        )
    # Rewind to the start of the marker's line so the block's common indentation is
    # uniform; textwrap.dedent would otherwise see an unindented first line.
    start = text.rfind("\n", 0, start) + 1
    block = textwrap.dedent(text[start:end])
    # The block references names defined elsewhere in the collector. Read their real values
    # straight out of the workflow rather than hardcoding them here — a hardcoded copy that
    # drifts from the source makes dependent tests pass without ever exercising the real path.
    wi = re.search(r'^\s*WI_SUFFIX\s*=\s*(["\'])(.*?)\1\s*$', text, re.M)
    if not wi:
        sys.exit("FATAL: could not read WI_SUFFIX from test-quarantine.md")
    ns = {"re": re, "json": json, "datetime": datetime, "hashlib": hashlib,
          "WI_SUFFIX": wi.group(2)}
    exec(compile(block, "test-quarantine.md::kbe-signature", "exec"), ns)
    for required in ("derive_signature", "stable_segments", "attach_kbe", "parse_environment",
                     "SIG_MIN_LEN", "SIG_MAX_LEN"):
        if required not in ns:
            sys.exit(f"FATAL: shipped block does not define {required}")
    return ns


def workflow_python_compiles():
    """The inlined collector must still be syntactically valid Python."""
    text = WORKFLOW.read_text(encoding="utf-8")
    m = re.search(r"python3 << 'SCRIPT'\n(.*?)\n\s*SCRIPT\n", text, re.S)
    if not m:
        return False, "could not locate the inlined python3 heredoc"
    try:
        compile(textwrap.dedent(m.group(1)), "test-quarantine.md::collector", "exec")
        return True, ""
    except SyntaxError as ex:
        return False, f"{ex.msg} at line {ex.lineno}"


def main():
    ns = load_shipped_module()
    derive = ns["derive_signature"]

    print("\nshipped collector integrity")
    ok, detail = workflow_python_compiles()
    check("inlined collector is valid Python", ok, detail)

    print("\nreal-world signatures")

    # Captured from dotnet/aspnetcore#68708. The hand-authored KBE a human filed for this
    # exact failure used "should remain aligned with the viewport top once the last item
    # has loaded, but top rendered index was -1". The derivation must land on that text.
    quickgrid = (
        "Xunit.Sdk.TrueException: Item 950 should remain aligned with the viewport top "
        "once the last item has loaded, but top rendered index was -1, scrollTop=0."
    )
    sig, reason = derive(quickgrid)
    check("quickgrid derives a signature", sig is not None, reason or "")
    check(
        "quickgrid matches the human-authored KBE text",
        sig is not None
        and "should remain aligned with the viewport top once the last item has loaded" in sig,
        repr(sig),
    )

    # dotnet/aspnetcore#68947 shape: volatile port and timeout duration must be excluded.
    webdriver = (
        "OpenQA.Selenium.WebDriverException : The HTTP request to the remote WebDriver "
        "server for URL http://localhost:52380/session timed out after 60 seconds."
    )
    sig, reason = derive(webdriver)
    check("webdriver derives a signature", sig is not None, reason or "")
    check("webdriver signature excludes the volatile port", sig is not None and "52380" not in sig, repr(sig))
    check("webdriver signature excludes the timeout duration", sig is not None and "60 seconds" not in sig, repr(sig))
    check("webdriver signature keeps the exception type", sig is not None and "WebDriverException" in sig, repr(sig))

    print("\nsubstring guarantee (ErrorMessage is a Contains match)")
    corpus = [
        quickgrid,
        webdriver,
        "System.InvalidOperationException: Circuit 3f2504e0-4f89-11d3-9a0c-0305e82c3301 was not found in the registry.",
        "System.IO.FileNotFoundException: Could not load file /home/vsts/work/1/s/artifacts/bin/Foo.dll while probing.",
        "Expected the response to contain the negotiated protocol handshake frame\nActual: connection closed at 0xdeadbeef",
        "System.TimeoutException: The operation did not complete within 00:00:30 while awaiting the circuit host.",
    ]
    all_substrings = True
    for text in corpus:
        sig, _ = derive(text)
        if sig is not None and sig not in text:
            all_substrings = False
            print(f"        not a substring: {sig!r}")
    check("every derived signature is a literal substring of its source", all_substrings)

    print("\nfail-closed behaviour")
    for label, text, expected in [
        ("empty text", "", "no-error-message"),
        ("whitespace only", "   \n  \t ", "no-error-message"),
        ("generic assertion", "Assert.True() Failure", "no-stable-signature-segment"),
        ("entirely volatile", "Timeout 30000 at 0x7ffd 192.168.1.5 #4412", "no-stable-signature-segment"),
        ("too short after split", "id 123456 ok", "no-stable-signature-segment"),
    ]:
        sig, reason = derive(text)
        check(f"{label} yields no signature", sig is None, repr(sig))
        check(f"{label} reports {expected}", reason == expected, repr(reason))

    print("\nbounds")
    long_text = "System.Exception: " + ("A" * 500)
    sig, _ = derive(long_text)
    check("long signature is capped", sig is not None and len(sig) <= ns["SIG_MAX_LEN"], len(sig or ""))
    check("capped signature is still a substring", sig is not None and sig in long_text)

    short = "x" * (ns["SIG_MIN_LEN"] - 1)
    sig, reason = derive(short)
    check("below-minimum text is rejected", sig is None, repr(sig))

    exact = "y" * ns["SIG_MIN_LEN"]
    sig, _ = derive(exact)
    check("exactly-minimum text is accepted", sig is not None)

    print("\nArcade blob validity")
    sig, _ = derive(quickgrid)
    blob = {"ErrorMessage": sig, "BuildRetry": False, "ExcludeConsoleLog": True}
    encoded = json.dumps(blob, indent=2)
    check("blob round-trips as JSON", json.loads(encoded) == blob)
    check("BuildRetry is false", blob["BuildRetry"] is False)
    check("ExcludeConsoleLog is true", blob["ExcludeConsoleLog"] is True)
    check("blob does not use the console-only [FAIL] form", "[FAIL]" not in (sig or ""))
    check("blob uses ErrorMessage, not ErrorPattern", "ErrorPattern" not in blob)

    # Quoting is the documented Arcade footgun: the value must survive JSON escaping.
    quoted = 'Assertion failed because the "expected" header was absent from the response'
    sig, _ = derive(quoted)
    blob = {"ErrorMessage": sig, "BuildRetry": False, "ExcludeConsoleLog": True}
    check("quoted signature survives JSON encoding", json.loads(json.dumps(blob))["ErrorMessage"] == sig)

    print("\nrendered section (attach_kbe)")
    attach = ns["attach_kbe"]
    WORK_ITEM_KEY = "Some.Assembly" + ns["WI_SUFFIX"]

    agg = {
        "Good.Test.Method": {
            "error": quickgrid,
            "leg": "Windows.Amd64.VS2026.Open",
            "evidence_build": 1569737,
            "builds": [1569737, 1569737, 1538879],
            "count": 3,
            "assembly": "Components.E2ETests--net11.0",
        },
        "No.Leg.Test": {"error": quickgrid, "evidence_build": 1569737, "builds": [1569737]},
        "No.Build.Test": {"error": quickgrid, "leg": "Linux-Release-xunit", "builds": []},
        "Generic.Test": {
            "error": "Assert.True() Failure",
            "leg": "Linux-Release-xunit",
            "evidence_build": 1569737,
            "builds": [1569737],
        },
        "No.Error.Test": {"leg": "Linux-Release-xunit", "evidence_build": 1569737, "builds": []},
        # Built from the real WI_SUFFIX read out of the workflow, so this genuinely exercises
        # the work-item skip rather than a suffix that only exists in this test.
        WORK_ITEM_KEY: {"error": quickgrid, "leg": "L", "evidence_build": 1, "builds": [1]},
    }
    bmeta = {
        "1569737": {"startedUtc": "2026-08-13T04:11:00Z", "def": 83},
        "1538879": {"startedUtc": "2026-08-01T09:02:00Z", "def": 83},
    }
    attach(agg, bmeta, "last 30 days, pipelines 83 and 87, `refs/heads/main`")

    good = agg["Good.Test.Method"]
    check("eligible test gets a kbe", "kbe" in good)
    check("eligible test has no reason", "kbe_reason" not in good)

    section = good.get("kbe", {}).get("section", "")
    check("section uses the Known Issue Error Message heading",
          section.startswith("### Known Issue Error Message"))
    check("section does not introduce a second '## Error Message'",
          "## Error Message" not in section)
    check("section carries the Build line", "Build: https://dev.azure.com/dnceng-public/" in section)
    check("section cites the evidence build", "buildId=1569737" in section)
    check("section carries the Leg Name", "Leg Name: Windows.Amd64.VS2026.Open" in section)
    check("section carries a provenance marker", "<!-- kbe-signature: v1 build=1569737" in section)
    check("section contains exactly one json fence", section.count("```json") == 1)

    print("\ncapture-details block")
    check("records the platform", "| Platform | Windows |" in section, section)
    check("records the configuration", "| Configuration | not-encoded |" in section)
    check("records the assembly", "`Components.E2ETests--net11.0`" in section)
    check("counts distinct builds, not occurrences", "across 2 build(s)" in section, section)
    check("reports the earliest observed failure", "first 2026-08-01T09:02:00Z" in section)
    check("reports the latest observed failure", "last 2026-08-13T04:11:00Z" in section)
    check("labels the window as a snapshot", "not the test's complete history" in section)
    check("names the real query window", "pipelines 83 and 87" in section)

    # Round-trip the fenced block the way a consumer (or dnceng) would.
    fenced = section.split("```json", 1)[1].split("```", 1)[0]
    parsed = json.loads(fenced)
    check("fenced blob parses as JSON", isinstance(parsed, dict))
    check("fenced ErrorMessage equals the derived signature",
          parsed.get("ErrorMessage") == good["kbe"]["blob"]["ErrorMessage"])
    check("fenced ErrorMessage is a substring of the real error",
          parsed.get("ErrorMessage", "\0") in quickgrid)
    check("fenced blob omits ErrorPattern (never mix the two forms)", "ErrorPattern" not in parsed)
    check("fenced BuildRetry is false", parsed.get("BuildRetry") is False)
    check("fenced ExcludeConsoleLog is true", parsed.get("ExcludeConsoleLog") is True)

    print("\nenvironment parsing (real observed TestRun names)")
    env = ns["parse_environment"]
    for leg, expected in [
        ("Quarantine-Mono-Linux-Release-xunit", ("Linux", "Release")),
        ("ComponentsE2E-CoreCLR-Linux-Release-xunit", ("Linux", "Release")),
        ("Windows-Release-xunit", ("Windows", "Release")),
        ("Windows.Amd64.VS2026.Open", ("Windows", "not-encoded")),
        ("Ubuntu.2404.Amd64.Open", ("Linux", "not-encoded")),
        ("OSX.26.Arm64.Open", ("macOS", "not-encoded")),
        ("macOS-Release-xunit", ("macOS", "Release")),
        ("Linux-Debug-js", ("Linux", "Debug")),
        ("SomethingCompletelyNew", ("unknown", "unknown")),
    ]:
        check(f"{leg} -> {expected}", env(leg) == expected, repr(env(leg)))
    check("empty leg is unknown, never defaulted", env("") == ("unknown", "unknown"))

    print("\nfail-closed rendering")
    for key, expected in [
        ("No.Leg.Test", "no-leg-name"),
        ("No.Build.Test", "no-evidence-build"),
        ("Generic.Test", "no-stable-signature-segment"),
        ("No.Error.Test", "no-error-message"),
        (WORK_ITEM_KEY, "work-item-not-an-individual-test"),
    ]:
        entry = agg[key]
        check(f"{key} emits no kbe", "kbe" not in entry, repr(entry.get("kbe")))
        check(f"{key} reports {expected}", entry.get("kbe_reason") == expected,
              repr(entry.get("kbe_reason")))

    print("\nend-to-end verification (verify_kbe_payload.py)")
    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import verify_kbe_payload as V

    # A realistic issue body: the template sections the agent writes, plus the pasted block.
    def issue_body(payload):
        return "\n\n".join([
            "## Failing Test(s)",
            "`Good.Test.Method`",
            "## Failure Frequency",
            "Failed 3 times over the past 30 days.",
            "## Error Message",
            "```text\nsomething readable a human wrote\n```",
            "## Build",
            "https://dev.azure.com/dnceng-public/public/_build/results?buildId=1569737",
            payload,
        ])

    ok, notes = V.verify(issue_body(section), "intact")
    check("intact body verifies", ok, "; ".join(notes))

    ok, _ = V.verify(issue_body(""), "no-payload")
    check("body with no payload is not an error (collector fails closed)", ok)

    # The failure this whole mechanism exists to catch: the agent rewrites the signature.
    paraphrased = section.replace(
        good["kbe"]["blob"]["ErrorMessage"],
        "Item should stay aligned with the viewport top",
    )
    ok, notes = V.verify(issue_body(paraphrased), "paraphrased")
    check("paraphrased signature is rejected", not ok, "; ".join(notes))
    check("rejection explains the hash mismatch",
          any("modified after capture" in n for n in notes), "; ".join(notes))

    # Whitespace-only mangling still breaks String.Contains, so it must also fail.
    rewrapped = section.replace(
        good["kbe"]["blob"]["ErrorMessage"],
        good["kbe"]["blob"]["ErrorMessage"].replace(" ", "  ", 1),
    )
    ok, _ = V.verify(issue_body(rewrapped), "rewrapped")
    check("re-wrapped signature is rejected", not ok)

    for label, mutate, why in [
        ("duplicate error heading",
         lambda b: b + "\n\n## Error Message\n\nsecond one\n", "two headings"),
        ("BuildRetry flipped",
         lambda b: b.replace('"BuildRetry": false', '"BuildRetry": true'), "BuildRetry"),
        ("ExcludeConsoleLog flipped",
         lambda b: b.replace('"ExcludeConsoleLog": true', '"ExcludeConsoleLog": false'), "console"),
        ("broken json",
         lambda b: b.replace('"BuildRetry": false,', '"BuildRetry": false'), "parse"),
        ("marker removed",
         lambda b: re.sub(r"<!-- kbe-signature.*?-->", "", b), "marker"),
        ("second json fence added",
         lambda b: b + "\n\n```json\n{}\n```\n", "fences"),
        ("leg name dropped",
         lambda b: re.sub(r"Leg Name: .*\n", "", b), "Leg Name"),
    ]:
        ok, notes = V.verify(mutate(issue_body(section)), label)
        check(f"{label} is rejected", not ok, "; ".join(notes))

    # ErrorPattern must never be mixed in.
    mixed = section.replace('"BuildRetry": false,', '"BuildRetry": false,\n  "ErrorPattern": ".*",')
    ok, notes = V.verify(issue_body(mixed), "mixed")
    check("mixing ErrorMessage with ErrorPattern is rejected", not ok, "; ".join(notes))

    # Proves the hash check is a real comparison rather than a blanket reject, and documents
    # the honest limitation: a coordinated rewrite of both text and hash still passes, so this
    # detects mangling, not forgery.
    print("\nhash check discriminates (2x2)")
    def synth(text, digest):
        blob = json.dumps({"ErrorMessage": text, "BuildRetry": False, "ExcludeConsoleLog": True},
                          indent=2)
        return (
            "### Known Issue Error Message\n\n"
            f"<!-- kbe-signature: v1 build=1569737 captured=2026-09-03T00:00:00Z sha256_12={digest} -->\n\n"
            "Build: https://dev.azure.com/dnceng-public/public/_build/results?buildId=1569737&view=results\n"
            "Leg Name: Linux-Release-xunit\n\n"
            f"```json\n{blob}\n```\n"
        )

    a = "should remain aligned with the viewport top once the last item has loaded"
    b = "roughly the same thing but reworded by the agent entirely"
    digest = lambda s: hashlib.sha256(s.encode("utf-8")).hexdigest()[:12]
    for label, text, dig, expected in [
        ("original text + original hash", a, digest(a), True),
        ("reworded text + original hash", b, digest(a), False),
        ("reworded text + matching hash", b, digest(b), True),
        ("original text + wrong hash", a, digest(b), False),
    ]:
        got, _ = V.verify(synth(text, dig), "x")
        check(f"{label} -> {expected}", got == expected, f"got {got}")

    print()
    if failures:
        print(f"FAILED ({len(failures)}): " + ", ".join(failures))
        return 1
    print("All signature tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
