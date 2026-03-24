<#
.SYNOPSIS
    Publishes a demo application as a self-contained deployment using a locally-built ASP.NET Core framework.

.DESCRIPTION
    This script automates the end-to-end process of:
      1. Building the ASP.NET Core repository to produce custom NuGet packages.
      2. Configuring the demo application to consume those packages.
      3. Publishing the demo app as a fully self-contained executable.
      4. Creating a portable source bundle so the presenter can open the project
         in VS Code without red squiggles. The bundle includes a local .dotnet/
         directory with the custom shared framework — no admin rights required
         and no modifications to the system-wide .NET installation.

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
$excludeDirs = @('bin', 'obj', 'publish', '.dotnet', '.dotnet-overlay', '.vscode', 'local-packages')
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

# ─────────────────────────────────────────────────────────────────────────────
# Step 9 — Install custom shared framework into local .dotnet/ directory
# ─────────────────────────────────────────────────────────────────────────────

Write-Step "Installing custom shared framework into bundle"

$bundleDotnetDir = Join-Path $bundlePath ".dotnet"
$fxDestDir       = Join-Path $bundleDotnetDir "shared" "Microsoft.AspNetCore.App" $customVersion
New-Item -ItemType Directory -Path $fxDestDir -Force | Out-Null

# Extract runtime assemblies from the runtime pack .nupkg.
$runtimePkg = Get-ChildItem $localPkgDir -Filter "Microsoft.AspNetCore.App.Runtime.$rid.*.nupkg" |
    Where-Object { $_.Name -notmatch '\.symbols\.' } |
    Select-Object -First 1

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspnetcore-runtime-extract-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
try {
    Expand-Archive -Path $runtimePkg.FullName -DestinationPath $tempDir

    $libDir = Get-ChildItem (Join-Path $tempDir "runtimes" $rid "lib") -Directory | Select-Object -First 1
    if ($libDir) {
        Copy-Item (Join-Path $libDir.FullName "*") $fxDestDir -Recurse -Force
    }

    $nativeDir = Join-Path $tempDir "runtimes" $rid "native"
    if (Test-Path $nativeDir) {
        Copy-Item (Join-Path $nativeDir "*") $fxDestDir -Recurse -Force
    }
}
finally {
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
}

$fxFileCount = (Get-ChildItem $fxDestDir -File).Count
Write-Detail "Extracted $fxFileCount assemblies to .dotnet/shared/Microsoft.AspNetCore.App/$customVersion/"

# --- Create run.ps1 that sets DOTNET_ROOT and launches the app ---
$runScript = @'
<#
.SYNOPSIS
    Runs the demo app using the bundled custom ASP.NET Core shared framework.
    No admin rights or system-wide .NET modifications required.
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$DotnetArgs
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot

# Locate the system dotnet.
$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $systemDotnet) {
    throw "Cannot find 'dotnet' on PATH. Install .NET 11 Preview SDK from https://dotnet.microsoft.com/download/dotnet/11.0"
}
$systemDotnetRoot = Split-Path (Resolve-Path $systemDotnet.Source) -Parent

# Create a merged view: local .dotnet overlays the system SDK.
# DOTNET_ROOT tells the host where to find the SDK.
# DOTNET_ADDITIONAL_DEPS is not needed — we overlay the shared framework directly.
# We use DOTNET_ROOT_ENV so the host finds the system SDK for everything EXCEPT
# the custom shared framework, which lives in our local .dotnet/.

# Strategy: copy the custom shared framework into a temporary overlay that
# mirrors the system SDK structure. This avoids modifying Program Files.
$overlayDir = Join-Path $scriptDir ".dotnet-overlay"
$overlayFxDir = Join-Path $overlayDir "shared" "Microsoft.AspNetCore.App"
if (-not (Test-Path $overlayFxDir)) {
    New-Item -ItemType Directory -Path $overlayFxDir -Force | Out-Null
}

# Link/copy the custom framework version into the overlay.
$localFxDir = Join-Path $scriptDir ".dotnet" "shared" "Microsoft.AspNetCore.App"
$customVersionDirs = Get-ChildItem $localFxDir -Directory -ErrorAction SilentlyContinue
foreach ($versionDir in $customVersionDirs) {
    $dest = Join-Path $overlayFxDir $versionDir.Name
    if (-not (Test-Path $dest)) {
        Copy-Item $versionDir.FullName $dest -Recurse
    }
}

