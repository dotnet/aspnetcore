# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$Name,
    [Parameter(Mandatory=$true)][string]$MsiPath,
    [Parameter(Mandatory=$false)][string]$CabPath,
    [Parameter(Mandatory=$true)][string]$NuspecFile,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$PackageVersion,
    [Parameter(Mandatory=$true)][string]$RepoRoot,
    [Parameter(Mandatory=$true)][string]$MajorVersion,
    [Parameter(Mandatory=$true)][string]$MinorVersion,
    [Parameter(Mandatory=$true)][string]$PackageIcon,
    [Parameter(Mandatory=$true)][string]$PackageIconFullPath,
    [Parameter(Mandatory=$true)][string]$PackageLicenseExpression
)

$NuGetDir = Join-Path $RepoRoot "artifacts\Tools\nuget\$Name\$Architecture"
$NuGetExe = Join-Path $NuGetDir "nuget.exe"

if (-not (Test-Path $NuGetDir)) {
    New-Item -ItemType Directory -Force -Path $NuGetDir | Out-Null
}

if (-not (Test-Path $NuGetExe)) {
    # Using 5.3.0 to workaround https://github.com/NuGet/Home/issues/5016
    Write-Output "Downloading nuget.exe to $NuGetExe"
    wget https://dist.nuget.org/win-x86-commandline/v5.3.0/nuget.exe -OutFile $NuGetExe
}

& $NuGetExe pack $NuspecFile `
    -Version $PackageVersion `
    -OutputDirectory $OutputDirectory `
    -NoDefaultExcludes `
    -NoPackageAnalysis `
    -Properties ASPNETCORE_RUNTIME_MSI=$MsiPath`;ASPNETCORE_CAB_FILE=$CabPath`;ARCH=$Architecture`;MAJOR=$MajorVersion`;MINOR=$MinorVersion`;PackageIcon=$PackageIcon`;PackageIconFullPath=$PackageIconFullPath`;PackageLicenseExpression=$PackageLicenseExpression`;
Exit $LastExitCode
