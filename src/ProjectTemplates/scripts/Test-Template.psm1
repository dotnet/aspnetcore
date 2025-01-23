#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Test-Template {
    [CmdletBinding()]
    param (
        [string] $TemplateName,
        [string[]] $TemplateArguments,
        [string] $TemplatePackagePath = "Microsoft.DotNet.Web.ProjectTemplates.*-dev.nupkg",
        [string] $PackagePattern = "(?<PackageId>([A-Za-z]+(\.[A-Za-z]+)*))\.(?<Version>\d+\.\d)\.(?<Suffix>.*)",
        [string] $MainProjectRelativePath = $null,
        [ValidateSet("Debug", "Release")]
        [string] $Configuration = "Release",
        [ValidatePattern("net\d+\.\d+")]
        [string] $TargetFramework = "net10.0"
    )

    if(-not (Test-Path "$PSScriptRoot/.dotnet")){
        $dotnetFolder = Get-Command dotnet | Select-Object -ExpandProperty Source | Split-Path -Parent;
        Write-Verbose "Copying dotnet folder from $dotnetFolder to $PSScriptRoot/.dotnet";
        Copy-Item -Path $dotnetFolder -Destination "$PSScriptRoot/.dotnet" -Recurse;
    }

    Write-Verbose "Patching Microsoft.AspNetCore.App";
    $builtRuntime = Resolve-Path "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/aspnetcore-runtime-*-dev-win-x64.zip" | Where-Object { $_ -match "aspnetcore-runtime-[0-9.]+-dev-win-x64.zip" };
    Write-Verbose "Patching Microsoft.AspNetCore.App from $builtRuntime";
    Remove-Item "$PSScriptRoot/.runtime" -Recurse -ErrorAction Ignore;
    Expand-Archive -Path $builtRuntime -DestinationPath "$PSScriptRoot/.runtime" -Force;
    Remove-Item "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App/*-dev" -Recurse -ErrorAction Ignore;
    Write-Verbose "Copying $PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App to $PSScriptRoot/.dotnet/shared";
    Copy-Item -Path "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App" -Destination "$PSScriptRoot/.dotnet/shared" -Recurse -Force;

    $env:DOTNET_ROOT = "$PSScriptRoot/.dotnet";
    $env:DOTNET_ROOT_X86 = "$PSScriptRoot/.dotnet";
    $env:Path = "$PSScriptRoot/.dotnet;$env:Path";
    $tmpDir = "$PSScriptRoot/$templateName";
    Remove-Item -Path $tmpDir -Recurse -ErrorAction Ignore;
    Push-Location ..;
    try {
        dotnet pack
    }
    finally {
        Pop-Location;
    }

    $PackagePath = Resolve-Path "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/$TemplatePackagePath";

    $PackageName = (Get-Item $PackagePath).Name;

    if (-not (Test-Path "$($env:USERPROFILE)/.templateengine/packages/$PackageName")) {
        Write-Verbose "Installing package from $PackagePath";
        dotnet new install $PackagePath;
    }
    else {
        Write-Verbose "Uninstalling package from $PackagePath";
        if (-not ($PackageName -match $PackagePattern)) {
            Write-Error "$PackageName did not match $PackagePattern";
        }
        $PackageId = $Matches["PackageId"];
        $PackageVersion = $Matches["Version"];
        Write-Verbose "Uninstalling existing package $PackageId.$PackageVersion";
        dotnet new uninstall "$PackageId.$PackageVersion";

        Write-Verbose "Installing package from $PackagePath";
        dotnet new install $PackagePath;
    }


    Write-Verbose "Creating directory $tmpDir"
    New-Item -ErrorAction Ignore -Path $tmpDir -ItemType Directory | Out-Null;
    Push-Location $tmpDir -StackName TemplateFolder;
    try {
        $TemplateArguments = , "new" + $TemplateArguments + , "--no-restore";
        Write-Verbose "Running dotnet command with arguments: $TemplateArguments";
        dotnet @TemplateArguments;

        $proj = Get-ChildItem $tmpDir -Recurse -File -Filter '*.csproj' -Depth 3;
        if ($proj.Length -eq 0) {
            $proj = Get-ChildItem $tmpDir -Recurse -File -Filter '*.fsproj' -Depth 3;
        }

        $importPath = "$PSScriptRoot/../test/Templates.Tests/bin/$Configuration/$TargetFramework/TestTemplates";
        # Define the XML string literals
        [xml]$importPropsXml = "<Import Project='$importPath/Directory.Build.props' />";
        [xml]$importTargetsXml = "<Import Project='$importPath/Directory.Build.targets' />";
        [xml]$propertyGroupXml = @"
<PropertyGroup>
    <DisablePackageReferenceRestrictions>true</DisablePackageReferenceRestrictions>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
</PropertyGroup>
"@;

        foreach ($projPath in $proj) {
            Write-Verbose "Updating project file '$projPath'";
            # Read the XML content from the file
            [xml]$xmlContent = Get-Content -Path $projPath;

            # Find the Project element and add the new elements
            $projectElement = $xmlContent.Project;
            $projectElement.PrependChild($xmlContent.ImportNode($propertyGroupXml.PropertyGroup, $true)) | Out-Null;
            $projectElement.PrependChild($xmlContent.ImportNode($importTargetsXml.Import, $true)) | Out-Null;
            $projectElement.PrependChild($xmlContent.ImportNode($importPropsXml.Import, $true)) | Out-Null;

            # Save the modified XML content back to the file
            $xmlContent.Save($projPath);
        }

        if ($null -ne $MainProjectRelativePath) {
            Push-Location $MainProjectRelativePath;
        }

        if ('--auth' -in $TemplateArguments -and 'Individual' -in $TemplateArguments) {
            Write-Verbose "Running dotnet ef migrations"
            dotnet.exe ef migrations add Initial;
        }

        $publishOutputDir = "./.publish";
        Write-Verbose "Running dotnet publish --configuration $Configuration --output $publishOutputDir";
        dotnet.exe publish --configuration $Configuration --output $publishOutputDir;
    }
    finally {
        Pop-Location -StackName TemplateFolder;
    }
}

Export-ModuleMember Test-Template;
