<#
.SYNOPSIS
    Publishes a demo application as a self-contained deployment using a locally-built ASP.NET Core framework.

.DESCRIPTION
    This script automates the end-to-end process of:
      1. Building the ASP.NET Core repository to produce custom NuGet packages.
      2. Configuring the demo application to consume those packages.
      3. Publishing the demo app as a fully self-contained executable.
      4. Creating a portable source bundle so the presenter can open the project
         in VS Code without red squiggles (NuGet packages + global.json included).

    The output folder can be copied to another machine and run directly — no .NET SDK
    or runtime installation is required on the target machine.

    The source bundle folder can be opened in VS Code on a machine that has a
    compatible .NET SDK installed (same major/minor preview or newer).

    Works with any ASP.NET Core application type: Blazor Server, MVC, Razor Pages,
    Minimal APIs, gRPC, SignalR, etc. They all use the Microsoft.AspNetCore.App
    shared framework, which this script overrides.

.PARAMETER RepoPath
    Path to the local aspnetcore repository clone containing the custom changes.

.PARAMETER ProjectPath
    Path to the .csproj file of the demo application to publish.

.PARAMETER OutputPath
    Directory where the self-contained published application will be placed.

.PARAMETER BuildRepo
    If $true (default), builds the ASP.NET Core repository with '-all -pack -Configuration Release'
    before publishing. Set to $false to skip the build when the repo has already been built.

.EXAMPLE
    # Full build + publish
    .\PublishDemoApp.ps1 -RepoPath C:\code\aspnetcore -ProjectPath C:\demos\MyApp\MyApp.csproj -OutputPath C:\demos\publish

.EXAMPLE
    # Skip the repo build (already built)
    .\PublishDemoApp.ps1 -RepoPath C:\code\aspnetcore -ProjectPath C:\demos\MyApp\MyApp.csproj -OutputPath C:\demos\publish -BuildRepo $false

.NOTES
    The target runtime identifier (RID) is auto-detected from the current machine.
    If the presentation machine has a different OS/architecture, edit the $rid variable
    in this script or add a -RuntimeIdentifier parameter.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RepoPath,

    [Parameter(Mandatory)]
    [string]$ProjectPath,

    [Parameter(Mandatory)]
    [string]$OutputPath,

    [Parameter()]
    [bool]$BuildRepo = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ─────────────────────────────────────────────────────────────────────────────
# Helper functions
# ─────────────────────────────────────────────────────────────────────────────

$script:currentStep = $null

function Write-Step([string]$Message) {
    $script:currentStep = $Message
    Write-Host "`n====> $Message" -ForegroundColor Cyan
}

function Write-Detail([string]$Message) {
    Write-Host "      $Message" -ForegroundColor DarkGray
}

function Write-OK([string]$Message) {
    Write-Host "      $Message" -ForegroundColor Green
}

function Write-Warn([string]$Message) {
    Write-Host "      WARNING: $Message" -ForegroundColor Yellow
}

