---
on:
  schedule: every 1mo
  workflow_dispatch:

description: >
  Monthly workflow that checks for a newer Microsoft.Windows.CsWin32 package version,
  updates it, and removes any workarounds that are no longer necessary after the update.

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
  bash: ["curl", "grep", "sed", "jq", "git", "cat", "ls", "find", "dotnet", "bash", "source", "chmod"]
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
    title-prefix: "[build-ops] "
    labels: [build-ops]
    draft: false
    base-branch: main
---

# Update CsWin32 Package Version and Remove Workarounds

Microsoft.Windows.CsWin32 is a source generator that produces P/Invoke signatures and types
for Win32 APIs used by HttpSys and IIS server implementations in ASP.NET Core.
When CsWin32 releases new versions, previously missing enum values or types may become available,
allowing us to remove hardcoded workarounds in the code.

## Task

Perform two independent checks and apply any necessary changes:

1. **Version update** — check if a newer stable version of CsWin32 is available and update it.
2. **Workaround cleanup** — check if any existing workarounds can be replaced with proper CsWin32-generated types.

---

### Step 1: Check for a newer CsWin32 version

Use the `get-nuget-version` tool with `package_id: "Microsoft.Windows.CsWin32"` to fetch the latest stable version.

Then compare it with the current version defined in `eng/Versions.props` under `<MicrosoftWindowsCsWin32Version>`.

- If a newer version is available, update the value in `eng/Versions.props`.
- If already up to date, note that and move on.

---

### Step 2: Restore and build

Regardless of whether the version was updated, you need to build the affected projects.
This is required both to validate a version bump AND to inspect CsWin32-generated source files for workaround cleanup.

**Important:** The workflow runs on Linux (`ubuntu-latest`). Use `.sh` scripts, not `.cmd`.

First, activate the local .NET SDK and restore dependencies from the repository root:

```bash
source activate.sh
./restore.sh
```

Then build the specific affected projects:

```bash
dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj
dotnet build src/Servers/IIS/IIS/src/Microsoft.AspNetCore.Server.IIS.csproj
```

Note: These projects target Windows APIs but the CsWin32 source generator runs at build time on any OS.
If the build fails on Linux due to platform-specific runtime dependencies, that's OK — the important thing
is that the source generator runs and produces files in `obj/`. You may need to pass `/p:TargetOS=windows`
or similar. Check if `dotnet build` completes the source generation step even with other errors.

If the build breaks after a version update due to CsWin32 API changes, investigate and attempt a fix
(e.g. adapting to renamed types or changed API surface).

After building, the CsWin32 source generator will have produced files in the `obj/` directory.
You can inspect them to check which enum values, types, and APIs are now available:

```bash
find src/Servers/HttpSys/obj -name "*.g.cs" | head -20
find src/Servers/IIS/obj -name "*.g.cs" | head -20
```

You will use these generated files in Step 3 to verify whether workarounds can be removed.

---

### Step 3: Search for and clean up CsWin32 workarounds

Regardless of whether the version was updated, scan the codebase for workarounds
that were introduced because CsWin32 didn't yet generate certain types, enum values, or APIs.
These workarounds may become unnecessary as CsWin32 evolves.

**Where to search:** all directories that use CsWin32 — find them by looking for `NativeMethods.txt` files
and `.csproj` files that reference `Microsoft.Windows.CsWin32`:

```bash
find src/ -name "NativeMethods.txt"
grep -rl "Microsoft.Windows.CsWin32" src/ eng/
```

Then search the source files in those directories for workaround patterns.

#### What to look for

Scan for these common workaround patterns in `.cs` files under the directories found above:

1. **Hardcoded integer casts to CsWin32 enum types** — e.g. `(SomeEnumType)11` with a comment naming
   the enum member that didn't exist yet. Search with:
   ```bash
   grep -rn "(HTTP_FEATURE_ID)" src/Servers/
   grep -rn "(HTTP_REQUEST_PROPERTY)" src/Servers/
   ```
   More generally, look for casts like `(SomeWin32Type)N` followed by a `/* ... */` comment.

2. **Comments referencing CsWin32 issues or workarounds** — search for:
   ```bash
   grep -rn "CsWin32" src/Servers/ src/Shared/
   grep -rn "cswin32" src/Servers/ src/Shared/
   grep -rn "workaround" src/Servers/ src/Shared/
   ```

3. **Manually-defined Win32 structs** — structs with `[StructLayout]` and comments like `// From SomeHeader.h`
   that could instead be generated by CsWin32. These often live in `NativeInterop/` subdirectories.

4. **Manual P/Invoke delegates or `[DllImport]`/`[LibraryImport]`** for APIs that CsWin32 could generate —
   especially any resolved via `GetProcAddress` at runtime as a compatibility shim.

#### How to check if a workaround is removable

After building (Step 2), CsWin32's source generator output lives in the `obj/` directory.
For each workaround you find, check if the missing symbol now exists:

```bash
# Example: check if an enum value is now generated
grep -r "NameOfEnumMember" src/Servers/HttpSys/obj/
# Example: check if a struct is now generated
grep -r "StructName" src/Servers/HttpSys/obj/
```

If the symbol exists in generated code, replace the workaround with the proper CsWin32-generated symbol.
If it's a struct, you may need to add it to the relevant `NativeMethods.txt` and rebuild first.

---

### How to verify workaround removability

You MUST actually attempt each workaround replacement — do not skip this step or report that verification is impossible.

For each workaround:

1. First, grep the `obj/` directory (after building in Step 2) to check if the symbol now exists in CsWin32-generated code:
   ```bash
   grep -r "SymbolName" src/Servers/HttpSys/obj/
   ```
2. If the symbol exists in generated code: make the replacement using the `edit` tool
3. Rebuild: `dotnet build src/Servers/HttpSys/src/Microsoft.AspNetCore.Server.HttpSys.csproj`
4. If it compiles successfully, keep the change
5. If it fails to compile, revert the change and leave the workaround in place — note it in the PR body as "still needed"

Do NOT skip verification by saying you lack build capability — you have `dotnet`, `bash`, and `source` available.
Make sure you ran `source activate.sh` before any `dotnet` commands.

---

## Decision: PR or No-Op

- If **any changes** were made (version bump and/or workaround removals), use the `create-pull-request` safe output to open a PR.
  - In the PR body, list all changes made and any workarounds that remain.
- If **no changes** are needed (version is current AND all workarounds are still necessary), use the `noop` safe output with a message explaining what was analyzed and why no changes are required.

## Guidelines

- Only update to **stable** releases — skip prerelease/beta versions.
- Use the `edit` tool to modify files directly. Do NOT use `git commit`, `git push`, or `git config` commands — the `create-pull-request` safe output handles committing and pushing automatically.
- When checking if generated types exist, prefer building the project or examining the generated output in `obj/` rather than guessing.
- Be conservative: if you're unsure whether a workaround can be removed, leave it in place and note it.
