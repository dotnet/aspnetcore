# This script validates NuGet package metadata information using this 
# tool: https://github.com/NuGet/NuGetGallery/tree/jver-verify/src/VerifyMicrosoftPackage

param(
  [Parameter(Mandatory=$true)][string] $PackagesPath # Path to where the packages to be validated are
)

try {
  & $PSScriptRoot\nuget-verification.ps1 ${PackagesPath}\*.nupkg
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'NuGetValidation' -Message $_
  ExitWithExitCode 1
}
