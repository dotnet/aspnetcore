# This file is a temporary workaround for internal builds to be able to restore from private AzDO feeds.
# This file should be removed as part of this issue: https://github.com/dotnet/arcade/issues/4080
#
# What the script does is iterate over all package sources in the pointed NuGet.config and add a credential entry
# under <packageSourceCredentials> for each Maestro managed private feed. Two additional credential 
# entries are also added for the two private static internal feeds: dotnet3-internal and dotnet3-internal-transport.
#
# This script needs to be called in every job that will restore packages and which the base repo has
# private AzDO feeds in the NuGet.config.
#
# See example YAML call for this script below. Note the use of the variable `$(dn-bot-dnceng-artifact-feeds-rw)`
# from the AzureDevOps-Artifact-Feeds-Pats variable group.
#
#  - task: PowerShell@2
#    displayName: Setup Private Feeds Credentials
#    condition: eq(variables['Agent.OS'], 'Windows_NT')
#    inputs:
#      filePath: $(Build.SourcesDirectory)/eng/common/SetupNugetSources.ps1
#      arguments: -ConfigFile $(Build.SourcesDirectory)/NuGet.config -Password $Env:Token
#    env:
#      Token: $(dn-bot-dnceng-artifact-feeds-rw)

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$ConfigFile,
    [Parameter(Mandatory = $true)][string]$Password
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

. $PSScriptRoot\tools.ps1

# Add source entry to PackageSources
function AddPackageSource($sources, $SourceName, $SourceEndPoint, $creds, $Username, $Password) {
    $packageSource = $sources.SelectSingleNode("add[@key='$SourceName']")
    
    if ($packageSource -eq $null)
    {
        $packageSource = $doc.CreateElement("add")
        $packageSource.SetAttribute("key", $SourceName)
        $packageSource.SetAttribute("value", $SourceEndPoint)
        $sources.AppendChild($packageSource) | Out-Null
    }
    else {
        Write-Host "Package source $SourceName already present."
    }
    
    AddCredential -Creds $creds -Source $SourceName -Username $Username -Password $Password
}

# Add a credential node for the specified source
function AddCredential($creds, $source, $username, $password) {
    # Looks for credential configuration for the given SourceName. Create it if none is found.
    $sourceElement = $creds.SelectSingleNode($Source)
    if ($sourceElement -eq $null)
    {
        $sourceElement = $doc.CreateElement($Source)
        $creds.AppendChild($sourceElement) | Out-Null
    }

    # Add the <Username> node to the credential if none is found.
    $usernameElement = $sourceElement.SelectSingleNode("add[@key='Username']")
    if ($usernameElement -eq $null)
    {
        $usernameElement = $doc.CreateElement("add")
        $usernameElement.SetAttribute("key", "Username")
        $sourceElement.AppendChild($usernameElement) | Out-Null
    }
    $usernameElement.SetAttribute("value", $Username)

    # Add the <ClearTextPassword> to the credential if none is found.
    # Add it as a clear text because there is no support for encrypted ones in non-windows .Net SDKs.
    #   -> https://github.com/NuGet/Home/issues/5526
    $passwordElement = $sourceElement.SelectSingleNode("add[@key='ClearTextPassword']")
    if ($passwordElement -eq $null)
    {
        $passwordElement = $doc.CreateElement("add")
        $passwordElement.SetAttribute("key", "ClearTextPassword")
        $sourceElement.AppendChild($passwordElement) | Out-Null
    }
    $passwordElement.SetAttribute("value", $Password)
}

function InsertMaestroPrivateFeedCredentials($Sources, $Creds, $Username, $Password) {
    $maestroPrivateSources = $Sources.SelectNodes("add[contains(@key,'darc-int')]")

    Write-Host "Inserting credentials for $($maestroPrivateSources.Count) Maestro's private feeds."
    
    ForEach ($PackageSource in $maestroPrivateSources) {
        Write-Host "`tInserting credential for Maestro's feed:" $PackageSource.Key
        AddCredential -Creds $creds -Source $PackageSource.Key -Username $Username -Password $Password
    }
}

if (!(Test-Path $ConfigFile -PathType Leaf)) {
  Write-PipelineTelemetryError -Category 'Build' -Message "Eng/common/SetupNugetSources.ps1 returned a non-zero exit code. Couldn't find the NuGet config file: $ConfigFile"
  ExitWithExitCode 1
}

if (!$Password) {
    Write-PipelineTelemetryError -Category 'Build' -Message 'Eng/common/SetupNugetSources.ps1 returned a non-zero exit code. Please supply a valid PAT'
    ExitWithExitCode 1
}

# Load NuGet.config
$doc = New-Object System.Xml.XmlDocument
$filename = (Get-Item $ConfigFile).FullName
$doc.Load($filename)

# Get reference to <PackageSources> or create one if none exist already
$sources = $doc.DocumentElement.SelectSingleNode("packageSources")
if ($sources -eq $null) {
    $sources = $doc.CreateElement("packageSources")
    $doc.DocumentElement.AppendChild($sources) | Out-Null
}

# Looks for a <PackageSourceCredentials> node. Create it if none is found.
$creds = $doc.DocumentElement.SelectSingleNode("packageSourceCredentials")
if ($creds -eq $null) {
    $creds = $doc.CreateElement("packageSourceCredentials")
    $doc.DocumentElement.AppendChild($creds) | Out-Null
}

$userName = "dn-bot"

# Insert credential nodes for Maestro's private feeds
InsertMaestroPrivateFeedCredentials -Sources $sources -Creds $creds -Username $userName -Password $Password

$dotnet31Source = $sources.SelectSingleNode("add[@key='dotnet3.1']")
if ($dotnet31Source -ne $null) {
    AddPackageSource -Sources $sources -SourceName "dotnet3.1-internal" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal/nuget/v2" -Creds $creds -Username $userName -Password $Password
    AddPackageSource -Sources $sources -SourceName "dotnet3.1-internal-transport" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/_packaging/dotnet3.1-internal-transport/nuget/v2" -Creds $creds -Username $userName -Password $Password
}

$dotnet5Source = $sources.SelectSingleNode("add[@key='dotnet5']")
if ($dotnet5Source -ne $null) {
    AddPackageSource -Sources $sources -SourceName "dotnet5-internal" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet5-internal/nuget/v2" -Creds $creds -Username $userName -Password $Password
    AddPackageSource -Sources $sources -SourceName "dotnet5-internal-transport" -SourceEndPoint "https://pkgs.dev.azure.com/dnceng/internal/_packaging/dotnet5-internal-transport/nuget/v2" -Creds $creds -Username $userName -Password $Password
}

$doc.Save($filename)