try {

# ─────────────────────────────────────────────────────────────────────────────
# Step 1 — Validate inputs
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Validating inputs"

if (-not (Test-Path $RepoPath -PathType Container)) {
    throw "Repository path not found: $RepoPath"
}
$RepoPath = (Resolve-Path $RepoPath).Path

if (-not (Test-Path $ProjectPath -PathType Leaf)) {
    throw "Project file not found: $ProjectPath"
}
$ProjectPath = (Resolve-Path $ProjectPath).Path
$ProjectDir  = Split-Path $ProjectPath -Parent

# OutputPath may not exist yet; resolve it without requiring existence.
$OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

Write-Detail "Repo:    $RepoPath"
Write-Detail "Project: $ProjectPath"
Write-Detail "Output:  $OutputPath"

# ─────────────────────────────────────────────────────────────────────────────
# Step 2 — Build the ASP.NET Core repository
# ─────────────────────────────────────────────────────────────────────────────

if ($BuildRepo) {
    Write-Step "Building ASP.NET Core repository (this will take a while)"

    $buildScript = Join-Path $RepoPath "eng\build.cmd"
    if (-not (Test-Path $buildScript)) {
        throw "Build script not found: $buildScript"
    }

    Push-Location $RepoPath
    try {
        & $buildScript -all -pack -Configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Repository build failed (exit code $LASTEXITCODE)."
        }
        Write-OK "Repository build completed."
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Step "Skipping repository build (BuildRepo = `$false)"
}

# ─────────────────────────────────────────────────────────────────────────────
# Step 3 — Discover build artifacts
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Discovering build artifacts"

$packagesDir = Join-Path $RepoPath "artifacts\packages\Release\Shipping"
if (-not (Test-Path $packagesDir)) {
    throw @"
Packages directory not found: $packagesDir
Ensure the repo has been built with: eng\build.cmd -all -pack -Configuration Release
"@
}

# --- Framework version (from the targeting-pack .nupkg filename) ---
$refPkg = Get-ChildItem $packagesDir -Filter "Microsoft.AspNetCore.App.Ref.*.nupkg" |
    Where-Object { $_.Name -notmatch '\.symbols\.' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $refPkg) {
    throw "Microsoft.AspNetCore.App.Ref package not found in: $packagesDir"
}

$customVersion = $refPkg.BaseName -replace '^Microsoft\.AspNetCore\.App\.Ref\.', ''
Write-Detail "Framework version: $customVersion"

# --- Target framework moniker ---
$versionsPropsPath = Join-Path $RepoPath "eng\Versions.props"
[xml]$versionsXml = Get-Content $versionsPropsPath
$tfm = ($versionsXml.Project.PropertyGroup |
    ForEach-Object { $_.DefaultNetCoreTargetFramework } |
    Where-Object { $_ -and $_.Trim() } |
    Select-Object -First 1)

if (-not $tfm) {
    throw "Could not determine DefaultNetCoreTargetFramework from: $versionsPropsPath"
}
Write-Detail "Target framework: $tfm"

# --- Runtime identifier ---
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$rid = if ($env:OS -eq 'Windows_NT' -or $IsWindows) { "win-$arch" }
       elseif ($IsMacOS)                              { "osx-$arch" }
       else                                           { "linux-$arch" }
Write-Detail "Runtime identifier: $rid"

# --- Verify the runtime pack for this RID exists ---
$runtimePkgName = "Microsoft.AspNetCore.App.Runtime.$rid.$customVersion.nupkg"
if (-not (Test-Path (Join-Path $packagesDir $runtimePkgName))) {
    $availableRids = Get-ChildItem $packagesDir -Filter "Microsoft.AspNetCore.App.Runtime.*.nupkg" |
        Where-Object { $_.Name -notmatch '\.symbols\.' } |
        ForEach-Object {
            if ($_.BaseName -match '^Microsoft\.AspNetCore\.App\.Runtime\.(.+)\.\d+\.\d+\.\d+') {
                $Matches[1]
            }
        } |
        Sort-Object -Unique
    throw @"
Runtime pack not found for RID '$rid' (looked for $runtimePkgName).
Available RIDs: $($availableRids -join ', ')
Build the repo for the target architecture or modify the `$rid variable in this script.
"@
}
Write-OK "All required packages found."

# ─────────────────────────────────────────────────────────────────────────────
# Step 4 — Validate demo project target framework
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Validating demo project"

[xml]$projectXml = Get-Content $ProjectPath
$projectTfm = ($projectXml.Project.PropertyGroup |
    ForEach-Object { $_.TargetFramework } |
    Where-Object { $_ -and $_.Trim() } |
    Select-Object -First 1)

if ($projectTfm) {
    if ($projectTfm -ne $tfm) {
        Write-Warn "Project targets '$projectTfm' but the repo builds '$tfm'."
        Write-Warn "The custom framework override applies only to '$tfm'."
        Write-Warn "Consider updating the demo project's <TargetFramework> to '$tfm'."
    }
    else {
        Write-OK "Project TFM '$projectTfm' matches repo TFM."
    }
}
else {
    Write-Detail "Could not determine project TFM (may use <TargetFrameworks> or an import)."
}

# ─────────────────────────────────────────────────────────────────────────────
# Step 5 — Configure NuGet package sources (idempotent)
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Configuring NuGet package sources"

$localSourceName  = "aspnetcore-local-dev"
$localSourceValue = $packagesDir

# Collect upstream feeds from the repo's NuGet.config so that .NET runtime
# preview packages (which may not be on nuget.org yet) resolve correctly.
$repoNugetConfigPath = Join-Path $RepoPath "NuGet.config"
$repoFeeds = [ordered]@{}
if (Test-Path $repoNugetConfigPath) {
    [xml]$repoNugetXml = Get-Content $repoNugetConfigPath
    foreach ($node in $repoNugetXml.SelectNodes("//configuration/packageSources/add")) {
        $key = $node.GetAttribute("key")
        $val = $node.GetAttribute("value")
        if ($key -and $val -and $val -match '^https?://') {
            $repoFeeds[$key] = $val
        }
    }
}

$nugetConfigPath = Join-Path $ProjectDir "NuGet.config"

if (Test-Path $nugetConfigPath) {
    # ── Update existing NuGet.config ──
    [xml]$nugetXml = Get-Content $nugetConfigPath

    $pkgSources = $nugetXml.SelectSingleNode("//configuration/packageSources")
    if (-not $pkgSources) {
        $pkgSources = $nugetXml.CreateElement("packageSources")
        $nugetXml.configuration.AppendChild($pkgSources) | Out-Null
    }

    $modified = $false

    # Ensure our local source is present (insert first for highest priority).
    $existing = $pkgSources.SelectSingleNode("add[@key='$localSourceName']")
    if ($existing) {
        if ($existing.GetAttribute("value") -ne $localSourceValue) {
            $existing.SetAttribute("value", $localSourceValue)
            $modified = $true
            Write-Detail "Updated path for '$localSourceName'."
        }
        else {
            Write-Detail "'$localSourceName' already configured."
        }
    }
    else {
        $el = $nugetXml.CreateElement("add")
        $el.SetAttribute("key", $localSourceName)
        $el.SetAttribute("value", $localSourceValue)
        if ($pkgSources.FirstChild) {
            # Skip past any <clear /> element.
            $insertBefore = $pkgSources.ChildNodes | Where-Object { $_.LocalName -ne 'clear' } | Select-Object -First 1
            if ($insertBefore) {
                $pkgSources.InsertBefore($el, $insertBefore) | Out-Null
            }
            else {
                $pkgSources.AppendChild($el) | Out-Null
            }
        }
        else {
            $pkgSources.AppendChild($el) | Out-Null
        }
        $modified = $true
        Write-Detail "Added '$localSourceName'."
    }

    # Ensure upstream preview feeds are present.
    foreach ($feedName in $repoFeeds.Keys) {
        if (-not $pkgSources.SelectSingleNode("add[@key='$feedName']")) {
            $el = $nugetXml.CreateElement("add")
            $el.SetAttribute("key", $feedName)
            $el.SetAttribute("value", $repoFeeds[$feedName])
            $pkgSources.AppendChild($el) | Out-Null
            $modified = $true
            Write-Detail "Added feed '$feedName'."
        }
    }

    if ($modified) {
        $nugetXml.Save($nugetConfigPath)
        Write-OK "Updated NuGet.config."
    }
    else {
        Write-OK "NuGet.config already up to date."
    }
}
else {
    # ── Create a new NuGet.config ──
    $sourceLines = [System.Collections.Generic.List[string]]::new()
    $sourceLines.Add("    <add key=`"$localSourceName`" value=`"$localSourceValue`" />")
    foreach ($feedName in $repoFeeds.Keys) {
        $sourceLines.Add("    <add key=`"$feedName`" value=`"$($repoFeeds[$feedName])`" />")
    }
    if (-not $repoFeeds.Contains("nuget.org")) {
        $sourceLines.Add("    <add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`" />")
    }

    $nugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
$($sourceLines -join "`n")
  </packageSources>
</configuration>
"@
    Set-Content -Path $nugetConfigPath -Value $nugetConfigContent -Encoding utf8
    Write-OK "Created NuGet.config."
}

# ─────────────────────────────────────────────────────────────────────────────
# Step 6 — Configure framework version override (idempotent)
# ─────────────────────────────────────────────────────────────────────────────
#
# MSBuild evaluates all <Update> operations after all <Include> operations,
# regardless of import order. This means our KnownFrameworkReference Update in
# Directory.Build.targets takes effect even though the SDK defines those items
# in a later import.  (Same mechanism the aspnetcore repo uses internally.)
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Configuring framework version override"

$targetsPath = Join-Path $ProjectDir "Directory.Build.targets"
$beginMarker = "<!-- BEGIN PublishDemoApp.ps1 CustomAspNetCoreFramework -->"
$endMarker   = "<!-- END PublishDemoApp.ps1 CustomAspNetCoreFramework -->"

$overrideBlock = @"
  $beginMarker
  <ItemGroup>
    <KnownFrameworkReference Update="Microsoft.AspNetCore.App">
      <TargetingPackVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</TargetingPackVersion>
      <LatestRuntimeFrameworkVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</LatestRuntimeFrameworkVersion>
      <DefaultRuntimeFrameworkVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</DefaultRuntimeFrameworkVersion>
    </KnownFrameworkReference>
  </ItemGroup>
  $endMarker
"@

if (Test-Path $targetsPath) {
    $content = Get-Content $targetsPath -Raw

    if ($content -match [regex]::Escape("BEGIN PublishDemoApp.ps1 CustomAspNetCoreFramework")) {
        # Replace the existing override block.
        $pattern = "(?s)\s*$([regex]::Escape($beginMarker)).*?$([regex]::Escape($endMarker))"
        $content = [regex]::Replace($content, $pattern, "`n$overrideBlock")
        Set-Content -Path $targetsPath -Value $content -NoNewline -Encoding utf8
        Write-OK "Updated framework override in Directory.Build.targets."
    }
    else {
        # Append the override block inside the existing <Project> element.
        $content = $content -replace '</Project>', "$overrideBlock`n</Project>"
        Set-Content -Path $targetsPath -Value $content -NoNewline -Encoding utf8
        Write-OK "Added framework override to existing Directory.Build.targets."
    }
}
else {
    $targetsContent = @"
<Project>
$overrideBlock
</Project>
"@
    Set-Content -Path $targetsPath -Value $targetsContent -Encoding utf8
    Write-OK "Created Directory.Build.targets with framework override."
}

# ─────────────────────────────────────────────────────────────────────────────
# Step 7 — Publish the demo app as self-contained
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Publishing demo app (self-contained, $rid)"

# Use the repo's locally-installed .NET SDK to guarantee the preview TFM is supported.
$dotnetRoot = Join-Path $RepoPath ".dotnet"
$dotnetExe  = if ($env:OS -eq 'Windows_NT' -or $IsWindows) { Join-Path $dotnetRoot "dotnet.exe" }
              else                                          { Join-Path $dotnetRoot "dotnet" }

if (-not (Test-Path $dotnetExe)) {
    throw @"
Repo .NET SDK not found at: $dotnetRoot
Run the repository restore first:  $RepoPath\restore.cmd
"@
}

Write-Detail "SDK:     $dotnetExe"
Write-Detail "RID:     $rid"
Write-Detail "Config:  Release"
Write-Detail "Output:  $OutputPath"

# Temporarily point at the repo's SDK for the publish.
$savedDotnetRoot = $env:DOTNET_ROOT
$savedPath       = $env:PATH

try {
    $env:DOTNET_ROOT = $dotnetRoot
    $env:PATH        = "$dotnetRoot$([System.IO.Path]::PathSeparator)$env:PATH"

    if (Test-Path $OutputPath) {
        Write-Detail "Cleaning existing output directory..."
        Remove-Item $OutputPath -Recurse -Force
    }

    & $dotnetExe publish $ProjectPath `
        --configuration Release `
        --runtime $rid `
        --self-contained `
        --output $OutputPath

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit code $LASTEXITCODE)."
    }
}
finally {
    $env:DOTNET_ROOT = $savedDotnetRoot
    $env:PATH        = $savedPath
}

