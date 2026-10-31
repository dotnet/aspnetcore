#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

Import-Module -Name "$PSScriptRoot/Test-Template.psm1"

Test-Template -TemplateName "webapi" -TemplateArguments @("webapi", "--use-program-main", "--use-minimal-apis")
