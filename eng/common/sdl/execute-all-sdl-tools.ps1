Param(
  [string] $GuardianPackageName,                                                                 # Required: the name of guardian CLI package (not needed if GuardianCliLocation is specified)
  [string] $NugetPackageDirectory,                                                               # Required: directory where NuGet packages are installed (not needed if GuardianCliLocation is specified)
  [string] $GuardianCliLocation,                                                                 # Optional: Direct location of Guardian CLI executable if GuardianPackageName & NugetPackageDirectory are not specified
  [string] $Repository=$env:BUILD_REPOSITORY_NAME,                                               # Required: the name of the repository (e.g. dotnet/arcade)
  [string] $BranchName=$env:BUILD_SOURCEBRANCH,                                                  # Optional: name of branch or version of gdn settings; defaults to master
  [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,                                         # Required: the directory where source files are located
  [string] $ArtifactsDirectory = (Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY ('artifacts')),  # Required: the directory where build artifacts are located
  [string] $AzureDevOpsAccessToken,                                                              # Required: access token for dnceng; should be provided via KeyVault

  # Optional: list of SDL tools to run on source code. See 'configure-sdl-tool.ps1' for tools list
  # format.
  [object[]] $SourceToolsList,
  # Optional: list of SDL tools to run on built artifacts. See 'configure-sdl-tool.ps1' for tools
  # list format.
  [object[]] $ArtifactToolsList,
  # Optional: list of SDL tools to run without automatically specifying a target directory. See
  # 'configure-sdl-tool.ps1' for tools list format.
  [object[]] $CustomToolsList,

  [bool] $TsaPublish=$False,                                                                     # Optional: true will publish results to TSA; only set to true after onboarding to TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaBranchName=$env:BUILD_SOURCEBRANCH,                                               # Optional: required for TSA publish; defaults to $(Build.SourceBranchName); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaRepositoryName=$env:BUILD_REPOSITORY_NAME,                                        # Optional: TSA repository name; will be generated automatically if not submitted; TSA is the automated framework used to upload test results as bugs.
  [string] $BuildNumber=$env:BUILD_BUILDNUMBER,                                                  # Optional: required for TSA publish; defaults to $(Build.BuildNumber)
  [bool] $UpdateBaseline=$False,                                                                 # Optional: if true, will update the baseline in the repository; should only be run after fixing any issues which need to be fixed
  [bool] $TsaOnboard=$False,                                                                     # Optional: if true, will onboard the repository to TSA; should only be run once; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaInstanceUrl,                                                                      # Optional: only needed if TsaOnboard or TsaPublish is true; the instance-url registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaCodebaseName,                                                                     # Optional: only needed if TsaOnboard or TsaPublish is true; the name of the codebase registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaProjectName,                                                                      # Optional: only needed if TsaOnboard or TsaPublish is true; the name of the project registered with TSA; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaNotificationEmail,                                                                # Optional: only needed if TsaOnboard is true; the email(s) which will receive notifications of TSA bug filings (e.g. alias@microsoft.com); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaCodebaseAdmin,                                                                    # Optional: only needed if TsaOnboard is true; the aliases which are admins of the TSA codebase (e.g. DOMAIN\alias); TSA is the automated framework used to upload test results as bugs.
  [string] $TsaBugAreaPath,                                                                      # Optional: only needed if TsaOnboard is true; the area path where TSA will file bugs in AzDO; TSA is the automated framework used to upload test results as bugs.
  [string] $TsaIterationPath,                                                                    # Optional: only needed if TsaOnboard is true; the iteration path where TSA will file bugs in AzDO; TSA is the automated framework used to upload test results as bugs.
  [string] $GuardianLoggerLevel='Standard',                                                      # Optional: the logger level for the Guardian CLI; options are Trace, Verbose, Standard, Warning, and Error
  [string[]] $CrScanAdditionalRunConfigParams,                                                   # Optional: Additional Params to custom build a CredScan run config in the format @("xyz:abc","sdf:1")
  [string[]] $PoliCheckAdditionalRunConfigParams,                                                # Optional: Additional Params to custom build a Policheck run config in the format @("xyz:abc","sdf:1")
  [string[]] $CodeQLAdditionalRunConfigParams,                                                   # Optional: Additional Params to custom build a Semmle/CodeQL run config in the format @("xyz < abc","sdf < 1")
  [bool] $BreakOnFailure=$False                                                                  # Optional: Fail the build if there were errors during the run
)

try {
  $ErrorActionPreference = 'Stop'
  Set-StrictMode -Version 2.0
  $disableConfigureToolsetImport = $true
  $global:LASTEXITCODE = 0

  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  #Replace repo names to the format of org/repo
  if (!($Repository.contains('/'))) {
    $RepoName = $Repository -replace '(.*?)-(.*)', '$1/$2';
  }
  else{
    $RepoName = $Repository;
  }

  if ($GuardianPackageName) {
    $guardianCliLocation = Join-Path $NugetPackageDirectory (Join-Path $GuardianPackageName (Join-Path 'tools' 'guardian.cmd'))
  } else {
    $guardianCliLocation = $GuardianCliLocation
  }

  $workingDirectory = (Split-Path $SourceDirectory -Parent)
  $ValidPath = Test-Path $guardianCliLocation

  if ($ValidPath -eq $False)
  {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message 'Invalid Guardian CLI Location.'
    ExitWithExitCode 1
  }

  Exec-BlockVerbosely {
    & $(Join-Path $PSScriptRoot 'init-sdl.ps1') -GuardianCliLocation $guardianCliLocation -Repository $RepoName -BranchName $BranchName -WorkingDirectory $workingDirectory -AzureDevOpsAccessToken $AzureDevOpsAccessToken -GuardianLoggerLevel $GuardianLoggerLevel
  }
  $gdnFolder = Join-Path $workingDirectory '.gdn'

  if ($TsaOnboard) {
    if ($TsaCodebaseName -and $TsaNotificationEmail -and $TsaCodebaseAdmin -and $TsaBugAreaPath) {
      Exec-BlockVerbosely {
        & $guardianCliLocation tsa-onboard --codebase-name "$TsaCodebaseName" --notification-alias "$TsaNotificationEmail" --codebase-admin "$TsaCodebaseAdmin" --instance-url "$TsaInstanceUrl" --project-name "$TsaProjectName" --area-path "$TsaBugAreaPath" --iteration-path "$TsaIterationPath" --working-directory $workingDirectory --logger-level $GuardianLoggerLevel
      }
      if ($LASTEXITCODE -ne 0) {
        Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Guardian tsa-onboard failed with exit code $LASTEXITCODE."
        ExitWithExitCode $LASTEXITCODE
      }
    } else {
      Write-PipelineTelemetryError -Force -Category 'Sdl' -Message 'Could not onboard to TSA -- not all required values ($TsaCodebaseName, $TsaNotificationEmail, $TsaCodebaseAdmin, $TsaBugAreaPath) were specified.'
      ExitWithExitCode 1
    }
  }

  # Configure a list of tools with a default target directory. Populates the ".gdn/r" directory.
  function Configure-ToolsList([object[]] $tools, [string] $targetDirectory) {
    if ($tools -and $tools.Count -gt 0) {
      Exec-BlockVerbosely {
        & $(Join-Path $PSScriptRoot 'configure-sdl-tool.ps1') `
          -GuardianCliLocation $guardianCliLocation `
          -WorkingDirectory $workingDirectory `
          -TargetDirectory $targetDirectory `
          -GdnFolder $gdnFolder `
          -ToolsList $tools `
          -AzureDevOpsAccessToken $AzureDevOpsAccessToken `
          -GuardianLoggerLevel $GuardianLoggerLevel `
          -CrScanAdditionalRunConfigParams $CrScanAdditionalRunConfigParams `
          -PoliCheckAdditionalRunConfigParams $PoliCheckAdditionalRunConfigParams `
          -CodeQLAdditionalRunConfigParams $CodeQLAdditionalRunConfigParams
        if ($BreakOnFailure) {
          Exit-IfNZEC "Sdl"
        }
      }
    }
  }

  # Configure Artifact and Source tools with default Target directories.
  Configure-ToolsList $ArtifactToolsList $ArtifactsDirectory
  Configure-ToolsList $SourceToolsList $SourceDirectory
  # Configure custom tools with no default Target directory.
  Configure-ToolsList $CustomToolsList $null

  # At this point, all tools are configured in the ".gdn" directory. Run them all in a single call.
  # (If we used "run" multiple times, each run would overwrite data from earlier runs.)
  Exec-BlockVerbosely {
    & $(Join-Path $PSScriptRoot 'run-sdl.ps1') `
      -GuardianCliLocation $guardianCliLocation `
      -WorkingDirectory $workingDirectory `
      -UpdateBaseline $UpdateBaseline `
      -GdnFolder $gdnFolder
  }

  if ($TsaPublish) {
    if ($TsaBranchName -and $BuildNumber) {
      if (-not $TsaRepositoryName) {
        $TsaRepositoryName = "$($Repository)-$($BranchName)"
      }
      Exec-BlockVerbosely {
        & $guardianCliLocation tsa-publish --all-tools --repository-name "$TsaRepositoryName" --branch-name "$TsaBranchName" --build-number "$BuildNumber" --onboard $True --codebase-name "$TsaCodebaseName" --notification-alias "$TsaNotificationEmail" --codebase-admin "$TsaCodebaseAdmin" --instance-url "$TsaInstanceUrl" --project-name "$TsaProjectName" --area-path "$TsaBugAreaPath" --iteration-path "$TsaIterationPath" --working-directory $workingDirectory  --logger-level $GuardianLoggerLevel
      }
      if ($LASTEXITCODE -ne 0) {
        Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Guardian tsa-publish failed with exit code $LASTEXITCODE."
        ExitWithExitCode $LASTEXITCODE
      }
    } else {
      Write-PipelineTelemetryError -Force -Category 'Sdl' -Message 'Could not publish to TSA -- not all required values ($TsaBranchName, $BuildNumber) were specified.'
      ExitWithExitCode 1
    }
  }

  if ($BreakOnFailure) {
    Write-Host "Failing the build in case of breaking results..."
    Exec-BlockVerbosely {
      & $guardianCliLocation break --working-directory $workingDirectory --logger-level $GuardianLoggerLevel
    }
  } else {
    Write-Host "Letting the build pass even if there were breaking results..."
  }
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  exit 1
}
