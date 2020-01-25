# This script validates NuGet package metadata information using this 
# tool: https://github.com/NuGet/NuGetGallery/tree/jver-verify/src/VerifyMicrosoftPackage

param(
  [Parameter(Mandatory=$true)][string] $PackagesPath,           # Path to where the packages to be validated are
  [Parameter(Mandatory=$true)][string] $ToolDestinationPath     # Where the validation tool should be downloaded to
)

try {
  . $PSScriptRoot\post-build-utils.ps1

  $url = 'https://raw.githubusercontent.com/NuGet/NuGetGallery/jver-verify/src/VerifyMicrosoftPackage/verify.ps1'

  New-Item -ItemType 'directory' -Path ${ToolDestinationPath} -Force

  Invoke-WebRequest $url -OutFile ${ToolDestinationPath}\verify.ps1 

  & ${ToolDestinationPath}\verify.ps1 ${PackagesPath}\*.nupkg
} 
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'NuGetValidation' -Message $_
  ExitWithExitCode 1
}