# ─────────────────────────────────────────────────────────────────────────────
# Step 8 — Create portable source bundle
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Creating portable source bundle"

$bundlePath = Join-Path (Split-Path $ProjectDir -Parent) "$((Split-Path $ProjectDir -Leaf))-source-bundle"
if (Test-Path $bundlePath) {
    Remove-Item $bundlePath -Recurse -Force
}
New-Item -ItemType Directory -Path $bundlePath -Force | Out-Null

# --- Copy project source files (exclude build artifacts and publish output) ---
$excludeDirs = @('bin', 'obj', 'publish', 'source-bundle', 'local-packages')
Get-ChildItem $ProjectDir -Force | Where-Object {
    $excludeDirs -notcontains $_.Name
} | ForEach-Object {
    if ($_.PSIsContainer) {
        Copy-Item $_.FullName (Join-Path $bundlePath $_.Name) -Recurse -Force
    }
    else {
        Copy-Item $_.FullName $bundlePath -Force
    }
}
Write-Detail "Copied project source files."

# --- Copy shipping NuGet packages into local-packages/ ---
$localPkgDir = Join-Path $bundlePath "local-packages"
New-Item -ItemType Directory -Path $localPkgDir -Force | Out-Null
Copy-Item (Join-Path $packagesDir "*.nupkg") $localPkgDir -Force
# Remove symbol packages — not needed and saves space.
Get-ChildItem $localPkgDir -Filter "*.symbols.nupkg" | Remove-Item -Force
$pkgCount = (Get-ChildItem $localPkgDir -Filter "*.nupkg").Count
Write-Detail "Copied $pkgCount NuGet packages to local-packages/."

