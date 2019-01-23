#!/usr/bin/env powershell
#requires -version 4

[CmdletBinding(PositionalBinding = $false)]
param()

. $PSScriptRoot\Test-Template.ps1

Test-Template "webapp" "webapp -au Individual" "Microsoft.DotNet.Web.ProjectTemplates.3.0.3.0.0-alpha1.nupkg" $false
