#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$workflowPath = Join-Path $PSScriptRoot '../../.github/workflows/skill-evals.yml'
$workflow = Get-Content $workflowPath -Raw

if ($workflow -match '(?m)^\s{2}pull_request_review:') {
    throw 'The PAT-backed workflow must not activate from pull_request_review.'
}
if ($workflow -match 'REVIEW_(BODY|ACTOR|SHA|PR_NUMBER)|pull_request_review') {
    throw 'The workflow still contains pull-request-review resolution logic.'
}

$selectedRefCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('SELECTED_REF: ${{ github.ref }}')
)).Count
if ($selectedRefCount -ne 3) {
    throw "Expected three full-ref default-branch guards; found $selectedRefCount."
}

$expectedRefCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('EXPECTED_REF="refs/heads/$DEFAULT_BRANCH"')
)).Count
if ($expectedRefCount -ne 3) {
    throw "Expected three closed default-branch comparisons; found $expectedRefCount."
}

$copilotCredentialExpression = @'
COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}
'@.Trim()
$copilotCredentialExpressionCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape($copilotCredentialExpression)
)).Count
if ($copilotCredentialExpressionCount -ne 1) {
    throw "Expected one static Copilot PAT mapping with the repository fallback; found $copilotCredentialExpressionCount."
}

$installStepCount = ([regex]::Matches(
    $workflow,
    [regex]::Escape('- name: Install evaluation tools')
)).Count
if ($installStepCount -ne 2) {
    throw "Expected tokenless pinned-tool installation in validation and execution jobs; found $installStepCount."
}

foreach ($expectedInstallFragment in @(
    'npm ci \',
    '--userconfig "$RUNNER_TEMP/evaluation.npmrc"',
    '--registry https://packagefeedproxy.microsoft.io/npm/',
    'node_modules/.bin/vally" --version',
    'node_modules/.bin/copilot" --version',
    'cd "$RUNNER_TEMP/evaluation-tools/node_modules/@github/copilot-sdk/dist"',
    "import.meta.resolve('@github/copilot-linux-x64/sdk')",
    'SKILL_EVAL_VALLY: ${{ runner.temp }}/evaluation-tools/node_modules/.bin/vally'
)) {
    $fragmentCount = ([regex]::Matches(
        $workflow,
        [regex]::Escape($expectedInstallFragment)
    )).Count
    if ($fragmentCount -ne 2) {
        throw "Expected two pinned-tool workflow references to '$expectedInstallFragment'; found $fragmentCount."
    }
}

if ($workflow -notmatch [regex]::Escape(
    'cp _trusted-control-plane/eng/skill-evals/evaluation-tools/package.json \'
)) {
    throw 'The credentialed job does not install the trusted evaluation tool manifest.'
}

$evaluationToolsRoot = Join-Path $PSScriptRoot 'evaluation-tools'
$evaluationToolsManifest = Get-Content (
    Join-Path $evaluationToolsRoot 'package.json'
) -Raw | ConvertFrom-Json
if ($evaluationToolsManifest.dependencies.'@github/copilot' -ne '1.0.80') {
    throw 'The evaluation tool manifest does not pin @github/copilot 1.0.80.'
}
if ($evaluationToolsManifest.overrides.'@github/copilot' -ne '1.0.80') {
    throw 'The evaluation tool manifest does not constrain transitive Copilot packages to 1.0.80.'
}
if ($evaluationToolsManifest.dependencies.'@microsoft/vally-cli' -ne '0.14.0') {
    throw 'The evaluation tool manifest does not pin @microsoft/vally-cli 0.14.0.'
}

$evaluationToolsLock = Get-Content (
    Join-Path $evaluationToolsRoot 'package-lock.json'
) -Raw | ConvertFrom-Json -AsHashtable
$lockedRoot = $evaluationToolsLock.packages['']
if ($lockedRoot.dependencies['@github/copilot'] -ne '1.0.80' -or
    $lockedRoot.dependencies['@microsoft/vally-cli'] -ne '0.14.0') {
    throw 'The evaluation tool lockfile root does not match the pinned manifest.'
}
foreach ($lockedPackage in @(
    'node_modules/@github/copilot-linux-x64',
    'node_modules/@microsoft/vally-cli'
)) {
    if (-not $evaluationToolsLock.packages.ContainsKey($lockedPackage)) {
        throw "The evaluation tool lockfile does not contain '$lockedPackage'."
    }
}
foreach ($lockedPackage in $evaluationToolsLock.packages.Keys) {
    if ($lockedPackage -like 'node_modules/@github/copilot-sdk/node_modules/@github/copilot*') {
        throw "The evaluation tool lockfile contains a conflicting nested Copilot package: '$lockedPackage'."
    }
}

$patPoolJob = [regex]::Match(
    $workflow,
    '(?ms)^  pat_pool:\r?\n(?<job>.*?)(?=^  \S|\z)'
)
if (-not $patPoolJob.Success) {
    throw 'The workflow does not contain the trusted PAT-pool selector job.'
}

$patPoolJobText = $patPoolJob.Groups['job'].Value
foreach ($patNumber in 0..9) {
    $secretReference = "COPILOT_PAT_${patNumber}: `${{ secrets.COPILOT_PAT_${patNumber} }}"
    if ($patPoolJobText -notmatch [regex]::Escape($secretReference)) {
        throw "The PAT-pool selector does not inspect COPILOT_PAT_$patNumber."
    }
}
if ($patPoolJobText -notmatch [regex]::Escape('PAT_INDEX=$((RANDOM % ${#PAT_NUMBERS[@]}))')) {
    throw 'The PAT-pool selector does not randomly balance across configured entries.'
}
if ($patPoolJobText -notmatch [regex]::Escape('echo "copilot_pat_number=$PAT_NUMBER" >> "$GITHUB_OUTPUT"')) {
    throw 'The PAT-pool selector does not expose the selected numeric slot.'
}

