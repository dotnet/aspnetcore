# Usage: .\collect-coverage.ps1 <FuzzerName> .\multipart-inputs\
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$FuzzerName,

    [Parameter(Mandatory=$true, Position=1)]
    [string]$InputPath,

    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "./coverage-report"
)

$ErrorActionPreference = "Stop"

# Use the local dotnet installation from the repository root
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
$dotnetPath = Join-Path $repoRoot ".dotnet\dotnet.exe"

if (-not (Test-Path $dotnetPath)) {
    Write-Error "Local dotnet installation not found at $dotnetPath. Run activate.ps1 from the repo root first."
    exit 1
}

Write-Host "Using dotnet: $dotnetPath" -ForegroundColor Cyan

# Check and install required dotnet tools
Write-Host "Checking for required tools..." -ForegroundColor Cyan
$installedTools = & $dotnetPath tool list

$coverletInstalled = $installedTools | Select-String "coverlet.console"
if (-not $coverletInstalled) {
    Write-Host "  Installing coverlet.console..." -ForegroundColor Yellow
    & $dotnetPath tool install --global coverlet.console
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install coverlet.console"
        exit 1
    }
} else {
    Write-Host "  coverlet.console: installed" -ForegroundColor Gray
}

$reportGeneratorInstalled = $installedTools | Select-String "dotnet-reportgenerator-globaltool"
if (-not $reportGeneratorInstalled) {
    Write-Host "  Installing dotnet-reportgenerator-globaltool..." -ForegroundColor Yellow
    & $dotnetPath tool install --global dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install reportgenerator."
        exit 1
    }
} else {
    Write-Host "  dotnet-reportgenerator-globaltool: installed" -ForegroundColor Gray
}

# Build the project
Write-Host "Building the project..." -ForegroundColor Cyan
& $dotnetPath build $PSScriptRoot
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Get the path to the test assembly
$artifactsPath = Join-Path $repoRoot "artifacts\bin\AspNetCoreFuzzing"
$assemblyPath = (Get-ChildItem -Path $artifactsPath -Recurse -Filter "AspNetCoreFuzzing.dll" | Select-Object -First 1).FullName

if (-not $assemblyPath) {
    Write-Error "Could not find AspNetCoreFuzzing.dll in $artifactsPath"
    exit 1
}

Write-Host "Found assembly: $assemblyPath" -ForegroundColor Green

# Get the list of instrumented assemblies
Write-Host "Getting instrumented assemblies for $FuzzerName..." -ForegroundColor Cyan
$instrumentedAssemblies = & $dotnetPath run --project $PSScriptRoot --no-build -- $FuzzerName --get-instrumented-assemblies
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get instrumented assemblies"
    exit 1
}

$instrumentedAssembliesArray = $instrumentedAssemblies | Where-Object { $_ -ne "" }

if ($instrumentedAssembliesArray.Count -eq 0) {
    Write-Error "No instrumented assemblies found for $FuzzerName"
    exit 1
}

Write-Host "Instrumented assemblies:" -ForegroundColor Green
foreach ($asm in $instrumentedAssembliesArray) {
    Write-Host "  - $asm" -ForegroundColor Gray
}

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build include filters for coverlet
$includeFilters = @()
foreach ($asm in $instrumentedAssembliesArray) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($asm)
    $includeFilters += "[${assemblyName}]*"
}

# Build coverlet arguments
$coverletArgs = @(
    $assemblyPath,
    "--target", $dotnetPath,
    "--targetargs", "run --project $PSScriptRoot --no-build -- $FuzzerName $InputPath",
    "--output", "$OutputDir/",
    "--format", "opencover",
    "--use-source-link",
    "--does-not-return-attribute", "DoesNotReturn"
)

# Add include filters
foreach ($filter in $includeFilters) {
    $coverletArgs += "--include"
    $coverletArgs += $filter
}

# Run coverlet
Write-Host ""
Write-Host "Collecting coverage..." -ForegroundColor Cyan
Write-Host "Running: coverlet $($coverletArgs -join ' ')" -ForegroundColor Gray
Write-Host ""
& $dotnetPath coverlet @coverletArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Coverage collection failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Coverage report generated in: $OutputDir" -ForegroundColor Green

# Find the generated OpenCover XML file
$coverageFile = Get-ChildItem -Path $OutputDir -Filter "*.opencover.xml" | Select-Object -First 1
if ($coverageFile) {
    Write-Host "  - OpenCover report: $($coverageFile.FullName)" -ForegroundColor Gray
}

# Generate HTML report using ReportGenerator
Write-Host ""
Write-Host "Generating HTML report..." -ForegroundColor Cyan

if ($coverageFile) {
    $htmlOutputDir = Join-Path $OutputDir "html"
    & $dotnetPath reportgenerator "-reports:$($coverageFile.FullName)" "-targetdir:$htmlOutputDir" "-reporttypes:Html"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "HTML report generated: $htmlOutputDir\index.html" -ForegroundColor Green
    } else {
        Write-Warning "Failed to generate HTML report"
    }
}
