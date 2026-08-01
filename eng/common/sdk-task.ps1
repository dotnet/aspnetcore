[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = 'Debug',
  [string] $task,
  [string] $verbosity = 'minimal',
  [string] $msbuildEngine = $null,
  # Restore defaults to on; -restore is retained only so existing consumers that pass it don't break. Use -norestore to opt out.
  [switch] $restore = $true,
  [switch] $norestore,
  [switch] $prepareMachine,
  [switch][Alias('nobl')]$excludeCIBinaryLog,
  [switch]$noWarnAsError,
  [switch] $help,
  [string] $runtimeSourceFeed = '',
  [string] $runtimeSourceFeedKey = '',
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

$ci = $true
$binaryLog = if ($excludeCIBinaryLog) { $false } else { $true }
$warnAsError = if ($noWarnAsError) { $false } else { $true }

# Reconcile the restore state before importing tools.ps1: it reads $restore at import time to
# decide whether toolset/SDK acquisition installs. -norestore must win so that skipping restore
# also skips toolset initialization, not just the explicit Restore build below.
if ($norestore) { $restore = $false }

# sdk-task runs a standalone Arcade SDK task and does not need repo-specific toolset setup.
# Skip importing configure-toolset.ps1 so its side effects (e.g. a repo's configure-toolset.ps1
# calling exit) don't terminate this script before the task runs.
$disableConfigureToolsetImport = $true

. $PSScriptRoot\tools.ps1

function Print-Usage() {
  Write-Host "Common settings:"
  Write-Host "  -task <value>           Name of Arcade task (name of a project in toolset directory of the Arcade SDK package)"
  Write-Host "  -restore                (Legacy) Restore runs by default; retained for backward compatibility. Use -norestore to skip"
  Write-Host "  -norestore              Skip restoring dependencies"
  Write-Host "  -verbosity <value>      Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]"
  Write-Host "  -help                   Print help and exit"
  Write-Host ""

  Write-Host "Advanced settings:"
  Write-Host "  -prepareMachine         Prepare machine for CI run"
  Write-Host "  -msbuildEngine <value>  Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host "  -excludeCIBinaryLog     When running on CI, allow no binary log (short: -nobl)"
  Write-Host ""
  Write-Host "Command line arguments not listed above are passed thru to msbuild."
}

function Build([string]$target) {
  $logSuffix = if ($target -eq 'Execute') { '' } else { ".$target" }
  $log = Join-Path $LogDir "$task$logSuffix.binlog"
  $binaryLogArg = if ($binaryLog) { "/bl:$log" } else { "" }
  $outputPath = Join-Path $ToolsetDir "$task\"

  MSBuild $taskProject `
    $binaryLogArg `
    /t:$target `
    /p:Configuration=$configuration `
    /p:RepoRoot=$RepoRoot `
    /p:BaseIntermediateOutputPath=$outputPath `
    /v:$verbosity `
    @properties
}

try {
  if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
    Print-Usage
    exit 0
  }

  if ($task -eq "") {
    Write-PipelineTelemetryError -Category 'Build' -Message "Missing required parameter '-task <value>'"
    Print-Usage
    ExitWithExitCode 1
  }

  if( $msbuildEngine -eq "vs") {
    # Ensure desktop MSBuild is available for sdk tasks.
    $global:_MSBuildExe = InitializeVisualStudioMSBuild
  }

  $taskProject = GetSdkTaskProject $task
  if (!(Test-Path $taskProject)) {
    Write-PipelineTelemetryError -Category 'Build' -Message "Unknown task: $task"
    ExitWithExitCode 1
  }

  if ($restore) {
    Build 'Restore'
  }

  Build 'Execute'
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'Build' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0
