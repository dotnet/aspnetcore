#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tags each repo according to VersionPrefix in version.props of that repo
.PARAMETER Shipping
    Only list repos that are shipping
#>
[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [switch]$Shipping = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"

Assert-Git

$RepoRoot = Resolve-Path "$PSScriptRoot/../"

Get-Submodules $RepoRoot -Shipping:$Shipping | Format-Table -Property 'module','versionPrefix'
