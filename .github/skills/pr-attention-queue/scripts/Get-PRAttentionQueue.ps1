#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Produces an actionable, read-only pull-request attention queue.

.DESCRIPTION
    Queries all open pull requests in a repository, resolves a named or ad hoc label/path scope,
    classifies each matched pull request by next actor, and emits a capped Markdown digest or the
    full JSON universe. The script never mutates GitHub.

.EXAMPLE
    ./Get-PRAttentionQueue.ps1
    Uses the default Blazor preset.

.EXAMPLE
    ./Get-PRAttentionQueue.ps1 -Label area-identity -Path 'src/Identity/**'
    Uses an ad hoc scope where either selector can include a pull request.

.EXAMPLE
    ./Get-PRAttentionQueue.ps1 -AllRepo -OutputFormat Json
    Classifies every open pull request and emits the full universe.
#>

[CmdletBinding()]
param(
    [string]$Repository = "dotnet/aspnetcore",
    [string]$Preset,
    [string[]]$Label = @(),
    [string[]]$Path = @(),
    [string[]]$RequireLabel = @(),
    [string[]]$ExcludeLabel = @(),
    [string[]]$Author = @(),
    [switch]$AllRepo,
    [ValidateSet("Markdown", "Json")]
    [string]$OutputFormat = "Markdown",
    [ValidateRange(0, 100)]
    [int]$MaxReviewNow = 5,
    [ValidateRange(0, 100)]
    [int]$MaxNeedsRescue = 3,
    [ValidateRange(0, 100)]
    [int]$MaxReadyToMerge = 3,
    [ValidateRange(0, 100)]
    [int]$MaxReviewNowPerAuthor = 2,
    [string]$InputPath,
    [datetime]$Now = [datetime]::UtcNow
)

$ErrorActionPreference = "Stop"

Import-Module -Scope Local -Force (Join-Path $PSScriptRoot "PRAttentionQueue.psm1")

Invoke-PRAttentionQueue `
    -Repository $Repository `
    -Preset $Preset `
    -Label $Label `
    -Path $Path `
    -RequireLabel $RequireLabel `
    -ExcludeLabel $ExcludeLabel `
    -Author $Author `
    -AllRepo:$AllRepo `
    -OutputFormat $OutputFormat `
    -MaxReviewNow $MaxReviewNow `
    -MaxNeedsRescue $MaxNeedsRescue `
    -MaxReadyToMerge $MaxReadyToMerge `
    -MaxReviewNowPerAuthor $MaxReviewNowPerAuthor `
    -InputPath $InputPath `
    -Now $Now
