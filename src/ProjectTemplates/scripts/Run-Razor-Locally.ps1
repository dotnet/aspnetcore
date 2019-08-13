#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapp" "webapp -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.5.0.5.0.0-dev.nupkg" $false
