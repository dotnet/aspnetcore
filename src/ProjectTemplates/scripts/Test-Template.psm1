#!/usr/bin/env pwsh
#requires -version 4

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

function Check-DiagnosticsDll {
    param(
        [string]$Path,
        [string]$Description
    )

    if (Test-Path $Path) {
        $fileInfo = Get-Item $Path;
        Write-Verbose "[dll check] $Description - found - Size: $($fileInfo.Length) bytes, Modified: $($fileInfo.LastWriteTime)";
        return $true;
    } else {
        Write-Verbose "[dll check] $Description - not found at $Path";
        return $false;
    }
}

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

    Write-Verbose "=== DOTNET ENVIRONMENT DEBUG START ==="
    Write-Verbose "PSScriptRoot: $PSScriptRoot"
    Write-Verbose "Current DOTNET_ROOT: $($env:DOTNET_ROOT)"
    Write-Verbose "Current DOTNET_ROOT_X86: $($env:DOTNET_ROOT_X86)"
    Write-Verbose "Current PATH: $($env:PATH)"

    # Check what Get-Command dotnet finds
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetCommand) {
        Write-Verbose "Get-Command dotnet found: $($dotnetCommand.Source)"
        Write-Verbose "Dotnet version from Get-Command location:"
        & $dotnetCommand.Source --version | ForEach-Object { Write-Verbose "  $_" }
    } else {
        Write-Verbose "Get-Command dotnet found: NONE"
    }

    if(-not (Test-Path "$PSScriptRoot/.dotnet")){
        Write-Verbose "Local .dotnet folder does not exist, creating it..."

        # Original logic: use Get-Command dotnet
        $dotnetFolder = Get-Command dotnet | Select-Object -ExpandProperty Source | Split-Path -Parent;
        Write-Verbose "Dotnet folder from Get-Command: $dotnetFolder"

        # Let's also check what's in the repo root
        $repoRoot = "$PSScriptRoot/../../../";
        $repoDotnetFolder = "$repoRoot/.dotnet";
        Write-Verbose "Repository root: $repoRoot"
        Write-Verbose "Repository .dotnet folder: $repoDotnetFolder"
        Write-Verbose "Repository .dotnet exists: $(Test-Path $repoDotnetFolder)"

        if (Test-Path $repoDotnetFolder) {
            Write-Verbose "Repository .dotnet contents:"
            Get-ChildItem $repoDotnetFolder -ErrorAction SilentlyContinue | ForEach-Object { Write-Verbose "  $($_.Name)" }

            # Check version in repo dotnet
            $repoDotnetExe = "$repoDotnetFolder/dotnet.exe"
            if (Test-Path $repoDotnetExe) {
                Write-Verbose "Repository dotnet version:"
                & $repoDotnetExe --version | ForEach-Object { Write-Verbose "  $_" }
            }
        }

        Write-Verbose "Copying dotnet folder from $dotnetFolder to $PSScriptRoot/.dotnet";
        Copy-Item -Path $dotnetFolder -Destination "$PSScriptRoot/.dotnet" -Recurse;

        Write-Verbose "Copy completed. Verifying local .dotnet folder..."
        if (Test-Path "$PSScriptRoot/.dotnet") {
            Write-Verbose "Local .dotnet folder created successfully"
            $localDotnetExe = "$PSScriptRoot/.dotnet/dotnet.exe"
            if (Test-Path $localDotnetExe) {
                Write-Verbose "Local dotnet version after copy:"
                & $localDotnetExe --version | ForEach-Object { Write-Verbose "  $_" }
            }
        } else {
            Write-Verbose "ERROR: Local .dotnet folder was not created!"
        }
    } else {
        Write-Verbose "Local .dotnet folder already exists"
        $localDotnetExe = "$PSScriptRoot/.dotnet/dotnet.exe"
        if (Test-Path $localDotnetExe) {
            Write-Verbose "Existing local dotnet version:"
            & $localDotnetExe --version | ForEach-Object { Write-Verbose "  $_" }
        }
    }

    Write-Verbose "Patching Microsoft.AspNetCore.App";

    # [dll check] 1) Check Microsoft.AspNetCore.Diagnostics.dll in PSScriptRoot before patching
    $psScriptDiagnosticsDll = "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App/10.0.0-dev/Microsoft.AspNetCore.Diagnostics.dll";
    Check-DiagnosticsDll -Path $psScriptDiagnosticsDll -Description "1) PSScriptRoot Microsoft.AspNetCore.Diagnostics.dll before patching";

    $builtRuntime = Resolve-Path "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/aspnetcore-runtime-*-dev-win-x64.zip" | Where-Object { $_ -match "aspnetcore-runtime-[0-9.]+-dev-win-x64.zip" };
    Write-Verbose "Built runtime package: $builtRuntime";

    # [dll check] 3) Check Microsoft.AspNetCore.Diagnostics.dll in artifacts
    $artifactsDiagnosticsDll = "$PSScriptRoot/../../../artifacts/bin/Microsoft.AspNetCore.Diagnostics/$Configuration/$TargetFramework/Microsoft.AspNetCore.Diagnostics.dll";
    if (-not (Check-DiagnosticsDll -Path $artifactsDiagnosticsDll -Description "3) Artifacts Microsoft.AspNetCore.Diagnostics.dll")) {
        # Try alternative location
        $artifactsDiagnosticsDll2 = "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/Microsoft.AspNetCore.Diagnostics.$TargetFramework.*.nupkg";
        $artifactsNupkg = Get-ChildItem $artifactsDiagnosticsDll2 -ErrorAction SilentlyContinue | Select-Object -First 1;
        if ($artifactsNupkg) {
            Write-Verbose "[dll check] Found Microsoft.AspNetCore.Diagnostics nupkg: $($artifactsNupkg.FullName)";
        } else {
            Write-Verbose "[dll check] No Microsoft.AspNetCore.Diagnostics nupkg found in artifacts";
        }
    }

    Write-Verbose "Patching Microsoft.AspNetCore.App from $builtRuntime";
    Remove-Item "$PSScriptRoot/.runtime" -Recurse -ErrorAction Ignore;
    Expand-Archive -Path $builtRuntime -DestinationPath "$PSScriptRoot/.runtime" -Force;

    Write-Verbose "Extracted runtime contents:";
    if (Test-Path "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App") {
        Get-ChildItem "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App" | ForEach-Object { Write-Verbose "  $($_.Name)" }
    }

    # [dll check] 2) Check Microsoft.AspNetCore.Diagnostics.dll in unpacked zip contents
    $unpackedDiagnosticsDll = "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App/10.0.0-dev/Microsoft.AspNetCore.Diagnostics.dll";
    if (-not (Check-DiagnosticsDll -Path $unpackedDiagnosticsDll -Description "2) Unpacked Microsoft.AspNetCore.Diagnostics.dll")) {
        Write-Warning "[dll check] Critical: Unpacked Microsoft.AspNetCore.Diagnostics.dll not found!";
    }

    Write-Verbose "Removing ALL existing ASP.NET Core runtimes from local .dotnet...";
    try {
        $aspNetCoreRuntimePath = "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App";
        if (Test-Path $aspNetCoreRuntimePath) {
            $existingRuntimes = Get-ChildItem $aspNetCoreRuntimePath -ErrorAction SilentlyContinue;
            if ($existingRuntimes) {
                Write-Verbose "Found existing ASP.NET Core runtimes to remove: $($existingRuntimes.Name -join ', ')";
                Remove-Item $aspNetCoreRuntimePath -Recurse -Force -ErrorAction Stop;
                Write-Verbose "Successfully removed all existing ASP.NET Core runtimes";

                # Recreate the directory
                New-Item -Path $aspNetCoreRuntimePath -ItemType Directory -Force | Out-Null;
                Write-Verbose "Recreated ASP.NET Core runtime directory";
            } else {
                Write-Verbose "No existing ASP.NET Core runtimes found to remove";
            }
        } else {
            Write-Verbose "ASP.NET Core runtime directory does not exist";
            # Create the directory
            New-Item -Path $aspNetCoreRuntimePath -ItemType Directory -Force | Out-Null;
            Write-Verbose "Created ASP.NET Core runtime directory";
        }
    }
    catch {
        Write-Warning "Failed to remove existing ASP.NET Core runtimes: $($_.Exception.Message)";
        # Continue anyway - this might not be critical
    }

    Write-Verbose "Before patching - local .dotnet ASP.NET Core runtimes:";
    if (Test-Path "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App") {
        Get-ChildItem "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App" | ForEach-Object { Write-Verbose "  $($_.Name)" }
    }

    Write-Verbose "Copying $PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App to $PSScriptRoot/.dotnet/shared";
    try {
        Copy-Item -Path "$PSScriptRoot/.runtime/shared/Microsoft.AspNetCore.App" -Destination "$PSScriptRoot/.dotnet/shared" -Recurse -Force -ErrorAction Stop;
        Write-Verbose "Runtime copy completed successfully";
    }
    catch {
        Write-Error "Failed to copy runtime: $($_.Exception.Message)";
        throw;
    }

    Write-Verbose "After patching - local .dotnet ASP.NET Core runtimes:";
    if (Test-Path "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App") {
        Get-ChildItem "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App" | ForEach-Object { Write-Verbose "  $($_.Name)" }
    }

    # Verify critical files were patched correctly
    Write-Verbose "Verifying patched runtime files...";

    # [dll check] 4) Check Microsoft.AspNetCore.Diagnostics.dll after patching (existing check)
    $devRuntimePath = "$PSScriptRoot/.dotnet/shared/Microsoft.AspNetCore.App/10.0.0-dev";
    if (Test-Path $devRuntimePath) {
        $diagnosticsDll = "$devRuntimePath/Microsoft.AspNetCore.Diagnostics.dll";
        if (-not (Check-DiagnosticsDll -Path $diagnosticsDll -Description "4) Microsoft.AspNetCore.Diagnostics.dll after patching")) {
            Write-Warning "[dll check] Critical: Microsoft.AspNetCore.Diagnostics.dll not found in patched runtime!";
        }
    } else {
        Write-Warning "[dll check] Dev runtime folder not found at $devRuntimePath";
    }

    Write-Verbose "=== SETTING UP ENVIRONMENT ==="
    Write-Verbose "Setting DOTNET_ROOT to: $PSScriptRoot/.dotnet"
    $env:DOTNET_ROOT = "$PSScriptRoot/.dotnet";
    Write-Verbose "Setting DOTNET_ROOT_X86 to: $PSScriptRoot/.dotnet"
    $env:DOTNET_ROOT_X86 = "$PSScriptRoot/.dotnet";
    Write-Verbose "Prepending to PATH: $PSScriptRoot/.dotnet"
    $env:Path = "$PSScriptRoot/.dotnet;$env:Path";

    Write-Verbose "Final environment variables:"
    Write-Verbose "  DOTNET_ROOT: $($env:DOTNET_ROOT)"
    Write-Verbose "  DOTNET_ROOT_X86: $($env:DOTNET_ROOT_X86)"
    Write-Verbose "  PATH (first 200 chars): $($env:PATH.Substring(0, [Math]::Min(200, $env:PATH.Length)))"

    # Verify which dotnet we'll use after env changes
    $finalDotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($finalDotnetCommand) {
        Write-Verbose "After env setup, Get-Command dotnet finds: $($finalDotnetCommand.Source)"
        Write-Verbose "Final dotnet version:"
        & $finalDotnetCommand.Source --version | ForEach-Object { Write-Verbose "  $_" }
        Write-Verbose "Final dotnet --info:"
        & $finalDotnetCommand.Source --info | ForEach-Object { Write-Verbose "  $_" }
    }
    Write-Verbose "=== DOTNET ENVIRONMENT DEBUG END ==="
    $tmpDir = "$PSScriptRoot/$templateName";
    Write-Verbose "Template working directory: $tmpDir"
    Remove-Item -Path $tmpDir -Recurse -ErrorAction Ignore;

    # Check if template packages exist (should be built by main CI build process)
    $PackagePathPattern = "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/$TemplatePackagePath"
    Write-Verbose "Looking for template packages at: $PackagePathPattern"

    $PackagePath = Get-ChildItem -Path "$PSScriptRoot/../../../artifacts/packages/$Configuration/Shipping/" -Filter $TemplatePackagePath -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $PackagePath) {
        Write-Error "Template packages not found at $PackagePathPattern. Ensure the main build has completed with -pack option before running template tests."
        return
    }

    $PackagePath = $PackagePath.FullName
    Write-Verbose "Template package path: $PackagePath"

    $PackageName = (Get-Item $PackagePath).Name;
    Write-Verbose "Template package name: $PackageName"

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
        Write-Verbose "Current working directory: $(Get-Location)"
        Write-Verbose "About to run dotnet new - checking dotnet version one more time:"
        dotnet --version | ForEach-Object { Write-Verbose "  $_" }

        dotnet @TemplateArguments;

        Write-Verbose "Template creation completed. Checking created files:"
        Get-ChildItem . -Recurse -File | Select-Object -First 10 | ForEach-Object { Write-Verbose "  $($_.FullName)" }

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
        Write-Verbose "Publishing template to: $publishOutputDir"
        Write-Verbose "About to run dotnet publish - final dotnet version check:"
        dotnet --version | ForEach-Object { Write-Verbose "  $_" }
        Write-Verbose "Running dotnet publish --configuration $Configuration --output $publishOutputDir";
        dotnet.exe publish --configuration $Configuration --output $publishOutputDir;
    }
    finally {
        Pop-Location -StackName TemplateFolder;
    }
}

Export-ModuleMember Test-Template;