$selectorScriptMatch = [regex]::Match(
    $patPoolJobText,
    '(?ms)^      - name: Select Copilot token from pool\r?\n.*?^        run: \|\r?\n(?<script>.*)\z'
)
if (-not $selectorScriptMatch.Success) {
    throw 'The PAT-pool selector script could not be extracted for behavioral testing.'
}

$selectorScript = (
    $selectorScriptMatch.Groups['script'].Value -split '\r?\n' |
        ForEach-Object {
            if ($_.StartsWith('          ')) {
                $_.Substring(10)
            } elseif ($_.Length -eq 0) {
                ''
            } else {
                throw "Unexpected indentation in the PAT-pool selector script: '$_'"
            }
        }
) -join "`n"

$bash = Get-Command bash -ErrorAction Stop
$selectorTestRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-skill-eval-pat-pool-test-' + [guid]::NewGuid().ToString('N')
)
New-Item -ItemType Directory -Path $selectorTestRoot | Out-Null

function Invoke-PatPoolSelector {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable] $Pool
    )

    $caseRoot = Join-Path $selectorTestRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $caseRoot | Out-Null
    $scriptPath = Join-Path $caseRoot 'select-pat.sh'
    $outputPath = Join-Path $caseRoot 'github-output.txt'
    $summaryPath = Join-Path $caseRoot 'github-summary.md'
    Set-Content $scriptPath $selectorScript -Encoding utf8NoBOM

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $bash.Source
    $startInfo.ArgumentList.Add($scriptPath)
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['GITHUB_OUTPUT'] = $outputPath
    $startInfo.Environment['GITHUB_STEP_SUMMARY'] = $summaryPath
    foreach ($patNumber in 0..9) {
        $key = "COPILOT_PAT_$patNumber"
        $startInfo.Environment[$key] = if ($Pool.ContainsKey($patNumber)) {
            [string] $Pool[$patNumber]
        } else {
            ''
        }
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'The PAT-pool selector process did not start.'
    }

    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "The PAT-pool selector exited with code $($process.ExitCode): $standardError"
    }

    $githubOutput = if (Test-Path $outputPath) {
        Get-Content $outputPath -Raw
    } else {
        ''
    }
    $summary = if (Test-Path $summaryPath) {
        Get-Content $summaryPath -Raw
    } else {
        ''
    }
    $selectedPatMatch = [regex]::Match(
        $githubOutput,
        '(?m)^copilot_pat_number=(?<number>[0-9])\r?$'
    )
    $combinedOutput = "$standardOutput`n$standardError`n$githubOutput`n$summary"
    foreach ($patValue in $Pool.Values) {
        if ($combinedOutput.Contains([string] $patValue, [StringComparison]::Ordinal)) {
            throw 'The PAT-pool selector exposed a credential value in its output.'
        }
    }

    return [pscustomobject]@{
        Number = if ($selectedPatMatch.Success) {
            $selectedPatMatch.Groups['number'].Value
        } else {
            $null
        }
        StandardOutput = $standardOutput
    }
}

try {
    $emptyPool = Invoke-PatPoolSelector -Pool @{}
    if ($null -ne $emptyPool.Number -or
        $emptyPool.StandardOutput -notmatch 'None of the PAT pool entries had values') {
        throw 'The PAT-pool selector did not preserve the empty-pool fallback path.'
    }

    $singlePat = Invoke-PatPoolSelector -Pool @{ 2 = 'single-slot-secret' }
    if ($singlePat.Number -ne '2') {
        throw "The PAT-pool selector did not select the only populated slot: '$($singlePat.Number)'."
    }

    $multiplePats = Invoke-PatPoolSelector -Pool @{
        1 = 'first-multi-slot-secret'
        4 = 'second-multi-slot-secret'
        9 = 'third-multi-slot-secret'
    }
    if ($multiplePats.Number -notin @('1', '4', '9')) {
        throw "The PAT-pool selector selected an empty slot: '$($multiplePats.Number)'."
    }
} finally {
    Remove-Item -Recurse -Force $selectorTestRoot
}

$reportJob = [regex]::Match(
    $workflow,
    '(?ms)^  report:\r?\n(?<job>.*?)(?=^  \S|\z)'
)
if (-not $reportJob.Success) {
    throw 'The workflow does not contain the report job.'
}

$reportCondition = [regex]::Match(
    $reportJob.Groups['job'].Value,
    '(?ms)^    if: >-\r?\n(?<condition>(?:      .*(?:\r?\n|$))+?)(?=^    \S|\z)'
)
if (-not $reportCondition.Success) {
    throw 'The report job does not contain a multiline condition.'
}

$claimedConditionCount = ([regex]::Matches(
    $reportCondition.Groups['condition'].Value,
    "(?m)^      needs\.run\.outputs\.claimed != 'false'\r?$"
)).Count
if ($claimedConditionCount -ne 1) {
    throw "Expected the report condition to retain the claimed-output guard; found $claimedConditionCount."
}

Write-Host 'Skill-eval workflow guard self-test passed.'
