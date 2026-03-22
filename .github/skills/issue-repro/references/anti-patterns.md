# Anti-Patterns (Full Reference)

The critical rules are in SKILL.md. This file has the complete list with examples.

## Step Result Decision Table

| You Run | What Happens | Step Result | Why |
|---------|--------------|-------------|-----|
| `dotnet build` | Build succeeds, exits 0 | `success` | Command succeeded |
| `dotnet build` | Build fails with error | `failure` | Command failed |
| `dotnet build` (reporter said it fails) | Build fails | **`failure`** | Command still failed — matching the report doesn't change the technical outcome |
| `curl http://localhost:5000/api` | Returns 500 but exits 0 | `wrong-output` | Process succeeded, HTTP output is incorrect |
| App starts, returns wrong JSON | curl exits 0, response body wrong | `wrong-output` | Process succeeded, output incorrect |

## Full Anti-Pattern List

1. **Inline binary content.** Use `artifacts` array with URLs, not inline data.
2. **Unlimited output.** Truncate: 2KB success, 4KB failure, 50 lines stack trace.
3. **Source code investigation.** Stop at "did it reproduce." Root cause is the fix team's job.
4. **Prompting the user.** Auto-proceed with logged warning. Skill must be non-interactive.
5. **Absolute paths in output.** Redact `/Users/{name}/` → `$HOME/`.
6. **Giving up too early.** Try multiple approaches, versions, platforms before `not-reproduced`.
7. **Stopping at build success.** Many ASP.NET Core bugs manifest at RUNTIME or only when HTTP requests are made.
8. **Editorial judgment in conclusion.** `reproduced` = reported behavior occurred, even if by-design.
9. **Mismarking step results.** `result` = technical outcome, not expectation match.
10. **Abandoning on environment issues.** Missing workloads, port conflicts are fixable — not blockers.
11. **Skipping Docker for Linux bugs.** Try Docker before concluding `needs-platform`.
12. **Reusing build artifacts across versions.** Fresh project dirs or `rm -rf bin/ obj/` between versions.
13. **Git push.** NEVER `git push`. Output goes to `artifacts/ai/repro/` only.
14. **Using repo SDK.** Run `dotnet --info` in `/tmp/aspnetcore/repro/`, NOT in the repo (which has `global.json`).
15. **No repo markdown artifacts.** NEVER create `REPRO_SUMMARY.md` or similar in the repo working tree.
16. **Fabricating output.** NEVER claim "reproduced" from static code analysis. Execute the code.
17. **Modifying product source.** Repro ONLY creates new test projects in `/tmp/aspnetcore/repro/`. Never edit `src/`.
18. **Skipping validation.** NEVER skip `validate-repro.ps1` / `validate-repro.py`. Mentally reviewing JSON is not validation.

## Output Limits

| Field | Max Size |
|-------|----------|
| `reproductionSteps[].output` (success) | 2KB |
| `reproductionSteps[].output` (failure) | 4KB |
| `errorMessages.stackTrace` | 5KB / 50 lines |

**Redaction:** `/Users/{name}/` → `$HOME/`, tokens → `[REDACTED]`

## Additional Context

19. **Never use `sudo`.** Find alternatives: user-local installs, Docker with user, different approach.

20. **Intermittent bugs require multiple runs.** Run 3–5 times before declaring `not-reproduced`. Report ratio (e.g., "Intermittent: 2/5 runs").

21. **ASP.NET Core requires HTTP requests.** Many bugs only surface when an actual HTTP request is made to the server, not just at startup. Use `curl`, `HttpClient`, or similar to exercise the endpoint.
