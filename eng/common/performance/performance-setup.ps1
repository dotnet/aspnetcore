Param(
    [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,
    [string] $CoreRootDirectory,
    [string] $BaselineCoreRootDirectory,
    [string] $Architecture="x64",
    [string] $Framework="net5.0",
    [string] $CompilationMode="Tiered",
    [string] $Repository=$env:BUILD_REPOSITORY_NAME,
    [string] $Branch=$env:BUILD_SOURCEBRANCH,
    [string] $CommitSha=$env:BUILD_SOURCEVERSION,
    [string] $BuildNumber=$env:BUILD_BUILDNUMBER,
    [string] $RunCategories="Libraries Runtime",
    [string] $Csproj="src\benchmarks\micro\MicroBenchmarks.csproj",
    [string] $Kind="micro",
    [switch] $LLVM,
    [switch] $MonoInterpreter,
    [switch] $MonoAOT, 
    [switch] $Internal,
    [switch] $Compare,
    [string] $MonoDotnet="",
    [string] $Configurations="CompilationMode=$CompilationMode RunKind=$Kind"
)

$RunFromPerformanceRepo = ($Repository -eq "dotnet/performance") -or ($Repository -eq "dotnet-performance")
$UseCoreRun = ($CoreRootDirectory -ne [string]::Empty)
$UseBaselineCoreRun = ($BaselineCoreRootDirectory -ne [string]::Empty)

$PayloadDirectory = (Join-Path $SourceDirectory "Payload")
$PerformanceDirectory = (Join-Path $PayloadDirectory "performance")
$WorkItemDirectory = (Join-Path $SourceDirectory "workitem")
$ExtraBenchmarkDotNetArguments = "--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart --stopOnFirstError true"
$Creator = $env:BUILD_DEFINITIONNAME
$PerfLabArguments = ""
$HelixSourcePrefix = "pr"

$Queue = "Windows.10.Amd64.ClientRS4.DevEx.15.8.Open"

# TODO: Implement a better logic to determine if Framework is .NET Core or >= .NET 5.
if ($Framework.StartsWith("netcoreapp") -or ($Framework -eq "net5.0")) {
    $Queue = "Windows.10.Amd64.ClientRS5.Open"
}

if ($Compare) {
    $Queue = "Windows.10.Amd64.19H1.Tiger.Perf.Open"
    $PerfLabArguments = ""
    $ExtraBenchmarkDotNetArguments = ""
}

if ($Internal) {
    $Queue = "Windows.10.Amd64.19H1.Tiger.Perf"
    $PerfLabArguments = "--upload-to-perflab-container"
    $ExtraBenchmarkDotNetArguments = ""
    $Creator = ""
    $HelixSourcePrefix = "official"
}

if($MonoDotnet -ne "")
{
    $Configurations += " LLVM=$LLVM MonoInterpreter=$MonoInterpreter MonoAOT=$MonoAOT"
    if($ExtraBenchmarkDotNetArguments -eq "")
    {
        #FIX ME: We need to block these tests as they don't run on mono for now
        $ExtraBenchmarkDotNetArguments = "--exclusion-filter *Perf_Image* *Perf_NamedPipeStream*"
    }
    else
    {
        #FIX ME: We need to block these tests as they don't run on mono for now
        $ExtraBenchmarkDotNetArguments += " --exclusion-filter *Perf_Image* *Perf_NamedPipeStream*"
    }
}

# FIX ME: This is a workaround until we get this from the actual pipeline
$CommonSetupArguments="--channel master --queue $Queue --build-number $BuildNumber --build-configs $Configurations --architecture $Architecture"
$SetupArguments = "--repository https://github.com/$Repository --branch $Branch --get-perf-hash --commit-sha $CommitSha $CommonSetupArguments"


#This grabs the LKG version number of dotnet and passes it to our scripts
$VersionJSON = Get-Content global.json | ConvertFrom-Json
$DotNetVersion = $VersionJSON.tools.dotnet
$SetupArguments = "--dotnet-versions $DotNetVersion $SetupArguments"


if ($RunFromPerformanceRepo) {
    $SetupArguments = "--perf-hash $CommitSha $CommonSetupArguments"
    
    robocopy $SourceDirectory $PerformanceDirectory /E /XD $PayloadDirectory $SourceDirectory\artifacts $SourceDirectory\.git
}
else {
    git clone --branch master --depth 1 --quiet https://github.com/dotnet/performance $PerformanceDirectory
}

if($MonoDotnet -ne "")
{
    $UsingMono = "true"
    $MonoDotnetPath = (Join-Path $PayloadDirectory "dotnet-mono")
    Move-Item -Path $MonoDotnet -Destination $MonoDotnetPath
}

if ($UseCoreRun) {
    $NewCoreRoot = (Join-Path $PayloadDirectory "Core_Root")
    Move-Item -Path $CoreRootDirectory -Destination $NewCoreRoot
}
if ($UseBaselineCoreRun) {
    $NewBaselineCoreRoot = (Join-Path $PayloadDirectory "Baseline_Core_Root")
    Move-Item -Path $BaselineCoreRootDirectory -Destination $NewBaselineCoreRoot
}

$DocsDir = (Join-Path $PerformanceDirectory "docs")
robocopy $DocsDir $WorkItemDirectory

# Set variables that we will need to have in future steps
$ci = $true

. "$PSScriptRoot\..\pipeline-logging-functions.ps1"

# Directories
Write-PipelineSetVariable -Name 'PayloadDirectory' -Value "$PayloadDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'PerformanceDirectory' -Value "$PerformanceDirectory" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'WorkItemDirectory' -Value "$WorkItemDirectory" -IsMultiJobVariable $false

# Script Arguments
Write-PipelineSetVariable -Name 'Python' -Value "py -3" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'ExtraBenchmarkDotNetArguments' -Value "$ExtraBenchmarkDotNetArguments" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'SetupArguments' -Value "$SetupArguments" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'PerfLabArguments' -Value "$PerfLabArguments" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'BDNCategories' -Value "$RunCategories" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'TargetCsproj' -Value "$Csproj" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Kind' -Value "$Kind" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Architecture' -Value "$Architecture" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'UseCoreRun' -Value "$UseCoreRun" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'UseBaselineCoreRun' -Value "$UseBaselineCoreRun" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'RunFromPerfRepo' -Value "$RunFromPerformanceRepo" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Compare' -Value "$Compare" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'MonoDotnet' -Value "$UsingMono" -IsMultiJobVariable $false

# Helix Arguments
Write-PipelineSetVariable -Name 'Creator' -Value "$Creator" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'Queue' -Value "$Queue" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name 'HelixSourcePrefix' -Value "$HelixSourcePrefix" -IsMultiJobVariable $false
Write-PipelineSetVariable -Name '_BuildConfig' -Value "$Architecture.$Kind.$Framework" -IsMultiJobVariable $false

exit 0