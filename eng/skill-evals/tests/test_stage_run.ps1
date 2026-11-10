#requires -Version 7.0

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot '../test_stage_run.ps1') @args
