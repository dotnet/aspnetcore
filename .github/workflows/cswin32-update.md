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
  bash: ["curl", "grep", "sed", "jq", "git", "cat", "ls", "find"]
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

### Step 2: Build check after version update

If the version was updated in Step 1, run the build for the affected projects to make sure nothing is broken.
The affected project directories are:

- `src/Servers/HttpSys/`
- `src/Servers/IIS/IIS/`
- `src/Shared/test/Shared.Tests/`

Use the build script at the repo root: `./eng/build.cmd` or run `dotnet build` on the individual `.csproj` files.
In order to build specific projects, navigate to them (e.g. `src/Servers/HttpSys`) and run local `build.cmd`.
If the build breaks, investigate and attempt a fix (e.g. adapting to renamed types or changed API surface).

---

### Step 3: Check for removable workarounds

Regardless of whether the version was updated, check the following known workaround locations.
For each one, determine whether the CsWin32-generated code **now includes** the required enum value, type, or API — and if so, replace the workaround with the proper generated symbol.

Below are examples of workarounds which you may encounter:

#### 3a. Hardcoded `HTTP_FEATURE_ID` casts in `src/Servers/HttpSys/src/NativeInterop/HttpApi.cs`

Look for lines like:

```csharp
(HTTP_FEATURE_ID)11 /* HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello */
(HTTP_FEATURE_ID)15 /* HTTP_FEATURE_ID.HttpFeatureQueryCipherInfo */
```

If the enum `HTTP_FEATURE_ID` now defines `HttpFeatureCacheTlsClientHello` and/or `HttpFeatureQueryCipherInfo`,
replace the integer cast with the proper enum member, e.g.:

```csharp
HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello
HTTP_FEATURE_ID.HttpFeatureQueryCipherInfo
```

To check: look at the generated source or build the project and see if the enum member exists.
A quick way is to search the obj directory after a build, or try replacing and see if it compiles.

#### 3b. Inline array workaround in `src/Shared/HttpSys/NativeInterop/SocketAddress.cs`

Look for the comment referencing https://github.com/microsoft/CsWin32/issues/1086:

```csharp
// when CsWin32 gets support for inline arrays, remove 'AsReadOnlySpan' call below.
return new IPAddress(_sockaddr.sin6_addr.u.Byte.AsReadOnlySpan());
```

If CsWin32 now generates inline array support for `in6_addr` (i.e., you can do
`_sockaddr.sin6_addr.u.Byte` as a span directly without `.AsReadOnlySpan()`),
remove the workaround and the comment.

#### 3c. Manually-defined struct `SecPkgContext_CipherInfo` in `src/Shared/HttpSys/NativeInterop/SecPkgContext_CipherInfo.cs`

This struct from `Schannel.h` is defined manually. Check if CsWin32 can now generate it.
If so:
- Add the appropriate entry to `src/Servers/HttpSys/src/NativeMethods.txt`
- Remove the manual struct definition
- Update any usages to use the CsWin32-generated namespace

#### 3d. Manual P/Invoke delegates in `src/Servers/HttpSys/src/NativeInterop/HttpApi.cs`

The delegates `HttpGetRequestPropertyInvoker` and `HttpSetRequestPropertyInvoker` manually wrap
`HttpQueryRequestProperty` and `HttpSetRequestProperty` resolved via `GetProcAddress`.
Check if CsWin32 can now generate these function signatures directly.
If so:
- Add entries to `NativeMethods.txt` (e.g. `HttpQueryRequestProperty`, `HttpSetRequestProperty`)
- Replace the manual delegates with the generated P/Invoke methods
- This is a bigger change — only do it if you are confident the generated signatures match

---

### How to verify workaround removability

For each workaround:

1. Try making the replacement (use proper enum value, remove `.AsReadOnlySpan()`, etc.)
2. Build the affected project
3. If it compiles successfully, keep the change. You can additionally run tests to ensure no breaking change.
4. If it fails to compile, revert and leave the workaround in place — note it in the PR body as "still needed"

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
