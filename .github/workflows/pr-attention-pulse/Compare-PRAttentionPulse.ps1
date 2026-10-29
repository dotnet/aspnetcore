#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CurrentPath,
    [Parameter(Mandatory)][string]$PreviousPath,
    [Parameter(Mandatory)][string]$OutputPath
)

$ErrorActionPreference = "Stop"

function New-UnavailableChanges
{
    param([Parameter(Mandatory)][string]$Reason)

    return [ordered]@{
        status = "unavailable"
        reason = $Reason
        previousGeneratedAt = $null
        items = @()
    }
}

function Get-IndexedItems
{
    param([Parameter(Mandatory)][object]$Area)

    $result = @{}
    foreach ($item in @($Area.followUpIndex))
    {
        $number = [int]$item.number
        if ($number -lt 1 -or $result.ContainsKey($number))
        {
            throw "The Pulse follow-up index contains an invalid or duplicate PR number."
        }

        $result[$number] = $item
    }

    return $result
}

function Test-CompatiblePulse
{
    param([AllowNull()][object]$Pulse)

    if ($null -eq $Pulse -or $Pulse.schemaVersion -cne "2.0.0" -or
        $Pulse.areas -isnot [array] -or @($Pulse.areas).Count -ne 2)
    {
        return $false
    }

    $areaIds = @($Pulse.areas | ForEach-Object { [string]$_.id })
    return @($areaIds | Select-Object -Unique).Count -eq 2 -and
        $areaIds -ccontains "blazor" -and
        $areaIds -ccontains "repository-wide"
}

function Compare-PulseArea
{
    param(
        [Parameter(Mandatory)][object]$CurrentArea,
        [AllowNull()][object]$PreviousArea
    )

    if ($CurrentArea.status -cne "complete")
    {
        return New-UnavailableChanges -Reason "current-area-unavailable"
    }
    if ($null -eq $PreviousArea)
    {
        return New-UnavailableChanges -Reason "previous-area-missing"
    }
    if ($PreviousArea.status -cne "complete")
    {
        return New-UnavailableChanges -Reason "previous-area-unavailable"
    }
    if ($CurrentArea.followUpIndex -isnot [array] -or $PreviousArea.followUpIndex -isnot [array])
    {
        return New-UnavailableChanges -Reason "follow-up-index-unavailable"
    }

    $previousItems = Get-IndexedItems -Area $PreviousArea
    $changes = @(
        foreach ($current in @($CurrentArea.followUpIndex))
        {
            $number = [int]$current.number
            if (-not $previousItems.ContainsKey($number))
            {
                continue
            }

            $previous = $previousItems[$number]
            $signals = [Collections.Generic.List[string]]::new()
            if (-not [string]::Equals([string]$previous.headSha, [string]$current.headSha, [StringComparison]::Ordinal))
            {
                $signals.Add("head-updated")
            }
            if (-not [string]::Equals([string]$previous.nextActor, [string]$current.nextActor, [StringComparison]::Ordinal))
            {
                $signals.Add("next-actor-changed")
            }
            if ([string]$previous.checkState -cin @("Failed", "Pending") -and
                [string]::Equals([string]$current.checkState, "Passing", [StringComparison]::Ordinal))
            {
                $signals.Add("ci-recovered")
            }
            if (-not [string]::Equals([string]$previous.mergeEligibility, "eligible", [StringComparison]::Ordinal) -and
                [string]::Equals([string]$current.mergeEligibility, "eligible", [StringComparison]::Ordinal))
            {
                $signals.Add("merge-became-eligible")
            }
            if ($signals.Count -eq 0)
            {
                continue
            }

            [ordered]@{
                number = $number
                title = [string]$current.title
                signals = @($signals)
                previous = [ordered]@{
                    headSha = $previous.headSha
                    checkState = [string]$previous.checkState
                    bucket = [string]$previous.bucket
                    nextActor = [string]$previous.nextActor
                    mergeEligibility = [string]$previous.mergeEligibility
                }
                current = [ordered]@{
                    headSha = $current.headSha
                    checkState = [string]$current.checkState
                    bucket = [string]$current.bucket
                    nextActor = [string]$current.nextActor
                    mergeEligibility = [string]$current.mergeEligibility
                }
            }
        }
    )

    return [ordered]@{
        status = "complete"
        reason = $null
        previousGeneratedAt = [string]$PreviousArea.source.generatedAt
        items = @($changes | Sort-Object { $_["number"] })
    }
}

$current = Get-Content -LiteralPath $CurrentPath -Raw | ConvertFrom-Json -Depth 100
$previousEnvelope = Get-Content -LiteralPath $PreviousPath -Raw | ConvertFrom-Json -Depth 100
if (-not (Test-CompatiblePulse -Pulse $current))
{
    throw "The current Pulse snapshot is incompatible."
}

$previous = $null
if ($previousEnvelope.status -ceq "available")
{
    $previous = $previousEnvelope.pulse
    if (-not (Test-CompatiblePulse -Pulse $previous))
    {
        $previous = $null
        $previousEnvelope = [pscustomobject]@{ status = "unavailable"; reason = "previous-snapshot-incompatible" }
    }
}

for ($index = 0; $index -lt $current.areas.Count; $index++)
{
    $currentArea = $current.areas[$index]
    $changes = if ($null -eq $previous)
    {
        New-UnavailableChanges -Reason ([string]$previousEnvelope.reason)
    }
    else
    {
        $previousArea = @($previous.areas | Where-Object id -CEQ $currentArea.id | Select-Object -First 1)
        Compare-PulseArea -CurrentArea $currentArea -PreviousArea $(if ($previousArea.Count -eq 1) { $previousArea[0] } else { $null })
    }
    $currentArea | Add-Member -NotePropertyName changes -NotePropertyValue ([pscustomobject]$changes) -Force
}

$json = $current | ConvertTo-Json -Depth 100 -Compress -EscapeHandling EscapeNonAscii
[IO.File]::WriteAllText($OutputPath, $json + "`n", [Text.UTF8Encoding]::new($false))
