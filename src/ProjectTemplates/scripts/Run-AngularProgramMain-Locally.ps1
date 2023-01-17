#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "angular" "angular --use-program-main" "Microsoft.DotNet.Web.Spa.ProjectTemplates.8.0.8.0.0-dev.nupkg" $false
