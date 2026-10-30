#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Initialize", "Next", "Receipt", "State", "Simulate", "Canary")]
    [string]$Command,
    [string]$LedgerPath,
    [string]$ConfigPath,
    [string]$ReceiptPath,
    [switch]$ImportRequired,
    [switch]$AllowFixtureQueue,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Import-Module -Scope Local -Force -DisableNameChecking (Join-Path $PSScriptRoot "SyncfusionReviewOrchestrator.psm1")

$result = switch ($Command) {
    "Initialize" {
        if (-not $LedgerPath -or -not $ConfigPath) {
            throw "Initialize requires -LedgerPath and -ConfigPath."
        }

        New-SfroCampaign `
            -LedgerPath $LedgerPath `
            -ConfigPath $ConfigPath `
            -ImportRequired:$ImportRequired `
            -AllowFixtureQueue:$AllowFixtureQueue `
            -DryRun:$DryRun
    }
    "Next" {
        if (-not $LedgerPath) {
            throw "Next requires -LedgerPath."
        }

        Invoke-SfroNext -LedgerPath $LedgerPath -DryRun:$DryRun
    }
    "Receipt" {
        if (-not $LedgerPath -or -not $ReceiptPath) {
            throw "Receipt requires -LedgerPath and -ReceiptPath."
        }

        $receipt = ConvertFrom-SfroJson (Get-Content -LiteralPath $ReceiptPath -Raw)
        Add-SfroReceipt -LedgerPath $LedgerPath -Receipt $receipt
    }
    "State" {
        if (-not $LedgerPath) {
            throw "State requires -LedgerPath."
        }

        Get-SfroState -LedgerPath $LedgerPath
    }
    "Simulate" {
        if (-not $ConfigPath) {
            throw "Simulate requires -ConfigPath."
        }

        Invoke-SfroSimulation -ConfigPath $ConfigPath
    }
    "Canary" {
        Invoke-SfroCanary
    }
}

$result | ConvertTo-Json -Depth 100
