#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AgentOutputPath,
    [Parameter(Mandatory)][string]$PulseInputPath,

    [Parameter(Mandatory)][string]$ExpectedBodyPath,

    [Parameter(Mandatory)][string]$SanitizerModulePath
)

$ErrorActionPreference = "Stop"
$renderedBodyPath = "$ExpectedBodyPath.rendered"
$normalizedBodyPath = "$ExpectedBodyPath.normalized"
try
{
    Import-Module -Scope Local -Force (Join-Path $PSScriptRoot "PRAttentionPulseContract.psm1")

    $agentOutput = Get-Content -LiteralPath $AgentOutputPath -Raw | ConvertFrom-Json -Depth 100
    $pulse = Get-Content -LiteralPath $PulseInputPath -Raw | ConvertFrom-Json -Depth 100
    $expectedBody = Get-Content -LiteralPath $ExpectedBodyPath -Raw
    $renderedBody = ConvertTo-PRAttentionPulseBody -Pulse $pulse
    [IO.File]::WriteAllText($renderedBodyPath, $renderedBody, [Text.UTF8Encoding]::new($false))

    $nodeScript = @'
const fs = require("fs");
global.core = { info() {}, warning() {}, error() {} };
const { sanitizeContent } = require(process.argv[2]);
const input = fs.readFileSync(process.argv[3], "utf8");
const output = sanitizeContent(input, {
  allowedAliases: [],
});
fs.writeFileSync(process.argv[4], output, "utf8");
'@
    $nodeScript | node - $SanitizerModulePath $renderedBodyPath $normalizedBodyPath
    if ($LASTEXITCODE -ne 0)
    {
        throw "The pinned gh-aw output sanitizer failed."
    }
    $normalizedRenderedBody = Get-Content -LiteralPath $normalizedBodyPath -Raw

    if (-not [string]::Equals($expectedBody, $normalizedRenderedBody, [StringComparison]::Ordinal))
    {
        throw "The trusted Pulse body does not match the sanitized input."
    }

    Assert-PRAttentionPulseOutput `
        -AgentOutput $agentOutput `
        -Pulse $pulse `
        -ExpectedBody $expectedBody
}
catch
{
    $safeOutputsPath = Join-Path (Split-Path -Parent $AgentOutputPath) "safeoutputs.jsonl"
    Remove-Item -LiteralPath $safeOutputsPath -Force -ErrorAction SilentlyContinue

    $quarantinePath = "$AgentOutputPath.rejected"
    $emptyOutputPath = "$AgentOutputPath.empty"
    Remove-Item -LiteralPath $quarantinePath, $emptyOutputPath -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $AgentOutputPath)
    {
        Move-Item -LiteralPath $AgentOutputPath -Destination $quarantinePath -Force
    }

    $emptyOutput = '{"items":[],"errors":[]}' + [Environment]::NewLine
    [IO.File]::WriteAllText($emptyOutputPath, $emptyOutput, [Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $emptyOutputPath -Destination $AgentOutputPath -Force
    Remove-Item -LiteralPath $quarantinePath -Force -ErrorAction SilentlyContinue

    throw
}
finally
{
    Remove-Item -LiteralPath $renderedBodyPath, $normalizedBodyPath -Force -ErrorAction SilentlyContinue
}
