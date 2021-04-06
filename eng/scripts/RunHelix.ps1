<#
.SYNOPSIS
    Runs the specified test project on a Helix machine.
.DESCRIPTION
    This script runs the Helix msbuild task on the given project and publishes then uploads the output and runs tests on the Helix machine(s) passed in.
.PARAMETER Project
    The test project to publish and send to Helix.
.PARAMETER HelixQueues
    Set the Helix queues to use. The list is '+' or ';'-separated.
    Some supported queues:
    Ubuntu.1604.Amd64.Open
    Ubuntu.1804.Amd64.Open
    Windows.10.Amd64.Open
    Windows.81.Amd64.Open
    Windows.7.Amd64.Open
    OSX.1014.Amd64.Open
    Centos.7.Amd64.Open
    Debian.9.Amd64.Open
    Redhat.7.Amd64.Open
.PARAMETER RunQuarantinedTests
    By default quarantined tests are not run. Set this to $true to run only the quarantined tests.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$Project,
    [string]$HelixQueues = "Windows.10.Amd64.Open",
    [string]$TargetArchitecture = "x64",
    [bool]$RunQuarantinedTests = $false
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Set-StrictMode -Version 1

$env:BUILD_REASON="PullRequest"
$env:BUILD_SOURCEBRANCH="local"
$env:BUILD_REPOSITORY_NAME="aspnetcore"
$env:SYSTEM_TEAMPROJECT="aspnetcore"

Write-Host -ForegroundColor Yellow "If running tests that need the shared Fx, run './build -pack -all' before this."
Write-Host -ForegroundColor Yellow "And if packing for a different platform, add '/p:CrossgenOutput=false'."

$HelixQueues = $HelixQueues -replace ";", "%3B"
dotnet msbuild $Project /t:Helix /p:Configuration=Release /p:TargetArchitecture="$TargetArchitecture" /p:IsRequiredCheck=true `
    /p:IsHelixDaily=true /p:HelixTargetQueues=$HelixQueues /p:RunQuarantinedTests=$RunQuarantinedTests `
    /p:_UseHelixOpenQueues=true /p:CrossgenOutput=false /p:ASPNETCORE_TEST_LOG_DIR=artifacts/log
