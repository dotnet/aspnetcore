param(
    [string]$BuildXmlFile = $null
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

Push-Location $PSScriptRoot
try {

    if (!$BuildXmlFile)
    {
        Write-Error "No BuildXmlFile is provided. For 2.2 releases, this file should be in a location like \\vsufile\patches\sign\NET\CORE_BUILDS\2.2.X\2.2.9\20191125-01\assets\orchestration-metadata\manifests\build.xml."
        exit 1
    }

    [xml]$versionProps = Get-Content "$PSScriptRoot/../../../version.props"
    $LastVersion = "$($versionProps.Project.PropertyGroup.AspNetCoreMajorVersion).$($versionProps.Project.PropertyGroup.AspNetCoreMinorVersion | select -first 1).$($versionProps.Project.PropertyGroup.AspNetCorePatchVersion - 1)"

    [xml]$buildXml = Get-Content $BuildXmlFile
    $feedUrl = $buildXml.OrchestratedBuild.Endpoint.Url
    $baseFeedUrl = $feedUrl -replace 'final/index.json',''

    Write-Host "Last patch version = $LastVersion"
    Write-Host "BaseURL = $baseFeedUrl"

    function CreateBaseLineFromZip($url, $filePath) {
        dotnet run $url $filePath

        if ($lastexitcode -eq 404) {
            Write-Host -f Yellow "It appears there was no patch zip in the last release, so creating an empty baseline file in $filePath."
            Set-Content -path $filePath ''
        }
        elseif($lastexitcode -ne 0) {
            Write-Error "ZipGenerator failed"
            exit 1
        }
    }

    CreateBaseLineFromZip `
        "$baseFeedUrl/final/assets/aspnetcore/Runtime/${LastVersion}/nuGetPackagesArchive-ci-server-${LastVersion}.patch.zip" `
        "../Archive.CiServer.Patch/ArchiveBaseline.${LastVersion}.txt"

    CreateBaseLineFromZip `
        "$baseFeedUrl/final/assets/aspnetcore/Runtime/${LastVersion}/nuGetPackagesArchive-ci-server-compat-${LastVersion}.patch.zip" `
        "../Archive.CiServer.Patch.Compat/ArchiveBaseline.${LastVersion}.txt"
}
finally {
    Pop-Location
}
