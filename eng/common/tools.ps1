# Initialize variables if they aren't already defined

$ci = if (Test-Path variable:ci) { $ci } else { $false }
$configuration = if (Test-Path variable:configuration) { $configuration } else { "Debug" }
$nodereuse = if (Test-Path variable:nodereuse) { $nodereuse } else { !$ci }
$prepareMachine = if (Test-Path variable:prepareMachine) { $prepareMachine } else { $false }
$restore = if (Test-Path variable:restore) { $restore } else { $true }
$verbosity = if (Test-Path variable:verbosity) { $verbosity } else { "minimal" }
$warnaserror = if (Test-Path variable:warnaserror) { $warnaserror } else { $true }
$msbuildEngine = if (Test-Path variable:msbuildEngine) { $msbuildEngine } else { $null }
$useInstalledDotNetCli = if (Test-Path variable:useInstalledDotNetCli) { $useInstalledDotNetCli } else { $true }

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Create-Directory([string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function Unzip([string]$zipfile, [string]$outpath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function InitializeDotNetCli([bool]$install) {
  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  $env:DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we do not need all ASP.NET packages restored.
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Source Build uses DotNetCoreSdkDir variable
  if ($env:DotNetCoreSdkDir -ne $null) {
    $env:DOTNET_INSTALL_DIR = $env:DotNetCoreSdkDir
  }

  # Find the first path on %PATH% that contains the dotnet.exe
  if ($useInstalledDotNetCli -and ($env:DOTNET_INSTALL_DIR -eq $null)) {
    $env:DOTNET_INSTALL_DIR = ${env:PATH}.Split(';') | where { ($_ -ne "") -and (Test-Path (Join-Path $_ "dotnet.exe")) }
  }

  $dotnetSdkVersion = $GlobalJson.tools.dotnet

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if (($env:DOTNET_INSTALL_DIR -ne $null) -and (Test-Path(Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetSdkVersion"))) {
    $dotnetRoot = $env:DOTNET_INSTALL_DIR
  } else {
    $dotnetRoot = Join-Path $RepoRoot ".dotnet"
    $env:DOTNET_INSTALL_DIR = $dotnetRoot

    if (-not (Test-Path(Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetSdkVersion"))) {
      if ($install) {
        InstallDotNetSdk $dotnetRoot $dotnetSdkVersion
      } else {
        Write-Host "Unable to find dotnet with SDK version '$dotnetSdkVersion'" -ForegroundColor Red
        ExitWithExitCode 1
      }
    }
  }

  return $dotnetRoot
}

function GetDotNetInstallScript([string] $dotnetRoot) {
  $installScript = "$dotnetRoot\dotnet-install.ps1"
  if (!(Test-Path $installScript)) {
    Create-Directory $dotnetRoot
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
  }

  return $installScript
}

function InstallDotNetSdk([string] $dotnetRoot, [string] $version) {
  $installScript = GetDotNetInstallScript $dotnetRoot
  & $installScript -Version $version -InstallDir $dotnetRoot
  if ($lastExitCode -ne 0) {
    Write-Host "Failed to install dotnet cli (exit code '$lastExitCode')." -ForegroundColor Red
    ExitWithExitCode $lastExitCode
  }
}

#
# Locates Visual Studio MSBuild installation. 
# The preference order for MSBuild to use is as follows:
#
#   1. MSBuild from an active VS command prompt
#   2. MSBuild from a compatible VS installation
#   3. MSBuild from the xcopy tool package
#
# Returns full path to msbuild.exe.
# Throws on failure.
#
function InitializeVisualStudioMSBuild {
  $vsMinVersionStr = if (!$GlobalJson.tools.vs.version) { $GlobalJson.tools.vs.version } else { "15.9" }
  $vsMinVersion = [Version]::new($vsMinVersionStr) 

  # Try msbuild command available in the environment.
  if ($env:VSINSTALLDIR -ne $null) {
    $msbuildCmd = Get-Command "msbuild.exe" -ErrorAction SilentlyContinue
    if ($msbuildCmd -ne $null) {
      if ($msbuildCmd.Version -ge $vsMinVersion) {
        return $msbuildCmd.Path
      }

      # Report error - the developer environment is initialized with incompatible VS version.
      throw "Developer Command Prompt for VS $($env:VisualStudioVersion) is not recent enough. Please upgrade to $vsMinVersionStr or build from a plain CMD window"
    }
  }

  # Locate Visual Studio installation or download x-copy msbuild.
  $vsInfo = LocateVisualStudio  
  if ($vsInfo -ne $null) {
    $vsInstallDir = $vsInfo.installationPath
    $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

    InitializeVisualStudioEnvironmentVariables $vsInstallDir $vsMajorVersion
  } else {

    if (Get-Member -InputObject $GlobalJson.tools -Name "xcopy-msbuild") {
      $xcopyMSBuildVersion = $GlobalJson.tools.'xcopy-msbuild'
      $vsMajorVersion = $xcopyMSBuildVersion.Split('.')[0]
    } else {
      $vsMajorVersion = $vsMinVersion.Major
      $xcopyMSBuildVersion = "$vsMajorVersion.$($vsMinVersion.Minor).0-alpha"
    }

    $vsInstallDir = InstallXCopyMSBuild $xcopyMSBuildVersion
  }

  $msbuildVersionDir = if ([int]$vsMajorVersion -lt 16) { "$vsMajorVersion.0" } else { "Current" }
  return Join-Path $vsInstallDir "MSBuild\$msbuildVersionDir\Bin\msbuild.exe"
}

function InitializeVisualStudioEnvironmentVariables([string] $vsInstallDir, [string] $vsMajorVersion) {
  $env:VSINSTALLDIR = $vsInstallDir
  Set-Item "env:VS$($vsMajorVersion)0COMNTOOLS" (Join-Path $vsInstallDir "Common7\Tools\")
  
  $vsSdkInstallDir = Join-Path $vsInstallDir "VSSDK\"
  if (Test-Path $vsSdkInstallDir) {
    Set-Item "env:VSSDK$($vsMajorVersion)0Install" $vsSdkInstallDir
    $env:VSSDKInstall = $vsSdkInstallDir
  }
}

function InstallXCopyMSBuild([string] $packageVersion) {
  $packageName = "RoslynTools.MSBuild"
  $packageDir = Join-Path $ToolsDir "msbuild\$packageVersion"
  $packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"

  if (!(Test-Path $packageDir)) {
    Create-Directory $packageDir
    Write-Host "Downloading $packageName $packageVersion"
    Invoke-WebRequest "https://dotnet.myget.org/F/roslyn-tools/api/v2/package/$packageName/$packageVersion/" -OutFile $packagePath
    Unzip $packagePath $packageDir
  }

  return Join-Path $packageDir "tools"
}

#
# Locates Visual Studio instance that meets the minimal requirements specified by tools.vs object in global.json.
#
# The following properties of tools.vs are recognized:
#   "version": "{major}.{minor}"    
#       Two part minimal VS version, e.g. "15.9", "16.0", etc.
#   "components": ["componentId1", "componentId2", ...] 
#       Array of ids of workload components that must be available in the VS instance.
#       See e.g. https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-enterprise?view=vs-2017
#
# Returns JSON describing the located VS instance (same format as returned by vswhere), 
# or $null if no instance meeting the requirements is found on the machine.
#
function LocateVisualStudio {
  $vswhereVersion = Get-Member -InputObject $GlobalJson.tools -Name "vswhere"
  if ($vsWhereVersion -eq $null) {
    $vswhereVersion = "2.5.2"
  }

  $vsWhereDir = Join-Path $ToolsDir "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir "vswhere.exe"

  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir
    Write-Host "Downloading vswhere"
    Invoke-WebRequest "https://github.com/Microsoft/vswhere/releases/download/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
  }

  $vs = $GlobalJson.tools.vs
  $args = @("-latest", "-prerelease", "-format", "json", "-requires", "Microsoft.Component.MSBuild")
  
  if (Get-Member -InputObject $vs -Name "version") { 
    $args += "-version"
    $args += $vs.version
  }

  if (Get-Member -InputObject $vs -Name "components") { 
    foreach ($component in $vs.components) {
      $args += "-requires"
      $args += $component
    }    
  }

  $vsInfo =& $vsWhereExe $args | ConvertFrom-Json

  if ($lastExitCode -ne 0) {
    return $null
  }

  # use first matching instance
  return $vsInfo[0]
}

function ConfigureTools { 
  # Include custom tools configuration
  $script = Join-Path $EngRoot "configure-toolset.ps1"

  if (Test-Path $script) {
    . $script
  }
}

function InitializeTools() {
  ConfigureTools

  $tools = $GlobalJson.tools

  # Initialize dotnet cli if listed in 'tools'
  $dotnetRoot = $null
  if (Get-Member -InputObject $tools -Name "dotnet") {
    $dotnetRoot = InitializeDotNetCli -install:$restore
  }

  if (-not $msbuildEngine) {
    # Presence of tools.vs indicates the repo needs to build using VS msbuild on Windows.
    if (Get-Member -InputObject $tools -Name "vs") {
      $msbuildEngine = "vs"
    } elseif ($dotnetRoot -ne $null) {
      $msbuildEngine = "dotnet"
    } else {
      Write-Host "-msbuildEngine must be specified, or /global.json must specify 'tools.dotnet' or 'tools.vs'." -ForegroundColor Red
      ExitWithExitCode 1
    }
  }

  if ($msbuildEngine -eq "dotnet") {
    if (!$dotnetRoot) {
      Write-Host "/global.json must specify 'tools.dotnet'." -ForegroundColor Red
      ExitWithExitCode 1
    }

    $script:buildDriver = Join-Path $dotnetRoot "dotnet.exe"
    $script:buildArgs = "msbuild"
  } elseif ($msbuildEngine -eq "vs") {
    try {
      $script:buildDriver = InitializeVisualStudioMSBuild
      $script:buildArgs = ""
    } catch { 
      Write-Host $_ -ForegroundColor Red
      ExitWithExitCode 1
    }
  } else {
    Write-Host "Unexpected value of -msbuildEngine: '$msbuildEngine'." -ForegroundColor Red
    ExitWithExitCode 1
  }

  InitializeToolSet $script:buildDriver $script:buildArgs
  InitializeCustomToolset
}

function InitializeToolset([string] $buildDriver, [string]$buildArgs) {
  $toolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Sdk'
  $toolsetLocationFile = Join-Path $ToolsetDir "$toolsetVersion.txt"

  if (Test-Path $toolsetLocationFile) {
    $path = Get-Content $toolsetLocationFile -TotalCount 1
    if (Test-Path $path) {
      $script:ToolsetBuildProj = $path
      return
    }
  }

  if (-not $restore) {
    Write-Host  "Toolset version $toolsetVersion has not been restored."
    ExitWithExitCode 1
  }

  $ToolsetRestoreLog = Join-Path $LogDir "ToolsetRestore.binlog"
  $proj = Join-Path $ToolsetDir "restore.proj"

  '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' | Set-Content $proj
  MSBuild $proj /t:__WriteToolsetLocation /clp:None /bl:$ToolsetRestoreLog /p:__ToolsetLocationOutputFile=$toolsetLocationFile

  if ($lastExitCode -ne 0) {
    Write-Host "Failed to restore toolset (exit code '$lastExitCode'). See log: $ToolsetRestoreLog" -ForegroundColor Red
    ExitWithExitCode $lastExitCode
  }

  $path = Get-Content $toolsetLocationFile -TotalCount 1
  if (!(Test-Path $path)) {
    throw "Invalid toolset path: $path"
  }

  $script:ToolsetBuildProj = $path
}

function InitializeCustomToolset {
  if (-not $restore) {
    return
  }

  $script = Join-Path $EngRoot "restore-toolset.ps1"

  if (Test-Path $script) {
    . $script
  }
}

function ExitWithExitCode([int] $exitCode) {
  if ($ci -and $prepareMachine) {
    Stop-Processes
  }
  exit $exitCode
}

function Stop-Processes() {
  Write-Host "Killing running build processes..."
  Get-Process -Name "msbuild" -ErrorAction SilentlyContinue | Stop-Process
  Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process
  Get-Process -Name "vbcscompiler" -ErrorAction SilentlyContinue | Stop-Process
}

function MsBuild() {
  $warnaserrorSwitch = if ($warnaserror) { "/warnaserror" } else { "" }
  & $buildDriver $buildArgs $warnaserrorSwitch /m /nologo /clp:Summary /v:$verbosity /nr:$nodereuse $args
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$EngRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$ToolsetDir = Join-Path $ArtifactsDir "toolset"
$ToolsDir = Join-Path $RepoRoot ".tools"
$LogDir = Join-Path (Join-Path $ArtifactsDir "log") $configuration
$TempDir = Join-Path (Join-Path $ArtifactsDir "tmp") $configuration
$GlobalJson = Get-Content -Raw -Path (Join-Path $RepoRoot "global.json") | ConvertFrom-Json

if ($env:NUGET_PACKAGES -eq $null) {
  # Use local cache on CI to ensure deterministic build,
  # use global cache in dev builds to avoid cost of downloading packages.
  $env:NUGET_PACKAGES = if ($ci) { Join-Path $RepoRoot ".packages" }
                        else { Join-Path $env:UserProfile ".nuget\packages" }
}

Create-Directory $ToolsetDir
Create-Directory $LogDir

if ($ci) {
  Create-Directory $TempDir
  $env:TEMP = $TempDir
  $env:TMP = $TempDir
}
