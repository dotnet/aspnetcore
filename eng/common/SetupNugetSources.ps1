# This script adds internal feeds required to build commits that depend on internal package sources. For instance,
# dotnet6-internal would be added automatically if dotnet6 was found in the nuget.config file. In addition also enables
# disabled internal Maestro (darc-int*) feeds.
# 
# Optionally, this script also adds a credential entry for each of the internal feeds if supplied. This credential
# is added via the standard environment variable VSS_NUGET_EXTERNAL_FEED_ENDPOINTS. See
# https://github.com/microsoft/artifacts-credprovider/tree/v1.1.1?tab=readme-ov-file#environment-variables for more details
#
# See example call for this script below.
#
#  - task: PowerShell@2
#    displayName: Setup Internal Feeds
#    condition: eq(variables['Agent.OS'], 'Windows_NT')
#    inputs:
#      filePath: $(Build.SourcesDirectory)/eng/common/SetupNugetSources.ps1
#      arguments: -ConfigFile $(Build.SourcesDirectory)/NuGet.config
#  - task: NuGetAuthenticate@1
# 
# Note that the NuGetAuthenticate task should be called after SetupNugetSources.
# This ensures that:
# - Appropriate creds are set for the added internal feeds (if not supplied to the scrupt)
# - The credential provider is installed
#
# This logic is also abstracted into enable-internal-sources.yml.

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$ConfigFile,
    [string]$Password
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

. $PSScriptRoot\tools.ps1

$feedEndpoints = $null

# If a credential is provided, ensure that we don't overwrite the current set of
# credentials that may have been provided by a previous call to the credential provider.
if ($Password -and $null -ne $env:VSS_NUGET_EXTERNAL_FEED_ENDPOINTS) {
    $feedEndpoints = $env:VSS_NUGET_EXTERNAL_FEED_ENDPOINTS | ConvertFrom-Json
} elseif ($Password) {
    $feedEndpoints = @{ endpointCredentials = @() }
}

# Add source entry to PackageSources
function AddPackageSource($sources, $SourceName, $SourceEndPoint, $pwd) {
    $packageSource = $sources.SelectSingleNode("add[@key='$SourceName']")
    
    if ($null -eq $packageSource)
    {
        Write-Host "`tAdding package source" $SourceName
        $packageSource = $doc.CreateElement("add")
        $packageSource.SetAttribute("key", $SourceName)
        $packageSource.SetAttribute("value", $SourceEndPoint)
        $sources.AppendChild($packageSource) | Out-Null
    }
    else {
        Write-Host "Package source $SourceName already present."
    }

    if ($pwd) {
        $feedEndpoints.endpointCredentials = AddCredential -endpointCredentials $feedEndpoints.endpointCredentials -source $SourceEndPoint -pwd $pwd
    }
}

# Add a new feed endpoint credential
function AddCredential([array]$endpointCredentials, $source, $pwd) {
    $endpointCredentials += @{
        endpoint = $source;
        password = $pwd
    }
    return $endpointCredentials
}

function InsertMaestroInternalFeedCredentials($Sources, $pwd) {
    $maestroInternalSources = $Sources.SelectNodes("add[contains(@key,'darc-int')]")

    ForEach ($PackageSource in $maestroInternalSources) {
        Write-Host "`tAdding credential for Maestro's feed:" $PackageSource.Key
        $feedEndpoints.endpointCredentials = AddCredential -endpointCredentials $feedEndpoints.endpointCredentials -source $PackageSource.value -pwd $pwd
    }
}

function EnableInternalPackageSources($DisabledPackageSources) {
    $maestroInternalSources = $DisabledPackageSources.SelectNodes("add[contains(@key,'darc-int')]")
    ForEach ($DisabledPackageSource in $maestroInternalSources) {
        Write-Host "`tEnsuring internal source '$($DisabledPackageSource.key)' is enabled by deleting it from disabledPackageSource"
        # Due to https://github.com/NuGet/Home/issues/10291, we must actually remove the disabled entries
        $DisabledPackageSources.RemoveChild($DisabledPackageSource)
    }
}

if (!(Test-Path $ConfigFile -PathType Leaf)) {
  Write-PipelineTelemetryError -Category 'Build' -Message "Eng/common/SetupNugetSources.ps1 returned a non-zero exit code. Couldn't find the NuGet config file: $ConfigFile"
  ExitWithExitCode 1
}

# Load NuGet.config
$doc = New-Object System.Xml.XmlDocument
$filename = (Get-Item $ConfigFile).FullName
$doc.Load($filename)

# Get reference to <PackageSources> or create one if none exist already
$sources = $doc.DocumentElement.SelectSingleNode("packageSources")
if ($null -eq $sources) {
    $sources = $doc.CreateElement("packageSources")
    $doc.DocumentElement.AppendChild($sources) | Out-Null
}

# Check for disabledPackageSources; we'll enable any darc-int ones we find there
$disabledSources = $doc.DocumentElement.SelectSingleNode("disabledPackageSources")
if ($null -ne $disabledSources) {
    Write-Host "Checking for any darc-int disabled package sources in the disabledPackageSources node"
    EnableInternalPackageSources -DisabledPackageSources $disabledSources
}

if ($Password) {
    InsertMaestroInternalFeedCredentials -Sources $sources -pwd $Password
}

# 3.1 uses a different feed url format so it's handled differently here
$dotnet31Source = $sources.SelectSingleNode("add[@key='dotnet3.1']")
if ($null -ne $dotnet31Source) {
    AddPackageSource -Sources $sources -SourceName "dotnet3.1-internal" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal/nuget/v3/index.json" -pwd $Password
    AddPackageSource -Sources $sources -SourceName "dotnet3.1-internal-transport" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal-transport/nuget/v3/index.json" -pwd $Password
}

$dotnetVersions = @('5','6','7','8')

foreach ($dotnetVersion in $dotnetVersions) {
    $feedPrefix = "dotnet" + $dotnetVersion;
    $dotnetSource = $sources.SelectSingleNode("add[@key='$feedPrefix']")
    if ($dotnetSource) {
        AddPackageSource -Sources $sources -SourceName "$feedPrefix-internal" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/internal/_packaging/$feedprefix-internal/nuget/v3/index.json" -pwd $Password
        AddPackageSource -Sources $sources -SourceName "$feedPrefix-internal-transport" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/internal/_packaging/$feedPrefix-internal-transport/nuget/v3/index.json" -pwd $Password
    }
}

$doc.Save($filename)

# If any credentials were added or altered, update the VSS_NUGET_EXTERNAL_FEED_ENDPOINTS environment variable
if ($null -ne $feedEndpoints) {
    # ci is set to true so vso logging commands will be used.
    $ci = $true
    Write-PipelineSetVariable -Name 'VSS_NUGET_EXTERNAL_FEED_ENDPOINTS' -Value $($feedEndpoints | ConvertTo-Json) -IsMultiJobVariable $false
    Write-PipelineSetVariable -Name 'NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED' -Value "False" -IsMultiJobVariable $false
}