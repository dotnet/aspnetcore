# Triage Anti-Patterns

❌ **NEVER do these during triage.** Triage is READ-ONLY analysis.

---

### CRITICAL: Triage is READ-ONLY

**If you edit a source file during triage, you have FAILED.**

Do NOT create, edit, or run any of these: `.cs`, `.csproj`, `.sln`, `.targets`, `.props`, source `.json`.
Do NOT run `dotnet build`, `dotnet test`, `dotnet run`, or execute any reporter code.

If reproduction is needed, set `suggestedAction: "needs-investigation"` and stop — that is the `issue-repro` skill's job.

---

### 0. No git push, no branch operations

**NEVER run `git push`.** Output goes to `artifacts/ai/triage/` only. No commits to shared branches.

### 1. Pre-baked delegation

NEVER write your classification in a sub-agent prompt. Sub-agents investigate and return evidence. YOU classify based on their evidence.

### 2. Age-based closure

NEVER close an issue because it's old. Old issues with no code fix are STILL OPEN BUGS/REQUESTS.

### 3. Security issue via GitHub

NEVER comment on, label, or publicize security vulnerabilities via GitHub. Direct reporters to secure@microsoft.com (see SECURITY.md).

### 4. Assertion without citation

NEVER write "the code does X" without a `{file, lines}` entry in `codeInvestigation`. No file:line = no claim.

### 5. Batch shortcuts

When triaging multiple issues, each gets FULL investigation. Parallel investigation is fine; parallel conclusions are not.

### 6. .NET Forward Compatibility

NEVER conclude "doesn't support .NET X" when the library targets a lower TFM. .NET is forward-compatible — a `net8.0` library works on `net9.0` apps via TFM fallback. NEVER suggest "downgrade .NET" as a workaround.

Exception: platform-specific TFMs (e.g., `net8.0-browser`) where WASM-specific tooling is required.

### 7. No repo markdown artifacts

NEVER create markdown summary files or plans in the repository working tree. All working files belong in the session workspace (`~/.copilot/session-state/`).

### 8. Fabrication

NEVER invent code investigation findings, claim "the code shows X" without reading the actual file, or fabricate file paths / line numbers. If you cannot find the code, say so.

### 9. Skipping validation

NEVER skip the validation script (`validate-triage.ps1` / `validate-triage.py`). NEVER assume the JSON is valid without running it. NEVER persist to `artifacts/` without seeing ✅ from the validator.

### 10. Extensions repo confusion

dotnet/aspnetcore relies heavily on `Microsoft.Extensions.*` from dotnet/extensions. If the bug is in `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`, etc., note it as `External` — the fix belongs in that repo. Don't classify as an aspnetcore `area-*` bug without confirming the code is in `src/`.

### 11. Hosting vs Framework confusion

Issues with `dotnet run`, SDK templates, or `global.json` may belong to dotnet/sdk or dotnet/runtime, not aspnetcore. Check whether the repro reproduces outside of ASP.NET Core before classifying.
