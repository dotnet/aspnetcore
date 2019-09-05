Param(
  [string] $GuardianPackageName,                                                           # Required: the name of guardian CLI package (not needed if GuardianCliLocation is specified)
  [string] $NugetPackageDirectory,                                                         # Required: directory where NuGet packages are installed (not needed if GuardianCliLocation is specified)
  [string] $GuardianCliLocation,                                                           # Optional: Direct location of Guardian CLI executable if GuardianPackageName & NugetPackageDirectory are not specified
  [string] $Repository=$env:BUILD_REPOSITORY_NAME,                                         # Required: the name of the repository (e.g. dotnet/arcade)
  [string] $BranchName=$env:BUILD_SOURCEBRANCH,                                            # Optional: name of branch or version of gdn settings; defaults to master
  [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,                                   # Required: the directory where source files are located
  [string] $ArtifactsDirectory = (Join-Path $env:BUILD_SOURCESDIRECTORY ("artifacts")),    # Required: the directory where build artifacts are located
  [string] $AzureDevOpsAccessToken,                                                        # Required: access token for dnceng; should be provided via KeyVault
  [string[]] $SourceToolsList,                                                             # Optional: list of SDL tools to run on source code
  [string[]] $ArtifactToolsList,                                                           # Optional: list of SDL tools to run on built artifacts
  [bool] $TsaPublish=$False,                                                               # Optional: true will publish results to TSA; only set to true after onboarding to TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaBranchName=$env:BUILD_SOURCEBRANCH,                                         # Optional: required for TSA publish; defaults to $(Build.SourceBranchName); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaRepositoryName=$env:BUILD_REPOSITORY_NAME,                                  # Optional: TSA repository name; will be generated automatically if not submitted; TSA is the automated framework used to upload test results as bugs.
  [string] $BuildNumber=$env:BUILD_BUILDNUMBER,                                            # Optional: required for TSA publish; defaults to $(Build.BuildNumber)
  [bool] $UpdateBaseline=$False,                                                           # Optional: if true, will update the baseline in the repository; should only be run after fixing any issues which need to be fixed
  [bool] $TsaOnboard=$False,                                                               # Optional: if true, will onboard the repository to TSA; should only be run once; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaInstanceUrl,                                                                # Optional: only needed if TsaOnboard or TsaPublish is true; the instance-url registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaCodebaseName,                                                               # Optional: only needed if TsaOnboard or TsaPublish is true; the name of the codebase registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaProjectName,                                                                # Optional: only needed if TsaOnboard or TsaPublish is true; the name of the project registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaNotificationEmail,                                                          # Optional: only needed if TsaOnboard is true; the email(s) which will receive notifications of TSA bug filings (e.g. alias@microsoft.com); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaCodebaseAdmin,                                                              # Optional: only needed if TsaOnboard is true; the aliases which are admins of the TSA codebase (e.g. DOMAIN\alias); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaBugAreaPath,                                                                # Optional: only needed if TsaOnboard is true; the area path where TSA will file bugs in AzDO; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaIterationPath,                                                              # Optional: only needed if TsaOnboard is true; the iteration path where TSA will file bugs in AzDO; TSA is the automated framework used to upload test results as bugs.
  [string] $GuardianLoggerLevel="Standard",                                                # Optional: the logger level for the Guardian CLI; options are Trace, Verbose, Standard, Warning, and Error
  [string[]] $CrScanAdditionalRunConfigParams,                                             # Optional: Additional Params to custom build a CredScan run config in the format @("xyz:abc","sdf:1")
  [string[]] $PoliCheckAdditionalRunConfigParams                                           # Optional: Additional Params to custom build a Policheck run config in the format @("xyz:abc","sdf:1")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$LASTEXITCODE = 0

#Replace repo names to the format of org/repo
if (!($Repository.contains('/'))) {
  $RepoName = $Repository -replace '(.*?)-(.*)', '$1/$2';
}
else{
  $RepoName = $Repository;
}

if ($GuardianPackageName) {
  $guardianCliLocation = Join-Path $NugetPackageDirectory (Join-Path $GuardianPackageName (Join-Path "tools" "guardian.cmd"))
} else {
  $guardianCliLocation = $GuardianCliLocation
}

$ValidPath = Test-Path $guardianCliLocation

if ($ValidPath -eq $False)
{
  Write-Host "Invalid Guardian CLI Location."
  exit 1
}

& $(Join-Path $PSScriptRoot "init-sdl.ps1") -GuardianCliLocation $guardianCliLocation -Repository $RepoName -BranchName $BranchName -WorkingDirectory (Split-Path $SourceDirectory -Parent) -AzureDevOpsAccessToken $AzureDevOpsAccessToken -GuardianLoggerLevel $GuardianLoggerLevel
$gdnFolder = Join-Path (Split-Path $SourceDirectory -Parent) ".gdn"

if ($TsaOnboard) {
  if ($TsaCodebaseName -and $TsaNotificationEmail -and $TsaCodebaseAdmin -and $TsaBugAreaPath) {
    Write-Host "$guardianCliLocation tsa-onboard --codebase-name `"$TsaCodebaseName`" --notification-alias `"$TsaNotificationEmail`" --codebase-admin `"$TsaCodebaseAdmin`" --instance-url `"$TsaInstanceUrl`" --project-name `"$TsaProjectName`" --area-path `"$TsaBugAreaPath`" --iteration-path `"$TsaIterationPath`" --working-directory $ArtifactsDirectory --logger-level $GuardianLoggerLevel"
    & $guardianCliLocation tsa-onboard --codebase-name "$TsaCodebaseName" --notification-alias "$TsaNotificationEmail" --codebase-admin "$TsaCodebaseAdmin" --instance-url "$TsaInstanceUrl" --project-name "$TsaProjectName" --area-path "$TsaBugAreaPath" --iteration-path "$TsaIterationPath" --working-directory $ArtifactsDirectory --logger-level $GuardianLoggerLevel
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian tsa-onboard failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  } else {
    Write-Host "Could not onboard to TSA -- not all required values ($$TsaCodebaseName, $$TsaNotificationEmail, $$TsaCodebaseAdmin, $$TsaBugAreaPath) were specified."
    exit 1
  }
}

if ($ArtifactToolsList -and $ArtifactToolsList.Count -gt 0) {
  & $(Join-Path $PSScriptRoot "run-sdl.ps1") -GuardianCliLocation $guardianCliLocation -WorkingDirectory $ArtifactsDirectory -TargetDirectory $ArtifactsDirectory -GdnFolder $gdnFolder -ToolsList $ArtifactToolsList -AzureDevOpsAccessToken $AzureDevOpsAccessToken -UpdateBaseline $UpdateBaseline -GuardianLoggerLevel $GuardianLoggerLevel -CrScanAdditionalRunConfigParams $CrScanAdditionalRunConfigParams -PoliCheckAdditionalRunConfigParams $PoliCheckAdditionalRunConfigParams
}
if ($SourceToolsList -and $SourceToolsList.Count -gt 0) {
  & $(Join-Path $PSScriptRoot "run-sdl.ps1") -GuardianCliLocation $guardianCliLocation -WorkingDirectory $ArtifactsDirectory -TargetDirectory $SourceDirectory -GdnFolder $gdnFolder -ToolsList $SourceToolsList -AzureDevOpsAccessToken $AzureDevOpsAccessToken -UpdateBaseline $UpdateBaseline -GuardianLoggerLevel $GuardianLoggerLevel -CrScanAdditionalRunConfigParams $CrScanAdditionalRunConfigParams -PoliCheckAdditionalRunConfigParams $PoliCheckAdditionalRunConfigParams
}

if ($UpdateBaseline) {
  & (Join-Path $PSScriptRoot "push-gdn.ps1") -Repository $RepoName -BranchName $BranchName -GdnFolder $GdnFolder -AzureDevOpsAccessToken $AzureDevOpsAccessToken -PushReason "Update baseline"
}

if ($TsaPublish) {
  if ($TsaBranchName -and $BuildNumber) {
    if (-not $TsaRepositoryName) {
      $TsaRepositoryName = "$($Repository)-$($BranchName)"
    }
    Write-Host "$guardianCliLocation tsa-publish --all-tools --repository-name `"$TsaRepositoryName`" --branch-name `"$TsaBranchName`" --build-number `"$BuildNumber`" --codebase-name `"$TsaCodebaseName`" --notification-alias `"$TsaNotificationEmail`" --codebase-admin `"$TsaCodebaseAdmin`" --instance-url `"$TsaInstanceUrl`" --project-name `"$TsaProjectName`" --area-path `"$TsaBugAreaPath`" --iteration-path `"$TsaIterationPath`" --working-directory $ArtifactsDirectory --logger-level $GuardianLoggerLevel"
    & $guardianCliLocation tsa-publish --all-tools --repository-name "$TsaRepositoryName" --branch-name "$TsaBranchName" --build-number "$BuildNumber" --onboard $True --codebase-name "$TsaCodebaseName" --notification-alias "$TsaNotificationEmail" --codebase-admin "$TsaCodebaseAdmin" --instance-url "$TsaInstanceUrl" --project-name "$TsaProjectName" --area-path "$TsaBugAreaPath" --iteration-path "$TsaIterationPath" --working-directory $ArtifactsDirectory  --logger-level $GuardianLoggerLevel
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Guardian tsa-publish failed with exit code $LASTEXITCODE."
      exit $LASTEXITCODE
    }
  } else {
    Write-Host "Could not publish to TSA -- not all required values ($$TsaBranchName, $$BuildNumber) were specified."
    exit 1
  }
}