# Link all existing system shared frameworks into the overlay so they're visible too.
$systemFxRoots = @(
    (Join-Path $systemDotnetRoot "shared")
)
foreach ($systemFxRoot in $systemFxRoots) {
    if (Test-Path $systemFxRoot) {
        foreach ($fxFamily in (Get-ChildItem $systemFxRoot -Directory)) {
            $overlayFamily = Join-Path $overlayDir "shared" $fxFamily.Name
            if (-not (Test-Path $overlayFamily)) {
                New-Item -ItemType Directory -Path $overlayFamily -Force | Out-Null
            }
            foreach ($fxVersion in (Get-ChildItem $fxFamily.FullName -Directory)) {
                $overlayVersion = Join-Path $overlayFamily $fxVersion.Name
                if (-not (Test-Path $overlayVersion)) {
                    # Use directory junction (no admin needed, instant, no disk space)
                    cmd /c mklink /J "$overlayVersion" "$($fxVersion.FullName)" | Out-Null
                }
            }
        }
    }
}

# Also junction the SDK, host, and packs directories.
foreach ($dirName in @('sdk', 'host', 'packs', 'templates')) {
    $src = Join-Path $systemDotnetRoot $dirName
    $dst = Join-Path $overlayDir $dirName
    if ((Test-Path $src) -and -not (Test-Path $dst)) {
        cmd /c mklink /J "$dst" "$src" | Out-Null
    }
}

# Copy the dotnet.exe host into the overlay so DOTNET_ROOT is self-contained.
$dotnetExeName = if ($env:OS -eq 'Windows_NT' -or $IsWindows) { 'dotnet.exe' } else { 'dotnet' }
$overlayDotnet = Join-Path $overlayDir $dotnetExeName
if (-not (Test-Path $overlayDotnet)) {
    Copy-Item (Join-Path $systemDotnetRoot $dotnetExeName) $overlayDotnet
    # Also copy the .dll if present (dotnet host may need it).
    $dotnetDll = Join-Path $systemDotnetRoot 'dotnet.dll'
    if (Test-Path $dotnetDll) { Copy-Item $dotnetDll $overlayDir }
}

$env:DOTNET_ROOT = $overlayDir
$env:PATH = "$overlayDir$([System.IO.Path]::PathSeparator)$env:PATH"

Write-Host "Using custom ASP.NET Core framework from .dotnet/" -ForegroundColor Cyan
Write-Host "DOTNET_ROOT = $overlayDir" -ForegroundColor DarkGray
Write-Host ""

if ($DotnetArgs) {
    & $overlayDotnet @DotnetArgs
}
else {
    & $overlayDotnet run
}
'@
Set-Content -Path (Join-Path $bundlePath "run.ps1") -Value $runScript -Encoding utf8
Write-Detail "Created run.ps1 (launcher with DOTNET_ROOT overlay)."

# --- Create .vscode/settings.json for VS Code IntelliSense ---
$vscodeDir = Join-Path $bundlePath ".vscode"
New-Item -ItemType Directory -Path $vscodeDir -Force | Out-Null

# VS Code C# extension: use the overlay dotnet so OmniSharp/Roslyn sees the custom framework.
$vscodeSettings = @"
{
    "dotnet.dotnetPath": ".dotnet-overlay",
    "omnisharp.dotnetPath": ".dotnet-overlay"
}
"@
Set-Content -Path (Join-Path $vscodeDir "settings.json") -Value $vscodeSettings -Encoding utf8
Write-Detail "Created .vscode/settings.json pointing to local overlay."

# --- Create setup.ps1 for first-time initialization ---
$setupScript = @'
<#
.SYNOPSIS
    First-time setup: creates the .dotnet-overlay directory by junctioning the
    system .NET SDK directories and overlaying the custom shared framework.
    No admin rights required. Run this once before opening in VS Code.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot

Write-Host "Setting up .dotnet-overlay..." -ForegroundColor Cyan

$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $systemDotnet) {
    throw "Cannot find 'dotnet' on PATH. Install .NET 11 Preview SDK from https://dotnet.microsoft.com/download/dotnet/11.0"
}
$systemDotnetRoot = Split-Path (Resolve-Path $systemDotnet.Source) -Parent
Write-Host "  System SDK: $systemDotnetRoot" -ForegroundColor DarkGray

