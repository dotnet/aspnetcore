#requires -version 5

<#
.SYNOPSIS
Clean this repository.

.DESCRIPTION
This script cleans this repository interactively, leaving downloaded infrastructure untouched.
Clean operation is interactive to avoid losing new but unstaged files. Press 'c' then [Enter]
to perform the proposed deletions.

.EXAMPLE
Perform default clean operation.

    clean.ps1

.EXAMPLE
Clean everything but downloaded infrastructure and VS / VS Code folders.

    clean.ps1 -e .vs/ -e .vscode/
#>

[CmdletBinding(PositionalBinding = $false)]
param(
    # Other lifecycle targets
    [switch]$Help, # Show help

    # Capture the rest
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GitArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help $PSCommandPath
    exit 0
}

git clean -dix -e .dotnet/ -e .tools/ @GitArguments
git checkout -- $(git ls-files -d)
