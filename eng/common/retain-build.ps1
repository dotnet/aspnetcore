
Param(
[Parameter(Mandatory=$true)][int] $buildId,
[Parameter(Mandatory=$true)][string] $azdoOrgUri, 
[Parameter(Mandatory=$true)][string] $azdoProject,
[Parameter(Mandatory=$true)][string] $token
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
. $PSScriptRoot\tools.ps1


function Get-AzDOHeaders(
    [string] $token)
{
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":${token}"))
    $headers = @{"Authorization"="Basic $base64AuthInfo"}
    return $headers
}

function Update-BuildRetention(
    [string] $azdoOrgUri,
    [string] $azdoProject,
    [int] $buildId,
    [string] $token)
{
    $headers = Get-AzDOHeaders -token $token
    $requestBody = "{
        `"keepForever`": `"true`"
    }"

    $requestUri = "${azdoOrgUri}/${azdoProject}/_apis/build/builds/${buildId}?api-version=6.0"
    write-Host "Attempting to retain build using the following URI: ${requestUri} ..."

    try {
        Invoke-RestMethod -Uri $requestUri -Method Patch -Body $requestBody -Header $headers -contentType "application/json"
        Write-Host "Updated retention settings for build ${buildId}."
    }
    catch {
        Write-PipelineTelemetryError -Category "Build" -Message "Failed to update retention settings for build: $_.Exception.Response.StatusDescription"
        ExitWithExitCode 1
    }
}

Update-BuildRetention -azdoOrgUri $azdoOrgUri -azdoProject $azdoProject -buildId $buildId -token $token
ExitWithExitCode 0
