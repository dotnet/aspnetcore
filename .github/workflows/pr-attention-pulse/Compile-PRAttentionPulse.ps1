#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [switch]$Validate
)

$ErrorActionPreference = "Stop"
$policyVariable = "GHAW_POLICY_MODELS_ALLOWED"
$previousValue = [Environment]::GetEnvironmentVariable($policyVariable, "Process")

try
{
    [Environment]::SetEnvironmentVariable($policyVariable, "gpt-5.6-sol", "Process")
    $arguments = @("aw", "compile", "pr-attention-pulse", "--strict")
    if ($Validate)
    {
        $arguments += "--validate"
    }

    & gh @arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "gh aw compile failed with exit code $LASTEXITCODE."
    }
}
finally
{
    [Environment]::SetEnvironmentVariable($policyVariable, $previousValue, "Process")
}
