param(
  [Parameter(Mandatory=$true)][string] $InputPath,              # Full path to directory where Symbols.NuGet packages to be checked are stored
  [Parameter(Mandatory=$true)][string] $ExtractPath,            # Full path to directory where the packages will be extracted during validation
  [Parameter(Mandatory=$false)][string] $GHRepoName,            # GitHub name of the repo including the Org. E.g., dotnet/arcade
  [Parameter(Mandatory=$false)][string] $GHCommit,              # GitHub commit SHA used to build the packages
  [Parameter(Mandatory=$true)][string] $SourcelinkCliVersion    # Version of SourceLink CLI to use
)

. $PSScriptRoot\post-build-utils.ps1

# Cache/HashMap (File -> Exist flag) used to consult whether a file exist 
# in the repository at a specific commit point. This is populated by inserting
# all files present in the repo at a specific commit point.
$global:RepoFiles = @{}

# Maximum number of jobs to run in parallel
$MaxParallelJobs = 6

# Wait time between check for system load
$SecondsBetweenLoadChecks = 10

$ValidatePackage = {
  param( 
    [string] $PackagePath                                 # Full path to a Symbols.NuGet package
  )

  . $using:PSScriptRoot\..\tools.ps1

  # Ensure input file exist
  if (!(Test-Path $PackagePath)) {
    Write-Host "Input file does not exist: $PackagePath"
    return 1
  }

  # Extensions for which we'll look for SourceLink information
  # For now we'll only care about Portable & Embedded PDBs
  $RelevantExtensions = @('.dll', '.exe', '.pdb')
 
  Write-Host -NoNewLine 'Validating ' ([System.IO.Path]::GetFileName($PackagePath)) '...'

  $PackageId = [System.IO.Path]::GetFileNameWithoutExtension($PackagePath)
  $ExtractPath = Join-Path -Path $using:ExtractPath -ChildPath $PackageId
  $FailedFiles = 0

  Add-Type -AssemblyName System.IO.Compression.FileSystem

  [System.IO.Directory]::CreateDirectory($ExtractPath)  | Out-Null

  try {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)

    $zip.Entries | 
      Where-Object {$RelevantExtensions -contains [System.IO.Path]::GetExtension($_.Name)} |
        ForEach-Object {
          $FileName = $_.FullName
          $Extension = [System.IO.Path]::GetExtension($_.Name)
          $FakeName = -Join((New-Guid), $Extension)
          $TargetFile = Join-Path -Path $ExtractPath -ChildPath $FakeName 

          # We ignore resource DLLs
          if ($FileName.EndsWith('.resources.dll')) {
            return
          }

          [System.IO.Compression.ZipFileExtensions]::ExtractToFile($_, $TargetFile, $true)

          $ValidateFile = {
            param( 
              [string] $FullPath,                                # Full path to the module that has to be checked
              [string] $RealPath,
              [ref] $FailedFiles
            )

            $sourcelinkExe = "$env:USERPROFILE\.dotnet\tools"
            $sourcelinkExe = Resolve-Path "$sourcelinkExe\sourcelink.exe"
            $SourceLinkInfos = & $sourcelinkExe print-urls $FullPath | Out-String

            if ($LASTEXITCODE -eq 0 -and -not ([string]::IsNullOrEmpty($SourceLinkInfos))) {
              $NumFailedLinks = 0

              # We only care about Http addresses
              $Matches = (Select-String '(http[s]?)(:\/\/)([^\s,]+)' -Input $SourceLinkInfos -AllMatches).Matches

              if ($Matches.Count -ne 0) {
                $Matches.Value |
                  ForEach-Object {
                    $Link = $_
                    $CommitUrl = "https://raw.githubusercontent.com/${using:GHRepoName}/${using:GHCommit}/"
                    
                    $FilePath = $Link.Replace($CommitUrl, "")
                    $Status = 200
                    $Cache = $using:RepoFiles

                    if ( !($Cache.ContainsKey($FilePath)) ) {
                      try {
                        $Uri = $Link -as [System.URI]
                      
                        # Only GitHub links are valid
                        if ($Uri.AbsoluteURI -ne $null -and ($Uri.Host -match 'github' -or $Uri.Host -match 'githubusercontent')) {
                          $Status = (Invoke-WebRequest -Uri $Link -UseBasicParsing -Method HEAD -TimeoutSec 5).StatusCode
                        }
                        else {
                          $Status = 0
                        }
                      }
                      catch {
                        write-host $_
                        $Status = 0
                      }
                    }

                    if ($Status -ne 200) {
                      if ($NumFailedLinks -eq 0) {
                        if ($FailedFiles.Value -eq 0) {
                          Write-Host
                        }

                        Write-Host "`tFile $RealPath has broken links:"
                      }

                      Write-Host "`t`tFailed to retrieve $Link"

                      $NumFailedLinks++
                    }
                  }
              }

              if ($NumFailedLinks -ne 0) {
                $FailedFiles.value++
                $global:LASTEXITCODE = 1
              }
            }
          }
        
          &$ValidateFile $TargetFile $FileName ([ref]$FailedFiles)
        }
  }
  catch {
  
  }
  finally {
    $zip.Dispose() 
  }

  if ($FailedFiles -eq 0) {
    Write-Host 'Passed.'
    return 0
  }
  else {
    Write-PipelineTelemetryError -Category 'SourceLink' -Message "$PackagePath has broken SourceLink links."
    return 1
  }
}

