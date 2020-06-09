param(
  [Parameter(Mandatory=$true)][string] $InputPath,              # Full path to directory where NuGet packages to be checked are stored
  [Parameter(Mandatory=$true)][string] $ExtractPath,            # Full path to directory where the packages will be extracted during validation
  [Parameter(Mandatory=$true)][string] $DotnetSymbolVersion,    # Version of dotnet symbol to use
  [Parameter(Mandatory=$false)][switch] $ContinueOnError,       # If we should keep checking symbols after an error
  [Parameter(Mandatory=$false)][switch] $Clean                  # Clean extracted symbols directory after checking symbols
)

# Maximum number of jobs to run in parallel
$MaxParallelJobs = 6

# Wait time between check for system load
$SecondsBetweenLoadChecks = 10

$CountMissingSymbols = {
  param( 
    [string] $PackagePath          # Path to a NuGet package
  )

  . $using:PSScriptRoot\..\tools.ps1

  Add-Type -AssemblyName System.IO.Compression.FileSystem

  # Ensure input file exist
  if (!(Test-Path $PackagePath)) {
    Write-PipelineTaskError "Input file does not exist: $PackagePath"
    return 1
  }
  
  # Extensions for which we'll look for symbols
  $RelevantExtensions = @('.dll', '.exe', '.so', '.dylib')

  # How many files are missing symbol information
  $MissingSymbols = 0

  $PackageId = [System.IO.Path]::GetFileNameWithoutExtension($PackagePath)
  $PackageGuid = New-Guid
  $ExtractPath = Join-Path -Path $using:ExtractPath -ChildPath $PackageGuid
  $SymbolsPath = Join-Path -Path $ExtractPath -ChildPath 'Symbols'
  
  try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $ExtractPath)
  }
  catch {
    Write-Host "Something went wrong extracting $PackagePath"
    Write-Host $_
    return -1
  }

  Get-ChildItem -Recurse $ExtractPath |
    Where-Object {$RelevantExtensions -contains $_.Extension} |
    ForEach-Object {
      $FileName = $_.FullName
      if ($FileName -Match '\\ref\\') {
        Write-Host "`t Ignoring reference assembly file " $FileName
        return
      }

      $FirstMatchingSymbolDescriptionOrDefault = {
      param( 
        [string] $FullPath,                  # Full path to the module that has to be checked
        [string] $TargetServerParam,         # Parameter to pass to `Symbol Tool` indicating the server to lookup for symbols
        [string] $SymbolsPath
      )

      $FileName = [System.IO.Path]::GetFileName($FullPath)
      $Extension = [System.IO.Path]::GetExtension($FullPath)

      # Those below are potential symbol files that the `dotnet symbol` might
      # return. Which one will be returned depend on the type of file we are
      # checking and which type of file was uploaded.

      # The file itself is returned
      $SymbolPath = $SymbolsPath + '\' + $FileName

      # PDB file for the module
      $PdbPath = $SymbolPath.Replace($Extension, '.pdb')

      # PDB file for R2R module (created by crossgen)
      $NGenPdb = $SymbolPath.Replace($Extension, '.ni.pdb')

      # DBG file for a .so library
      $SODbg = $SymbolPath.Replace($Extension, '.so.dbg')

      # DWARF file for a .dylib
      $DylibDwarf = $SymbolPath.Replace($Extension, '.dylib.dwarf')
    
      $dotnetSymbolExe = "$env:USERPROFILE\.dotnet\tools"
      $dotnetSymbolExe = Resolve-Path "$dotnetSymbolExe\dotnet-symbol.exe"

      & $dotnetSymbolExe --symbols --modules --windows-pdbs $TargetServerParam $FullPath -o $SymbolsPath | Out-Null

      if (Test-Path $PdbPath) {
        return 'PDB'
      }
      elseif (Test-Path $NGenPdb) {
        return 'NGen PDB'
      }
      elseif (Test-Path $SODbg) {
        return 'DBG for SO'
      }  
      elseif (Test-Path $DylibDwarf) {
        return 'Dwarf for Dylib'
      }  
      elseif (Test-Path $SymbolPath) {
        return 'Module'
      }
      else {
        return $null
      }
    }

      $SymbolsOnMSDL = & $FirstMatchingSymbolDescriptionOrDefault $FileName '--microsoft-symbol-server' $SymbolsPath
      $SymbolsOnSymWeb = & $FirstMatchingSymbolDescriptionOrDefault $FileName '--internal-server' $SymbolsPath

      Write-Host -NoNewLine "`t Checking file " $FileName "... "
  
      if ($SymbolsOnMSDL -ne $null -and $SymbolsOnSymWeb -ne $null) {
        Write-Host "Symbols found on MSDL ($SymbolsOnMSDL) and SymWeb ($SymbolsOnSymWeb)"
      }
      else {
        $MissingSymbols++

        if ($SymbolsOnMSDL -eq $null -and $SymbolsOnSymWeb -eq $null) {
          Write-Host 'No symbols found on MSDL or SymWeb!'
        }
        else {
          if ($SymbolsOnMSDL -eq $null) {
            Write-Host 'No symbols found on MSDL!'
          }
          else {
            Write-Host 'No symbols found on SymWeb!'
          }
        }
      }
    }
  
  if ($using:Clean) {
    Remove-Item $ExtractPath -Recurse -Force
  }

  if ($MissingSymbols -ne 0)
  {
    Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "Missing symbols for $MissingSymbols modules in the package $FileName"
  }
  
  Pop-Location

  return $MissingSymbols
}

