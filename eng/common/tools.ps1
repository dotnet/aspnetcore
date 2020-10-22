# Initialize variables if they aren't already defined.
# These may be defined as parameters of the importing script, or set after importing this script.

# CI mode - set to true on CI server for PR validation build or official build.
[bool]$ci = if (Test-Path variable:ci) { $ci } else { $false }

# Build configuration. Common values include 'Debug' and 'Release', but the repository may use other names.
[string]$configuration = if (Test-Path variable:configuration) { $configuration } else { 'Debug' }

# Set to true to opt out of outputting binary log while running in CI
[bool]$excludeCIBinarylog = if (Test-Path variable:excludeCIBinarylog) { $excludeCIBinarylog } else { $false }

# Set to true to output binary log from msbuild. Note that emitting binary log slows down the build.
[bool]$binaryLog = if (Test-Path variable:binaryLog) { $binaryLog } else { $ci -and !$excludeCIBinarylog }

# Set to true to use the pipelines logger which will enable Azure logging output.
# https://github.com/Microsoft/azure-pipelines-tasks/blob/master/docs/authoring/commands.md
# This flag is meant as a temporary opt-opt for the feature while validate it across
# our consumers. It will be deleted in the future.
[bool]$pipelinesLog = if (Test-Path variable:pipelinesLog) { $pipelinesLog } else { $ci }

# Turns on machine preparation/clean up code that changes the machine state (e.g. kills build processes).
[bool]$prepareMachine = if (Test-Path variable:prepareMachine) { $prepareMachine } else { $false }

# True to restore toolsets and dependencies.
[bool]$restore = if (Test-Path variable:restore) { $restore } else { $true }

# Adjusts msbuild verbosity level.
[string]$verbosity = if (Test-Path variable:verbosity) { $verbosity } else { 'minimal' }

# Set to true to reuse msbuild nodes. Recommended to not reuse on CI.
[bool]$nodeReuse = if (Test-Path variable:nodeReuse) { $nodeReuse } else { !$ci }

# Configures warning treatment in msbuild.
[bool]$warnAsError = if (Test-Path variable:warnAsError) { $warnAsError } else { $true }

# Specifies which msbuild engine to use for build: 'vs', 'dotnet' or unspecified (determined based on presence of tools.vs in global.json).
[string]$msbuildEngine = if (Test-Path variable:msbuildEngine) { $msbuildEngine } else { $null }

# True to attempt using .NET Core already that meets requirements specified in global.json
# installed on the machine instead of downloading one.
[bool]$useInstalledDotNetCli = if (Test-Path variable:useInstalledDotNetCli) { $useInstalledDotNetCli } else { $true }

# Enable repos to use a particular version of the on-line dotnet-install scripts.
#    default URL: https://dot.net/v1/dotnet-install.ps1
[string]$dotnetInstallScriptVersion = if (Test-Path variable:dotnetInstallScriptVersion) { $dotnetInstallScriptVersion } else { 'v1' }

# True to use global NuGet cache instead of restoring packages to repository-local directory.
[bool]$useGlobalNuGetCache = if (Test-Path variable:useGlobalNuGetCache) { $useGlobalNuGetCache } else { !$ci }

# An array of names of processes to stop on script exit if prepareMachine is true.
$processesToStopOnExit = if (Test-Path variable:processesToStopOnExit) { $processesToStopOnExit } else { @('msbuild', 'dotnet', 'vbcscompiler') }

$disableConfigureToolsetImport = if (Test-Path variable:disableConfigureToolsetImport) { $disableConfigureToolsetImport } else { $null }

set-strictmode -version 2.0
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# If specified, provides an alternate path for getting .NET Core SDKs and Runtimes. This script will still try public sources first.
[string]$runtimeSourceFeed = if (Test-Path variable:runtimeSourceFeed) { $runtimeSourceFeed } else { $null }
# Base-64 encoded SAS token that has permission to storage container described by $runtimeSourceFeed
[string]$runtimeSourceFeedKey = if (Test-Path variable:runtimeSourceFeedKey) { $runtimeSourceFeedKey } else { $null }

# If false, use copy of dotnet-install from /eng/common/dotnet-install-scripts (for custom behaviors).
# otherwise will fetch from public location.
[bool]$useDefaultDotnetInstall = if (Test-Path variable:useDefaultDotnetInstall) { $useDefaultDotnetInstall } else { $false }

