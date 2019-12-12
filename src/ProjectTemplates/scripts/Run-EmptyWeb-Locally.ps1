#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "web" "web" "Microsoft.DotNet.Web.ProjectTemplates.5.0.5.0.0-dev.nupkg" $false
