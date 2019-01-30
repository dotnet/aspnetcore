Artifacts
=========

Building this repo produces build artifacts in the directory structure described below. Build outputs are organized into logical groups based on artifact type and the intended usage of the artifacts.

```
artifacts/
  installers/
    $(Configuration)/
        *.msi            = Windows installers
        *.deb, *.rpm     = Linux installers
        *.zip, *.tar.gz  = archives versions of installers
  packages/
    $(Configuration)/
        Shipping/        = Packages which are intended for use by customers. These, along with installers, represent the 'product'.
            *.nupkg      = NuGet packages which ship to nuget.org
            *.jar        = Java packages which ship to Maven Central and others
            *.tgz        = NPM packages which ship to npmjs.org
        NonShipping/
            *.nupkg      = NuGet packages for internal use only. Used to hand off bits to Microsoft partner teams. Not intended for use by customers.
  VSSetup/
    $(Configuration)/
        *.vsix           = Visual Studio extensions
```
