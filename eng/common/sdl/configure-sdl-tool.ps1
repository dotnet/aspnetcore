Param(
  [string] $GuardianCliLocation,
  [string] $WorkingDirectory,
  [string] $TargetDirectory,
  [string] $GdnFolder,
  # The list of Guardian tools to configure. For each object in the array:
  # - If the item is a [hashtable], it must contain these entries:
  #   - Name = The tool name as Guardian knows it.
  #   - Scenario = (Optional) Scenario-specific name for this configuration entry. It must be unique
  #     among all tool entries with the same Name.
  #   - Args = (Optional) Array of Guardian tool configuration args, like '@("Target > C:\temp")'
  # - If the item is a [string] $v, it is treated as '@{ Name="$v" }'
  [object[]] $ToolsList,
  [string] $GuardianLoggerLevel='Standard',
  # Optional: Additional params to add to any tool using CredScan.
  [string[]] $CrScanAdditionalRunConfigParams,
  # Optional: Additional params to add to any tool using PoliCheck.
  [string[]] $PoliCheckAdditionalRunConfigParams,
  # Optional: Additional params to add to any tool using CodeQL/Semmle.
  [string[]] $CodeQLAdditionalRunConfigParams,
  # Optional: Additional params to add to any tool using Binskim.
  [string[]] $BinskimAdditionalRunConfigParams
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$disableConfigureToolsetImport = $true
$global:LASTEXITCODE = 0

try {
  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  # Normalize tools list: all in [hashtable] form with defined values for each key.
  $ToolsList = $ToolsList |
    ForEach-Object {
      if ($_ -is [string]) {
        $_ = @{ Name = $_ }
      }

      if (-not ($_['Scenario'])) { $_.Scenario = "" }
      if (-not ($_['Args'])) { $_.Args = @() }
      $_
    }
  
  Write-Host "List of tools to configure:"
  $ToolsList | ForEach-Object { $_ | Out-String | Write-Host }

  # We store config files in the r directory of .gdn
  $gdnConfigPath = Join-Path $GdnFolder 'r'
  $ValidPath = Test-Path $GuardianCliLocation

  if ($ValidPath -eq $False)
  {
    Write-PipelineTelemetryError -Force -Category 'Sdl' -Message "Invalid Guardian CLI Location."
    ExitWithExitCode 1
  }

  foreach ($tool in $ToolsList) {
    # Put together the name and scenario to make a unique key.
    $toolConfigName = $tool.Name
    if ($tool.Scenario) {
      $toolConfigName += "_" + $tool.Scenario
    }

    Write-Host "=== Configuring $toolConfigName..."

    $gdnConfigFile = Join-Path $gdnConfigPath "$toolConfigName-configure.gdnconfig"

    # For some tools, add default and automatic args.
    switch -Exact ($tool.Name) {
      'credscan' {
        if ($targetDirectory) {
          $tool.Args += "`"TargetDirectory < $TargetDirectory`""
        }
        $tool.Args += "`"OutputType < pre`""
        $tool.Args += $CrScanAdditionalRunConfigParams
      }
      'policheck' {
        if ($targetDirectory) {
          $tool.Args += "`"Target < $TargetDirectory`""
        }
        $tool.Args += $PoliCheckAdditionalRunConfigParams
      }
      {$_ -in 'semmle', 'codeql'} {
        if ($targetDirectory) {
          $tool.Args += "`"SourceCodeDirectory < $TargetDirectory`""
        }
        $tool.Args += $CodeQLAdditionalRunConfigParams
      }
      'binskim' {
        if ($targetDirectory) {
          $tool.Args += "`"Target < $TargetDirectory`""
        }
        $tool.Args += $BinskimAdditionalRunConfigParams
      }
    }

    # Create variable pointing to the args array directly so we can use splat syntax later.
    $toolArgs = $tool.Args

    # Configure the tool. If args array is provided or the current tool has some default arguments
    # defined, add "--args" and splat each element on the end. Arg format is "{Arg id} < {Value}",
    # one per parameter. Doc page for "guardian configure":
    # https://dev.azure.com/securitytools/SecurityIntegration/_wiki/wikis/Guardian/1395/configure
    Exec-BlockVerbosely {
      & $GuardianCliLocation configure `
        --working-directory $WorkingDirectory `
        --tool $tool.Name `
        --output-path $gdnConfigFile `
        --logger-level $GuardianLoggerLevel `
        --noninteractive `
        --force `
        $(if ($toolArgs) { "--args" }) @toolArgs
      Exit-IfNZEC "Sdl"
    }

    Write-Host "Created '$toolConfigName' configuration file: $gdnConfigFile"
  }
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
