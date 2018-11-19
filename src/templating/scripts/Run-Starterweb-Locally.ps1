#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "mvc" "mvc -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.3.0.3.0.0-alpha1.nupkg" $false
