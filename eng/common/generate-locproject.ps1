Param(
    [Parameter(Mandatory=$true)][string] $SourcesDirectory,     # Directory where source files live; if using a Localize directory it should live in here
    [string] $LanguageSet = 'VS_Main_Languages',                # Language set to be used in the LocProject.json
    [switch] $UseCheckedInLocProjectJson,                       # When set, generates a LocProject.json and compares it to one that already exists in the repo; otherwise just generates one
    [switch] $CreateNeutralXlfs                                 # Creates neutral xlf files. Only set to false when running locally
)

# Generates LocProject.json files for the OneLocBuild task. OneLocBuildTask is described here:
# https://ceapex.visualstudio.com/CEINTL/_wiki/wikis/CEINTL.wiki/107/Localization-with-OneLocBuild-Task

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
. $PSScriptRoot\tools.ps1

Import-Module -Name (Join-Path $PSScriptRoot 'native\CommonLibrary.psm1')

$exclusionsFilePath = "$SourcesDirectory\Localize\LocExclusions.json"
$exclusions = @{ Exclusions = @() }
if (Test-Path -Path $exclusionsFilePath)
{
    $exclusions = Get-Content "$exclusionsFilePath" | ConvertFrom-Json
}

Push-Location "$SourcesDirectory" # push location for Resolve-Path -Relative to work

# Template files
$jsonFiles = @()
$jsonFiles += Get-ChildItem -Recurse -Path "$SourcesDirectory" | Where-Object { $_.FullName -Match "\.template\.config\\localize\\en\..+\.json" } # .NET templating pattern
$jsonFiles += Get-ChildItem -Recurse -Path "$SourcesDirectory" | Where-Object { $_.FullName -Match "en\\strings\.json" } # current winforms pattern

$xlfFiles = @()

$allXlfFiles = Get-ChildItem -Recurse -Path "$SourcesDirectory\*\*.xlf"
$langXlfFiles = @()
if ($allXlfFiles) {
    $null = $allXlfFiles[0].FullName -Match "\.([\w-]+)\.xlf" # matches '[langcode].xlf'
    $firstLangCode = $Matches.1
    $langXlfFiles = Get-ChildItem -Recurse -Path "$SourcesDirectory\*\*.$firstLangCode.xlf"
}
$langXlfFiles | ForEach-Object {
    $null = $_.Name -Match "([^.]+)\.[\w-]+\.xlf" # matches '[filename].[langcode].xlf'
    
    $destinationFile = "$($_.Directory.FullName)\$($Matches.1).xlf"
    $xlfFiles += Copy-Item "$($_.FullName)" -Destination $destinationFile -PassThru
}

$locFiles = $jsonFiles + $xlfFiles

$locJson = @{
    Projects = @(
        @{
            LanguageSet = $LanguageSet
            LocItems = @(
                $locFiles | ForEach-Object {
                    $outputPath = "Localize\$(($_.DirectoryName | Resolve-Path -Relative) + "\")" 
                    $continue = $true
                    foreach ($exclusion in $exclusions.Exclusions) {
                        if ($outputPath.Contains($exclusion))
                        {
                            $continue = $false
                        }
                    }
                    $sourceFile = ($_.FullName | Resolve-Path -Relative)
                    if (!$CreateNeutralXlfs -and $_.Extension -eq '.xlf') {
                        Remove-Item -Path $sourceFile
                    }
                    if ($continue)
                    {
                        return @{
                            SourceFile = $sourceFile
                            CopyOption = "LangIDOnName"
                            OutputPath = $outputPath
                        }
                    }
                }
            )
        }
    )
}

$json = ConvertTo-Json $locJson -Depth 5
Write-Host "(NETCORE_ENGINEERING_TELEMETRY=Build) LocProject.json generated:`n`n$json`n`n"
Pop-Location

if (!$UseCheckedInLocProjectJson) {
    New-Item "$SourcesDirectory\Localize\LocProject.json" -Force # Need this to make sure the Localize directory is created
    Set-Content "$SourcesDirectory\Localize\LocProject.json" $json
}
else {
    New-Item "$SourcesDirectory\Localize\LocProject-generated.json" -Force # Need this to make sure the Localize directory is created
    Set-Content "$SourcesDirectory\Localize\LocProject-generated.json" $json

    if ((Get-FileHash "$SourcesDirectory\Localize\LocProject-generated.json").Hash -ne (Get-FileHash "$SourcesDirectory\Localize\LocProject.json").Hash) {
        Write-PipelineTaskError -Type "warning" -Message "Existing LocProject.json differs from generated LocProject.json. Download LocProject-generated.json and compare them."
        
        exit 1
    }
    else {
        Write-Host "Generated LocProject.json and current LocProject.json are identical."
    }
}