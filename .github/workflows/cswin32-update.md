---
on:
  schedule: every 1mo
  workflow_dispatch:

description: >
  Monthly workflow that checks for a newer Microsoft.Windows.CsWin32 package version,
  updates it, and removes any workarounds that are no longer necessary.

permissions:
  contents: read
  pull-requests: read
  issues: read

network:
  allowed:
    - defaults
    - dotnet
    - containers

tools:
  github:
  edit:
  bash: ["curl", "grep", "sed", "jq", "git", "cat", "ls", "find", "bash", "head", "tail", "dotnet", "source", "chmod"]
  web-fetch:

safe-inputs:
  get-nuget-version:
    description: "Get the latest stable version of a NuGet package from api.nuget.org"
    inputs:
      package_id:
        type: string
        required: true
        description: "The NuGet package ID (e.g. Microsoft.Windows.CsWin32)"
    run: |
      PACKAGE_ID_LOWER=$(echo "$INPUT_PACKAGE_ID" | tr '[:upper:]' '[:lower:]')
      VERSIONS=$(curl -s "https://api.nuget.org/v3-flatcontainer/${PACKAGE_ID_LOWER}/index.json")
      LATEST=$(echo "$VERSIONS" | jq -r '.versions[]' | grep -v '-' | tail -1)
      echo "{\"package\": \"$INPUT_PACKAGE_ID\", \"latest_version\": \"$LATEST\"}"

safe-outputs:
  create-pull-request:
    title-prefix: "[package] "
    labels: [area-networking]
    draft: false
    base-branch: main
---

# Update CsWin32 Packag

Microsoft.Windows.CsWin32 is a source generator that produces P/Invoke signatures and types
for Win32 APIs used by HttpSys and IIS server implementations in ASP.NET Core.
When CsWin32 releases new versions, previously missing enum values or types may become available, allowing us to remove hardcoded workarounds in the code.

## Important: Shell Environment

Each `shell()` tool call runs in a **separate process**. Environment variables set by `source activate.sh`
do NOT persist to subsequent tool calls. You MUST chain commands using `bash -c '...'` to keep the
environment within a single invocation.

**Correct** (single bash invocation):
```bash
bash -c 'source activate.sh && dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj'
```

