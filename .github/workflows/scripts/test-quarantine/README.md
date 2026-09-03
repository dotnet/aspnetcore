# test-quarantine evidence capture

Deterministic support for the Known Issue payload that
[`.github/workflows/test-quarantine.md`](../../test-quarantine.md) attaches to every new
quarantine issue.

## Why this exists

Quarantine issues used to record a human paraphrase of a failure. That paraphrase is not
matchable, and the Azure DevOps build it came from ages out of public retention, so a few
weeks later the issue is the only surviving record and it no longer contains the actual
error text. Anyone trying to fix the test later has nothing precise to work from.

The workflow now captures the real failure signature at the moment the evidence still
exists, in the format Arcade's Known Issues already use, and the agent pastes it verbatim
instead of describing it.

## What gets emitted

A section appended to the issue body:

```md
### Known Issue Error Message

<!-- kbe-signature: v1 build=<id> captured=<utc> sha256_12=<digest> -->

Build: https://dev.azure.com/dnceng-public/public/_build/results?buildId=<id>&view=results
Leg Name: <azure devops test run name>

```json
{
  "ErrorMessage": "<literal substring of the real errorMessage>",
  "BuildRetry": false,
  "ExcludeConsoleLog": true
}
```

<details><summary>Capture details</summary>
… test, assembly, test run, platform, configuration, evidence build, capture time,
and the observed-failure window …
</details>
```

### Design decisions worth knowing

**The signature is a literal substring.** Arcade matches `ErrorMessage` with
`String.Contains`. Volatile fragments (ports, GUIDs, timings, addresses, paths, counters)
are located and the longest *contiguous* stable run between them is taken. Fragments are
never stitched back together, because a stitched string would never match anything.

**It comes from `errorMessage`, not the console log.** Arcade matches test errors against
the error message, the stack trace, and — only when `ExcludeConsoleLog` is `false` — the
Helix console. The common `FQN.+\[FAIL\]` pattern seen in other repos works only for
issues that opt into the console log.

**`ExcludeConsoleLog: true`.** One xunit console log is shared by every test in a Helix
work item, so including it attributes unrelated failures to the issue. This was observed
live on dotnet/aspnetcore#62308, whose signature targets one test but whose hit table
lists a different one.

**`BuildRetry: false`.** A quarantined test must never cause a build retry.

**The heading is `### Known Issue Error Message`, not `## Error Message`.** The quarantine
issue template already uses `## Error Message` for human-readable prose. dotnet/aspnetcore#57416
is a live, dnceng-validated Known Build Error whose blob sits under this exact heading with
no `## Error Message` section at all, which confirms the heading is not load-bearing for
validation.

**The `Known Build Error` label is deliberately NOT applied.** A Known Build Error means
"this still blocks other people's builds". A quarantined test no longer blocks anything,
so labeling would put ~115 already-handled issues onto dnceng's board. In dotnet/runtime
only ~9% of `disabled-test` issues also carry `Known Build Error`, and in those cases the
Known Build Error came *first*, while the test was still failing. Applying the label is a
separate, later, human-gated decision. This milestone only prepares a valid payload.

## Files

| File | Purpose |
| --- | --- |
| `test_kbe_signature.py` | Extracts the shipped derivation from the workflow and tests it |
| `verify_kbe_payload.py` | Standalone checker for any issue body; no network access |

### `test_kbe_signature.py`

Does **not** re-implement the derivation. It extracts the code between the
`# --- BEGIN kbe-signature` / `# --- END kbe-signature ---` sentinels in
`test-quarantine.md` and executes it, so a green run is evidence about the *shipped* code.
If the sentinels are moved or removed, the test fails loudly rather than silently passing
against nothing. It also compiles the whole inlined collector to catch syntax breakage.

```console
$ python3 .github/workflows/scripts/test-quarantine/test_kbe_signature.py
```

Notable coverage: the derivation independently reproduces the signature a human expert
hand-wrote for the real dotnet/aspnetcore#68708 Known Build Error; every derived signature
is asserted to be a literal substring of its source; generic text such as
`Assert.True() Failure` fails closed instead of producing a signature that would match
half the repository.

### `verify_kbe_payload.py`

Validates a rendered payload found in an issue body: marker present and well-formed,
exactly one parseable JSON fence, required Arcade fields and settings, `Build:`/`Leg Name:`
present, no duplicated `## Error Message` heading, and `ErrorMessage` still hashing to the
digest recorded in the marker.

```console
$ python3 .github/workflows/scripts/test-quarantine/verify_kbe_payload.py body.md
$ gh issue view 12345 -R dotnet/aspnetcore --json body -q .body | \
    python3 .github/workflows/scripts/test-quarantine/verify_kbe_payload.py -
```

A body with no payload at all is reported as "nothing to verify" and is **not** a failure:
the collector fails closed whenever it cannot derive a trustworthy signature, and an honest
absence is a valid outcome.

## Known limitations

- **Verification is not wired into the workflow as a job.** `verify_kbe_payload.py` is
  deliberately a standalone auditing tool rather than a gate inside `test-quarantine`. The
  payload is produced by deterministic collector code and the agent's only task is to paste
  it, so an in-workflow gate would add a new write surface and a new failure mode to guard
  against a narrow, already-instructed-against mistake. The agent instead performs a
  mandatory four-point self-check before submitting, and the digest in the marker means any
  issue can be audited after the fact — from any machine, with no access to the originating
  run. Promoting verification to an enforced gate belongs with the milestone that acts on
  these payloads, not the one that produces them.
- **The hash detects mangling, not forgery.** Re-hashing proves the agent copied the text
  rather than paraphrasing, re-wrapping or truncating it. A coordinated rewrite of both the
  text and the digest would still pass. The test suite includes a 2×2 case that
  demonstrates exactly this boundary.
- **Nothing here proves the signature matches the cited build.** That requires a live Azure
  DevOps query and is intentionally out of scope; it is the next milestone.
- **Grouped issues.** When one issue covers several tests, the payload is included only if
  every test in the group derived a byte-identical signature. Otherwise it is omitted
  rather than implying one test's signature represents the others.
- **Work items are skipped.** Their text comes from the shared Helix console, which is the
  source `ExcludeConsoleLog` is set to ignore.
- **Unrecognized environments record `unknown`.** Platform and configuration are parsed from
  the real Azure DevOps test run name. When a name is not recognized the literal string
  `unknown` is recorded rather than a plausible-looking default, because this block is read
  as captured fact after the build data is gone.

## Changing the derivation

1. Edit the code inside the sentinels in `.github/workflows/test-quarantine.md`. Never edit
   `test-quarantine.lock.yml` by hand.
2. Run `test_kbe_signature.py`.
3. Recompile with the version this repository is pinned to:
   `gh extension install github/gh-aw --pin v0.86.2 && gh aw compile test-quarantine`.
   Compiling with a different CLI version rewrites `.github/aw/actions-lock.json` and bumps
   the recorded compiler version, which mixes a toolchain change into an unrelated one.
