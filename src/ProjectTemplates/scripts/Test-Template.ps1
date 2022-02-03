#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Test-Template($templateName, $templateArgs, $templateNupkg, $isBlazorWasm) {
    if ($isBlazorWasm -and $templateArgs.Contains("hosted")) {
        $isBlazorWasmHosted = $true
    }
    else {
        $isBlazorWasmHosted = $false
    }
    $tmpDir = "$PSScriptRoot/$templateName"
    Remove-Item -Path $tmpDir -Recurse -ErrorAction Ignore
    Push-Location ..
    try {
        dotnet pack
    }
    finally {
        Pop-Location
    }

    Run-DotnetNew "--install", "$PSScriptRoot/../../../artifacts/packages/Debug/Shipping/$templateNupkg"

    New-Item -ErrorAction Ignore -Path $tmpDir -ItemType Directory
    Push-Location $tmpDir
    try {
        Run-DotnetNew $templateArgs, "--no-restore"

        if ($templateArgs -match 'F#') {
            $extension = "fsproj"
        }
        else {
            $extension = "csproj"
        }

        if ($isBlazorWasmHosted) {
            $proj = @("$tmpDir/Server/$templateName.Server.$extension",
                      "$tmpDir/Client/$templateName.Client.$extension",
                      "$tmpDir/Shared/$templateName.Shared.$extension")
        }
        else {
            $proj = @("$tmpDir/$templateName.$extension")
        }

        foreach ($projPath in $proj) {
            $projContent = Get-Content -Path $projPath -Raw
            if ($isBlazorWasmHosted) {
                $importPath = "$PSScriptRoot/../test/bin/Debug/net7.0/TestTemplates"
            }
            else {
                $importPath = "$PSScriptRoot/../test/bin/Debug/net7.0/TestTemplates"
            }
            $projContent = $projContent -replace ('(?:<Project Sdk="Microsoft.NET.(?<SdkSuffix>Sdk\.\w+)">)', ('<Project Sdk="Microsoft.NET.${SdkSuffix}">
                <Import Project="' + $importPath + '/Directory.Build.props" />
                <Import Project="' + $importPath + '/Directory.Build.targets" />
                <PropertyGroup>
                    <DisablePackageReferenceRestrictions>true</DisablePackageReferenceRestrictions>
                </PropertyGroup>'))
            $projContent | Set-Content $projPath
        }

        if ($isBlazorWasmHosted) {
            Push-Location Server
        }
        dotnet.exe ef migrations add mvc
        dotnet.exe publish --configuration Release
        Set-Location .\bin\Release\net7.0\publish
        if ($isBlazorWasm -eq $false) {
            Invoke-Expression "./$templateName.exe"
        }
        if ($isBlazorWasmHosted) {
            # Identity Server only runs in Development by default due to key signing requirements
            $env:ASPNETCORE_ENVIRONMENT="Development"
            Invoke-Expression "./$templateName.Server.exe"
            $env:ASPNETCORE_ENVIRONMENT=""
        }
    }
    finally {
        Pop-Location
        if ($isBlazorWasmHosted) {
            Pop-Location
        }
    }
}

function Run-DotnetNew($arguments) {
    $expression = "dotnet new $arguments"
    Invoke-Expression $expression
}
