# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactsPath,

    [Parameter(Mandatory = $true)]
    [string]$PackageManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [int]$ExpectedShippingPackageCount
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-PackageMetadata([string]$PackagePath)
{
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)

    try
    {
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) })
        if ($nuspecEntries.Count -ne 1)
        {
            throw "Expected exactly one .nuspec file in '$PackagePath', but found $($nuspecEntries.Count)."
        }

        $stream = $nuspecEntries[0].Open()
        $reader = New-Object System.IO.StreamReader($stream)

        try
        {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally
        {
            $reader.Dispose()
            $stream.Dispose()
        }

        $idNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
        $versionNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")

        if ($null -eq $idNode -or [string]::IsNullOrWhiteSpace($idNode.InnerText))
        {
            throw "Package '$PackagePath' does not contain a package ID."
        }

        if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText))
        {
            throw "Package '$PackagePath' does not contain a package version."
        }

        return @{
            Id = $idNode.InnerText
            Version = $versionNode.InnerText
        }
    }
    finally
    {
        $archive.Dispose()
    }
}

[xml]$packageManifest = Get-Content -LiteralPath $PackageManifestPath -Raw
$packageCategories = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($packageArtifact in $packageManifest.Project.ItemGroup.PackageArtifact)
{
    $packageId = [string]$packageArtifact.Include
    $category = [string]$packageArtifact.Category

    if ([string]::IsNullOrWhiteSpace($packageId) -or [string]::IsNullOrWhiteSpace($category))
    {
        throw "Every PackageArtifact in '$PackageManifestPath' must have an Include and Category value."
    }

    if ($packageCategories.ContainsKey($packageId))
    {
        throw "Package '$packageId' is declared more than once in '$PackageManifestPath'."
    }

    $packageCategories.Add($packageId, $category)
}

if (Test-Path -LiteralPath $OutputPath)
{
    Remove-Item -LiteralPath $OutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputPath | Out-Null

$regularPackages = @(Get-ChildItem -LiteralPath $ArtifactsPath -Filter '*.nupkg' -File |
    Where-Object { -not $_.Name.EndsWith('.symbols.nupkg', [System.StringComparison]::OrdinalIgnoreCase) })

if ($regularPackages.Count -eq 0)
{
    throw "No regular NuGet packages were found in '$ArtifactsPath'."
}

$seenPackageIds = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
$shippingPackageCount = 0

foreach ($package in $regularPackages)
{
    $metadata = Get-PackageMetadata $package.FullName
    $packageId = $metadata.Id

    if ($seenPackageIds.ContainsKey($packageId))
    {
        throw "Found multiple regular packages for '$packageId': '$($seenPackageIds[$packageId])' and '$($package.FullName)'."
    }

    $seenPackageIds.Add($packageId, $package.FullName)

    if (-not $packageCategories.ContainsKey($packageId))
    {
        throw "Package '$packageId' from '$($package.FullName)' is not declared in '$PackageManifestPath'."
    }

    $category = $packageCategories[$packageId]
    if ($category -eq 'ship')
    {
        Copy-Item -LiteralPath $package.FullName -Destination (Join-Path $OutputPath $package.Name)
        $shippingPackageCount++
    }
    elseif ($category -ne 'noship')
    {
        throw "Package '$packageId' has unsupported category '$category' in '$PackageManifestPath'."
    }
}

if ($shippingPackageCount -ne $ExpectedShippingPackageCount)
{
    throw "Expected $ExpectedShippingPackageCount shipping packages, but found $shippingPackageCount."
}

Write-Host "Prepared $shippingPackageCount shipping packages in '$OutputPath'."
