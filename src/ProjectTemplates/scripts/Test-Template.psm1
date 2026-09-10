#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Test-Template {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string] $TemplateName,
        [Parameter(Mandatory = $true)]
        [string[]] $TemplateArguments,
        [string] $TemplatePackagePath = "Microsoft.DotNet.Web.ProjectTemplates.*-dev.nupkg",
        [string] $TemplateProjectPath = "Web.ProjectTemplates/Microsoft.DotNet.Web.ProjectTemplates.csproj",
        [string] $PackagePattern = "(?<PackageId>([A-Za-z]+(\.[A-Za-z]+)*))\.(?<Version>\d+\.\d)\.(?<Suffix>.*)",
        [string] $MainProjectRelativePath = $null,
        [ValidateSet("Debug", "Release")]
        [string] $Configuration = "Release",
        [ValidatePattern("net\d+\.\d+")]
        [string] $TargetFramework = "net11.0",
        [switch] $NoRestore,
        [string[]] $PublishArguments = @()
    )

    $isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
    if (-not $isWindows -or [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne [System.Runtime.InteropServices.Architecture]::X64) {
        throw "The local template scripts currently require Windows x64."
    }

    $repoRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
    $projectTemplatesRoot = (Resolve-Path "$PSScriptRoot/..").Path
    $shippingPackagesPath = "$repoRoot/artifacts/packages/$Configuration/Shipping"
    $nonShippingPackagesPath = "$repoRoot/artifacts/packages/$Configuration/NonShipping"
    $testTemplatesPath = "$projectTemplatesRoot/test/Templates.Tests/bin/$Configuration/$TargetFramework/TestTemplates"
    $templateProject = "$projectTemplatesRoot/$TemplateProjectPath"
    $tmpDir = "$PSScriptRoot/$TemplateName"

    Remove-Item -Path $tmpDir -Recurse -ErrorAction Ignore

    $dotnetCommand = Get-Command dotnet
    $repoDotNetRoot = (Resolve-Path "$repoRoot/.dotnet").Path
    $isolatedDotNetRoot = "$PSScriptRoot/.dotnet"
    $usesRepoSdk = $dotnetCommand.Source.StartsWith($repoDotNetRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $usesIsolatedSdk = $dotnetCommand.Source.StartsWith($isolatedDotNetRoot, [System.StringComparison]::OrdinalIgnoreCase)
    if (-not $usesRepoSdk -and -not $usesIsolatedSdk) {
        throw "Activate the repository SDK with '. ./activate.ps1' before running this script."
    }

    $builtRuntimes = @(Get-ChildItem -Path $shippingPackagesPath -Filter "aspnetcore-runtime-*-dev-win-x64.zip" -File -ErrorAction Ignore)
    if ($builtRuntimes.Count -ne 1) {
        throw "Expected exactly one locally built ASP.NET Core runtime archive in '$shippingPackagesPath', but found $($builtRuntimes.Count). Run the full product build described in src/ProjectTemplates/README.md."
    }

    foreach ($requiredPath in @(
        $nonShippingPackagesPath,
        "$testTemplatesPath/Directory.Build.props",
        "$testTemplatesPath/Directory.Build.targets",
        $templateProject
    )) {
        if (-not (Test-Path $requiredPath)) {
            throw "Required local template artifact '$requiredPath' is missing. Run its producer build described in src/ProjectTemplates/README.md."
        }
    }

    $packArguments = @(
        "pack",
        $templateProject,
        "--no-dependencies",
        "--configuration",
        $Configuration
    )
    if ($NoRestore) {
        $packArguments += "--no-restore"
    }
    Invoke-DotNet -Arguments $packArguments

    $templatePackages = @(Get-ChildItem -Path $shippingPackagesPath -Filter $TemplatePackagePath -File)
    if ($templatePackages.Count -ne 1) {
        throw "Expected exactly one template package matching '$TemplatePackagePath' in '$shippingPackagesPath', but found $($templatePackages.Count)."
    }

    if (-not (Test-Path $isolatedDotNetRoot)) {
        Write-Verbose "Copying the repository SDK from $repoDotNetRoot to $isolatedDotNetRoot"
        Copy-Item -Path $repoDotNetRoot -Destination $isolatedDotNetRoot -Recurse
    }

    $builtRuntime = $builtRuntimes[0].FullName
    Write-Verbose "Patching Microsoft.AspNetCore.App from $builtRuntime"
    Remove-Item "$PSScriptRoot/.runtime" -Recurse -ErrorAction Ignore
    Expand-Archive -Path $builtRuntime -DestinationPath "$PSScriptRoot/.runtime" -Force
    Remove-Item "$isolatedDotNetRoot/shared/Microsoft.AspNetCore.App/*-dev" -Recurse -ErrorAction Ignore
    Copy-Item -Path "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App" -Destination "$isolatedDotNetRoot/shared" -Recurse -Force

    $env:DOTNET_ROOT = $isolatedDotNetRoot
    $env:DOTNET_ROOT_X86 = $isolatedDotNetRoot
    if (-not $env:Path.StartsWith("$isolatedDotNetRoot;", [System.StringComparison]::OrdinalIgnoreCase)) {
        $env:Path = "$isolatedDotNetRoot;$env:Path"
    }

    $packagePath = $templatePackages[0].FullName
    $packageName = $templatePackages[0].Name

    if (-not (Test-Path "$($env:USERPROFILE)/.templateengine/packages/$packageName")) {
        Write-Verbose "Installing package from $packagePath"
        Invoke-DotNet -Arguments @("new", "install", $packagePath)
    }
    else {
        if (-not ($packageName -match $PackagePattern)) {
            throw "$packageName did not match $PackagePattern."
        }

        $packageId = $Matches["PackageId"]
        $packageVersion = $Matches["Version"]
        Write-Verbose "Uninstalling existing package $packageId.$packageVersion"
        Invoke-DotNet -Arguments @("new", "uninstall", "$packageId.$packageVersion")

        Write-Verbose "Installing package from $packagePath"
        Invoke-DotNet -Arguments @("new", "install", $packagePath)
    }

    Write-Verbose "Creating directory $tmpDir"
    New-Item -Path $tmpDir -ItemType Directory | Out-Null
    Push-Location $tmpDir -StackName TemplateFolder
    try {
        $newArguments = @("new") + $TemplateArguments + @("--no-restore")
        Write-Verbose "Running dotnet command with arguments: $newArguments"
        Invoke-DotNet -Arguments $newArguments

        $projects = @(Get-ChildItem $tmpDir -Recurse -File -Filter '*.csproj')
        if ($projects.Count -eq 0) {
            $projects = @(Get-ChildItem $tmpDir -Recurse -File -Filter '*.fsproj')
        }
        if ($projects.Count -eq 0) {
            throw "The template did not create a project beneath '$tmpDir'."
        }

        [xml]$importPropsXml = "<Import Project='$testTemplatesPath/Directory.Build.props' />"
        [xml]$importTargetsXml = "<Import Project='$testTemplatesPath/Directory.Build.targets' />"
        [xml]$propertyGroupXml = @"
<PropertyGroup>
    <DisablePackageReferenceRestrictions>true</DisablePackageReferenceRestrictions>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
</PropertyGroup>
"@

        foreach ($project in $projects) {
            $projectPath = $project.FullName
            Write-Verbose "Updating project file '$projectPath'"
            [xml]$xmlContent = Get-Content -Path $projectPath

            $projectElement = $xmlContent.Project
            $projectElement.PrependChild($xmlContent.ImportNode($propertyGroupXml.PropertyGroup, $true)) | Out-Null
            $projectElement.PrependChild($xmlContent.ImportNode($importTargetsXml.Import, $true)) | Out-Null
            $projectElement.PrependChild($xmlContent.ImportNode($importPropsXml.Import, $true)) | Out-Null

            $xmlContent.Save($projectPath)
        }

        $mainProjectDirectory = $tmpDir
        if ($null -ne $MainProjectRelativePath) {
            $mainProjectDirectory = Join-Path $tmpDir $MainProjectRelativePath
        }

        $mainProjects = @(Get-ChildItem $mainProjectDirectory -File -Filter '*.csproj')
        if ($mainProjects.Count -eq 0) {
            $mainProjects = @(Get-ChildItem $mainProjectDirectory -File -Filter '*.fsproj')
        }
        if ($mainProjects.Count -ne 1) {
            throw "Expected exactly one main project in '$mainProjectDirectory', but found $($mainProjects.Count)."
        }

        $mainProject = $mainProjects[0].FullName
        if ('--auth' -in $TemplateArguments -and 'Individual' -in $TemplateArguments) {
            Invoke-DotNet -Arguments @("restore", $mainProject)
            Write-Verbose "Running dotnet ef migrations"
            Invoke-DotNet -Arguments @(
                "ef",
                "migrations",
                "add",
                "Initial",
                "--project",
                $mainProject,
                "--startup-project",
                $mainProject
            )
        }

        $publishOutputDir = Join-Path $mainProjectDirectory ".publish"
        $dotnetPublishArguments = @(
            "publish",
            $mainProject,
            "--configuration",
            $Configuration,
            "--output",
            $publishOutputDir
        )
        $dotnetPublishArguments += $PublishArguments
        Invoke-DotNet -Arguments $dotnetPublishArguments

        Write-Host "Published $TemplateName to $publishOutputDir"
        Write-Host "Run the generated application with the isolated SDK under $isolatedDotNetRoot."
    }
    finally {
        Pop-Location -StackName TemplateFolder
    }
}

Export-ModuleMember Test-Template
