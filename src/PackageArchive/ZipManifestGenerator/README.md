ZipManifestGenerator
--------------------

This console app is used to generate the list of files in a zip archive.

Usage:
```
Usage: <ZIP> <OUTPUT>

<ZIP>      A file path or URL to the ZIP file.
<OUTPUT>   The output file path for the ZIP manifest file.

Example: dotnet run ./archive.zip files.txt
```

## Example for servicing updates

To generate a new manifest for the incremental CI server package caches, you would run

```ps1
$ProdConBuild='20180919-01'
$Version='2.1.5'

$patchUrl = "https://dotnetfeed.blob.core.windows.net/orchestrated-release-2-1/${ProdconBuild}/final/assets/aspnetcore/Runtime/${Version}/nuGetPackagesArchive-ci-server-${Version}.patch.zip"

dotnet run $patchUrl "../Archive.CiServer.Patch/ArchiveBaseline.${Version}.txt"

$compatPatchUrl = "https://dotnetfeed.blob.core.windows.net/orchestrated-release-2-1/${ProdconBuild}/final/assets/aspnetcore/Runtime/${Version}/nuGetPackagesArchive-ci-server-compat-${Version}.patch.zip"

dotnet run $compatPatchUrl "../Archive.CiServer.Patch.Compat/ArchiveBaseline.${Version}.txt"
```

For convenience, this folder contains [./UpdateBaselines.ps1](./UpdateBaselines.ps1) to run these steps.
