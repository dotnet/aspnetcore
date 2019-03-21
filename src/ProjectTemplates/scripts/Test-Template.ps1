#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Test-Template($templateName, $templateArgs, $templateNupkg, $isSPA) {
    $tmpDir = "$PSScriptRoot/$templateName"
    Remove-Item -Path $tmpDir -Recurse -ErrorAction Ignore
    dotnet pack

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

        $proj = "$tmpDir/$templateName.$extension"
        $projContent = Get-Content -Path $proj -Raw
        $projContent = $projContent -replace ('<Project Sdk="Microsoft.NET.Sdk.Web">', "<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <Import Project=""$PSScriptRoot/../test/bin/Debug/netcoreapp3.0/TemplateTests.props"" />
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Sdk.Razor"" Version=""`$(MicrosoftNETSdkRazorPackageVersion)"" />
  </ItemGroup>
  <PropertyGroup>
    <DisablePackageReferenceRestrictions>true</DisablePackageReferenceRestrictions>
  </PropertyGroup>")
        $projContent | Set-Content $proj
        dotnet ef migrations add mvc
        dotnet publish --configuration Release
        dotnet bin\Release\netcoreapp3.0\publish\$templateName.dll
    }
    finally {
        Pop-Location
        Run-DotnetNew "--debug:reinit"
    }
}

function Run-DotnetNew($arguments) {
    $expression = "dotnet new $arguments"
    Invoke-Expression $expression
}
