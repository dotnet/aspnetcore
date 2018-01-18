[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ciServer,
    [Parameter(Mandatory = $true)][string]$ciUsername,
    [Parameter(Mandatory = $true)][string]$ciPassword
)

& ./run.ps1 install-tools

Push-Location $PSScriptRoot/TeamCityApi.Console
try {
    & dotnet restore
    & dotnet run all-statistics --ci-server $ciServer --ci-username $ciUsername --ci-password $ciPassword
}
finally {
    Pop-Location
}
