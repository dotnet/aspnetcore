# Deep Investigation: Binlog Comparison

When a test **passes on the target branch but fails on a PR**, comparing MSBuild binlogs from both runs reveals the exact difference in task parameters without guessing.

## When to Use This Pattern

- Test assertion compares "expected vs actual" build outputs (e.g., CSC args, reference lists)
- A build succeeds on one branch but fails on another with different MSBuild behavior
- You need to find which MSBuild property/item change caused a specific task to behave differently

## The Pattern: Delegate to Subagents

> ⚠️ **Do NOT download, load, and parse binlogs in the main conversation context.** This burns 10+ turns on mechanical work. Delegate to subagents instead.

### Step 1: Identify the two work items to compare

Use `Get-CIStatus.ps1` to find the failing Helix job + work item, then find a corresponding passing build (recent PR merged to the target branch, or a CI run on that branch).

**Finding Helix job IDs from build artifacts (binlogs to find binlogs):**
When the failing work item's Helix job ID isn't visible (e.g., canceled jobs, or finding a matching job from a passing build), the IDs are inside the build's `SendToHelix.binlog`:

1. Download the build artifact with `az`:
   ```
   az pipelines runs artifact list --run-id $buildId --org "https://dev.azure.com/dnceng-public" -p public --query "[].name" -o tsv
   az pipelines runs artifact download --run-id $buildId --artifact-name "TestBuild_linux_x64" --path "$env:TEMP\artifact" --org "https://dev.azure.com/dnceng-public" -p public
   ```
2. Load the `SendToHelix.binlog` and search for `Sent Helix Job` to find the GUIDs.
3. Query each Helix job GUID with the CI script:
   ```
   ./scripts/Get-CIStatus.ps1 -HelixJob "{GUID}" -FindBinlogs
   ```

**For Helix work item binlogs (the common case):**
The CI script shows binlog URLs directly when you query a specific work item:
```
./scripts/Get-CIStatus.ps1 -HelixJob "{JOB_ID}" -WorkItem "{WORK_ITEM}"
# Output includes: 🔬 msbuild.binlog: https://helix...blob.core.windows.net/...
```

### Step 2: Dispatch parallel subagents for extraction

Launch two `task` subagents (can run in parallel), each with a prompt like:

```
Download the msbuild.binlog from Helix job {JOB_ID} work item {WORK_ITEM}.
Use the CI skill script to get the artifact URL:
  ./scripts/Get-CIStatus.ps1 -HelixJob "{JOB_ID}" -WorkItem "{WORK_ITEM}"
Download the binlog, load it, find the {TASK_NAME} task, and extract CommandLineArguments.
Normalize paths (see table below) and sort args.
Parse into individual args using regex: (?:"[^"]+"|/[^\s]+|[^\s]+)
Report the total arg count prominently.
```

**Important:** When diffing, look for **extra or missing args** (different count), not value differences in existing args. A Debug/Release difference in `/define:` is expected noise — an extra `/analyzerconfig:` or `/reference:` arg is the real signal.

### Step 3: Diff the results

With two normalized arg lists, `Compare-Object` instantly reveals the difference.

## Common Binlog Search Patterns

When investigating binlogs, these search query patterns are most useful:

- Search for a property: `analysislevel`
- Search within a target: `under($target AddGlobalAnalyzerConfigForPackage_MicrosoftCodeAnalysisNetAnalyzers)`
- Find all properties matching a pattern: `GlobalAnalyzerConfig`

## Path Normalization

Helix work items run on different machines with different paths. Normalize before comparing:

| Pattern | Replacement | Example |
|---------|-------------|---------|
| `/datadisks/disk1/work/[A-F0-9]{8}` | `{W}` | Helix work directory (Linux) |
| `C:\h\w\[A-F0-9]{8}` | `{W}` | Helix work directory (Windows) |
| `Program-[a-f0-9]{64}` | `Program-{H}` | Runfile content hash |
| `dotnetSdkTests\.[a-zA-Z0-9]+` | `dotnetSdkTests.{T}` | Temp test directory |

### After normalizing paths, focus on structural differences

> ⚠️ **Ignore value-only differences in existing args** (e.g., Debug vs Release in `/define:`, different hash paths). These are expected configuration differences. Focus on **extra or missing args** — a different arg count indicates a real build behavior change.

## Example: CscArguments Investigation

A merge PR (release/10.0.3xx → main) had 208 CSC args vs 207 on main. The diff:

```
FAIL-ONLY: /analyzerconfig:{W}/p/d/sdk/11.0.100-ci/Sdks/Microsoft.NET.Sdk/analyzers/build/config/analysislevel_11_default.globalconfig
```

### What the binlog properties showed

Both builds had identical property resolution:
- `EffectiveAnalysisLevel = 11.0`
- `_GlobalAnalyzerConfigFileName = analysislevel_11_default.globalconfig`
- `_GlobalAnalyzerConfigFile = .../config/analysislevel_11_default.globalconfig`

### The actual root cause

The `AddGlobalAnalyzerConfigForPackage` target has an `Exists()` condition:
```xml
<ItemGroup Condition="Exists('$(_GlobalAnalyzerConfigFile_...)')">
  <EditorConfigFiles Include="$(_GlobalAnalyzerConfigFile_...)" />
</ItemGroup>
```

The merge's SDK layout **shipped** `analysislevel_11_default.globalconfig` on disk (from a newer roslyn-analyzers that flowed from 10.0.3xx), while main's SDK didn't have that file yet. Same property values, different files on disk = different build behavior.

### Lesson learned

Same MSBuild property resolution + different files on disk = different build behavior. Always check what's actually in the SDK layout, not just what the targets compute.

## Anti-Patterns

> ❌ **Don't manually split/parse CSC command lines in the main conversation.** CSC args have quoted paths, spaces, and complex structure. Regex parsing in PowerShell is fragile and burns turns on trial-and-error. Use a subagent.

> ❌ **Don't assume the MSBuild property diff explains the behavior diff.** Two branches can compute identical property values but produce different outputs because of different files on disk, different NuGet packages, or different task assemblies. Compare the actual task invocation.

> ❌ **Don't load large binlogs and browse them interactively in main context.** Use targeted searches rather than browsing interactively. Get in, get the data, get out.