# --- Create NuGet.config with relative path ---
$bundleNugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="aspnetcore-local-dev" value="./local-packages" />
$($repoFeeds.Keys | ForEach-Object { "    <add key=`"$_`" value=`"$($repoFeeds[$_])`" />" } | Out-String -NoNewline)
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
Set-Content -Path (Join-Path $bundlePath "NuGet.config") -Value $bundleNugetConfig -Encoding utf8
Write-Detail "Created NuGet.config with relative local-packages path."

# --- Create Directory.Build.targets with framework override ---
$bundleTargets = @"
<Project>
  <!-- BEGIN PublishDemoApp.ps1 CustomAspNetCoreFramework -->
  <ItemGroup>
    <KnownFrameworkReference Update="Microsoft.AspNetCore.App">
      <TargetingPackVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</TargetingPackVersion>
      <LatestRuntimeFrameworkVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</LatestRuntimeFrameworkVersion>
      <DefaultRuntimeFrameworkVersion Condition="'%(TargetFramework)' == '$tfm'">$customVersion</DefaultRuntimeFrameworkVersion>
    </KnownFrameworkReference>
  </ItemGroup>
  <!-- END PublishDemoApp.ps1 CustomAspNetCoreFramework -->
</Project>
"@
Set-Content -Path (Join-Path $bundlePath "Directory.Build.targets") -Value $bundleTargets -Encoding utf8
Write-Detail "Created Directory.Build.targets with framework override."

# --- Create global.json with rollForward so a newer preview SDK is accepted ---
$globalJsonPath = Join-Path $RepoPath "global.json"
$sdkVersion = ((Get-Content $globalJsonPath -Raw | ConvertFrom-Json).sdk.version)
$bundleGlobalJson = @"
{
  "sdk": {
    "version": "$sdkVersion",
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
"@
Set-Content -Path (Join-Path $bundlePath "global.json") -Value $bundleGlobalJson -Encoding utf8
Write-Detail "Created global.json (SDK $sdkVersion with rollForward)."

# --- Create setup.ps1 that installs the custom shared framework into the SDK ---
$setupScript = @'
<#
.SYNOPSIS
    Installs the custom-built ASP.NET Core shared framework into your local .NET SDK.

.DESCRIPTION
    This script extracts the ASP.NET Core runtime pack from local-packages/ and
    overlays it into your .NET SDK's shared framework directory. Run this ONCE
    before using 'dotnet run' or debugging in VS Code.

    This is non-destructive — it creates a new version directory alongside your
    existing framework versions (the same approach the ASP.NET Core repo uses
    for remote test execution on Helix machines).
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = $PSScriptRoot

# Determine .NET root
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($env:DOTNET_ROOT -and (Test-Path $env:DOTNET_ROOT)) {
    $dotnetRoot = $env:DOTNET_ROOT
}
elseif ($dotnetCmd) {
    $dotnetRoot = Split-Path (Resolve-Path $dotnetCmd.Source) -Parent
}
else {
    throw "Cannot find .NET SDK. Install from https://dotnet.microsoft.com/download/dotnet/11.0"
}
Write-Host "Using .NET root: $dotnetRoot" -ForegroundColor Cyan

# Detect RID
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$rid = if ($env:OS -eq 'Windows_NT' -or $IsWindows) { "win-$arch" }
       elseif ($IsMacOS)                              { "osx-$arch" }
       else                                           { "linux-$arch" }

# Find the runtime pack .nupkg
$localPkgs = Join-Path $scriptDir "local-packages"
$runtimePkg = Get-ChildItem $localPkgs -Filter "Microsoft.AspNetCore.App.Runtime.$rid.*.nupkg" |
    Where-Object { $_.Name -notmatch '\.symbols\.' } |
    Select-Object -First 1

if (-not $runtimePkg) {
    throw "Runtime pack not found for RID '$rid' in $localPkgs"
}

# Extract version from filename
$runtimeVersion = $runtimePkg.BaseName -replace "^Microsoft\.AspNetCore\.App\.Runtime\.$([regex]::Escape($rid))\.", ''

$destDir = Join-Path $dotnetRoot "shared" "Microsoft.AspNetCore.App" $runtimeVersion
if (Test-Path $destDir) {
    Write-Host "Shared framework $runtimeVersion already installed at:" -ForegroundColor Green
    Write-Host "  $destDir" -ForegroundColor Green
    Write-Host "Nothing to do." -ForegroundColor Green
    exit 0
}

Write-Host "Installing Microsoft.AspNetCore.App $runtimeVersion for $rid..." -ForegroundColor Cyan

# Extract .nupkg (it's a zip)
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspnetcore-runtime-$runtimeVersion"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
Expand-Archive -Path $runtimePkg.FullName -DestinationPath $tempDir

# Copy runtime assemblies to shared framework directory
New-Item -ItemType Directory -Path $destDir -Force | Out-Null

$libDir = Get-ChildItem (Join-Path $tempDir "runtimes" $rid "lib") -Directory | Select-Object -First 1
if ($libDir) {
    Copy-Item (Join-Path $libDir.FullName "*") $destDir -Recurse -Force
    Write-Host "  Copied runtime assemblies." -ForegroundColor DarkGray
}

$nativeDir = Join-Path $tempDir "runtimes" $rid "native"
if (Test-Path $nativeDir) {
    Copy-Item (Join-Path $nativeDir "*") $destDir -Recurse -Force
    Write-Host "  Copied native binaries." -ForegroundColor DarkGray
}

# Clean up temp
Remove-Item $tempDir -Recurse -Force

Write-Host ""
Write-Host "Installed successfully to:" -ForegroundColor Green
Write-Host "  $destDir" -ForegroundColor Green
Write-Host ""
Write-Host "You can now use 'dotnet run' and debug in VS Code." -ForegroundColor Green
'@
Set-Content -Path (Join-Path $bundlePath "setup.ps1") -Value $setupScript -Encoding utf8
Write-Detail "Created setup.ps1 (shared framework installer)."

# --- Create a README for the presenter ---
$readmeContent = @"
# Demo App — Source Bundle

This folder contains the full source code and all dependencies needed to open,
build, and edit this project in VS Code (or any IDE) on another machine.

## Prerequisites

Install a **.NET 11 Preview SDK** (or later) from:
  https://dotnet.microsoft.com/download/dotnet/11.0

## First-time setup

Run the setup script **once** to install the custom ASP.NET Core shared framework
into your local .NET SDK (this is non-destructive — it adds a new version
alongside your existing frameworks):

    pwsh ./setup.ps1

## Open in VS Code

1. Open this folder in VS Code.
2. The C# extension will restore packages from the included ``local-packages/`` folder.
3. No internet connection is required for NuGet restore.

## Run the app

    dotnet run

## Framework version

This project uses a custom-built ASP.NET Core framework:
  **$customVersion** (targeting $tfm)

The ``setup.ps1`` script installs the runtime assemblies so ``dotnet run`` works.
The ``Directory.Build.targets`` file overrides the SDK's framework version so that
compilation uses the custom targeting pack from ``local-packages/``.
"@
Set-Content -Path (Join-Path $bundlePath "README.md") -Value $readmeContent -Encoding utf8

$bundleFiles   = Get-ChildItem $bundlePath -Recurse -File
$bundleSizeMB  = [math]::Round(($bundleFiles | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
Write-OK "Source bundle: $bundlePath"
Write-OK "Bundle files:  $($bundleFiles.Count)"
Write-OK "Bundle size:   ${bundleSizeMB} MB"

# ─────────────────────────────────────────────────────────────────────────────
# Done
# ─────────────────────────────────────────────────────────────────────────────

$publishedFiles = Get-ChildItem $OutputPath -Recurse -File
$totalSizeMB    = [math]::Round(($publishedFiles | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Step "All done!"
Write-Host ""
Write-OK "Published app:   $OutputPath  (${totalSizeMB} MB, self-contained)"
Write-OK "Source bundle:   $bundlePath  (${bundleSizeMB} MB, for VS Code editing)"
Write-OK "Framework:       Microsoft.AspNetCore.App $customVersion"
Write-Host ""
Write-Host "  Published app  — copy to the target machine and run the executable directly." -ForegroundColor White
Write-Host "  Source bundle  — open in VS Code on a machine with .NET 11 Preview SDK." -ForegroundColor White
Write-Host "                   No internet needed for NuGet restore." -ForegroundColor White
Write-Host ""

}
catch {
    Write-Host "" -ForegroundColor Red
    Write-Host "FATAL: Failed during step '$currentStep'" -ForegroundColor Red
    Write-Host "" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ScriptStackTrace) {
        Write-Host "" -ForegroundColor DarkGray
        Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    }
    exit 1
}
