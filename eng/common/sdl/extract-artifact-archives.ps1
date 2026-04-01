# This script looks for each archive file in a directory and extracts it into the target directory.
# For example, the file "$InputPath/bin.tar.gz" extracts to "$ExtractPath/bin.tar.gz.extracted/**".
# Uses the "tar" utility added to Windows 10 / Windows 2019 that supports tar.gz and zip.
param(
  # Full path to directory where archives are stored.
  [Parameter(Mandatory=$true)][string] $InputPath,
  # Full path to directory to extract archives into. May be the same as $InputPath.
  [Parameter(Mandatory=$true)][string] $ExtractPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$disableConfigureToolsetImport = $true

try {
  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  Measure-Command {
    $jobs = @()

    # Find archive files for non-Windows and Windows builds.
    $archiveFiles = @(
      Get-ChildItem (Join-Path $InputPath "*.tar.gz")
      Get-ChildItem (Join-Path $InputPath "*.zip")
    )

    foreach ($targzFile in $archiveFiles) {
      $jobs += Start-Job -ScriptBlock {
        $file = $using:targzFile
        $fileName = [System.IO.Path]::GetFileName($file)
        $extractDir = Join-Path $using:ExtractPath "$fileName.extracted"

        New-Item $extractDir -ItemType Directory -Force | Out-Null

        Write-Host "Extracting '$file' to '$extractDir'..."

        # Pipe errors to stdout to prevent PowerShell detecting them and quitting the job early.
        # This type of quit skips the catch, so we wouldn't be able to tell which file triggered the
        # error. Save output so it can be stored in the exception string along with context.
        $output = tar -xf $file -C $extractDir 2>&1
        # Handle NZEC manually rather than using Exit-IfNZEC: we are in a background job, so we
        # don't have access to the outer scope.
        if ($LASTEXITCODE -ne 0) {
          throw "Error extracting '$file': non-zero exit code ($LASTEXITCODE). Output: '$output'"
        }

        Write-Host "Extracted to $extractDir"
      }
    }

    Receive-Job $jobs -Wait
  }
}
catch {
  Write-Host $_
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
