# Helix Work Item Artifacts

Guide to finding and analyzing artifacts from Helix test runs.

## Accessing Artifacts

### Via the Script

Query a specific work item to see its artifacts:

```powershell
./scripts/Get-CIStatus.ps1 -HelixJob "4b24b2c2-..." -WorkItem "Microsoft.NET.Sdk.Tests.dll.1" -ShowLogs
```

### Via API

```bash
# Get work item details including Files array
curl -s "https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{workItemName}"
```

The `Files` array contains artifacts with `FileName` and `Uri` properties.

## Artifact Availability Varies

**Not all test types produce the same artifacts.** What you see depends on the repo, test type, and configuration:

- **Build/publish tests** (SDK, WASM) → Multiple binlogs
- **AOT compilation tests** (iOS/Android) → `AOTBuild.binlog` plus device logs
- **Standard unit tests** → Console logs only, no binlogs
- **Crash failures** (exit code 134) → Core dumps may be present

Always query the specific work item to see what's available rather than assuming a fixed structure.

## Common Artifact Patterns

| File Pattern | Purpose | When Useful |
|--------------|---------|-------------|
| `*.binlog` | MSBuild binary logs | AOT/build failures, MSB4018 errors |
| `console.*.log` | Console output | Always available, general output |
| `run-*.log` | XHarness execution logs | Mobile test failures |
| `device-*.log` | Device-specific logs | iOS/Android device issues |
| `dotnetTestLog.*.log` | dotnet test output | Test framework issues |
| `vstest.*.log` | VSTest output | aspnetcore/SDK test issues |
| `core.*`, `*.dmp` | Core dumps | Crashes, hangs |
| `testResults.xml` | Test results | Detailed pass/fail info |

Artifacts may be at the root level or nested in subdirectories like `xharness-output/logs/`.