**Wrong** (separate calls — dotnet won't find the SDK):
```
shell(source): source activate.sh
shell(dotnet): dotnet build ...    ← ERROR: SDK not found
```

Before running dotnet commands, you must first restore dependencies. This downloads the .NET SDK
into `.dotnet/` and restores NuGet packages. Combine restore + build in one bash invocation:

```bash
bash -c 'source activate.sh && ./restore.sh && dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj'
```

The initial restore may take a few minutes. After it completes, subsequent builds reuse the cached SDK.

## Task

Perform these checks and apply changes:

1. **Version update** — check if a newer stable version of CsWin32 is available and update it.
2. **Restore and build** — restore dependencies and build projects to produce CsWin32-generated sources.
3. **Research** — check CsWin32 release notes and referenced issues to understand what changed.
4. **Workaround cleanup** — find workarounds and fix any that are now removable (verify with generated code and rebuild).

---

### Step 1: Check for a newer CsWin32 version

Use the `get-nuget-version` tool with `package_id: "Microsoft.Windows.CsWin32"` to fetch the latest stable version.

Compare with the current version in `eng/Versions.props` under `<MicrosoftWindowsCsWin32Version>`.

- If a newer version is available, update the value in `eng/Versions.props` using the `edit` tool.
- If already up to date, note that and proceed. Workaround cleanup is valuable even without a version bump.

A version bump alone (without workaround changes) is a valid PR.

---

### Step 2: Restore and build

Restore dependencies and build the affected projects. This serves two purposes:
- Validates a version bump (if done in Step 1)
- Produces CsWin32-generated source files in `obj/` that you'll inspect in Step 4

**IMPORTANT**: You MUST chain `source activate.sh` with subsequent commands using `bash -c '...'`,
because each shell tool call is a separate process and environment variables don't persist.

First, restore (this downloads the .NET SDK and restores NuGet packages):

```bash
bash -c 'source activate.sh && ./restore.sh'
```

Then build the specific affected projects:

```bash
bash -c 'source activate.sh && dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj'
bash -c 'source activate.sh && dotnet build src/Servers/IIS/IIS/src/Microsoft.AspNetCore.Server.IIS.csproj'
```

Note: These projects target Windows APIs but the CsWin32 source generator runs at build time on any OS.
If the build fails on Linux due to platform-specific runtime dependencies, that's OK — the important thing
is that the source generator runs and produces files in `obj/`. Check if the source generation step completes
even if later compilation steps have errors.

After building, inspect CsWin32's generated output:

```bash
find src/Servers/HttpSys/obj -name "*.g.cs" | head -20
find src/Servers/IIS/obj -name "*.g.cs" | head -20
```

---

### Step 3: Research what changed in CsWin32

Use `web_fetch` to check CsWin32's release notes and changelog:

```
https://github.com/microsoft/CsWin32/releases
```

Look for:
- New enum values added to Win32 types
- New types or structs supported
- New API or API overloads
- Bug fixes for inline arrays or source generation
- Issues referenced in this codebase's workaround comments

Also check the status of referenced GitHub issues. For example, if a workaround comment
references `https://github.com/microsoft/CsWin32/issues/1086`, use `web_fetch` to check
if the issue is closed/resolved.

---

### Step 4: Find and fix CsWin32 workarounds

Scan the codebase for workarounds that were introduced because CsWin32 didn't yet generate
certain types, enum values, or APIs.

**Locate CsWin32 consumer directories:**

```bash
find src/ -name "NativeMethods.txt"
grep -rl "Microsoft.Windows.CsWin32" src/ eng/
```

Then search for workaround patterns in those directories. Below are only example patterns - you can look for any opportunity to improve the code, fix workarounds and apply suggestions (aka use new APIs) introduced in newer versions of CsWin32 package.

---

#### Pattern A: Hardcoded integer casts to CsWin32 enum types

Search for casts to Win32 enum types with inline comments naming the missing member:

```bash
grep -rn "HTTP_FEATURE_ID" src/Servers/
grep -rn "HTTP_REQUEST_PROPERTY" src/Servers/
```

These look like:
```csharp
(HTTP_FEATURE_ID)11 /* HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello */
(HTTP_REQUEST_PROPERTY)14 /* HTTP_REQUEST_PROPERTY.HttpRequestPropertyTlsCipherInfo */
```

**How to fix:** The inline comment tells you exactly what the correct enum member name is.
Replace the entire cast expression `(EnumType)N /* EnumType.MemberName */` with just `EnumType.MemberName`.
Remove the now-unnecessary comment.

**When to fix:** First, check if the enum member exists in the CsWin32-generated code from Step 2:
```bash
grep -r "HttpFeatureCacheTlsClientHello" src/Servers/HttpSys/obj/
```
If found in generated code, make the replacement. If not found, leave the workaround in place.

**Example fix:**
```csharp
// BEFORE:
(HTTP_FEATURE_ID)11 /* HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello */
// AFTER:
HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello
```

---

#### Pattern B: Comments referencing CsWin32 issues or workarounds

```bash
grep -rn -i "cswin32" src/Servers/ src/Shared/
grep -rn -i "workaround.*win32\|win32.*workaround" src/Servers/ src/Shared/
```

Read the context around each match. If a comment references a specific CsWin32 GitHub issue URL:
1. Use `web_fetch` to check if the issue is closed/resolved
2. If resolved, apply the fix that the comment describes or implies
3. Remove the workaround comment

For example, if you find:
```csharp
// when CsWin32 gets support for inline arrays, remove 'AsReadOnlySpan' call below.
// https://github.com/microsoft/CsWin32/issues/1086
```
Check if issue #1086 is closed. If yes, try removing the `.AsReadOnlySpan()` workaround,
then rebuild to verify:
```bash
bash -c 'source activate.sh && dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj'
```

---

#### Pattern C: Manually-defined Win32 structs

```bash
grep -rn "StructLayout" src/Servers/ src/Shared/
grep -rn "// From.*\.h" src/Servers/ src/Shared/
```

Look for structs with `[StructLayout]` and comments like `// From Schannel.h` that are
Win32 types CsWin32 could generate instead.

**How to fix:**
1. Add the struct name to the appropriate `NativeMethods.txt` file (one entry per line, maintain alphabetical order)
2. Delete the manual struct definition file or remove the struct from its file
3. Update `using` directives in files that referenced the manual struct if the CsWin32-generated version is in a different namespace

**When to fix:** Check if CsWin32 now generates the struct by searching `obj/`:
```bash
grep -r "SecPkgContext_CipherInfo" src/Servers/HttpSys/obj/ src/Servers/IIS/obj/
```
If the struct exists in generated code, add it to `NativeMethods.txt` and remove the manual definition.
Then rebuild to verify:
```bash
bash -c 'source activate.sh && dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj'
```

---

#### Pattern D: Manual P/Invoke declarations

```bash
grep -rn "GetProcAddress" src/Servers/ src/Shared/
grep -rn "DllImport\|LibraryImport" src/Servers/ src/Shared/
```

Look for manual P/Invoke delegates or `[DllImport]`/`[LibraryImport]` declarations for APIs that
CsWin32 could generate — especially those resolved via `GetProcAddress` at runtime.

**How to fix:**
1. Add the API name to the appropriate `NativeMethods.txt`
2. Remove the manual P/Invoke declaration and its delegate type
3. Replace call sites with the CsWin32-generated `PInvoke.ApiName(...)` call

**When to fix:** Check if the API name exists in CsWin32's generated output:
```bash
grep -r "HttpQueryRequestProperty\|HttpSetRequestProperty" src/Servers/HttpSys/obj/
```
If CsWin32 generates the API, add it to `NativeMethods.txt`, remove the manual P/Invoke,
and update call sites. Rebuild to verify.

---

### Additional workaround patterns

The patterns above are examples. Also search for:
- Any comment mentioning "CsWin32", "source generator", or "Win32Metadata"
- Any TODO comments related to Windows interop
- Any hardcoded constants that comment out the Win32 symbolic name

```bash
grep -rn "TODO.*Win32\|TODO.*CsWin32\|TODO.*interop" src/Servers/ src/Shared/
```

---

## Decision: PR or No-Op

**Create a PR** (using `create-pull-request`) if ANY of these are true:
- You updated the CsWin32 version in `eng/Versions.props`
- You removed or replaced any workaround
- You added entries to any `NativeMethods.txt` file

The PR body MUST include:
- What version change was made (if any)
- A list of each workaround that was fixed, with the file and a brief description
- A list of workarounds that remain and why they couldn't be fixed yet (e.g., symbol not found in generated code)
- Build output summary confirming compilation success

**Call `noop`** ONLY if ALL of these are true:
- The CsWin32 version is already the latest stable release
- No workarounds were found that can be fixed (verified by checking `obj/` generated code and CsWin32 issue status)
- You searched for ALL patterns above and found nothing actionable

The `noop` message must list what was checked (version, patterns searched, issues checked).

## Rules

- Only update to **stable** releases — skip prerelease/beta versions.
- Use the `edit` tool to modify files. Do NOT use `git commit`, `git push`, or `git config` commands — the `create-pull-request` safe output handles that.
- Always chain `source activate.sh &&` before any `dotnet` command using `bash -c '...'`. Each shell tool call is a separate process.
- After making each workaround fix, rebuild the affected project to verify it compiles. If it fails, revert the change and note it in the PR body.
- When a workaround comment names the correct symbol (e.g., `/* HttpFeatureCacheTlsClientHello */`), use that as the replacement.
- When adding entries to `NativeMethods.txt`, add each on its own line, maintaining alphabetical order.
- If you are updating to a newer CsWin32 version, attempt ALL workaround fixes — the new version is likely to support the missing symbols.
- Prefer creating a PR with verified fixes over calling `noop`. Use the build to confirm each change.
