# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

param(
    [Parameter(Mandatory=$true)][string]$Name,
    [Parameter(Mandatory=$true)][string]$MsiPath,
    [Parameter(Mandatory=$true)][string]$NuspecFile,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$Architecture,
    [Parameter(Mandatory=$true)][string]$PackageVersion,
    [Parameter(Mandatory=$true)][string]$RepoRoot,
    [Parameter(Mandatory=$true)][string]$MajorVersion,
    [Parameter(Mandatory=$true)][string]$MinorVersion,
    [Parameter(Mandatory=$true)][string]$PackageIcon,
    [Parameter(Mandatory=$true)][string]$PackageIconFullPath,
    [Parameter(Mandatory=$true)][string]$PackageLicenseExpression,
    [Parameter(Mandatory=$true)][string]$DotNetPath
)

$NuspecProperties = "ASPNETCORE_RUNTIME_MSI=$MsiPath;ARCH=$Architecture;MAJOR=$MajorVersion;MINOR=$MinorVersion;PackageIcon=$PackageIcon;PackageIconFullPath=$PackageIconFullPath;PackageLicenseExpression=$PackageLicenseExpression"

& "$DotNetPath" pack "$NuspecFile" `
    --property "$NuspecProperties" `
    --version $PackageVersion `
    -o "$OutputDirectory"

Exit $LastExitCode
