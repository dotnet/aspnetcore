param(
  [Parameter(Mandatory=$true)][string] $InputPath,              # Full path to directory where artifact packages are stored
  [Parameter(Mandatory=$true)][string] $ExtractPath            # Full path to directory where the packages will be extracted
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$disableConfigureToolsetImport = $true

function ExtractArtifacts {
  if (!(Test-Path $InputPath)) {
    Write-Host "Input Path does not exist: $InputPath"
    ExitWithExitCode 0
  }
  $Jobs = @()
  Get-ChildItem "$InputPath\*.nupkg" |
    ForEach-Object {
      $Jobs += Start-Job -ScriptBlock $ExtractPackage -ArgumentList $_.FullName
    }

  foreach ($Job in $Jobs) {
    Wait-Job -Id $Job.Id | Receive-Job
  }
}

try {
  # `tools.ps1` checks $ci to perform some actions. Since the SDL
  # scripts don't necessarily execute in the same agent that run the
  # build.ps1/sh script this variable isn't automatically set.
  $ci = $true
  . $PSScriptRoot\..\tools.ps1

  $ExtractPackage = {
    param( 
      [string] $PackagePath                                 # Full path to a NuGet package
    )

    if (!(Test-Path $PackagePath)) {
      Write-PipelineTelemetryError -Category 'Build' -Message "Input file does not exist: $PackagePath"
      ExitWithExitCode 1
    }

    $RelevantExtensions = @('.dll', '.exe', '.pdb')
    Write-Host -NoNewLine 'Extracting ' ([System.IO.Path]::GetFileName($PackagePath)) '...'

    $PackageId = [System.IO.Path]::GetFileNameWithoutExtension($PackagePath)
    $ExtractPath = Join-Path -Path $using:ExtractPath -ChildPath $PackageId

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    [System.IO.Directory]::CreateDirectory($ExtractPath);

    try {
      $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
  
      $zip.Entries | 
      Where-Object {$RelevantExtensions -contains [System.IO.Path]::GetExtension($_.Name)} |
        ForEach-Object {
            $TargetPath = Join-Path -Path $ExtractPath -ChildPath (Split-Path -Path $_.FullName)
            [System.IO.Directory]::CreateDirectory($TargetPath);

            $TargetFile = Join-Path -Path $ExtractPath -ChildPath $_.FullName
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($_, $TargetFile)
          }
    }
    catch {
      Write-Host $_
      Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
      ExitWithExitCode 1
    }
    finally {
      $zip.Dispose() 
    }
  }
  Measure-Command { ExtractArtifacts }
}
catch {
  Write-Host $_
  Write-PipelineTelemetryError -Force -Category 'Sdl' -Message $_
  ExitWithExitCode 1
}
