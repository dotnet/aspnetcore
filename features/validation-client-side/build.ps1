# Build script for the client-side validation prototype.
# Compiles the TypeScript validation library and rebuilds the sample apps.
# Can be executed from any directory.

param(
    [switch]$SkipTests,
    [switch]$SkipMvc
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir '..\..')

$webJsDir = Join-Path $repoRoot 'src\Components\Web.JS'
$blazorCsproj = Join-Path $repoRoot 'src\Components\Samples\BlazorSSR\BlazorSSR.csproj'
$mvcCsproj = Join-Path $repoRoot 'src\Mvc\samples\MvcFormSample\MvcFormSample.csproj'

# Activate the local .NET SDK
. (Join-Path $repoRoot 'activate.ps1')

# 1. Run JS tests
if (-not $SkipTests) {
    Write-Host "`n=== Running JS tests ===" -ForegroundColor Cyan
    Push-Location $webJsDir
    npx jest test/Validation --no-coverage
    if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'JS tests failed.' }
    Pop-Location
}

# 2. Build the validation JS bundle
Write-Host "`n=== Building validation JS bundle ===" -ForegroundColor Cyan
Push-Location $webJsDir
npx rollup -c rollup.config.mjs --environment configuration:Release
if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'Rollup build failed.' }
Pop-Location

# 3. Build the BlazorSSR sample (copies JS automatically via MSBuild target)
Write-Host "`n=== Building BlazorSSR sample ===" -ForegroundColor Cyan
dotnet build $blazorCsproj --no-restore -v q
if ($LASTEXITCODE -ne 0) { throw 'BlazorSSR build failed.' }

# 4. Build the MVC sample
if (-not $SkipMvc) {
    Write-Host "`n=== Building MVC sample ===" -ForegroundColor Cyan
    dotnet build $mvcCsproj --no-restore -v q
    if ($LASTEXITCODE -ne 0) { throw 'MVC sample build failed.' }
}

Write-Host "`n=== Done ===" -ForegroundColor Green