function Create-Directory ([string[]] $path) {
    New-Item -Path $path -Force -ItemType 'Directory' | Out-Null
}

function Unzip([string]$zipfile, [string]$outpath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

# This will exec a process using the console and return it's exit code.
# This will not throw when the process fails.
# Returns process exit code.
function Exec-Process([string]$command, [string]$commandArgs) {
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
  $startInfo.FileName = $command
  $startInfo.Arguments = $commandArgs
  $startInfo.UseShellExecute = $false
  $startInfo.WorkingDirectory = Get-Location

  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = $startInfo
  $process.Start() | Out-Null

  $finished = $false
  try {
    while (-not $process.WaitForExit(100)) {
      # Non-blocking loop done to allow ctr-c interrupts
    }

    $finished = $true
    return $global:LASTEXITCODE = $process.ExitCode
  }
  finally {
    # If we didn't finish then an error occurred or the user hit ctrl-c.  Either
    # way kill the process
    if (-not $finished) {
      $process.Kill()
    }
  }
}

# createSdkLocationFile parameter enables a file being generated under the toolset directory
# which writes the sdk's location into. This is only necessary for cmd --> powershell invocations
# as dot sourcing isn't possible.
function InitializeDotNetCli([bool]$install, [bool]$createSdkLocationFile) {
  if (Test-Path variable:global:_DotNetInstallDir) {
    return $global:_DotNetInstallDir
  }

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  $env:DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we do not need all ASP.NET packages restored.
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Disable telemetry on CI.
  if ($ci) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT=1
  }

  # Source Build uses DotNetCoreSdkDir variable
  if ($env:DotNetCoreSdkDir -ne $null) {
    $env:DOTNET_INSTALL_DIR = $env:DotNetCoreSdkDir
  }

  # Find the first path on %PATH% that contains the dotnet.exe
  if ($useInstalledDotNetCli -and (-not $globalJsonHasRuntimes) -and ($env:DOTNET_INSTALL_DIR -eq $null)) {
    $dotnetExecutable = GetExecutableFileName 'dotnet'
    $dotnetCmd = Get-Command $dotnetExecutable -ErrorAction SilentlyContinue

    if ($dotnetCmd -ne $null) {
      $env:DOTNET_INSTALL_DIR = Split-Path $dotnetCmd.Path -Parent
    }
  }

  $dotnetSdkVersion = $GlobalJson.tools.dotnet

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if ((-not $globalJsonHasRuntimes) -and ($env:DOTNET_INSTALL_DIR -ne $null) -and (Test-Path(Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetSdkVersion"))) {
    $dotnetRoot = $env:DOTNET_INSTALL_DIR
  } else {
    $dotnetRoot = Join-Path $RepoRoot '.dotnet'

    if (-not (Test-Path(Join-Path $dotnetRoot "sdk\$dotnetSdkVersion"))) {
      if ($install) {
        InstallDotNetSdk $dotnetRoot $dotnetSdkVersion
      } else {
        Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Unable to find dotnet with SDK version '$dotnetSdkVersion'"
        ExitWithExitCode 1
      }
    }

    $env:DOTNET_INSTALL_DIR = $dotnetRoot
  }

  # Creates a temporary file under the toolset dir.
  # The following code block is protecting against concurrent access so that this function can
  # be called in parallel.
  if ($createSdkLocationFile) {
    do {
      $sdkCacheFileTemp = Join-Path $ToolsetDir $([System.IO.Path]::GetRandomFileName())
    }
    until (!(Test-Path $sdkCacheFileTemp))
    Set-Content -Path $sdkCacheFileTemp -Value $dotnetRoot

    try {
      Rename-Item -Force -Path $sdkCacheFileTemp 'sdk.txt'
    } catch {
      # Somebody beat us
      Remove-Item -Path $sdkCacheFileTemp
    }
  }

  # Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
  # build steps from using anything other than what we've downloaded.
  # It also ensures that VS msbuild will use the downloaded sdk targets.
  $env:PATH = "$dotnetRoot;$env:PATH"

  # Make Sure that our bootstrapped dotnet cli is available in future steps of the Azure Pipelines build
  Write-PipelinePrependPath -Path $dotnetRoot

  Write-PipelineSetVariable -Name 'DOTNET_MULTILEVEL_LOOKUP' -Value '0'
  Write-PipelineSetVariable -Name 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE' -Value '1'

  return $global:_DotNetInstallDir = $dotnetRoot
}

function GetDotNetInstallScript([string] $dotnetRoot) {
  $installScript = Join-Path $dotnetRoot 'dotnet-install.ps1'
  if (!(Test-Path $installScript)) {
    create-directory $dotnetroot

    if ($useDefaultDotnetInstall)
    {
      $progresspreference = 'silentlycontinue' # don't display the console progress ui - it's a huge perf hit

      $maxretries = 5
      $retries = 1

      $uri = "https://dot.net/$dotnetinstallscriptversion/dotnet-install.ps1"

      while($true) {
        try {
          write-host "get $uri"
          invoke-webrequest $uri -outfile $installscript
          break
        }
        catch {
          write-host "failed to download '$uri'"
          write-error $_.exception.message -erroraction continue
        }

        if (++$retries -le $maxretries) {
          $delayinseconds = [math]::pow(2, $retries) - 1 # exponential backoff
          write-host "retrying. waiting for $delayinseconds seconds before next attempt ($retries of $maxretries)."
          start-sleep -seconds $delayinseconds
        }
        else {
          throw "unable to download file in $maxretries attempts."
        }
      }
    }
    else
    {
      # Use a special version of the script from eng/common that understands the existence of a "productVersion.txt" in a dotnet path.
      # See https://github.com/dotnet/arcade/issues/6047 for details
      $engCommonCopy = Resolve-Path (Join-Path $PSScriptRoot 'dotnet-install-scripts\dotnet-install.ps1')
      Copy-Item $engCommonCopy -Destination $installScript -Force
    }
  }
  return $installScript
}

function InstallDotNetSdk([string] $dotnetRoot, [string] $version, [string] $architecture = '', [switch] $noPath) {
  InstallDotNet $dotnetRoot $version $architecture '' $false $runtimeSourceFeed $runtimeSourceFeedKey -noPath:$noPath
}

function InstallDotNet([string] $dotnetRoot,
  [string] $version,
  [string] $architecture = '',
  [string] $runtime = '',
  [bool] $skipNonVersionedFiles = $false,
  [string] $runtimeSourceFeed = '',
  [string] $runtimeSourceFeedKey = '',
  [switch] $noPath) {

  $installScript = GetDotNetInstallScript $dotnetRoot
  $installParameters = @{
    Version = $version
    InstallDir = $dotnetRoot
  }

  if ($architecture) { $installParameters.Architecture = $architecture }
  if ($runtime) { $installParameters.Runtime = $runtime }
  if ($skipNonVersionedFiles) { $installParameters.SkipNonVersionedFiles = $skipNonVersionedFiles }
  if ($noPath) { $installParameters.NoPath = $True }

  try {
    & $installScript @installParameters
  }
  catch {
    if ($runtimeSourceFeed -or $runtimeSourceFeedKey) {
      Write-Host "Failed to install dotnet from public location. Trying from '$runtimeSourceFeed'"
      if ($runtimeSourceFeed) { $installParameters.AzureFeed = $runtimeSourceFeed }

      if ($runtimeSourceFeedKey) {
        $decodedBytes = [System.Convert]::FromBase64String($runtimeSourceFeedKey)
        $decodedString = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
        $installParameters.FeedCredential = $decodedString
      }

      try {
        & $installScript @installParameters
      }
      catch {
        Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Failed to install dotnet from custom location '$runtimeSourceFeed'."
        ExitWithExitCode 1
      }
    } else {
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Failed to install dotnet from public location."
      ExitWithExitCode 1
    }
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
function InitializeVisualStudioMSBuild([bool]$install, [object]$vsRequirements = $null) {
  if (-not (IsWindowsPlatform)) {
    throw "Cannot initialize Visual Studio on non-Windows"
  }

  if (Test-Path variable:global:_MSBuildExe) {
    return $global:_MSBuildExe
  }

  # Minimum VS version to require.
  $vsMinVersionReqdStr = '16.8'
  $vsMinVersionReqd = [Version]::new($vsMinVersionReqdStr)

  # If the version of msbuild is going to be xcopied,
  # use this version. Version matches a package here:
  # https://dev.azure.com/dnceng/public/_packaging?_a=package&feed=dotnet-eng&package=RoslynTools.MSBuild&protocolType=NuGet&version=16.8.0-preview3&view=overview
  $defaultXCopyMSBuildVersion = '16.8.0-preview3'

  if (!$vsRequirements) { $vsRequirements = $GlobalJson.tools.vs }
  $vsMinVersionStr = if ($vsRequirements.version) { $vsRequirements.version } else { $vsMinVersionReqdStr }
  $vsMinVersion = [Version]::new($vsMinVersionStr)

  # Try msbuild command available in the environment.
  if ($env:VSINSTALLDIR -ne $null) {
    $msbuildCmd = Get-Command 'msbuild.exe' -ErrorAction SilentlyContinue
    if ($msbuildCmd -ne $null) {
      # Workaround for https://github.com/dotnet/roslyn/issues/35793
      # Due to this issue $msbuildCmd.Version returns 0.0.0.0 for msbuild.exe 16.2+
      $msbuildVersion = [Version]::new((Get-Item $msbuildCmd.Path).VersionInfo.ProductVersion.Split([char[]]@('-', '+'))[0])

      if ($msbuildVersion -ge $vsMinVersion) {
        return $global:_MSBuildExe = $msbuildCmd.Path
      }

      # Report error - the developer environment is initialized with incompatible VS version.
      throw "Developer Command Prompt for VS $($env:VisualStudioVersion) is not recent enough. Please upgrade to $vsMinVersionStr or build from a plain CMD window"
    }
  }

  # Locate Visual Studio installation or download x-copy msbuild.
  $vsInfo = LocateVisualStudio $vsRequirements
  if ($vsInfo -ne $null) {
    $vsInstallDir = $vsInfo.installationPath
    $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

    InitializeVisualStudioEnvironmentVariables $vsInstallDir $vsMajorVersion
  } else {

    if (Get-Member -InputObject $GlobalJson.tools -Name 'xcopy-msbuild') {
      $xcopyMSBuildVersion = $GlobalJson.tools.'xcopy-msbuild'
      $vsMajorVersion = $xcopyMSBuildVersion.Split('.')[0]
    } else {
      #if vs version provided in global.json is incompatible (too low) then use the default version for xcopy msbuild download
      if($vsMinVersion -lt $vsMinVersionReqd){
        Write-Host "Using xcopy-msbuild version of $defaultXCopyMSBuildVersion since VS version $vsMinVersionStr provided in global.json is not compatible"
        $xcopyMSBuildVersion = $defaultXCopyMSBuildVersion
      }
      else{
        # If the VS version IS compatible, look for an xcopy msbuild package
        # with a version matching VS.
        # Note: If this version does not exist, then an explicit version of xcopy msbuild
        # can be specified in global.json. This will be required for pre-release versions of msbuild.
        $vsMajorVersion = $vsMinVersion.Major
        $vsMinorVersion = $vsMinVersion.Minor
        $xcopyMSBuildVersion = "$vsMajorVersion.$vsMinorVersion.0"
      }
    }

    $vsInstallDir = $null
    if ($xcopyMSBuildVersion.Trim() -ine "none") {
        $vsInstallDir = InitializeXCopyMSBuild $xcopyMSBuildVersion $install
        if ($vsInstallDir -eq $null) {
            throw "Could not xcopy msbuild. Please check that package 'RoslynTools.MSBuild @ $xcopyMSBuildVersion' exists on feed 'dotnet-eng'."
        }
    }
    if ($vsInstallDir -eq $null) {
      throw 'Unable to find Visual Studio that has required version and components installed'
    }
  }

  $msbuildVersionDir = if ([int]$vsMajorVersion -lt 16) { "$vsMajorVersion.0" } else { "Current" }
  return $global:_MSBuildExe = Join-Path $vsInstallDir "MSBuild\$msbuildVersionDir\Bin\msbuild.exe"
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

function InstallXCopyMSBuild([string]$packageVersion) {
  return InitializeXCopyMSBuild $packageVersion -install $true
}

function InitializeXCopyMSBuild([string]$packageVersion, [bool]$install) {
  $packageName = 'RoslynTools.MSBuild'
  $packageDir = Join-Path $ToolsDir "msbuild\$packageVersion"
  $packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"

  if (!(Test-Path $packageDir)) {
    if (!$install) {
      return $null
    }

    Create-Directory $packageDir
    Write-Host "Downloading $packageName $packageVersion"
    $ProgressPreference = 'SilentlyContinue' # Don't display the console progress UI - it's a huge perf hit
    Invoke-WebRequest "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/flat2/$packageName/$packageVersion/$packageName.$packageVersion.nupkg" -OutFile $packagePath
    Unzip $packagePath $packageDir
  }

  return Join-Path $packageDir 'tools'
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
function LocateVisualStudio([object]$vsRequirements = $null){
  if (-not (IsWindowsPlatform)) {
    throw "Cannot run vswhere on non-Windows platforms."
  }

  if (Get-Member -InputObject $GlobalJson.tools -Name 'vswhere') {
    $vswhereVersion = $GlobalJson.tools.vswhere
  } else {
    $vswhereVersion = '2.5.2'
  }

  $vsWhereDir = Join-Path $ToolsDir "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir 'vswhere.exe'

  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir
    Write-Host 'Downloading vswhere'
    try {
      Invoke-WebRequest "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/vswhere/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
    }
    catch {
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message $_
    }
  }

  if (!$vsRequirements) { $vsRequirements = $GlobalJson.tools.vs }
  $args = @('-latest', '-prerelease', '-format', 'json', '-requires', 'Microsoft.Component.MSBuild', '-products', '*')

  if (Get-Member -InputObject $vsRequirements -Name 'version') {
    $args += '-version'
    $args += $vsRequirements.version
  }

  if (Get-Member -InputObject $vsRequirements -Name 'components') {
    foreach ($component in $vsRequirements.components) {
      $args += '-requires'
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

function InitializeBuildTool() {
  if (Test-Path variable:global:_BuildTool) {
    return $global:_BuildTool
  }

  if (-not $msbuildEngine) {
    $msbuildEngine = GetDefaultMSBuildEngine
  }

  # Initialize dotnet cli if listed in 'tools'
  $dotnetRoot = $null
  if (Get-Member -InputObject $GlobalJson.tools -Name 'dotnet') {
    $dotnetRoot = InitializeDotNetCli -install:$restore
  }

  if ($msbuildEngine -eq 'dotnet') {
    if (!$dotnetRoot) {
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "/global.json must specify 'tools.dotnet'."
      ExitWithExitCode 1
    }
    $dotnetPath = Join-Path $dotnetRoot (GetExecutableFileName 'dotnet')
    $buildTool = @{ Path = $dotnetPath; Command = 'msbuild'; Tool = 'dotnet'; Framework = 'netcoreapp2.1' }
  } elseif ($msbuildEngine -eq "vs") {
    try {
      $msbuildPath = InitializeVisualStudioMSBuild -install:$restore
    } catch {
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message $_
      ExitWithExitCode 1
    }

    $buildTool = @{ Path = $msbuildPath; Command = ""; Tool = "vs"; Framework = "net472" }
  } else {
    Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Unexpected value of -msbuildEngine: '$msbuildEngine'."
    ExitWithExitCode 1
  }

  return $global:_BuildTool = $buildTool
}

function GetDefaultMSBuildEngine() {
  # Presence of tools.vs indicates the repo needs to build using VS msbuild on Windows.
  if (Get-Member -InputObject $GlobalJson.tools -Name 'vs') {
    return 'vs'
  }

  if (Get-Member -InputObject $GlobalJson.tools -Name 'dotnet') {
    return 'dotnet'
  }

  Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "-msbuildEngine must be specified, or /global.json must specify 'tools.dotnet' or 'tools.vs'."
  ExitWithExitCode 1
}

function GetNuGetPackageCachePath() {
  if ($env:NUGET_PACKAGES -eq $null) {
    # Use local cache on CI to ensure deterministic build. 
    # Avoid using the http cache as workaround for https://github.com/NuGet/Home/issues/3116
    # use global cache in dev builds to avoid cost of downloading packages.
    # For directory normalization, see also: https://github.com/NuGet/Home/issues/7968
    if ($useGlobalNuGetCache) {
      $env:NUGET_PACKAGES = Join-Path $env:UserProfile '.nuget\packages\'
    } else {
      $env:NUGET_PACKAGES = Join-Path $RepoRoot '.packages\'
      $env:RESTORENOCACHE = $true
    }
  }

  return $env:NUGET_PACKAGES
}

# Returns a full path to an Arcade SDK task project file.
function GetSdkTaskProject([string]$taskName) {
  return Join-Path (Split-Path (InitializeToolset) -Parent) "SdkTasks\$taskName.proj"
}

function InitializeNativeTools() {
  if (-Not (Test-Path variable:DisableNativeToolsetInstalls) -And (Get-Member -InputObject $GlobalJson -Name "native-tools")) {
    $nativeArgs= @{}
    if ($ci) {
      $nativeArgs = @{
        InstallDirectory = "$ToolsDir"
      }
    }
    & "$PSScriptRoot/init-tools-native.ps1" @nativeArgs
  }
}

function InitializeToolset() {
  if (Test-Path variable:global:_ToolsetBuildProj) {
    return $global:_ToolsetBuildProj
  }

  $nugetCache = GetNuGetPackageCachePath

  $toolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Sdk'
  $toolsetLocationFile = Join-Path $ToolsetDir "$toolsetVersion.txt"

  if (Test-Path $toolsetLocationFile) {
    $path = Get-Content $toolsetLocationFile -TotalCount 1
    if (Test-Path $path) {
      return $global:_ToolsetBuildProj = $path
    }
  }

  if (-not $restore) {
    Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "Toolset version $toolsetVersion has not been restored."
    ExitWithExitCode 1
  }

  $buildTool = InitializeBuildTool

  $proj = Join-Path $ToolsetDir 'restore.proj'
  $bl = if ($binaryLog) { '/bl:' + (Join-Path $LogDir 'ToolsetRestore.binlog') } else { '' }

  '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' | Set-Content $proj

  MSBuild-Core $proj $bl /t:__WriteToolsetLocation /clp:ErrorsOnly`;NoSummary /p:__ToolsetLocationOutputFile=$toolsetLocationFile

  $path = Get-Content $toolsetLocationFile -Encoding UTF8 -TotalCount 1
  if (!(Test-Path $path)) {
    throw "Invalid toolset path: $path"
  }

  return $global:_ToolsetBuildProj = $path
}

function ExitWithExitCode([int] $exitCode) {
  if ($ci -and $prepareMachine) {
    Stop-Processes
  }
  exit $exitCode
}

function Stop-Processes() {
  Write-Host 'Killing running build processes...'
  foreach ($processName in $processesToStopOnExit) {
    Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process
  }
}

#
# Executes msbuild (or 'dotnet msbuild') with arguments passed to the function.
# The arguments are automatically quoted.
# Terminates the script if the build fails.
#
function MSBuild() {
  if ($pipelinesLog) {
    $buildTool = InitializeBuildTool

    if ($ci -and $buildTool.Tool -eq 'dotnet') {
      $env:NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS = 20
      $env:NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS = 20
      Write-PipelineSetVariable -Name 'NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS' -Value '20'
      Write-PipelineSetVariable -Name 'NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS' -Value '20'
    }

    $toolsetBuildProject = InitializeToolset
    $path = Split-Path -parent $toolsetBuildProject
    $path = Join-Path $path (Join-Path $buildTool.Framework 'Microsoft.DotNet.Arcade.Sdk.dll')
    $args += "/logger:$path"
  }

  MSBuild-Core @args
}

#
# Executes msbuild (or 'dotnet msbuild') with arguments passed to the function.
# The arguments are automatically quoted.
# Terminates the script if the build fails.
#
function MSBuild-Core() {
  if ($ci) {
    if (!$binaryLog -and !$excludeCIBinarylog) {
      Write-PipelineTelemetryError -Category 'Build' -Message 'Binary log must be enabled in CI build, or explicitly opted-out from with the -excludeCIBinarylog switch.'
      ExitWithExitCode 1
    }

    if ($nodeReuse) {
      Write-PipelineTelemetryError -Category 'Build' -Message 'Node reuse must be disabled in CI build.'
      ExitWithExitCode 1
    }
  }

  $buildTool = InitializeBuildTool

  $cmdArgs = "$($buildTool.Command) /m /nologo /clp:Summary /v:$verbosity /nr:$nodeReuse /p:ContinuousIntegrationBuild=$ci"

  if ($warnAsError) {
    $cmdArgs += ' /warnaserror /p:TreatWarningsAsErrors=true'
  }
  else {
    $cmdArgs += ' /p:TreatWarningsAsErrors=false'
  }

  foreach ($arg in $args) {
    if ($arg -ne $null -and $arg.Trim() -ne "") {
      $cmdArgs += " `"$arg`""
    }
  }

  $env:ARCADE_BUILD_TOOL_COMMAND = "$($buildTool.Path) $cmdArgs"

  $exitCode = Exec-Process $buildTool.Path $cmdArgs

  if ($exitCode -ne 0) {
    Write-PipelineTelemetryError -Category 'Build' -Message 'Build failed.'

    $buildLog = GetMSBuildBinaryLogCommandLineArgument $args
    if ($buildLog -ne $null) {
      Write-Host "See log: $buildLog" -ForegroundColor DarkGray
    }

    ExitWithExitCode $exitCode
  }
}

function GetMSBuildBinaryLogCommandLineArgument($arguments) {
  foreach ($argument in $arguments) {
    if ($argument -ne $null) {
      $arg = $argument.Trim()
      if ($arg.StartsWith('/bl:', "OrdinalIgnoreCase")) {
        return $arg.Substring('/bl:'.Length)
      }

      if ($arg.StartsWith('/binaryLogger:', 'OrdinalIgnoreCase')) {
        return $arg.Substring('/binaryLogger:'.Length)
      }
    }
  }

  return $null
}

function GetExecutableFileName($baseName) {
  if (IsWindowsPlatform) {
    return "$baseName.exe"
  }
  else {
    return $baseName
  }
}

function IsWindowsPlatform() {
  return [environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
}

function Get-Darc($version) {
  $darcPath  = "$TempDir\darc\$(New-Guid)"
  if ($version -ne $null) {
    & $PSScriptRoot\darc-init.ps1 -toolpath $darcPath -darcVersion $version | Out-Host
  } else {
    & $PSScriptRoot\darc-init.ps1 -toolpath $darcPath | Out-Host
  }
  return "$darcPath\darc.exe"
}

. $PSScriptRoot\pipeline-logging-functions.ps1

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$EngRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$ToolsetDir = Join-Path $ArtifactsDir 'toolset'
$ToolsDir = Join-Path $RepoRoot '.tools'
$LogDir = Join-Path (Join-Path $ArtifactsDir 'log') $configuration
$TempDir = Join-Path (Join-Path $ArtifactsDir 'tmp') $configuration
$GlobalJson = Get-Content -Raw -Path (Join-Path $RepoRoot 'global.json') | ConvertFrom-Json
# true if global.json contains a "runtimes" section
$globalJsonHasRuntimes = if ($GlobalJson.tools.PSObject.Properties.Name -Match 'runtimes') { $true } else { $false }

Create-Directory $ToolsetDir
Create-Directory $TempDir
Create-Directory $LogDir

Write-PipelineSetVariable -Name 'Artifacts' -Value $ArtifactsDir
Write-PipelineSetVariable -Name 'Artifacts.Toolset' -Value $ToolsetDir
Write-PipelineSetVariable -Name 'Artifacts.Log' -Value $LogDir
Write-PipelineSetVariable -Name 'TEMP' -Value $TempDir
Write-PipelineSetVariable -Name 'TMP' -Value $TempDir

# Import custom tools configuration, if present in the repo.
# Note: Import in global scope so that the script set top-level variables without qualification.
if (!$disableConfigureToolsetImport) {
  $configureToolsetScript = Join-Path $EngRoot 'configure-toolset.ps1'
  if (Test-Path $configureToolsetScript) {
    . $configureToolsetScript
    if ((Test-Path variable:failOnConfigureToolsetError) -And $failOnConfigureToolsetError) {
      if ((Test-Path variable:LastExitCode) -And ($LastExitCode -ne 0)) {
        Write-PipelineTelemetryError -Category 'Build' -Message 'configure-toolset.ps1 returned a non-zero exit code'
        ExitWithExitCode $LastExitCode
      }
    }
  }
}
