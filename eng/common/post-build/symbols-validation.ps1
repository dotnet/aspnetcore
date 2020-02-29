param(
  [Parameter(Mandatory=$true)][string] $InputPath,              # Full path to directory where NuGet packages to be checked are stored
  [Parameter(Mandatory=$true)][string] $ExtractPath,            # Full path to directory where the packages will be extracted during validation
  [Parameter(Mandatory=$true)][string] $DotnetSymbolVersion     # Version of dotnet symbol to use
)

function FirstMatchingSymbolDescriptionOrDefault {
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

function CountMissingSymbols {
  param( 
    [string] $PackagePath          # Path to a NuGet package
  )

  # Ensure input file exist
  if (!(Test-Path $PackagePath)) {
    Write-PipelineTaskError "Input file does not exist: $PackagePath"
    ExitWithExitCode 1
  }
  
  # Extensions for which we'll look for symbols
  $RelevantExtensions = @('.dll', '.exe', '.so', '.dylib')

  # How many files are missing symbol information
  $MissingSymbols = 0

  $PackageId = [System.IO.Path]::GetFileNameWithoutExtension($PackagePath)
  $PackageGuid = New-Guid
  $ExtractPath = Join-Path -Path $ExtractPath -ChildPath $PackageGuid
  $SymbolsPath = Join-Path -Path $ExtractPath -ChildPath 'Symbols'
  
  [System.IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $ExtractPath)

  Get-ChildItem -Recurse $ExtractPath |
    Where-Object {$RelevantExtensions -contains $_.Extension} |
    ForEach-Object {
      if ($_.FullName -Match '\\ref\\') {
        Write-Host "`t Ignoring reference assembly file " $_.FullName
        return
      }

      $SymbolsOnMSDL = FirstMatchingSymbolDescriptionOrDefault $_.FullName '--microsoft-symbol-server' $SymbolsPath
      $SymbolsOnSymWeb = FirstMatchingSymbolDescriptionOrDefault $_.FullName '--internal-server' $SymbolsPath

      Write-Host -NoNewLine "`t Checking file " $_.FullName "... "
  
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
  
  Pop-Location

  return $MissingSymbols
}

function CheckSymbolsAvailable {
  if (Test-Path $ExtractPath) {
    Remove-Item $ExtractPath -Force  -Recurse -ErrorAction SilentlyContinue
  }

  Get-ChildItem "$InputPath\*.nupkg" |
    ForEach-Object {
      $FileName = $_.Name

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
      $Status = CountMissingSymbols "$InputPath\$FileName"

      if ($Status -ne 0) {
        Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "Missing symbols for $Status modules in the package $FileName"
        ExitWithExitCode $exitCode
      }

      Write-Host
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

  Add-Type -AssemblyName System.IO.Compression.FileSystem
  
  InstallDotnetSymbol

  CheckSymbolsAvailable
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'CheckSymbols' -Message $_
  ExitWithExitCode 1
}
