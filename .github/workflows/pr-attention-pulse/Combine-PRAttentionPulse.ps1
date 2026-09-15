#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BlazorInputPath,

    [Parameter(Mandatory)]
    [string]$RepositoryWideInputPath,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [ValidateRange(8192, 262144)]
    [int]$MaxOutputBytes = 131072
)

$ErrorActionPreference = "Stop"

function Read-PulseArea
{
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][bool]$OpenByDefault,
        [Parameter(Mandatory)][string]$ExpectedFilterName,
        [Parameter(Mandatory)][bool]$ExpectedAllRepositoryPullRequests
    )

    $area = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 100
    $complete = [string]::Equals([string]$area.status, "complete", [StringComparison]::Ordinal)
    if (-not [string]::Equals([string]$area.schemaVersion, "1.0.0", [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$area.scope, $Id, [StringComparison]::Ordinal) -or
        [string]$area.status -notin @("complete", "unavailable") -or
        $area.candidateCountsAvailable -isnot [bool] -or
        $area.candidateCountsAvailable -ne $complete -or
        -not [string]::Equals([string]$area.source.repository, "dotnet/aspnetcore", [StringComparison]::Ordinal))
    {
        throw "The '$Id' Pulse area envelope is invalid."
    }

    if ($complete)
    {
        if (-not [string]::Equals([string]$area.source.filter.name, $ExpectedFilterName, [StringComparison]::Ordinal) -or
            $area.source.filter.allRepositoryPullRequests -isnot [bool] -or
            $area.source.filter.allRepositoryPullRequests -ne $ExpectedAllRepositoryPullRequests)
        {
            throw "The '$Id' Pulse area has the wrong resolved scope."
        }
    }

    $result = [ordered]@{
        id = $Id
        label = $Label
        openByDefault = $OpenByDefault
        status = [string]$area.status
        attemptedAt = [string]$area.attemptedAt
        candidateCountsAvailable = [bool]$area.candidateCountsAvailable
        source = $area.source
    }
    if ([string]::Equals([string]$area.status, "complete", [StringComparison]::Ordinal))
    {
        $result["views"] = $area.views
    }
    else
    {
        $result["errorCategory"] = [string]$area.errorCategory
    }

    return $result
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory))
{
    New-Item -ItemType Directory -Force $outputDirectory | Out-Null
}

try
{
    $blazor = Read-PulseArea `
        -Path $BlazorInputPath `
        -Id "blazor" `
        -Label "Blazor" `
        -OpenByDefault $true `
        -ExpectedFilterName "blazor" `
        -ExpectedAllRepositoryPullRequests $false
    $repositoryWide = Read-PulseArea `
        -Path $RepositoryWideInputPath `
        -Id "repository-wide" `
        -Label "Repository-wide" `
        -OpenByDefault $false `
        -ExpectedFilterName "adhoc" `
        -ExpectedAllRepositoryPullRequests $true

    $completeCount = @(@($blazor, $repositoryWide) | Where-Object { $_.status -ceq "complete" }).Count
    $status = if ($completeCount -eq 2)
    {
        "complete"
    }
    elseif ($completeCount -eq 0)
    {
        "unavailable"
    }
    else
    {
        "partial"
    }

    $result = [ordered]@{
        schemaVersion = "2.0.0"
        status = $status
        areas = @($blazor, $repositoryWide)
    }
    $json = $result | ConvertTo-Json -Depth 30 -Compress
    if ([Text.Encoding]::UTF8.GetByteCount($json) -gt $MaxOutputBytes)
    {
        throw "The combined sanitized Pulse envelope exceeds the configured size limit."
    }

    [IO.File]::WriteAllText($OutputPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}
finally
{
    Remove-Item -LiteralPath $BlazorInputPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $RepositoryWideInputPath -Force -ErrorAction SilentlyContinue
}
