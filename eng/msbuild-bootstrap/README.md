# MSBuild Bootstrap Overlay (diagnostic)

**TEMPORARY — DO NOT MERGE.**

This directory contains a custom `Microsoft.Build.Tasks.Core.dll` built from a one-line
diagnostic change to the MSBuild `Copy` task, used to investigate the intermittent
**MSB3030 "Could not copy file ... because it was not found"** failures in parallel CI
builds.

## What the custom DLL does

The `Copy` task unlinks an existing destination file before overwriting it. When parallel
project builds copy to/from a **shared output file**, that delete can briefly remove a file
another project is reading as a Copy **source**, surfacing as MSB3030. That delete is
otherwise invisible in the binary log.

The custom DLL adds a single `Low`-importance (binlog-only) log line right before the delete:

```
MSB-COPY-DELETE: deleting existing destination before overwrite: <path>
```

so the deleted path can be correlated with the source path of any MSB3030 in the same build.

## Provenance

- MSBuild source: `dotnet/msbuild` PR #14122 (branch `dev/veronikao/copy-delete-logging`).
- FileVersion: `18.9.0.32101`
- Product: `18.9.0-dev-26321-01+e775a976a210698020837be79dde8db8ddf218ef`
- SHA256: `0CBBBA602D76DD9CE0705BAEE8C180EC6C3BA2A79E6BA4B283760D82C0426E12`

The SDK in `global.json` (11.0.100-preview.6) ships MSBuild `18.9.0-preview`, so this overlay
is the same `18.9.0` line as the SDK (days of skew only).

## How it is applied

`eng/build.sh` (Linux/macOS) and `eng/build.ps1` (Windows) copy this DLL over
`$DOTNET_INSTALL_DIR/sdk/<sdk-version>/Microsoft.Build.Tasks.Core.dll` after toolset
initialization, printing the SHA256 before and after the swap.

## Related

- dotnet/msbuild#12927
- dotnet/aspnetcore#62807
