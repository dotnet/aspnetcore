# This script validates NuGet package metadata information using this 
# tool: https://github.com/NuGet/NuGetGallery/tree/jver-verify/src/VerifyMicrosoftPackage

param(
  [Parameter(Mandatory=$true)][string] $PackagesPath # Path to where the packages to be validated are
)

# `tools.ps1` checks $ci to perform some actions. Since the post-build
# scripts don't necessarily execute in the same agent that run the
# build.ps1/sh script this variable isn't automatically set.
$ci = $true
$disableConfigureToolsetImport = $true
. $PSScriptRoot\..\tools.ps1

try {
  & $PSScriptRoot\nuget-verification.ps1 ${PackagesPath}\*.nupkg
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'NuGetValidation' -Message $_
  ExitWithExitCode 1
}
