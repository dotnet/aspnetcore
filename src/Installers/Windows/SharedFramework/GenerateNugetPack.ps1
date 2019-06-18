# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$MsiPath,
    [Parameter(Mandatory=$true)][string]$CabPath,
    [Parameter(Mandatory=$true)][string]$NuspecFile,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$MajorVersion,
    [Parameter(Mandatory=$true)][string]$MinorVersion

)

$NuGetExe = "nuget.exe"

& $NuGetExe pack $NuspecFile -OutputDirectory $OutputDirectory -NoDefaultExcludes -NoPackageAnalysis -Properties ASPNETCORE_RUNTIME_MSI=$MsiPath`;ASPNETCORE_CAB_FILE=$CabPath`;ARCH=$Architecture`;MAJOR_VERSION=$MajorVersion`;MINOR_VERSION=$MinorVersion
Exit $LastExitCode