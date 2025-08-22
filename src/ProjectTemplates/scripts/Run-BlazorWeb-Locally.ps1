#!/usr/bin/env pwsh
#requires -version 4

# This script packages, installs and creates a template to help with rapid iteration in the templating area.
    [CmdletBinding(PositionalBinding = $false)]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [ValidateSet("net9.0", "net10.0")]
        [string] $Framework = "net10.0",
        [Parameter(Mandatory = $false)]
        [switch] $NoRestore,
        [Parameter(Mandatory = $false)]
        [switch] $ExcludeLaunchSettings,
        [Parameter(Mandatory = $false)]
        [ValidateSet("None", "Server", "WebAssembly", "Auto")]
        [string] $Interactivity = "Server",
        [Parameter(Mandatory = $false)]
        [switch] $Empty,
        [Parameter(Mandatory = $false)]
        [ValidateSet("None", "Individual")]
        [string] $Auth = "None",
        [Parameter(Mandatory = $false)]
        [switch] $UseLocalDb,
        [Parameter(Mandatory = $false)]
        [switch] $AllInteractive,
        [Parameter(Mandatory = $false)]
        [switch] $NoHttps,
        [Parameter(Mandatory = $false)]
        [switch] $UseProgramMain,
        [Parameter(Mandatory = $false)]
        [ValidateSet("Debug", "Release")]
        [string] $Configuration = "Release",
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $Args
    )

    Set-StrictMode -Version 2
    $ErrorActionPreference = 'Stop'

    $templateArguments = @("blazor");

    if ($ExcludeLaunchSettings) {
        $templateArguments += "--exclude-launch-settings"
    }

    if ($Interactivity) {
        $templateArguments += "--interactivity"
        $templateArguments += $Interactivity;
    }

    if ($Empty) {
        $templateArguments += "-e"
    }

    if ($Auth) {
        $templateArguments += "--auth";
        $templateArguments += $Auth;
    }

    $mainProjectRelativePath = $null;
    if($Interactivity -in @("Auto", "WebAssembly")){
        $mainProjectRelativePath = "MyBlazorApp";
    }

    if ($UseLocalDb) {
        $templateArguments += "-uld"
    }

    if ($AllInteractive) {
        $templateArguments += "-ai"
    }

    if ($NoHttps) {
        $templateArguments += "--no-https"
    }

    if ($UseProgramMain) {
        $templateArguments += "--use-program-main"
    }

    Import-Module -Name "$PSScriptRoot/Test-Template.psm1";

    Test-Template `
        -TemplateName "MyBlazorApp" `
        -TemplateArguments $templateArguments `
        -MainProjectRelativePath $mainProjectRelativePath `
        -TargetFramework $Framework `
        -Configuration $Configuration `
        -Verbose:$VerbosePreference;
