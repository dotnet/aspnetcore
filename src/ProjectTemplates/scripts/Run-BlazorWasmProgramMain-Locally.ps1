#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates a template to help with rapid iteration in the templating area.
[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "blazorwasm" "blazorwasm --use-program-main --hosted --auth Individual" "Microsoft.DotNet.Web.ProjectTemplates.9.0.9.0.0-dev.nupkg" $true
