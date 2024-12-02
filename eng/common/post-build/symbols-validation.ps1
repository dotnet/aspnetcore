param(
  [Parameter(Mandatory = $true)][string] $InputPath, # Full path to directory where NuGet packages to be checked are stored
  [Parameter(Mandatory = $true)][string] $ExtractPath, # Full path to directory where the packages will be extracted during validation
  [Parameter(Mandatory = $true)][string] $DotnetSymbolVersion, # Version of dotnet symbol to use
  [Parameter(Mandatory = $false)][switch] $CheckForWindowsPdbs, # If we should check for the existence of windows pdbs in addition to portable PDBs
  [Parameter(Mandatory = $false)][switch] $ContinueOnError, # If we should keep checking symbols after an error
  [Parameter(Mandatory = $false)][switch] $Clean,           # Clean extracted symbols directory after checking symbols
  [Parameter(Mandatory = $false)][string] $SymbolExclusionFile  # Exclude the symbols in the file from publishing to symbol server
)

. $PSScriptRoot\..\tools.ps1
# Maximum number of jobs to run in parallel
$MaxParallelJobs = 16

# Max number of retries
$MaxRetry = 5

# Wait time between check for system load
$SecondsBetweenLoadChecks = 10

# Set error codes
Set-Variable -Name "ERROR_BADEXTRACT" -Option Constant -Value -1
Set-Variable -Name "ERROR_FILEDOESNOTEXIST" -Option Constant -Value -2

$WindowsPdbVerificationParam = ""
if ($CheckForWindowsPdbs) {
  $WindowsPdbVerificationParam = "--windows-pdbs"
}

$ExclusionSet = New-Object System.Collections.Generic.HashSet[string];

if (!$InputPath -or !(Test-Path $InputPath)){
  Write-Host "No symbols to validate."
  ExitWithExitCode 0
}

#Check if the path exists
if ($SymbolExclusionFile -and (Test-Path $SymbolExclusionFile)){
  [string[]]$Exclusions = Get-Content "$SymbolExclusionFile"
  $Exclusions | foreach { if($_ -and $_.Trim()){$ExclusionSet.Add($_)} }
}
else{
  Write-Host "Symbol Exclusion file does not exists. No symbols to exclude."
}