function ValidateSourceLinkLinks {
  if ($GHRepoName -ne '' -and !($GHRepoName -Match '^[^\s\/]+/[^\s\/]+$')) {
    if (!($GHRepoName -Match '^[^\s-]+-[^\s]+$')) {
      Write-PipelineTelemetryError -Category 'SourceLink' -Message "GHRepoName should be in the format <org>/<repo> or <org>-<repo>. '$GHRepoName'"
      ExitWithExitCode 1
    }
    else {
      $GHRepoName = $GHRepoName -replace '^([^\s-]+)-([^\s]+)$', '$1/$2';
    }
  }

  if ($GHCommit -ne '' -and !($GHCommit -Match '^[0-9a-fA-F]{40}$')) {
    Write-PipelineTelemetryError -Category 'SourceLink' -Message "GHCommit should be a 40 chars hexadecimal string. '$GHCommit'"
    ExitWithExitCode 1
  }

  if ($GHRepoName -ne '' -and $GHCommit -ne '') {
    $RepoTreeURL = -Join('http://api.github.com/repos/', $GHRepoName, '/git/trees/', $GHCommit, '?recursive=1')
    $CodeExtensions = @('.cs', '.vb', '.fs', '.fsi', '.fsx', '.fsscript')

    try {
      # Retrieve the list of files in the repo at that particular commit point and store them in the RepoFiles hash
      $Data = Invoke-WebRequest $RepoTreeURL -UseBasicParsing | ConvertFrom-Json | Select-Object -ExpandProperty tree
  
      foreach ($file in $Data) {
        $Extension = [System.IO.Path]::GetExtension($file.path)

        if ($CodeExtensions.Contains($Extension)) {
          $RepoFiles[$file.path] = 1
        }
      }
    }
    catch {
      Write-Host "Problems downloading the list of files from the repo. Url used: $RepoTreeURL . Execution will proceed without caching."
    }
  }
  elseif ($GHRepoName -ne '' -or $GHCommit -ne '') {
    Write-Host 'For using the http caching mechanism both GHRepoName and GHCommit should be informed.'
  }
  
  if (Test-Path $ExtractPath) {
    Remove-Item $ExtractPath -Force -Recurse -ErrorAction SilentlyContinue
  }

  $ValidationFailures = 0

  # Process each NuGet package in parallel
  Get-ChildItem "$InputPath\*.symbols.nupkg" |
    ForEach-Object {
      Start-Job -ScriptBlock $ValidatePackage -ArgumentList $_.FullName | Out-Null
      $NumJobs = @(Get-Job -State 'Running').Count
      
      while ($NumJobs -ge $MaxParallelJobs) {
        Write-Host "There are $NumJobs validation jobs running right now. Waiting $SecondsBetweenLoadChecks seconds to check again."
        sleep $SecondsBetweenLoadChecks
        $NumJobs = @(Get-Job -State 'Running').Count
      }

      foreach ($Job in @(Get-Job -State 'Completed')) {
        $jobResult = Receive-Job -Id $Job.Id
        if ($jobResult -ne '0') {
          $ValidationFailures++
        }
        Remove-Job -Id $Job.Id
      }
    }

  foreach ($Job in @(Get-Job)) {
    $jobResult = Wait-Job -Id $Job.Id | Receive-Job
    if ($jobResult -ne '0') {
      $ValidationFailures++
    }
    Remove-Job -Id $Job.Id
  }
  if ($ValidationFailures -gt 0) {
    Write-PipelineTelemetryError -Category 'SourceLink' -Message "$ValidationFailures package(s) failed validation."
    ExitWithExitCode 1
  }
}

function InstallSourcelinkCli {
  $sourcelinkCliPackageName = 'sourcelink'

  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = "$dotnetRoot\dotnet.exe"
  $toolList = & "$dotnet" tool list --global

  if (($toolList -like "*$sourcelinkCliPackageName*") -and ($toolList -like "*$sourcelinkCliVersion*")) {
    Write-Host "SourceLink CLI version $sourcelinkCliVersion is already installed."
  }
  else {
    Write-Host "Installing SourceLink CLI version $sourcelinkCliVersion..."
    Write-Host 'You may need to restart your command window if this is the first dotnet tool you have installed.'
    & "$dotnet" tool install $sourcelinkCliPackageName --version $sourcelinkCliVersion --verbosity "minimal" --global 
  }
}

try {
  InstallSourcelinkCli

  ValidateSourceLinkLinks 
}
catch {
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'SourceLink' -Message $_
  ExitWithExitCode 1
}
