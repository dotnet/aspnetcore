<#
.Synopsis
This script is a workaround for the limitations in darc and resolving diamond dependencies

.Example
./UpdateDependencies.ps1 -workingdir ../../AspNetCore -channel release

#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Release', 'Dev')]
    $Channel,
    $WorkingDir
)
$ErrorActionPreference = 'stop'
# Update the default TLS support to 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Invoke-Block([scriptblock]$cmd) {
    $cmd | Out-String | Write-Verbose
    & $cmd

    # Need to check both of these cases for errors as they represent different items
    # - $?: did the powershell script block throw an error
    # - $lastexitcode: did a windows command executed by the script block end in error
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        if ($error -ne $null) {
            Write-Warning $error[0]
        }
        throw "Command failed to execute: $cmd"
    }
}


function SaveXml([xml]$xml, [string]$path, [switch]$OmitXmlDeclaration) {
    Write-Verbose "Saving to $path"

    $settings = New-Object System.XML.XmlWriterSettings
    $settings.OmitXmlDeclaration = $OmitXmlDeclaration
    $settings.Encoding = New-Object System.Text.UTF8Encoding( $true )
    $writer = [System.XML.XMLTextWriter]::Create($path, $settings)
    $xml.Save($writer)
    $writer.Close()
}

function LoadXml([string]$path) {
    Write-Verbose "Reading from $path"

    $obj = new-object xml
    $obj.PreserveWhitespace = $true
    $obj.Load($path)
    Write-Output $obj
}

function UpdateFromChain {
    param(
        [xml]$details,
        [string]$principalRepo,
        [string]$principalPackage,
        [string]$dependentRepo,
        [string]$dependentPackage
    )
    $sha = $details.SelectSingleNode("//Dependency[`@Name=`"$principalPackage`"]/Sha").InnerText
    $uri = "https://raw.githubusercontent.com/$principalRepo/$sha/eng/Version.Details.xml"
    $temp = "$env:TEMP/details.xml"
    Invoke-WebRequest -uri $uri -o $temp
    [xml] $extVersions = Get-Content $temp

    $dependentVersionDetails = $extVersions.SelectSingleNode("//Dependency[`@Name=`"$dependentPackage`"]")
    $preReleaseLabel = $dependentVersionDetails.Version.Substring( $dependentVersionDetails.Version.IndexOf('-'))
    $sha = $dependentVersionDetails.Sha

    foreach ($dep in $details.SelectNodes('//Dependency')) {
        if ($dep.Uri -eq "https://github.com/$dependentRepo") {
            $oldVersion = $dep.Version.Substring(0, $dep.Version.IndexOf('-'))
            $newVersion = "${oldVersion}${preReleaseLabel}"
            Write-Host "Updating $($dep.Name) from '$($dep.Version)' to '$newVersion'" 
            $dep.Sha = $sha
            $dep.Version = $newVersion
        }
    }
    Write-Output $details
}

function UpdateFromEFCore {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [xml]$details
    )

    UpdateFromChain $details `
        -principalRepo 'aspnet/EntityFrameworkCore' `
        -principalPackage 'Microsoft.EntityFrameworkCore' `
        -dependentRepo 'aspnet/Extensions' `
        -dependentPackage 'Microsoft.Extensions.Logging'
}

function UpdateFromExtensions {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [xml]$details,
        $depName = 'Microsoft.Extensions.Logging'
    )
    UpdateFromChain $details `
        -principalRepo 'aspnet/Extensions' `
        -principalPackage $depName `
        -dependentRepo 'dotnet/core-setup' `
        -dependentPackage 'Microsoft.NETCore.App'
}

function UpdateFromCoreSetup {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [xml]$details
    )
    UpdateFromChain $details `
        -principalRepo 'dotnet/core-setup' `
        -principalPackage 'Microsoft.NETCore.App' `
        -dependentRepo 'dotnet/corefx' `
        -dependentPackage 'Microsoft.NETCore.Platforms'
}

function UpdateProps([xml]$details, [string] $propsPath) {
    $versionProps = LoadXml $propsPath
    [System.Xml.XmlNamespaceManager] $nsMgr = New-Object -TypeName System.Xml.XmlNamespaceManager($versionProps.NameTable)
    $nsMgr.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003");

    foreach ($dep in $details.SelectNodes('//Dependency')) {
        $varName = $dep.Name -replace '\.',''
        $varName = $varName -replace '\-',''
        $varName = "${varName}PackageVersion"
        $versionVar = $versionProps.SelectSingleNode("//PropertyGroup/$varName")
        if ($versionVar -eq $null) {
            $versionVar = $versionProps.SelectSingleNode("//ns:PropertyGroup/ns:$varName", $nsMgr)
            
            if ($versionVar -eq $null) {
                Write-Warning "Could not find $varName in Versions.props"
                continue
            }
        }
        $versionVar.InnerText = $dep.Version
    }

    SaveXml $versionProps $propsPath -OmitXmlDeclaration
}

if (-not $WorkingDir) {
    $WorkingDir = Get-Location
}
$WorkingDir = Resolve-Path $WorkingDir
Push-Location $WorkingDir
try {

    
    $currentRepo = git remote get-url --push origin
    $currentRepo = $currentRepo -replace 'https://github\.com/', ''
    $currentRepo = $currentRepo -replace 'git@github.com:', ''
    $currentRepo = $currentRepo -replace '\.git', ''
    $detailsPath = "$WorkingDir/eng/Version.Details.xml"
    $propsPath = "$WorkingDir/eng/Versions.props"

    if ($currentRepo -eq 'aspnet/AspNetCore') {
        Invoke-Block { & darc update-dependencies --channel ".NET Core 3 $Channel" --source-repo 'aspnet/EntityFrameworkCore' }
        Invoke-Block { & darc update-dependencies --channel ".NET Core 3 $Channel" --source-repo 'aspnet/AspNetCore-Tooling' }
        [xml]$versionDetails = LoadXml $detailsPath `
            | UpdateFromEFCore `
            | UpdateFromExtensions `
            | UpdateFromCoreSetup
    }
    elseif ($currentRepo -eq 'aspnet/EntityFrameworkCore') {
        Invoke-Block { & darc update-dependencies --channel ".NET Core 3 $Channel" --source-repo 'aspnet/Extensions' }
        [xml]$versionDetails = LoadXml $detailsPath `
            | UpdateFromExtensions `
            | UpdateFromCoreSetup
    }
    elseif ($currentRepo -eq 'aspnet/AspNetCore-Tooling') {
        Invoke-Block { & darc update-dependencies --channel ".NET Core 3 $Channel" --source-repo 'aspnet/Extensions' }
        [xml]$versionDetails = LoadXml $detailsPath `
            | UpdateFromExtensions -depName 'Microsoft.Extensions.CommandLineUtils.Sources' `
            | UpdateFromCoreSetup
    }
    elseif ($currentRepo -eq 'aspnet/Extensions') {
        Invoke-Block { & darc update-dependencies --channel ".NET Core 3 $Channel" --source-repo 'dotnet/core-setup' }
        [xml]$versionDetails = LoadXml $detailsPath `
            | UpdateFromCoreSetup
    }
    else {
        throw "This script does not support updating dependencies for $currentRepo"
    }

    SaveXml $versionDetails $detailsPath
    UpdateProps $versionDetails $propsPath
}
finally {
    Pop-Location
}