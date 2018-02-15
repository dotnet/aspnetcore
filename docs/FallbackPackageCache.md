Fallback Package Cache (LZMA)
=============================

The fallback package cache (commonly called the LZMA) is a set of NuGet packages that are bundled in the .NET Core SDK installers.
The LZMA is a compressed file format, similar to a .zip.
On first use or on install, the .NET Core CLI will expand this LZMA file, extracting the packages inside to %DOTNET_INSTALL_DIR%/sdk/NuGetFallbackFolder.

## Requirements

The following requirements are used to determine which packages go into the fallback package cache:

 - A user should be able to restore the following templates and only use packages from the offline cache:
    - `dotnet new console`
    - `dotnet new library`
    - `dotnet new web`
    - `dotnet new razor`
    - `dotnet new mvc`

The following packages are NOT included in the offline cache.
  - Packages required for standalone publishing, aka projects that set a Runtime Identifier during restore
  - Packages required for F# and VB templates
  - Packages required for  Visual Studio code generation in ASP.NET Core projects
  - Packages required to restore .NET Framework projects
  - Packages required to restore test projects, such as xunit, MSTest, NUnit

The result of this typically means including the transitive graph of the following packages:

  - Packages that match bundled runtimes
    - Microsoft.NETCore.App
    - Microsoft.AspNetCore.App
    - Microsoft.AspNetCore.All
  - Packages that Microsoft.NET.Sdk adds implicitly
    - Microsoft.NETCore.App
    - NETStandard.Library
  - Packages that are a PackageReference/DotNetCliToolReference in basic ASP.NET Core templates. In addition to packages above, this typically includes:
    - Microsoft.EntityFrameworkCore.Tools{.DotNet}
    - Microsoft.VisualStudio.Web.CodeGeneration.Design
    - Microsoft.VisualStudio.Web.BrowserLink

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
  - Microsoft.AspNetCore.All/2.1.7 + netcoreapp2.1 dependencies (Matches the runtime in shared/Microsoft.AspNetCore.All/2.1.7/)
  - NETStandard.Library/2.0.1 + netstandard2.0 dependencies (Microsoft.NET.Sdk will implicitly reference "2.0.1")
