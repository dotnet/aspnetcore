#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates the blazorwasm-servicedefaults
# template to help with rapid iteration on the template content.
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet("net11.0")]
    [string] $Framework = "net11.0",
    [Parameter(Mandatory = $false)]
    [switch] $Hosted,
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Args
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$templateArguments = @("blazorwasm-servicedefaults");

if ($Hosted) {
    $templateArguments += "--hosted"
}

Import-Module -Name "$PSScriptRoot/Test-Template.psm1";

Test-Template `
    -TemplateName "MyBlazorWasmServiceDefaults" `
    -TemplateArguments $templateArguments `
    -TargetFramework $Framework `
    -Configuration $Configuration `
    -Verbose:$VerbosePreference;
