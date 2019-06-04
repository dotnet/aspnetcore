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
./GenerateBaselinesForLastPatch.ps1
```

This script is going to read the build manifests from https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release and
invoke the manifest generator for the last patch.
