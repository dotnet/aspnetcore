
[CmdletBinding()]
param(
    [switch]$NoCommit,
    [string]$GithubEmail,
    [string]$GithubUsername,
    [string]$GithubToken
)
# This script only works against master at the moment because only master prod-con builds allow you to access their results before the entire chain is finished.

$ErrorActionPreference = 'Stop'
Import-Module -Scope Local -Force "$PSScriptRoot/common.psm1"
Set-StrictMode -Version 1
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$githubRaw = "https://raw.githubusercontent.com"
$versionsRepo = "dotnet/versions"
$versionsBranch = "master"

$coreSetupRepo = "dotnet/core-setup"
$coreFxRepo = "dotnet/corefx"

$coreSetupVersions = "$githubRaw/$versionsRepo/$versionsBranch/build-info/$coreSetupRepo/master/Latest_Packages.txt"

$tempDir = "$PSScriptRoot/../obj"

mkdir -Path $tempDir -ErrorAction Ignore

$localCoreSetupVersions = "$tempDir/coresetup.packages"
Write-Host "Downloading $coreSetupVersions to $localCoreSetupVersions"
Invoke-WebRequest -OutFile $localCoreSetupVersions -Uri $coreSetupVersions

$msNetCoreAppPackageVersion = $null
$msNetCoreAppPackageName = "Microsoft.NETCore.App"

Set-GitHubInfo $GithubToken $GithubUsername $GithubEmail

$variables = @{}

foreach ($line in Get-Content $localCoreSetupVersions) {
    if ($line.StartsWith("$msNetCoreAppPackageName ")) {
        $msNetCoreAppPackageVersion = $line.Trim("$msNetCoreAppPackageName ")
    }
    $parts = $line.Split(' ')
    $packageName = $parts[0]

    $varName = "$packageName" + "PackageVersion"
    $varName = $varName.Replace('.', '')

    $packageVersion = $parts[1]
    if ($variables[$varName]) {
        if ($variables[$varName].Where( {$_ -eq $packageVersion}, 'First').Count -eq 0) {
            $variables[$varName] += $packageVersion
        }
    }
    else {
        $variables[$varName] = @($packageVersion)
    }
}

if (!$msNetCoreAppPackageVersion) {
    throw "$msNetCoreAppPackageName was not in $coreSetupVersions"
}

$coreAppDownloadLink = "https://dotnet.myget.org/F/dotnet-core/api/v2/package/$msNetCoreAppPackageName/$msNetCoreAppPackageVersion"
$netCoreAppNupkg = "$tempDir/microsoft.netcore.app.zip"
Invoke-WebRequest -OutFile $netCoreAppNupkg -Uri $coreAppDownloadLink
$expandedNetCoreApp = "$tempDir/microsoft.netcore.app/"
Expand-Archive -Path $netCoreAppNupkg -DestinationPath $expandedNetCoreApp -Force
$versionsTxt = "$expandedNetCoreApp/$msNetCoreAppPackageName.versions.txt"

$versionsCoreFxCommit = $null
foreach ($line in Get-Content $versionsTxt) {
    if ($line.StartsWith("dotnet/versions/corefx")) {
        $versionsCoreFxCommit = $line.Split(' ')[1]
        break
    }
}

if (!$versionsCoreFxCommit) {
    Throw "no 'dotnet/versions/corefx' in versions.txt of Microsoft.NETCore.App"
}

$coreFxVersionsUrl = "$githubRaw/$versionsRepo/$versionsCoreFxCommit/build-info/$coreFxRepo/$versionsBranch/Latest_Packages.txt"
$localCoreFxVersions = "$tempDir/$corefx.packages"
Invoke-WebRequest -OutFile $localCoreFxVersions -Uri $coreFxVersionsUrl

foreach ($line in Get-Content $localCoreFxVersions) {
    $parts = $line.Split(' ')

    $packageName = $parts[0]

    $varName = "$packageName" + "PackageVersion"
    $varName = $varName.Replace('.', '')
    $packageVersion = $parts[1]
    if ($variables[$varName]) {
        if ($variables[$varName].Where( {$_ -eq $packageVersion}, 'First').Count -eq 0) {
            $variables[$varName] += $packageVersion
        }
    }
    else {
        $variables[$varName] = @($packageVersion)
    }
}

$depsPath = Resolve-Path "$PSScriptRoot/../build/dependencies.props"
Write-Host "Loading deps from $depsPath"
[xml] $dependencies = LoadXml $depsPath

if (-not $NoCommit) {
    $baseBranch = "master"
    Invoke-Block { & git fetch origin }

    $currentBranch = Invoke-Block { & git rev-parse --abbrev-ref HEAD }
    $destinationBranch = "upgrade-netcore-deps"

    Invoke-Block { & git checkout -tb $destinationBranch "origin/$baseBranch" }
}

try {
    $updatedVars = UpdateVersions $variables $dependencies $depsPath
    if (-not $NoCommit) {
        $body = CommitUpdatedVersions $updatedVars $dependencies $depsPath "Upgrade to .NET Core $msNetCoreAppPackageVersion"

        if ($body) {
            CreatePR "aspnet" $GithubUsername $baseBranch $destinationBranch $body $GithubToken
        }
    }
}
finally {
    if (-not $NoCommit) {
        Invoke-Block { & git checkout $currentBranch }
    }
}
