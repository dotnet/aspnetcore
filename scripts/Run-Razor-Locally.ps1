#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Custom-Hive.ps1

Test-Template "razor" "Microsoft.DotNet.Web.ProjectTemplates.2.2.2.2.0-preview1-t000.nupkg" $false
