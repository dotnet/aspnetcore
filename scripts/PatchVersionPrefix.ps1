<#
.SYNOPSIS
    Updates the version.props file in repos to a newer patch version
.PARAMETER Repos
    A list of the repositories that should be patched
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Repos
)

$ErrorActionPreference = 'Stop'

function SaveXml($xml, [string]$path) {
    Write-Verbose "Saving to $path"
    $ErrorActionPreference = 'stop'

    $settings = New-Object System.XML.XmlWriterSettings
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding( $true )
    $writer = [System.XML.XMLTextWriter]::Create($path, $settings)
    $xml.Save($writer)
    $writer.Close()
}

function LoadXml([string]$path) {
    Write-Verbose "Reading to $path"

    $ErrorActionPreference = 'stop'
    $obj = new-object xml
    $obj.PreserveWhitespace = $true
    $obj.Load($path)
    return $obj
}

function BumpPatch([System.Xml.XmlNode]$node) {
    if (-not $node) {
        return
    }
    [version] $version = $node.InnerText
    $node.InnerText = "{0}.{1}.{2}" -f $version.Major, $version.Minor, ($version.Build + 1)
}

foreach ($repo in $Repos) {
    $path = "$PSScriptRoot/../modules/$repo/version.props"
    if (-not (Test-Path $path)) {
        Write-Warning "$path does not exist"
        continue
    }
    $path = Resolve-Path $path
    Write-Verbose "$path"
    [xml] $xml = LoadXml $path

    $suffix = $xml.SelectSingleNode('/Project/PropertyGroup/VersionSuffix')
    if (-not $suffix) {
        write-error "$path does not have VersionSuffix"
    }

    $versionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/VersionPrefix')
    $epxVersionPrefix = $xml.SelectSingleNode('/Project/PropertyGroup/ExperimentalProjectVersionPrefix')
    BumpPatch $epxVersionPrefix
    BumpPatch $versionPrefix
    SaveXml $xml $path
}

