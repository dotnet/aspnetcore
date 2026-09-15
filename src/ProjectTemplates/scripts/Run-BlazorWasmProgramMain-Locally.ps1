#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates a template to help with rapid iteration in the templating area.
Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module -Name "$PSScriptRoot/Test-Template.psm1"

Test-Template `
    -TemplateName "blazorwasm" `
    -TemplateArguments @("blazorwasm", "--use-program-main", "--auth", "Individual")
