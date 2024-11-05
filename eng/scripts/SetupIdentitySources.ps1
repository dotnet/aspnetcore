[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$ConfigFile,
    [Parameter(Mandatory = $true)][string]$IdentityModelPackageSource
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Add source entry to PackageSources
function AddPackageSource($sources, $SourceName, $SourceEndPoint) {
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
}

if (!(Test-Path $ConfigFile -PathType Leaf)) {
  Write-PipelineTelemetryError -Category 'Build' -Message "eng/scripts/SetupIdentitySources.ps1 returned a non-zero exit code. Couldn't find the NuGet config file: $ConfigFile"
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

AddPackageSource -Sources $sources -SourceName "identitymodel-nightlies" -SourceEndPoint $IdentityModelPackageSource

$doc.Save($filename)