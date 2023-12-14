#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapp" "webapp -au Individual --use-program-main" "Microsoft.DotNet.Web.ProjectTemplates.9.0.9.0.0-dev.nupkg" $false
