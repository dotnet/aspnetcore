#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Lists the version of all submodules and this repo
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
