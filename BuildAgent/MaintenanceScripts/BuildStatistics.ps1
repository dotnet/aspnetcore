[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ciServer,
    [Parameter(Mandatory=$true)][string]$ciUsername,
    [Parameter(Mandatory=$true)][string]$ciPassword
)

Push-Location $PSScriptRoot/TeamCityApi.Console

& dotnet restore
& dotnet run all-statistics --ci-server $ciServer --ci-username $ciUsername --ci-password $ciPassword

Pop-Location
