#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "mvc" "mvc -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.3.1.3.1.0-dev.nupkg" $false