$CountMissingSymbols = {
  param( 
    [string] $PackagePath, # Path to a NuGet package
    [string] $WindowsPdbVerificationParam # If we should check for the existence of windows pdbs in addition to portable PDBs
  )

  Add-Type -AssemblyName System.IO.Compression.FileSystem

  Write-Host "Validating $PackagePath "

  # Ensure input file exist
  if (!(Test-Path $PackagePath)) {
    Write-PipelineTaskError "Input file does not exist: $PackagePath"
    return [pscustomobject]@{
      result      = $using:ERROR_FILEDOESNOTEXIST
      packagePath = $PackagePath
    }
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
    return [pscustomobject]@{
      result      = $using:ERROR_BADEXTRACT
      packagePath = $PackagePath
    }
  }

  Get-ChildItem -Recurse $ExtractPath |
  Where-Object { $RelevantExtensions -contains $_.Extension } |
  ForEach-Object {
    $FileName = $_.FullName
    if ($FileName -Match '\\ref\\') {
      Write-Host "`t Ignoring reference assembly file " $FileName
      return
    }

    $FirstMatchingSymbolDescriptionOrDefault = {
      param( 
        [string] $FullPath, # Full path to the module that has to be checked
        [string] $TargetServerParam, # Parameter to pass to `Symbol Tool` indicating the server to lookup for symbols
        [string] $WindowsPdbVerificationParam, # Parameter to pass to potential check for windows-pdbs.
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

      $totalRetries = 0

      while ($totalRetries -lt $using:MaxRetry) {

        # Save the output and get diagnostic output
        $output = & $dotnetSymbolExe --symbols --modules $WindowsPdbVerificationParam $TargetServerParam $FullPath -o $SymbolsPath --diagnostics | Out-String

        if ((Test-Path $PdbPath) -and (Test-path $SymbolPath)) {
          return 'Module and PDB for Module'
        }
        elseif ((Test-Path $NGenPdb) -and (Test-Path $PdbPath) -and (Test-Path $SymbolPath)) {
          return 'Dll, PDB and NGen PDB'
        }
        elseif ((Test-Path $SODbg) -and (Test-Path $SymbolPath)) {
          return 'So and DBG for SO'
        }  
        elseif ((Test-Path $DylibDwarf) -and (Test-Path $SymbolPath)) {
          return 'Dylib and Dwarf for Dylib'
        }  
        elseif (Test-Path $SymbolPath) {
          return 'Module'
        }
        else
        {
          $totalRetries++
        }
      }
      
      return $null
    }

    $FileRelativePath = $FileName.Replace("$ExtractPath\", "")
    if (($($using:ExclusionSet) -ne $null) -and ($($using:ExclusionSet).Contains($FileRelativePath) -or ($($using:ExclusionSet).Contains($FileRelativePath.Replace("\", "/"))))){
      Write-Host "Skipping $FileName from symbol validation"
    }

    else {
      $FileGuid = New-Guid
      $ExpandedSymbolsPath = Join-Path -Path $SymbolsPath -ChildPath $FileGuid

      $SymbolsOnMSDL = & $FirstMatchingSymbolDescriptionOrDefault `
          -FullPath $FileName `
          -TargetServerParam '--microsoft-symbol-server' `
          -SymbolsPath "$ExpandedSymbolsPath-msdl" `
          -WindowsPdbVerificationParam $WindowsPdbVerificationParam
      $SymbolsOnSymWeb = & $FirstMatchingSymbolDescriptionOrDefault `
          -FullPath $FileName `
          -TargetServerParam '--internal-server' `
          -SymbolsPath "$ExpandedSymbolsPath-symweb" `
          -WindowsPdbVerificationParam $WindowsPdbVerificationParam

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
  }
  
  if ($using:Clean) {
    Remove-Item $ExtractPath -Recurse -Force
  }
  
  Pop-Location

  return [pscustomobject]@{
    result      = $MissingSymbols
    packagePath = $PackagePath
  }
}

function CheckJobResult(
  $result, 
  $packagePath,
  [ref]$DupedSymbols,
  [ref]$TotalFailures) {
  if ($result -eq $ERROR_BADEXTRACT) {
    Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "$packagePath has duplicated symbol files"
    $DupedSymbols.Value++
  } 
  elseif ($result -eq $ERROR_FILEDOESNOTEXIST) {
    Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "$packagePath does not exist"
    $TotalFailures.Value++
  }
  elseif ($result -gt '0') {
    Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "Missing symbols for $result modules in the package $packagePath"
    $TotalFailures.Value++
  }
  else {
    Write-Host "All symbols verified for package $packagePath"
  }
}

function CheckSymbolsAvailable {
  if (Test-Path $ExtractPath) {
    Remove-Item $ExtractPath -Force  -Recurse -ErrorAction SilentlyContinue
  }

  $TotalPackages = 0
  $TotalFailures = 0
  $DupedSymbols = 0

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

      $TotalPackages++

      Start-Job -ScriptBlock $CountMissingSymbols -ArgumentList @($FullName,$WindowsPdbVerificationParam) | Out-Null

      $NumJobs = @(Get-Job -State 'Running').Count

      while ($NumJobs -ge $MaxParallelJobs) {
        Write-Host "There are $NumJobs validation jobs running right now. Waiting $SecondsBetweenLoadChecks seconds to check again."
        sleep $SecondsBetweenLoadChecks
        $NumJobs = @(Get-Job -State 'Running').Count
      }

      foreach ($Job in @(Get-Job -State 'Completed')) {
        $jobResult = Wait-Job -Id $Job.Id | Receive-Job
        CheckJobResult $jobResult.result $jobResult.packagePath ([ref]$DupedSymbols) ([ref]$TotalFailures)
        Remove-Job -Id $Job.Id
      }
      Write-Host
    }

  foreach ($Job in @(Get-Job)) {
    $jobResult = Wait-Job -Id $Job.Id | Receive-Job
    CheckJobResult $jobResult.result $jobResult.packagePath ([ref]$DupedSymbols) ([ref]$TotalFailures)
  }

  if ($TotalFailures -gt 0 -or $DupedSymbols -gt 0) {
    if ($TotalFailures -gt 0) {
      Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "Symbols missing for $TotalFailures/$TotalPackages packages"
    }

    if ($DupedSymbols -gt 0) {
      Write-PipelineTelemetryError -Category 'CheckSymbols' -Message "$DupedSymbols/$TotalPackages packages had duplicated symbol files and could not be extracted"
    }
    
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
