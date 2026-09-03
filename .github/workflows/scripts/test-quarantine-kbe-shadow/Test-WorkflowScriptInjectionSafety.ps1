#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Static regression test: no GitHub Actions `${{ ... }}` expression may appear inside a `run:`
    script body in the test-quarantine-kbe-shadow workflows.

.DESCRIPTION
    Directly interpolating a `${{ ... }}` expression -- especially a workflow_dispatch input such
    as `${{ inputs.signature }}` -- into a `run:` script body is a script-injection vector: a value
    containing a quote, backtick, or newline can execute arbitrary commands on the runner. Every
    value this workflow needs inside a script must instead flow through a step (or job) `env:`
    binding and be read back as an opaque environment variable (e.g. `$env:SIGNATURE_INPUT`),
    which the shell/PowerShell parser never re-parses as script text regardless of its content.

    This test parses each target workflow file's YAML block-scalar `run:` bodies (both `run: |`
    and unquoted single-line `run: ...` forms) using a minimal, indentation-based reader -- no
    external YAML/GitHub Actions parser dependency -- and fails if any of them contains a `${{`
    token. It intentionally does not inspect `if:`, `env:`, `with:`, or `concurrency:` values: a
    `${{ }}` expression there is evaluated by the Actions engine itself, not by a shell, and is not
    a script-injection vector.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$workflowsRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$targetFiles = @(
    (Join-Path $workflowsRoot "test-quarantine-kbe-shadow.yml"),
    (Join-Path $workflowsRoot "test-quarantine-kbe-shadow-tests.yml")
)

function Get-IndentWidth
{
    param([Parameter(Mandatory = $true)][string]$Line)

    $trimmed = $Line.TrimStart(" ")
    return $Line.Length - $trimmed.Length
}

function Get-RunBlockBodies
{
    param([Parameter(Mandatory = $true)][string]$Path)

    $lines = [System.IO.File]::ReadAllLines($Path)
    $blocks = [System.Collections.Generic.List[object]]::new()
    $i = 0
    while ($i -lt $lines.Count)
    {
        $line = $lines[$i]
        $singleLineMatch = [regex]::Match($line, '^(\s*)run:\s*(?!\||>)(\S.*)$')
        $blockMatch = [regex]::Match($line, '^(\s*)run:\s*[|>][+-]?\s*$')

        if ($singleLineMatch.Success)
        {
            $null = $blocks.Add([ordered]@{ StartLine = $i + 1; Text = $singleLineMatch.Groups[2].Value })
            $i++
            continue
        }

        if ($blockMatch.Success)
        {
            $keyIndent = $blockMatch.Groups[1].Value.Length
            $bodyLines = [System.Collections.Generic.List[string]]::new()
            $j = $i + 1
            while ($j -lt $lines.Count)
            {
                $candidate = $lines[$j]
                if ($candidate.Trim().Length -eq 0)
                {
                    $bodyLines.Add($candidate)
                    $j++
                    continue
                }
                if ((Get-IndentWidth -Line $candidate) -le $keyIndent)
                {
                    break
                }
                $bodyLines.Add($candidate)
                $j++
            }
            $null = $blocks.Add([ordered]@{ StartLine = $i + 1; Text = ($bodyLines -join "`n") })
            $i = $j
            continue
        }

        $i++
    }

    return $blocks
}

try
{
    $failures = [System.Collections.Generic.List[string]]::new()

    foreach ($file in $targetFiles)
    {
        if (-not (Test-Path -LiteralPath $file))
        {
            throw "Target workflow file does not exist: $file"
        }

        $blocks = Get-RunBlockBodies -Path $file
        if ($blocks.Count -eq 0)
        {
            throw "No 'run:' blocks were found in $file; the parser may be broken (this test expects at least one)."
        }

        foreach ($block in $blocks)
        {
            if ($block.Text.Contains('${{'))
            {
                $null = $failures.Add("$($file):$($block.StartLine): 'run:' block contains a '`${{ ... }}' expression.")
            }
        }
    }

    if ($failures.Count -gt 0)
    {
        throw "Script-injection guard failed:`n" + ($failures -join "`n")
    }

    Write-Host "No 'run:' block in the test-quarantine-kbe-shadow workflows contains a GitHub Actions expression."
}
catch
{
    Write-Error $_
    exit 1
}
