#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates a template to help with rapid iteration in the templating area.
[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet("net9.0", "net10.0")]
    [string] $Framework = "net10.0",
    [Parameter(Mandatory = $false)]
    [switch] $NoRestore,
    [Parameter(Mandatory = $false)]
    [switch] $ExcludeLaunchSettings,
    [Parameter(Mandatory = $false)]
    [switch] $Empty,
    [Parameter(Mandatory = $false)]
    [ValidateSet("None", "Individual")]
    [string] $Auth = "None",
    [Parameter(Mandatory = $false)]
    [switch] $NoHttps,
    [Parameter(Mandatory = $false)]
    [switch] $UseProgramMain,
    [Parameter(Mandatory = $false)]
    [string] $Authority,
    [Parameter(Mandatory = $false)]
    [string] $ClientId,
    [switch] $Pwa,
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Args
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$templateArguments = @("blazorwasm");

if ($ExcludeLaunchSettings) {
    $templateArguments += "--exclude-launch-settings"
}

if ($Empty) {
    $templateArguments += "-e"
}

if ($Auth) {
    $templateArguments += "--auth";
    $templateArguments += $Auth;
}

$mainProjectRelativePath = $null;

if ($NoHttps) {
    $templateArguments += "--no-https"
}

if ($Authority) {
    $templateArguments += "--authority"
    $templateArguments += $Authority
}

if ($ClientId) {
    $templateArguments += "--client-id"
    $templateArguments += $ClientId
}

if ($Pwa) {
    $templateArguments += "--pwa"
}

if ($UseProgramMain) {
    $templateArguments += "--use-program-main"
}

Import-Module -Name "$PSScriptRoot/Test-Template.psm1";

Test-Template `
    -TemplateName "MyBlazorWasmApp" `
    -TemplateArguments $templateArguments `
    -MainProjectRelativePath $mainProjectRelativePath `
    -TargetFramework $Framework `
    -Configuration $Configuration `
    -Verbose:$VerbosePreference;
