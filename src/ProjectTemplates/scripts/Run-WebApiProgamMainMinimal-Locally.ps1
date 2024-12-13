#!/usr/bin/env pwsh
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapi" "webapi --use-program-main --use-minimal-apis" "Microsoft.DotNet.Web.ProjectTemplates.10.0.10.0.0-dev.nupkg" $false