$overlayDir = Join-Path $scriptDir ".dotnet-overlay"
if (Test-Path $overlayDir) {
    Write-Host "  Overlay already exists — recreating..." -ForegroundColor Yellow
    # Remove junctions carefully (don't recurse into them).
    Get-ChildItem $overlayDir -Directory | ForEach-Object {
        if ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
            cmd /c rmdir "$($_.FullName)" | Out-Null
        }
        else {
            # Check nested dirs for junctions too.
            Get-ChildItem $_.FullName -Directory -Recurse | Where-Object {
                $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint
            } | ForEach-Object {
                cmd /c rmdir "$($_.FullName)" | Out-Null
            }
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Get-ChildItem $overlayDir -File | Remove-Item -Force
}
New-Item -ItemType Directory -Path $overlayDir -Force | Out-Null

# Junction SDK, host, packs, templates from the system SDK.
foreach ($dirName in @('sdk', 'host', 'packs', 'templates')) {
    $src = Join-Path $systemDotnetRoot $dirName
    $dst = Join-Path $overlayDir $dirName
    if (Test-Path $src) {
        cmd /c mklink /J "$dst" "$src" | Out-Null
        Write-Host "  Linked $dirName/" -ForegroundColor DarkGray
    }
}

# Junction all system shared framework families and versions.
$overlaySharedDir = Join-Path $overlayDir "shared"
New-Item -ItemType Directory -Path $overlaySharedDir -Force | Out-Null
$systemSharedDir = Join-Path $systemDotnetRoot "shared"
if (Test-Path $systemSharedDir) {
    foreach ($fxFamily in (Get-ChildItem $systemSharedDir -Directory)) {
        $overlayFamily = Join-Path $overlaySharedDir $fxFamily.Name
        New-Item -ItemType Directory -Path $overlayFamily -Force | Out-Null
        foreach ($fxVersion in (Get-ChildItem $fxFamily.FullName -Directory)) {
            $overlayVersion = Join-Path $overlayFamily $fxVersion.Name
            cmd /c mklink /J "$overlayVersion" "$($fxVersion.FullName)" | Out-Null
        }
        Write-Host "  Linked shared/$($fxFamily.Name)/ ($(@(Get-ChildItem $fxFamily.FullName -Directory).Count) versions)" -ForegroundColor DarkGray
    }
}

# Overlay the custom ASP.NET Core shared framework (real copy, not junction).
$localFxDir = Join-Path $scriptDir ".dotnet" "shared" "Microsoft.AspNetCore.App"
foreach ($versionDir in (Get-ChildItem $localFxDir -Directory -ErrorAction SilentlyContinue)) {
    $dest = Join-Path $overlaySharedDir "Microsoft.AspNetCore.App" $versionDir.Name
    # Remove any junction to the system version with the same name.
    if (Test-Path $dest) {
        if ((Get-Item $dest).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
            cmd /c rmdir "$dest" | Out-Null
        }
        else {
            Remove-Item $dest -Recurse -Force
        }
    }
    Copy-Item $versionDir.FullName $dest -Recurse
    Write-Host "  Installed custom Microsoft.AspNetCore.App/$($versionDir.Name)" -ForegroundColor Green
}

# Copy the dotnet host executable.
$dotnetExeName = if ($env:OS -eq 'Windows_NT' -or $IsWindows) { 'dotnet.exe' } else { 'dotnet' }
Copy-Item (Join-Path $systemDotnetRoot $dotnetExeName) $overlayDir
$dotnetDll = Join-Path $systemDotnetRoot 'dotnet.dll'
if (Test-Path $dotnetDll) { Copy-Item $dotnetDll $overlayDir }
Write-Host "  Copied dotnet host." -ForegroundColor DarkGray

Write-Host ""
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host "  Overlay: $overlayDir" -ForegroundColor Green
Write-Host ""
Write-Host "You can now:" -ForegroundColor White
Write-Host "  - Open this folder in VS Code (IntelliSense will work)" -ForegroundColor White
Write-Host "  - Run the app:  pwsh ./run.ps1" -ForegroundColor White
Write-Host "  - Or directly:  .dotnet-overlay/dotnet run" -ForegroundColor White
'@
Set-Content -Path (Join-Path $bundlePath "setup.ps1") -Value $setupScript -Encoding utf8
Write-Detail "Created setup.ps1 (first-time overlay builder)."

# --- Create a README for the presenter ---
$readmeContent = @"
# Demo App — Source Bundle

This folder contains the full source code and all dependencies needed to open,
build, edit, and run this project on another machine.

No admin rights are required. The system .NET SDK is not modified.

## Prerequisites

Install a **.NET 11 Preview SDK** (or later) from:
  https://dotnet.microsoft.com/download/dotnet/11.0

## First-time setup

Run the setup script **once** to create a local ``.dotnet-overlay/`` directory.
This junctions your system SDK directories and overlays the custom shared
framework — no files are copied into Program Files:

    pwsh ./setup.ps1

## Open in VS Code

1. Open this folder in VS Code.
2. ``.vscode/settings.json`` is pre-configured to use the local overlay.
3. The C# extension will see the custom framework — no red squiggles.
4. NuGet packages restore from the included ``local-packages/`` folder.

## Run the app

    pwsh ./run.ps1

Or directly:

    .dotnet-overlay/dotnet run

## Framework version

This project uses a custom-built ASP.NET Core framework:
  **$customVersion** (targeting $tfm)
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
Write-Host "  Source bundle  — run 'pwsh ./setup.ps1' once, then open in VS Code." -ForegroundColor White
Write-Host "                   No admin rights or system modifications needed." -ForegroundColor White
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
