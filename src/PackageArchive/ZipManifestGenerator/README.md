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

## Servicing updates

For convenience, this folder contains [./UpdateBaselines.ps1](./UpdateBaselines.ps1) to update
the baselines from the last patch release.

Using version.props to figure out the last version, this script reads the build manifests from https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release and
invokes the manifest generator
