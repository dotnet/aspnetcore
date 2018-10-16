#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tags the given TeamCity build with the given tag.
.PARAMETER BuildId
    The BuildId of the build to be tagged.
.PARAMETER Tag
    The tag to put on this build.
#>

[cmdletbinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$BuildId,
    [Parameter(Mandatory = $true)]
    [string]$Tag,
    [Parameter(Mandatory = $true)]
    [string]$UserName,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$authInfo = "${UserName}:$Password"
$authEncoded = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($authInfo))
$basicAuthValue = "Basic $authEncoded"

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Authorization", $basicAuthValue)
$headers.Add("Content-Type", "text/plain")

$uri = "http://aspnetci/app/rest/builds/$BuildId/tags/"

Invoke-WebRequest -Uri $uri -Method 'POST' -Headers $headers -Body $Tag -ErrorAction SilentlyContinue
