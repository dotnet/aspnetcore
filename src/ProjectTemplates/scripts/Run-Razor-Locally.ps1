#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates a template to help with rapid iteration in the templating area.
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Mandatory = $false, Position = 0)]
    [ValidateSet("net9.0", "net10.0")]
    [string] $Framework = "net10.0",
    [Parameter(Mandatory = $false)]
    [switch] $ExcludeLaunchSettings,
    [Parameter(Mandatory = $false)]
    [ValidateSet("None", "Individual")]
    [string] $Auth = "None",
    [Parameter(Mandatory = $false)]
    [switch] $UseLocalDb,
    [Parameter(Mandatory = $false)]
    [switch] $NoHttps,
    [Parameter(Mandatory = $false)]
    [switch] $UseProgramMain,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Args
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$templateArguments = @("webapp");

if ($ExcludeLaunchSettings) {
    $templateArguments += "--exclude-launch-settings"
}

if ($Auth) {
    $templateArguments += "--auth";
    $templateArguments += $Auth;
}

if ($UseLocalDb) {
    $templateArguments += "-uld"
}

if ($NoHttps) {
    $templateArguments += "--no-https"
}

if ($UseProgramMain) {
    $templateArguments += "--use-program-main"
}

Import-Module -Name "$PSScriptRoot/Test-Template.psm1";

Test-Template `
    -TemplateName "MyWebApp" `
    -TemplateArguments $templateArguments `
    -TargetFramework $Framework `
    -Verbose:$VerbosePreference;
