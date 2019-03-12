#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapp" "webapp -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.3.0.3.0.0-preview4-t000.nupkg" $false
