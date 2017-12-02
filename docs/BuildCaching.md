Build Caching
=============

This repository can build the entire ASP.NET Core stack from scratch. However, in most cases, we only want to build part of the stack.

## Building with cached artifacts

This is the default behavior when `build.ps1` without arguments.

This repository may contains files under `releases/*.xml`. These files follow the [Bill of Materials](./BillOfMaterials.md) format.
Any artifacts described in these bom's are assumed to have been built in a previous release and are available for download.
The build may use this information to skip building certain assets when a build action would produce a duplicate asset.

## Disabling caching

You can for the build to skip reading the bill of materials and build everything from scratch by calling `build.ps1 -NoCache`.
This will cause the build to always rebuild assets.

## When to commit a new bill of material

Each build will produce a new Bill of Materials (bom). Normally, this bom isn't added to source code right away.
This bom should only be committed to source once the assets it describes are published and available from caching mechanisms.

## Caching mechanisms

These are some common caching mechanisms used to store build artifacts.

 - NuGet servers. NuGet.org, MyGet.org, and file shares can be used to restore previously-built nupkg files
 - Debian and RPM feeds. Some .deb and .rpm installers may be cached on https://packages.microsoft.com/repos.
 - Azure blob feeds. Arbitrary build artifacts may be cached on https://aspnetcore.blob.core.windows.net/.
