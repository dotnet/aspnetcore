[cmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

$RepoRoot = "$PSScriptRoot\..\.."
$projects = Get-ChildItem -Include **\*.*proj
& "$RepoRoot\build.ps1" -Projects $projects $MSBuildArguments
