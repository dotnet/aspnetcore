# MSBuild Bootstrap Investigation

**TEMPORARY - DO NOT MERGE**

This directory contains a custom `Microsoft.Build.Tasks.Core.dll` provided by the MSBuild team
for investigating intermittent `MSB3030: Could not copy file` errors in parallel builds.

See: https://github.com/dotnet/msbuild/issues/12927

The DLL contains additional logging to help diagnose the race condition in `_CopyFilesMarkedCopyLocal`.
It is overlaid onto the SDK's copy during CI builds via `eng/build.sh`.
