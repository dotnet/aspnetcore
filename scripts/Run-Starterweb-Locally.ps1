#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Custom-Hive.ps1

Test-Template "mvc" "mvc -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.2.2.2.2.0-preview3-t000.nupkg" $false