> **Note:** The Helix work item Details API has a known bug ([dotnet/dnceng#6072](https://github.com/dotnet/dnceng/issues/6072)) where
> file URIs for subdirectory files are incorrect, and unicode characters in filenames are rejected.
> The script works around this by using the separate `ListFiles` endpoint (`GET .../workitems/{workItemName}/files`)
> which returns direct blob storage URIs that work for all filenames regardless of subdirectories or unicode.

## Binlog Files

Binlogs are **only present for tests that invoke MSBuild** (build/publish tests, AOT compilation). Standard unit tests don't produce binlogs.

### Common Names

| File | Description |
|------|-------------|
| `build.msbuild.binlog` | Build phase |
| `publish.msbuild.binlog` | Publish phase |
| `AOTBuild.binlog` | AOT compilation |
| `msbuild.binlog` | General MSBuild operations |
| `msbuild0.binlog`, `msbuild1.binlog` | Per-test-run logs (numbered) |

### Analyzing Binlogs

**Online viewer (no download):**
1. Copy the binlog URI from the script output
2. Go to https://live.msbuildlog.com/
3. Paste the URL to load and analyze

**Download and view locally:**
```bash
curl -o build.binlog "https://helix.dot.net/api/jobs/{jobId}/workitems/{workItem}/files/build.msbuild.binlog?api-version=2019-06-17"
# Open with MSBuild Structured Log Viewer
```

**AI-assisted analysis:**
Use the MSBuild MCP server to analyze binlogs for errors and warnings.

## Core Dumps

Core dumps appear when tests crash (typically exit code 134 on Linux/macOS):

```
core.1000.34   # Format: core.{uid}.{pid}
```

## Mobile Test Artifacts (iOS/Android)

Mobile device tests typically include XHarness orchestration logs:

- `run-ios-device.log` / `run-android.log` - Execution log
- `device-{machine}-*.log` - Device output
- `list-ios-device-*.log` - Device discovery
- `AOTBuild.binlog` - AOT compilation (when applicable)
- `*.crash` - iOS crash reports

## Finding the Right Work Item

1. Run the script with `-ShowLogs` to see Helix job/work item info
2. Look for lines like:
   ```
   Helix Job: 4b24b2c2-ad5a-4c46-8a84-844be03b1d51
   Work Item: Microsoft.NET.Sdk.Tests.dll.1
   ```
3. Query that specific work item for full artifact list

## AzDO Build Artifacts (Pre-Helix)

Helix work items contain artifacts from **test execution**. But there's another source of binlogs: **AzDO build artifacts** from the build phase before tests are sent to Helix.

### When to Use Build Artifacts

- Failed work item has no binlogs (unit tests don't produce them)
- You need to see how tests were **built**, not how they **executed**
- Investigating build/restore issues that happen before Helix

### Listing Build Artifacts

```powershell
# List all artifacts for a build
$org = "dnceng-public"
$project = "public"
$buildId = 1280125

$url = "https://dev.azure.com/$org/$project/_apis/build/builds/$buildId/artifacts?api-version=5.0"
$artifacts = (Invoke-RestMethod -Uri $url).value

# Show artifacts with sizes
$artifacts | ForEach-Object {
    $sizeMB = [math]::Round($_.resource.properties.artifactsize / 1MB, 2)
    Write-Host "$($_.name) - $sizeMB MB"
}
```

### Common Build Artifacts

| Artifact Pattern | Contents | Size |
|------------------|----------|------|
| `TestBuild_*` | Test build outputs + binlogs | 30-100 MB |
| `BuildConfiguration` | Build config metadata | <1 MB |
| `TemplateEngine_*` | Template engine outputs | ~40 MB |
| `AoT_*` | AOT compilation outputs | ~3 MB |
| `FullFramework_*` | .NET Framework test outputs | ~40 MB |

### Downloading and Finding Binlogs

```powershell
# Download a specific artifact
$artifactName = "TestBuild_linux_x64"
$downloadUrl = "https://dev.azure.com/$org/$project/_apis/build/builds/$buildId/artifacts?artifactName=$artifactName&api-version=5.0&`$format=zip"
$zipPath = "$env:TEMP\$artifactName.zip"
$extractPath = "$env:TEMP\$artifactName"

Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

# Find binlogs
Get-ChildItem -Path $extractPath -Filter "*.binlog" -Recurse | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "$($_.Name) ($sizeMB MB) - $($_.FullName)"
}
```

### Typical Binlogs in Build Artifacts

| File | Description |
|------|-------------|
| `log/Release/Build.binlog` | Main build log |
| `log/Release/TestBuildTests.binlog` | Test build verification |
| `log/Release/ToolsetRestore.binlog` | Toolset restore |

### Build vs Helix Binlogs

| Source | When Generated | What It Shows |
|--------|----------------|---------------|
| AzDO build artifacts | During CI build phase | How tests were compiled/packaged |
| Helix work item artifacts | During test execution | What happened when tests ran `dotnet build` etc. |

If a test runs `dotnet build` internally (like SDK end-to-end tests), both sources may have relevant binlogs.

## Downloaded Artifact Layout

When you download artifacts via MCP tools or manually, the directory structure can be confusing. Here's what to expect.

### Helix Work Item Downloads

MCP tools for downloading Helix artifacts:
- **`hlx_download`** — downloads multiple files from a work item. Returns local file paths.
- **`hlx_download_url`** — downloads a single file by direct URI (from `hlx_files` output). Use when you know exactly which file you need.

> 💡 **Prefer remote investigation first**: search file contents, parse test results, and search logs remotely before downloading. Only download when you need to load binlogs or do offline analysis.

`hlx_download` saves files to a temp directory. The structure is **flat** — all files from the work item land in one directory:

```
C:\...\Temp\helix-{hash}\
├── console.d991a56d.log          # Console output
├── testResults.xml               # Test pass/fail details
├── msbuild.binlog                # Only if test invoked MSBuild
├── publish.msbuild.binlog        # Only if test did a publish
├── msbuild0.binlog               # Numbered: first test's build
├── msbuild1.binlog               # Numbered: second test's build
└── core.1000.34                  # Only on crash
```

**Key confusion point:** Numbered binlogs (`msbuild0.binlog`, `msbuild1.binlog`) correspond to individual test cases within the work item, not to build phases. A work item like `Microsoft.NET.Build.Tests.dll.18` runs dozens of tests, each invoking MSBuild separately. To map a binlog to a specific test:
1. Load it with the binlog analysis tools
2. Check the project paths inside — they usually contain the test name
3. Or check `testResults.xml` to correlate test execution order with binlog numbering

### AzDO Build Artifact Downloads

AzDO artifacts download as **ZIP files** with nested directory structures:

```
$env:TEMP\TestBuild_linux_x64\
└── TestBuild_linux_x64\          # Artifact name repeated as subfolder
    └── log\Release\
        ├── Build.binlog           # Main build
        ├── TestBuildTests.binlog   # Test build verification
        ├── ToolsetRestore.binlog   # Toolset restore
        └── SendToHelix.binlog     # Contains Helix job GUIDs
```

**Key confusion point:** The artifact name appears twice in the path (extract folder + subfolder inside the ZIP). Use the full nested path when loading binlogs.

### Mapping Binlogs to Failures

This table shows the **typical** source for each binlog type. The boundaries aren't absolute — some repos run tests on the build agent (producing test binlogs in AzDO artifacts), and Helix work items for SDK/Blazor tests invoke `dotnet build` internally (producing build binlogs as Helix artifacts).

| You want to investigate... | Look here first | But also check... |
|---------------------------|-----------------|-------------------|
| Why a test's internal `dotnet build` failed | Helix work item (`msbuild{N}.binlog`) | AzDO artifact if tests ran on agent |
| Why the CI build itself failed to compile | AzDO build artifact (`Build.binlog`) | — |
| Which Helix jobs were dispatched | AzDO build artifact (`SendToHelix.binlog`) | — |
| AOT compilation failure | Helix work item (`AOTBuild.binlog`) | — |
| Test build/publish behavior | Helix work item (`publish.msbuild.binlog`) | AzDO artifact (`TestBuildTests.binlog`) |

> **Rule of thumb:** If the failing job name contains "Helix" or "Send to Helix", the test binlogs are in Helix. If the job runs tests directly (common in dotnet/sdk), check AzDO artifacts.

### Tracking Downloaded Artifacts with SQL

When downloading from multiple work items (e.g., binlog comparison between passing and failing builds), use SQL to avoid losing track of what's where:

```sql
CREATE TABLE IF NOT EXISTS downloaded_artifacts (
  local_path TEXT PRIMARY KEY,
  helix_job TEXT,
  work_item TEXT,
  build_id INT,
  artifact_source TEXT,  -- 'helix' or 'azdo'
  file_type TEXT,        -- 'binlog', 'testResults', 'console', 'crash'
  notes TEXT             -- e.g., 'passing baseline', 'failing PR build'
);
```

Key queries:
```sql
-- Find the pair of binlogs for comparison
SELECT local_path, notes FROM downloaded_artifacts
WHERE file_type = 'binlog' ORDER BY notes;

-- What have I downloaded from a specific work item?
SELECT local_path, file_type FROM downloaded_artifacts
WHERE work_item = 'Microsoft.NET.Build.Tests.dll.18';
```

Use this whenever you're juggling artifacts from 2+ Helix jobs (especially during the binlog comparison pattern in [binlog-comparison.md](binlog-comparison.md)).

### Tips

- **Multiple binlogs ≠ multiple builds.** A single work item can produce several binlogs if the test suite runs multiple `dotnet build`/`dotnet publish` commands.
- **Helix and AzDO binlogs can overlap.** Helix binlogs are *usually* from test execution and AzDO binlogs from the build phase, but SDK/Blazor tests invoke MSBuild inside Helix (producing build-like binlogs), and some repos run tests directly on the build agent (producing test binlogs in AzDO). Check both sources if you can't find what you need.
- **Not all work items have binlogs.** Standard unit tests only produce `testResults.xml` and console logs.
- **Use `hlx_download` with `pattern:"*.binlog"`** to filter downloads and avoid pulling large console logs.

## Artifact Retention

Helix artifacts are retained for a limited time (typically 30 days). Download important artifacts promptly if needed for long-term analysis.
