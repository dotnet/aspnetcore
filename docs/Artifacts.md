Artifacts
=========

Building this repo produces build artifacts in the directory structure described below. Build outputs are organized into logical groups based on artifact type and the intended usage of the artifacts.

See also https://github.com/dotnet/arcade/blob/master/Documentation/ArcadeSdk.md This repo follows _most_ of the conventions described there.

```text
artifacts/
  installers/
    $(Configuration)/
      *.msi            = Windows installers
      *.deb, *.rpm     = Linux installers
      *.zip, *.tar.gz  = archives versions of installers
  log/
    runningProcesses*.txt = Process list from just before build completed
    runningProcesses*.bak = Process list from two minutes before runningProcesses*.txt files were written
    *.binlog           = Binary logs for a few build phases e.g. site extension build
    **/
      *.log            = Log files for test runs and individual tests
    $(Configuration)/
      *.binlog         = Binary logs for most build phases
  packages/
    $(Configuration)/
      Shipping/        = Packages which are intended for use by customers. These, along with installers, represent the 'product'.
        *.nupkg        = NuGet packages which ship to nuget.org
        *.jar          = Java packages which ship to Maven Central and others
        *.tgz          = NPM packages which ship to npmjs.org
      NonShipping/
        *.nupkg        = NuGet packages for internal use only. Used to hand off bits to Microsoft partner teams. Not intended for use by customers.
  symbols/
    $(Configuration)/
      $(TargetFramework)/
        *.pdb          = Loose symbol files for symbol server publication. Special cases where *.symbols.nupkg packaging is cumbersome.
  VSSetup/
    $(Configuration)/
      *.vsix           = Visual Studio extensions. None currently exist.
```
