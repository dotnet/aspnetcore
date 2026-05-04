#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Validates a repro JSON file against the repro schema.

.PARAMETER Path
    Path to the JSON file to validate.

.PARAMETER SchemaPath
    Optional path to the JSON schema. Defaults to repro-schema.json in the references folder.

.OUTPUTS
    Exit code 0 = valid, 1 = schema violations, 2 = structural error / unreadable.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $Path,

    [string] $SchemaPath = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve paths
$ResolvedPath = Resolve-Path $Path -ErrorAction SilentlyContinue
if (-not $ResolvedPath) {
    Write-Error "File not found: $Path"
    exit 2
}

if (-not $SchemaPath) {
    $ScriptDir  = $PSScriptRoot
    $SchemaPath = Join-Path $ScriptDir '..' 'references' 'repro-schema.json'
}
$ResolvedSchema = Resolve-Path $SchemaPath -ErrorAction SilentlyContinue
if (-not $ResolvedSchema) {
    Write-Error "Schema not found: $SchemaPath"
    exit 2
}

# Load JSON
try {
    $json = Get-Content $ResolvedPath.Path -Raw | ConvertFrom-Json -Depth 20
} catch {
    Write-Error "Cannot parse JSON: $_"
    exit 2
}

$errors = [System.Collections.Generic.List[string]]::new()

# ── Required top-level fields (always required) ─────────────────────────────
$alwaysRequired = @('meta', 'conclusion', 'notes', 'reproductionSteps', 'environment')
foreach ($field in $alwaysRequired) {
    if ($null -eq $json.$field) {
        $errors.Add("Missing required field: '$field'")
    }
}

# ── meta ────────────────────────────────────────────────────────────────────
if ($json.meta) {
    if ($json.meta.repo -ne 'dotnet/aspnetcore') {
        $errors.Add("meta.repo must be 'dotnet/aspnetcore' (got: '$($json.meta.repo)')")
    }
    if (-not $json.meta.number -or $json.meta.number -le 0) {
        $errors.Add("meta.number must be a positive integer")
    }
    if (-not $json.meta.analyzedAt) {
        $errors.Add("meta.analyzedAt is required")
    }
}

# ── conclusion ──────────────────────────────────────────────────────────────
$validConclusions = @('reproduced', 'not-reproduced', 'needs-platform', 'needs-hardware', 'partial', 'inconclusive')
if ($json.conclusion -and $json.conclusion -notin $validConclusions) {
    $errors.Add("conclusion '$($json.conclusion)' is not one of: $($validConclusions -join ', ')")
}

# ── Conditional required fields based on conclusion ──────────────────────────
$conclusionsRequiringOutput   = @('reproduced', 'not-reproduced')
$conclusionsRequiringBlockers = @('needs-platform', 'needs-hardware', 'partial', 'inconclusive')
if ($json.conclusion -in $conclusionsRequiringOutput) {
    if ($null -eq $json.output) {
        $errors.Add("'output' is required when conclusion is '$($json.conclusion)'")
    }
    if ($null -eq $json.versionResults) {
        $errors.Add("'versionResults' is required when conclusion is '$($json.conclusion)'")
    }
} elseif ($json.conclusion -in $conclusionsRequiringBlockers) {
    if ($null -eq $json.blockers -or $json.blockers.Count -eq 0) {
        $errors.Add("'blockers' (non-empty) is required when conclusion is '$($json.conclusion)'")
    }
}

# ── notes ───────────────────────────────────────────────────────────────────
if ($json.notes -and $json.notes.Length -lt 10) {
    $errors.Add("notes is too short (minimum 10 characters)")
}

