#!/usr/bin/env pwsh
#Requires -Version 7.0

$ErrorActionPreference = "Stop"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

$modulePath = Join-Path (Split-Path -Parent $PSScriptRoot) "scripts/PRAttentionQueue.psm1"
$module = Import-Module -Scope Local -Force $modulePath -PassThru
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "pr-attention-gh-retry-$PID"
$counterPath = Join-Path $tempRoot "counter.txt"
$commandPath = if ($IsWindows)
{
    Join-Path $tempRoot "gh.cmd"
}
else
{
    Join-Path $tempRoot "gh"
}

try
{
    New-Item -ItemType Directory -Force $tempRoot | Out-Null
    if ($IsWindows)
    {
        @"
@echo off
set /p count=<"$counterPath"
set /a count=count+1
> "$counterPath" echo %count%
if "%1"=="retry-success" if %count% LSS 2 (
  >&2 echo unexpected end of JSON input
  exit /b 1
)
if "%1"=="retry-exhaust" (
  >&2 echo unexpected end of JSON input
  exit /b 1
)
if "%1"=="nonexact" (
  >&2 echo prefix: unexpected end of JSON input
  exit /b 1
)
if "%1"=="text" (
  >&2 echo unexpected end of JSON input
  exit /b 1
)
if "%1"=="malformed" (
  echo {
  exit /b 0
)
if "%1"=="http" if %count% LSS 2 (
  >&2 echo HTTP 502 Bad Gateway
  exit /b 1
)
echo {"ok":true}
"@ | Set-Content -LiteralPath $commandPath -Encoding ascii
    }
    else
    {
        @"
#!/usr/bin/env bash
set -euo pipefail
count=`$(cat "$counterPath")
count=`$((count + 1))
printf '%s\n' "`$count" > "$counterPath"
case "`$1" in
  retry-success)
    if [ "`$count" -lt 2 ]; then printf '%s\n' 'unexpected end of JSON input' >&2; exit 1; fi
    ;;
  retry-exhaust)
    printf '%s\n' 'unexpected end of JSON input' >&2
    exit 1
    ;;
  nonexact)
    printf '%s\n' 'prefix: unexpected end of JSON input' >&2
    exit 1
    ;;
  text)
    printf '%s\n' 'unexpected end of JSON input' >&2
    exit 1
    ;;
  malformed)
    printf '%s\n' '{'
    exit 0
    ;;
  http)
    if [ "`$count" -lt 2 ]; then printf '%s\n' 'HTTP 502 Bad Gateway' >&2; exit 1; fi
    ;;
esac
printf '%s\n' '{"ok":true}'
"@ | Set-Content -LiteralPath $commandPath -Encoding utf8NoBOM
        & chmod +x $commandPath
    }

    Set-Content -LiteralPath $counterPath -Value "0" -Encoding ascii
    $result = & $module {
        param($Executable)
        Invoke-GhJson -Arguments @("retry-success") -Executable $Executable -MaxAttempts 2
    } $commandPath

    Assert-True ($result.ok -eq $true) "The exact GitHub CLI JSON read failure must retry and return the later valid response."
    Assert-True ((Get-Content -LiteralPath $counterPath -Raw).Trim() -eq "2") "The retry must stop after the later successful response."

    Set-Content -LiteralPath $counterPath -Value "0" -Encoding ascii
    try
    {
        & $module {
            param($Executable)
            Invoke-GhJson -Arguments @("retry-exhaust") -Executable $Executable -MaxAttempts 2
        } $commandPath
        throw "Expected the exact transient failure to exhaust its retry budget."
    }
    catch
    {
        Assert-True ($_.Exception.Message.Contains("attempt 2 of 2", [StringComparison]::Ordinal)) "Retry exhaustion must identify the final bounded attempt."
        Assert-True ($_.Exception.Message.Contains("unexpected end of JSON input", [StringComparison]::Ordinal)) "Retry exhaustion must preserve the observed CLI failure."
    }
    Assert-True ((Get-Content -LiteralPath $counterPath -Raw).Trim() -eq "2") "The exact transient failure must not exceed its retry budget."

    foreach ($case in @(
        @{ Name = "nonexact"; Text = $false; Message = "A prefixed malformed-input message must not be treated as the exact transient failure." },
        @{ Name = "text"; Text = $true; Message = "The JSON parser failure must not broaden Invoke-GhText retries." },
        @{ Name = "malformed"; Text = $false; Message = "Malformed stdout from a successful process must not be retried." }
    ))
    {
        Set-Content -LiteralPath $counterPath -Value "0" -Encoding ascii
        try
        {
            & $module {
                param($Executable, $Case)
                if ($Case.Text)
                {
                    Invoke-GhText -Arguments @($Case.Name) -Executable $Executable -MaxAttempts 2
                }
                else
                {
                    Invoke-GhJson -Arguments @($Case.Name) -Executable $Executable -MaxAttempts 2
                }
            } $commandPath $case
            throw "Expected '$($case.Name)' to fail."
        }
        catch
        {
            Assert-True ((Get-Content -LiteralPath $counterPath -Raw).Trim() -eq "1") $case.Message
        }
    }

    Set-Content -LiteralPath $counterPath -Value "0" -Encoding ascii
    $httpResult = & $module {
        param($Executable)
        Invoke-GhJson -Arguments @("http") -Executable $Executable -MaxAttempts 2
    } $commandPath
    Assert-True ($httpResult.ok -eq $true -and (Get-Content -LiteralPath $counterPath -Raw).Trim() -eq "2") "Existing bounded HTTP transient retries must remain intact."
}
finally
{
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "PR attention queue GitHub JSON retry test passed."
