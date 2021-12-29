param (
    [Parameter(Mandatory = $true)][string]$filePath
)

Clear-Host

. (Join-Path $PSScriptRoot "./build-utils.ps1")

$projectFileInfo = Get-ProjectFile $filePath

if ($projectFileInfo) {
    Write-Host "Building" $projectFileInfo -ForegroundColor Magenta
    . (Join-Path $PSScriptRoot "../../.dotnet/dotnet") "build" $projectFileInfo
}
else {
    Write-Host "Project file not found." -ForegroundColor Red
}
