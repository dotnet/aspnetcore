Package Archives
================

This repo builds multiple package archives which contain a NuGet fallback folder (also known as the fallback or offline package cache). The fallback folder is a set of NuGet packages that is bundled in the .NET Core SDK installers and available in Azure Web App service.

Package archives are available in four varieties.

* **LZMA** - `nuGetPackagesArchive-$(Version).lzma` - The LZMA is a compressed file format, similar to a .zip. On first use or on install, the .NET Core CLI will expand this LZMA file, extracting the packages inside to %DOTNET_INSTALL_DIR%/sdk/NuGetFallbackFolder. This contains all NuGet packages and their complete contents.
* **ci-server** - `nuGetPackagesArchive-ci-server-$(Version).zip` - this archive is optimized for CI server environments. It contains the same contents as the LZMA, but trimmed of xml docs (these are only required for IDEs) and .nupkg files (not required for NuGet as for the 2.0 SDK).
* **ci-server-patch** - `nuGetPackagesArchive-ci-server-$(Version).patch.zip` - this archive is the same as the ci-server archive, but each release is incremental - i.e. it does not bundle files that were present in a previous ci-server archive release. This can be used by first starting with a baseline `nuGetPackagesArchive-ci-server-$(Version).zip` and then applying the `.patch.zip` version for subsequent updates.
* **ci-server-compat-patch** - `nuGetPackagesArchive-ci-server-compat-$(Version).patch.zip` - similar to the ci-server-patch archive, but this includes .nupkg files to satisfy CI environments that may have older NuGet clients.

These archives are built using the projects in [src/PackageArchive/Archive.\*/](/src/PackageArchive/).

## Using a fallback folder

NuGet restore takes a list of fallback folders in the MSBuild property `RestoreAdditionalProjectFallbackFolders`. Unlike a folder restore source, restore will not copy the packages from a fallback folder into the global NuGet cache.

By default, the .NET Core SDK adds `$(DotNetInstallRoot)/sdk/NuGetFallbackFolder/` to this list. The .NET Core CLI expands its bundled `nuGetPackagesArchive.lzma` file into this location on first use or when the installers run. (See [Microsoft.NET.NuGetOfflineCache.targets](https://github.com/dotnet/sdk/blob/v2.1.300/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.NuGetOfflineCache.targets)).

## Scenarios

The following scenarios are used to determine which packages go into the fallback package cache.
These requirements are formalized as project files in [src/PackageArchive/Scenario.\*/](/src/PackageArchive/).

 - A user should be able to restore the following templates and only use packages from the offline cache:
    - `dotnet new console`
    - `dotnet new library`
    - `dotnet new web`
    - `dotnet new razor`
    - `dotnet new mvc`

The following packages are NOT included in the offline cache.
  - Packages required for standalone publishing, aka projects that set a Runtime Identifier during restore
  - Packages required for F# and VB templates
  - Packages required for Visual Studio code generation in ASP.NET Core projects
  - Packages required to restore .NET Framework projects
  - Packages required to restore test projects, such as xunit, MSTest, NUnit

The result of this typically means including the transitive graph of the following packages:

  - Packages that match bundled runtimes
    - Microsoft.NETCore.App
    - Microsoft.AspNetCore.App
  - Packages that Microsoft.NET.Sdk adds implicitly
    - Microsoft.NETCore.App
    - NETStandard.Library

### Example

Given the following parameters:
 - LatestNETCoreAppTFM = netcoreapp2.1
 - DefaultRuntimeVersion = 2.1
 - BundledRuntimeVersion = 2.1.8
 - BundledAspNetRuntimeVersion = 2.1.7
 - LatestNETStandardLibraryTFM = netstandard2.0
 - BundledNETStandardLibraryVersion = 2.0.1

The LZMA should contain
  - Microsoft.NETCore.App/2.1.0 + netcoreapp2.1 dependencies (Microsoft.NET.Sdk will implicitly reference "2.1", which NuGet to 2.1.0)
  - Microsoft.NETCore.App/2.1.8 + netcoreapp2.1 dependencies (Matches the runtime in shared/Microsoft.NETCore.App/2.1.8/)
  - NETStandard.Library/2.0.1 + netstandard2.0 dependencies (Microsoft.NET.Sdk will implicitly reference "2.0.1")