# ── reproductionSteps ───────────────────────────────────────────────────────
if ($json.reproductionSteps) {
    if ($json.reproductionSteps -isnot [array] -or $json.reproductionSteps.Count -eq 0) {
        $errors.Add("reproductionSteps must be a non-empty array")
    } else {
        $validLayers = @('setup', 'csharp', 'hosting', 'middleware', 'http', 'deployment')
        for ($i = 0; $i -lt $json.reproductionSteps.Count; $i++) {
            $step = $json.reproductionSteps[$i]
            if (-not $step.stepNumber) {
                $errors.Add("reproductionSteps[$i]: missing stepNumber")
            }
            if (-not $step.description) {
                $errors.Add("reproductionSteps[$i]: missing description")
            }
            if ($step.layer -and $step.layer -notin $validLayers) {
                $errors.Add("reproductionSteps[$i]: layer '$($step.layer)' not in: $($validLayers -join ', ')")
            }
            $validResults = @('success', 'failure', 'wrong-output', 'skip')
            if ($step.result -and $step.result -notin $validResults) {
                $errors.Add("reproductionSteps[$i]: result '$($step.result)' not in: $($validResults -join ', ')")
            }
        }
        # At least one step should have an http layer (for HTTP bugs) or csharp
        $hasCodeOrHttp = $json.reproductionSteps | Where-Object { $_.layer -in @('csharp', 'http') }
        if ($hasCodeOrHttp.Count -eq 0) {
            Write-Warning "No csharp or http layer steps found — this may be incomplete."
        }
    }
}

# ── versionResults ──────────────────────────────────────────────────────────
if ($json.versionResults) {
    $validVersionResults = @('reproduced', 'not-reproduced', 'error', 'not-tested')
    for ($i = 0; $i -lt $json.versionResults.Count; $i++) {
        $vr = $json.versionResults[$i]
        if (-not $vr.version) {
            $errors.Add("versionResults[$i]: missing version")
        }
        if ($vr.result -and $vr.result -notin $validVersionResults) {
            $errors.Add("versionResults[$i]: result '$($vr.result)' not in: $($validVersionResults -join ', ')")
        }
    }
}

# ── reproProject ────────────────────────────────────────────────────────────
if ($json.reproProject) {
    $validTypes = @('webapi', 'mvc', 'razorpages', 'blazor-wasm', 'blazor-server', 'blazor-ssr', 'grpc', 'console', 'docker', 'test', 'existing', 'simulation')
    if ($json.reproProject.type -and $json.reproProject.type -notin $validTypes) {
        $errors.Add("reproProject.type '$($json.reproProject.type)' not in: $($validTypes -join ', ')")
    }
}

# ── environment ─────────────────────────────────────────────────────────────
if ($json.environment) {
    $envRequired = @('os', 'arch', 'dotnetVersion', 'aspnetcoreVersion')
    foreach ($f in $envRequired) {
        if (-not $json.environment.$f) {
            $errors.Add("environment.$f is required")
        }
    }
    $validArch = @('x64', 'arm64', 'x86', 'wasm')
    if ($json.environment.arch -and $json.environment.arch -notin $validArch) {
        $errors.Add("environment.arch '$($json.environment.arch)' not in: $($validArch -join ', ')")
    }
}

# ── output ──────────────────────────────────────────────────────────────────
if ($json.output) {
    if ($json.output.actionability) {
        $act = $json.output.actionability
        $validActions = @('needs-investigation', 'close-as-fixed', 'close-as-by-design', 'close-with-docs', 'close-as-duplicate', 'convert-to-discussion', 'request-info', 'keep-open')
        if ($act.suggestedAction -and $act.suggestedAction -notin $validActions) {
            $errors.Add("output.actionability.suggestedAction '$($act.suggestedAction)' not in: $($validActions -join ', ')")
        }
        if ($null -ne $act.confidence -and ($act.confidence -lt 0 -or $act.confidence -gt 1)) {
            $errors.Add("output.actionability.confidence must be between 0 and 1")
        }
    }
}

# ── Report ───────────────────────────────────────────────────────────────────
if ($errors.Count -gt 0) {
    Write-Host "VALIDATION FAILED — $($errors.Count) error(s):" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  ✗ $e" -ForegroundColor Red
    }
    exit 1
}

Write-Host "VALIDATION PASSED — $($ResolvedPath.Path)" -ForegroundColor Green
Write-Host "  conclusion:    $($json.conclusion)"
Write-Host "  issue:         $($json.meta.repo)#$($json.meta.number)"
Write-Host "  steps:         $($json.reproductionSteps.Count)"
if ($json.versionResults) {
    Write-Host "  versions:      $($json.versionResults.Count)"
}
exit 0
