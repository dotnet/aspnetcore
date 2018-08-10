ZipManifestGenerator
---------

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
$url = "https://dotnetfeed.blob.core.windows.net/orchestrated-release-2-1/${ProdconBuild}/final/assets/aspnetcore/Runtime/${Version}/nuGetPackagesArchive-ci-server-${Version}.patch.zip"

dotnet run $url "../Archive.CiServer.Patch/ArchiveBaseline.${Version}.txt"
```