function CheckSymbolsAvailable {
  if (Test-Path $ExtractPath) {
    Remove-Item $ExtractPath -Force  -Recurse -ErrorAction SilentlyContinue
  }

  $TotalFailures = 0

  Get-ChildItem "$InputPath\*.nupkg" |
    ForEach-Object {
      $FileName = $_.Name
      $FullName = $_.FullName

      # These packages from Arcade-Services include some native libraries that
      # our current symbol uploader can't handle. Below is a workaround until
      # we get issue: https://github.com/dotnet/arcade/issues/2457 sorted.
      if ($FileName -Match 'Microsoft\.DotNet\.Darc\.') {
        Write-Host "Ignoring Arcade-services file: $FileName"
        Write-Host
        return
      }
      elseif ($FileName -Match 'Microsoft\.DotNet\.Maestro\.Tasks\.') {
        Write-Host "Ignoring Arcade-services file: $FileName"
        Write-Host
        return
      }

      Write-Host "Validating $FileName "

      Start-Job -ScriptBlock $CountMissingSymbols -ArgumentList $FullName | Out-Null

      $NumJobs = @(Get-Job -State 'Running').Count
      Write-Host $NumJobs

      while ($NumJobs -ge $MaxParallelJobs) {
        Write-Host "There are $NumJobs validation jobs running right now. Waiting $SecondsBetweenLoadChecks seconds to check again."
        sleep $SecondsBetweenLoadChecks
        $NumJobs = @(Get-Job -State 'Running').Count
      }

      foreach ($Job in @(Get-Job -State 'Completed')) {
        Receive-Job -Id $Job.Id
      }
      Write-Host
    }

  foreach ($Job in @(Get-Job)) {
    $jobResult = Wait-Job -Id $Job.Id | Receive-Job

    if ($jobResult -ne '0') {
      $TotalFailures++
    }
  }

  if ($TotalFailures -gt 0) {
    Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "Symbols missing for $TotalFailures packages"
    ExitWithExitCode 1
  }
  else {
    Write-Host "All symbols validated!"
  }
}

function InstallDotnetSymbol {
  $dotnetSymbolPackageName = 'dotnet-symbol'

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list --global

  if (($toolList -like "*$dotnetSymbolPackageName*") -and ($toolList -like "*$dotnetSymbolVersion*")) {
    Write-Host "dotnet-symbol version $dotnetSymbolVersion is already installed."
  }
  else {
    Write-Host "Installing dotnet-symbol version $dotnetSymbolVersion..."
    Write-Host 'You may need to restart your command window if this is the first dotnet tool you have installed.'
    & "$dotnet" tool install $dotnetSymbolPackageName --version $dotnetSymbolVersion --verbosity "minimal" --global
  }
}

try {
  . $PSScriptRoot\post-build-utils.ps1
  
  InstallDotnetSymbol

  foreach ($Job in @(Get-Job)) {
    Remove-Job -Id $Job.Id
  }

  CheckSymbolsAvailable
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'CheckSymbols' -Message $_
  ExitWithExitCode 1
}
