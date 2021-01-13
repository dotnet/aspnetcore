#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapp" "webapp -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.3.1.3.1.0-dev.nupkg" $false
