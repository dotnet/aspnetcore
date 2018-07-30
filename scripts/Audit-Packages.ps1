#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$fix = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'
$contentDir = "$PSScriptRoot/../src/Microsoft.DotNet.Web.Spa.ProjectTemplates/content"
foreach ($package in $contentDir) {
    $spaFrameworks = Get-ChildItem -Path $package -Directory

    foreach ($spaFramework in $spaFrameworks) {
        $spaFrameworkDir = Join-Path $contentDir $spaFramework
        $clientApp = Join-Path $spaFrameworkDir "ClientApp"
        Push-Location $clientApp
        try {
            Write-Output "Auditing $clientApp"
            if ($fix) {
                npm audit fix --force
            }
            else {
                npm audit
            }
        }
        finally {
            Pop-Location
        }
    }
}
