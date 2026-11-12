#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$componentRoot = (Resolve-Path (Join-Path $PSScriptRoot '../review-candidate-verifier')).Path
$repoRoot = (Resolve-Path (Join-Path $componentRoot '../../..')).Path
$script:checks = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)

    $script:checks++
    if (-not $Condition) {
        throw $Message
    }
}

function Test-FileHash {
    param([string]$Path, [string]$Hash)

    return (Get-FileHash $Path -Algorithm SHA256).Hash -eq $Hash
}

foreach ($package in 'package-a', 'package-b', 'package-c') {
    $bundleIndex = Get-Content (Join-Path $componentRoot "fixtures/$package/bundle-index.json") -Raw
    Assert-True (-not $bundleIndex.Contains('\')) "The $package bundle index contains non-portable backslash separators."
}
$componentText = Get-Content (Join-Path $componentRoot 'eval.vally.yaml') -Raw |
    & npx --yes --registry=https://packagefeedproxy.microsoft.io/npm/ 'yaml@2.9.1' --json --single --strict
if ($LASTEXITCODE -ne 0) {
    throw 'Could not parse verifier component specification.'
}
$component = $componentText | ConvertFrom-Json -AsHashtable -Depth 100
Assert-True (($component.stimuli.name -join ',') -eq 'a,b,c') 'Verifier component cases must be exactly a, b, and c.'
Assert-True ($component.defaults.runs -eq 5 -and $component.scoring.threshold -eq 0.8) 'The component gate must require 4/5.'
$componentProvenance = Get-Content (Join-Path $componentRoot 'provenance.json') -Raw |
    ConvertFrom-Json -AsHashtable -Depth 100
Assert-True ($componentProvenance.sourceRevisions.head -eq 'c0f86a6d725473f7a6f4c56d816ef941591053d2') (
    'The component source does not use the pinned head.'
)
foreach ($file in $componentProvenance.files) {
    Assert-True (Test-FileHash (Join-Path $componentRoot "fixtures/$($file.file)") $file.sha256) (
        "Component fixture hash mismatch: $($file.file)."
    )
}
foreach ($stimulus in $component.stimuli) {
    foreach ($file in @($component.agent_environment.files) + @($stimulus.agent_environment.files)) {
        Assert-True (Test-Path (Join-Path $componentRoot $file.src)) "Missing component input: $($file.src)."
    }
}
$checkoutRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-review-fixture-checkout-' + [guid]::NewGuid().ToString('N')
)
New-Item -ItemType Directory -Path $checkoutRoot | Out-Null
try {
    $componentRelative = 'eng/skill-evals/review-candidate-verifier/fixtures'
    $componentPaths = @(
        Get-ChildItem (Join-Path $componentRoot 'fixtures') -Recurse -File -Force |
            ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName).Replace('\', '/') }
    )
    & git -C $repoRoot -c core.autocrlf=true checkout-index `
        "--prefix=$checkoutRoot$([IO.Path]::DirectorySeparatorChar)" -- @componentPaths
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not materialize component fixtures through Git.'
    }
    foreach ($file in $componentProvenance.files) {
        Assert-True (
            Test-FileHash (Join-Path $checkoutRoot "$componentRelative/$($file.file)") $file.sha256
        ) "Git checkout changed component fixture bytes: $($file.file)."
    }
} finally {
    Remove-Item -LiteralPath $checkoutRoot -Recurse -Force
}

Write-Host "Review fixture self-test: $script:checks ran, $script:checks passed, 0 failed."
