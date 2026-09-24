#requires -Version 7.0

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3

$helper = Join-Path $PSScriptRoot 'prepare_investigate_issue_run.ps1'
$effectChecker = Join-Path $PSScriptRoot 'assert_investigate_issue_effects.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'aspnetcore-investigate-prepare-test-' + [guid]::NewGuid().ToString('N')
)
$fakeProjector = Join-Path $testRoot 'fake-projector.mjs'
$fakeVallyRoot = Join-Path $testRoot 'node_modules/@microsoft/vally-cli'
$fakeVally = Join-Path $fakeVallyRoot 'dist/index.js'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

New-Item -ItemType Directory -Path (Split-Path $fakeVally -Parent) -Force | Out-Null
try {
    Set-Content (Join-Path $fakeVallyRoot 'package.json') '{"version":"0.13.0"}' -Encoding utf8NoBOM
    Set-Content $fakeVally @'
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
const args = process.argv.slice(2);
if (args.includes("--dry-run")) {
  process.exit(0);
}
const value = (name) => args[args.indexOf(name) + 1];
const variant = value("--variant");
const output = value("--output-dir");
await writeFile(path.join(path.dirname(output), "actor-invoked.txt"), "invoked");
const cellId = path.basename(path.dirname(process.cwd()));
const stimulus = cellId.replace(/-r0-(baseline|skilled)$/, "");
const workDir = path.join(value("--workspace"), variant, stimulus);
await mkdir(workDir, { recursive: true });
const run = path.join(output, "native-run");
await mkdir(path.join(run, variant), { recursive: true });
await writeFile(path.join(run, "plan-snapshot.json"), JSON.stringify({
  evals: [{
    variant,
    runs: 1,
    plannedStimulusCount: 1,
    model: "model",
    judgeModel: "judge",
    executorName: "copilot-sdk",
    stimuli: [{ name: stimulus, graders: [{ type: "prompt" }] }],
  }],
}));
await writeFile(path.join(run, variant, "results.jsonl"), JSON.stringify({
  type: "trial-result",
  variant,
  status: "success",
  stimulus,
  evalName: "investigate-issue",
  model: "model",
  itemId: "trial-0",
  trajectory: { workDir, events: [], output: "Synthetic model-free runner output." },
  gradeResult: { score: variant === "skilled" ? 0.9 : 0.1, details: [{ graderType: "prompt", passed: true }] },
}) + "\n");
if (variant === "baseline") {
  process.exitCode = 1;
}
'@ -Encoding utf8NoBOM
    Set-Content $fakeProjector @'
import { readFile, writeFile } from "node:fs/promises";
const args = Object.fromEntries(Array.from({ length: process.argv.slice(2).length / 2 }, (_, i) => [
  process.argv.slice(2)[i * 2].slice(2),
  process.argv.slice(2)[i * 2 + 1],
]));
if (args.command === "list") {
  await writeFile(args.metadata, JSON.stringify([
    { name: "no-approval-performs-zero-execution", index: 39, privateCase: null },
    { name: "ordinary-report-uses-trusted-session-storage", index: 23, privateCase: {
      storageCase: "save", issueNumber: "999024", destinationFile: "issue-999024-investigation.md"
    }},
    { name: "session-storage-collision-preserves-existing-file", index: 26, privateCase: {
      storageCase: "collision", issueNumber: "999026", destinationFile: "issue-999026-investigation.md"
    }},
    { name: "session-storage-writer-failure-keeps-chat-report", index: 27, privateCase: {
      storageCase: "writer-failure", issueNumber: "999027", destinationFile: "issue-999027-investigation.md"
    }},
  ]));
} else {
  const source = await readFile(args.input, "utf8");
  const replacements = JSON.parse(await readFile(args.replacements, "utf8"));
  const cases = {
    "ordinary-report-uses-trusted-session-storage": {
      storageCase: "save", issueNumber: "999024", destinationFile: "issue-999024-investigation.md"
    },
    "session-storage-collision-preserves-existing-file": {
      storageCase: "collision", issueNumber: "999026", destinationFile: "issue-999026-investigation.md"
    },
    "session-storage-writer-failure-keeps-chat-report": {
      storageCase: "writer-failure", issueNumber: "999027", destinationFile: "issue-999027-investigation.md"
    },
  };
  const privateCase = cases[args.stimulus] ?? null;
  const requestedDestination = privateCase === null ? null : privateCase.storageCase === "writer-failure"
    ? replacements.__FAILING_ARTIFACT_PATH__
    : `${replacements.__SESSION_ARTIFACT_ROOT__}/${privateCase.destinationFile}`;
  await writeFile(args.output, source);
  await writeFile(args.metadata, JSON.stringify({
    privateCase,
    requestedDestination,
    canonicalInputHash: `canonical-${args.stimulus}`,
    effectiveInputHash: `effective-${args.stimulus}`,
    graderHash: `graders-${args.stimulus}`,
    scoringHash: `scoring-${args.stimulus}`,
    judgeConfigHash: `judge-${args.stimulus}`,
    resolvedConfigHash: `config-${args.stimulus}-${args.variant}`,
    frozenProjectionHash: `projection-${args.stimulus}-${args.variant}`,
    backendName: "local",
    executorName: "copilot-sdk",
    threshold: 0.6,
  }));
}
'@ -Encoding utf8NoBOM

    $unconfirmed = Join-Path $testRoot 'unconfirmed'
    $rejected = $false
    try {
        & $helper Prepare `
            -TrustedRoot $repoRoot `
            -CandidateRoot $repoRoot `
            -OutputRoot $unconfirmed `
            -CaseName ordinary-report-uses-trusted-session-storage `
            -Runs 1 `
            -ActorModel model `
            -JudgeModel judge `
            -VallyCli $fakeVally `
            -ProjectionScript $fakeProjector
    } catch {
        $rejected = $_.Exception.Message -like '*-ConfirmPrivateHost*'
    }
    Assert-True $rejected 'Prepare accepted an unconfirmed private host.'
    Assert-True (-not (Test-Path $unconfirmed)) 'Unconfirmed preparation created output.'
    Write-Host '  [OK] PrepareRejectsUnconfirmedPrivateHost'

    $evalText = Get-Content (
        Join-Path $repoRoot 'eng/skill-evals/investigate-issue/eval.vally.yaml'
    ) -Raw
    $noticePattern = [regex]::Matches(
        $evalText,
        "pattern: '(?<pattern>\(\?s\)\^[^']*non-public, non-publishing maintainer session[^']*)'"
    ) | ForEach-Object { $_.Groups['pattern'].Value }
    Assert-True (@($noticePattern).Count -eq 2) 'Expected two private-host notice graders.'
    foreach ($pattern in $noticePattern) {
        Assert-True (-not [regex]::IsMatch('', $pattern)) 'Private-host notice grader accepts empty output.'
        Assert-True (
            [regex]::IsMatch(
                'This skill requires a non-public, non-publishing maintainer session.',
                $pattern
            )
        ) 'Private-host notice grader rejects the plain notice.'
        Assert-True (
            [regex]::IsMatch(
                '> This skill requires a non-public, non-publishing maintainer session.',
                $pattern
            )
        ) 'Private-host notice grader rejects a Markdown blockquote notice.'
    }

    $mixedOutput = Join-Path $testRoot 'mixed-storage-and-ordinary'
    $rejected = $false
    try {
        & $helper Prepare `
            -TrustedRoot $repoRoot `
            -CandidateRoot $repoRoot `
            -OutputRoot $mixedOutput `
            -CaseName @(
                'no-approval-performs-zero-execution',
                'ordinary-report-uses-trusted-session-storage'
            ) `
            -Runs 1 `
            -ActorModel model `
            -JudgeModel judge `
            -ConfirmPrivateHost `
            -VallyCli $fakeVally `
            -ProjectionScript $fakeProjector
    } catch {
        $rejected = $_.Exception.Message -like '*separate manifest*'
    }
    Assert-True $rejected 'Prepare accepted mixed persistence and ordinary cases.'
    Assert-True (-not (Test-Path $mixedOutput)) 'Rejected mixed preparation created output.'
    Write-Host '  [OK] PrepareRequiresSeparatePersistenceManifest'

    $output = Join-Path $testRoot 'prepared'
    & $helper Prepare `
        -TrustedRoot $repoRoot `
        -CandidateRoot $repoRoot `
        -OutputRoot $output `
        -CaseName @(
            'ordinary-report-uses-trusted-session-storage',
            'session-storage-collision-preserves-existing-file',
            'session-storage-writer-failure-keeps-chat-report'
        ) `
        -Runs 5 `
        -ActorModel model `
        -JudgeModel judge `
        -ConfirmPrivateHost `
        -VallyCli $fakeVally `
        -ProjectionScript $fakeProjector

    $manifest = Get-Content (Join-Path $output 'manifest.json') -Raw | ConvertFrom-Json -Depth 100
    Assert-True ($manifest.expectedCells -eq 30) 'Three cases x five repetitions did not produce 30 cells.'
    Assert-True ($manifest.expectedPairs -eq 15) 'Three cases x five repetitions did not produce 15 pairs.'
    Assert-True (@($manifest.cells.cellId | Sort-Object -Unique).Count -eq 30) 'Cell IDs are not unique.'
    Assert-True (@($manifest.cells.stageRoot | Sort-Object -Unique).Count -eq 30) 'Stage roots are shared.'
    Assert-True (@($manifest.cells.outputRoot | Sort-Object -Unique).Count -eq 30) 'Output roots are shared.'
    $prepareReceipt = Get-Content $manifest.controller.prepareReceiptPath -Raw |
        ConvertFrom-Json -Depth 100
    Assert-True (
        $prepareReceipt.action -ceq 'ControllerPrepare' -and
        $prepareReceipt.captureSource -ceq 'prepare_investigate_issue_run.ps1' -and
        $prepareReceipt.manifestHash -ceq (
            Get-FileHash (Join-Path $output 'manifest.json') -Algorithm SHA256
        ).Hash.ToLowerInvariant() -and
        $prepareReceipt.invocation.privateHostConfirmed -eq $true -and
        $prepareReceipt.invocation.submittedInputHash -ceq
            $manifest.controller.submittedInvocationHash -and
        @($prepareReceipt.cells).Count -eq 30
    ) 'Prepare did not capture its trusted invocation and exact cell inputs.'
    foreach ($pair in $manifest.cells | Group-Object pairId) {
        Assert-True ($pair.Count -eq 2) "Pair '$($pair.Name)' does not contain two cells."
        Assert-True (@($pair.Group.variant | Sort-Object) -join ',' -ceq 'baseline,skilled') (
            "Pair '$($pair.Name)' does not contain baseline and skilled."
        )
        Assert-True ($pair.Group[0].pairNormalizedHash -ceq $pair.Group[1].pairNormalizedHash) (
            "Pair '$($pair.Name)' has unequal normalized inputs."
        )
    }
    Write-Host '  [OK] PerCellPreparationPreservesPairingAndCardinality'

    $realProjectionOutput = Join-Path $testRoot 'real-projection'
    & $helper Prepare `
        -TrustedRoot $repoRoot `
        -CandidateRoot $repoRoot `
        -OutputRoot $realProjectionOutput `
        -CaseName @(
            'ordinary-report-uses-trusted-session-storage',
            'session-storage-collision-preserves-existing-file',
            'session-storage-writer-failure-keeps-chat-report'
        ) `
        -Runs 2 `
        -ActorModel model `
        -JudgeModel judge `
        -ConfirmPrivateHost
    $realManifest = Get-Content (Join-Path $realProjectionOutput 'manifest.json') -Raw |
        ConvertFrom-Json -Depth 100
    $expectedIssues = @{
        'ordinary-report-uses-trusted-session-storage' = '999024'
        'session-storage-collision-preserves-existing-file' = '999026'
        'session-storage-writer-failure-keeps-chat-report' = '999027'
    }
    foreach ($cell in $realManifest.cells) {
        $projection = Get-Content (
            Join-Path (Split-Path $cell.stageRoot -Parent) 'projection.json'
        ) -Raw | ConvertFrom-Json
        $expectedFile = "issue-$($expectedIssues[$cell.stimulus])-investigation.md"
        Assert-True ($cell.destination -ceq $projection.requestedDestination) (
            "Manifest and projected destination differ for '$($cell.cellId)'."
        )
        Assert-True (
            $cell.frozenProjectionHash -ceq $realManifest.cases[
                [Array]::IndexOf(@($realManifest.cases.name), $cell.stimulus)
            ].projections.($cell.cellId).frozenProjectionHash
        ) "Cell projection ownership is not keyed by full cell ID for '$($cell.cellId)'."
        Assert-True (
            (Get-FileHash $cell.projectionPath -Algorithm SHA256).Hash.ToLowerInvariant() -ceq
            $cell.projectionHash
        ) "Cell projection file hash differs for '$($cell.cellId)'."
        Assert-True ((Split-Path $cell.destination -Leaf) -ceq $expectedFile) (
            "Prepared destination has the wrong issue identity for '$($cell.cellId)'."
        )
        if ($cell.storageCase -eq 'collision') {
            Assert-True (Test-Path $cell.destination -PathType Leaf) (
                "Collision sentinel was not seeded at the requested path for '$($cell.cellId)'."
            )
        } elseif ($cell.storageCase -eq 'writer-failure') {
            Assert-True (Test-Path (Split-Path $cell.destination -Parent) -PathType Leaf) (
                "Writer-failure parent was not seeded at the requested path for '$($cell.cellId)'."
            )
        } else {
            Assert-True (-not (Test-Path $cell.destination)) (
                "Save destination was unexpectedly pre-created for '$($cell.cellId)'."
            )
        }
    }
    $pairFailures = [Collections.Generic.List[string]]::new()
    foreach ($pair in $realManifest.cells | Group-Object pairId) {
        if ($pair.Group[0].pairNormalizedHash -cne $pair.Group[1].pairNormalizedHash) {
            $pairFailures.Add($pair.Name)
            Write-Host "  [FAIL] RealStoragePair-$($pair.Name): unequal normalized pair inputs"
        } else {
            Write-Host "  [OK] RealStoragePair-$($pair.Name)"
        }
    }
    Assert-True ($pairFailures.Count -eq 0) (
        'The real projection changed pair inputs beyond the declared per-cell paths: ' +
        ($pairFailures -join ', ')
    )
    $checkerReachedReceipts = $false
    try {
        & (Join-Path $repoRoot 'eng/skill-evals/assert_investigate_issue_run.ps1') `
            -Manifest (Join-Path $realProjectionOutput 'manifest.json')
    } catch {
        $checkerReachedReceipts = $_.Exception.Message -like '*has no execution receipt*'
    }
    Assert-True $checkerReachedReceipts (
        'The real repeated projection manifest failed before the expected absent-receipt boundary.'
    )
    Write-Host '  [OK] PinnedProjectionStorageDestinationMatchesPreparedFixtureAcrossRepeats'

    foreach ($cell in $manifest.cells | Where-Object storageCase -EQ 'collision') {
        Assert-True (Test-Path $cell.destination -PathType Leaf) "Collision sentinel is missing for '$($cell.cellId)'."
        Assert-True (
            (Get-FileHash $cell.destination -Algorithm SHA256).Hash.ToLowerInvariant() -ceq
            $cell.fixtureHashes.destination
        ) "Collision sentinel changed for '$($cell.cellId)'."
    }
    Write-Host '  [OK] CollisionPreservesExistingReport setup'

    foreach ($cell in $manifest.cells | Where-Object storageCase -EQ 'writer-failure') {
        $parent = Split-Path $cell.destination -Parent
        $before = [IO.File]::ReadAllBytes($parent)
        $failed = $false
        try {
            $stream = [IO.File]::Open(
                $cell.destination,
                [IO.FileMode]::CreateNew,
                [IO.FileAccess]::Write,
                [IO.FileShare]::None
            )
            $stream.Dispose()
        } catch {
            $failed = $true
        }
        Assert-True $failed "Exclusive create unexpectedly succeeded for '$($cell.cellId)'."
        Assert-True (-not (Test-Path $cell.destination)) "Writer failure created '$($cell.destination)'."
        Assert-True (
            [Convert]::ToHexString($before) -ceq
            [Convert]::ToHexString([IO.File]::ReadAllBytes($parent))
        ) "Writer failure changed parent bytes for '$($cell.cellId)'."
    }
    Write-Host '  [OK] WriterFailureFixtureActuallyFailsCreate'

    $firstSave = @($manifest.cells | Where-Object storageCase -EQ 'save')[0]
    $secondSave = @($manifest.cells | Where-Object storageCase -EQ 'save')[1]
    Set-Content (Join-Path $firstSave.artifactRoot 'cell-a-only.txt') 'owned by cell A'
    Assert-True (-not (Test-Path (Join-Path $secondSave.artifactRoot 'cell-a-only.txt'))) (
        'A repeated cell observed another cell artifact.'
    )
    Write-Host '  [OK] RepeatedCellsHaveNoArtifactOrTraceCrosstalk setup'

    function Assert-TamperRejectedBeforeActor {
        param([string]$Name, [scriptblock]$Mutate)

        $tamperOutput = Join-Path $testRoot "tamper-$Name"
        & $helper Prepare `
            -TrustedRoot $repoRoot `
            -CandidateRoot $repoRoot `
            -OutputRoot $tamperOutput `
            -CaseName session-storage-collision-preserves-existing-file `
            -Runs 1 `
            -ActorModel model `
            -JudgeModel judge `
            -ConfirmPrivateHost `
            -VallyCli $fakeVally `
            -ProjectionScript $fakeProjector
        $tamperManifestPath = Join-Path $tamperOutput 'manifest.json'
        $tamperManifest = Get-Content $tamperManifestPath -Raw | ConvertFrom-Json -Depth 100
        & $Mutate $tamperManifest
        $tamperHash = (Get-FileHash $tamperManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $rejected = $false
        try {
            & $helper Run -Manifest $tamperManifestPath -ApprovedManifestSha256 $tamperHash
        } catch {
            $rejected = $true
        }
        Assert-True $rejected "Tamper '$Name' was not rejected."
        Assert-True (@(
            Get-ChildItem $tamperOutput -Filter actor-invoked.txt -Recurse -Force -ErrorAction SilentlyContinue
        ).Count -eq 0) "Tamper '$Name' reached the actor."
        Write-Host "  [OK] $Name"
    }

    Assert-TamperRejectedBeforeActor 'HiddenFixtureMutationFailsBeforeActor' {
        param($m)
        Set-Content (Join-Path $m.cells[0].stageRoot 'eng/skill-evals/investigate-issue/fixtures/.hidden') 'changed'
    }
    Assert-TamperRejectedBeforeActor 'StagedFixtureMetadataMutationFailsBeforeActor' {
        param($m)
        Add-Content (
            Join-Path $m.cells[0].stageRoot 'eng/skill-evals/investigate-issue/fixtures/private-case-metadata.json'
        ) 'changed'
    }
    Assert-TamperRejectedBeforeActor 'CollisionSentinelMutationFailsBeforeActor' {
        param($m)
        Set-Content $m.cells[0].destination 'changed'
    }
    Assert-TamperRejectedBeforeActor 'CellWorkspaceMutationFailsBeforeActor' {
        param($m)
        Set-Content (Join-Path $m.cells[0].workspaceRoot 'changed.txt') 'changed'
    }
    $symlinkCapabilityRoot = Join-Path $testRoot 'symlink-capability'
    $symlinkCapabilityTarget = Join-Path $symlinkCapabilityRoot 'target'
    $symlinkCapabilityLink = Join-Path $symlinkCapabilityRoot 'link'
    New-Item -ItemType Directory -Path $symlinkCapabilityTarget -Force | Out-Null
    $symlinkSupported = $false
    try {
        [IO.Directory]::CreateSymbolicLink(
            $symlinkCapabilityLink,
            $symlinkCapabilityTarget
        ) | Out-Null
        $symlinkSupported = $true
    } catch [PlatformNotSupportedException] {
        Write-Host '  [SKIP] CellSymlinkFailsBeforeActor (symbolic links are unsupported)'
    } catch [UnauthorizedAccessException] {
        Write-Host '  [SKIP] CellSymlinkFailsBeforeActor (symbolic-link privilege is unavailable)'
    } catch [NotSupportedException] {
        Write-Host '  [SKIP] CellSymlinkFailsBeforeActor (symbolic links are unsupported)'
    } finally {
        if (Test-Path $symlinkCapabilityLink) {
            Remove-Item $symlinkCapabilityLink -Force
        }
    }
    if ($symlinkSupported) {
        Assert-TamperRejectedBeforeActor 'CellSymlinkFailsBeforeActor' {
            param($m)
            New-Item -ItemType SymbolicLink `
                -Path (Join-Path $m.cells[0].workspaceRoot 'linked-artifacts') `
                -Target $m.cells[0].artifactRoot | Out-Null
        }
    }
    Assert-TamperRejectedBeforeActor 'ControllerPreparationReceiptMutationFailsBeforeActor' {
        param($m)
        $receipt = Get-Content $m.controller.prepareReceiptPath -Raw | ConvertFrom-Json -Depth 100
        $receipt.cells[0].submittedHostInputHash = 'changed'
        $receipt | ConvertTo-Json -Depth 100 |
            Set-Content $m.controller.prepareReceiptPath -Encoding utf8NoBOM
    }

    $runOutput = Join-Path $testRoot 'run-prepared'
    & $helper Prepare `
        -TrustedRoot $repoRoot `
        -CandidateRoot $repoRoot `
        -OutputRoot $runOutput `
        -CaseName no-approval-performs-zero-execution `
        -Runs 1 `
        -ActorModel model `
        -JudgeModel judge `
        -ConfirmPrivateHost `
        -VallyCli $fakeVally `
        -ProjectionScript $fakeProjector
    $runManifest = Join-Path $runOutput 'manifest.json'
    $approvedHash = (Get-FileHash $runManifest -Algorithm SHA256).Hash.ToLowerInvariant()
    & $helper Run -Manifest $runManifest -ApprovedManifestSha256 $approvedHash
    $runData = Get-Content $runManifest -Raw | ConvertFrom-Json -Depth 100
    $baselineReceipt = Get-Content (
        Join-Path (Split-Path $runData.cells[0].outputRoot -Parent) 'receipt.json'
    ) -Raw | ConvertFrom-Json
    $skilledReceipt = Get-Content (
        Join-Path (Split-Path $runData.cells[1].outputRoot -Parent) 'receipt.json'
    ) -Raw | ConvertFrom-Json
    Assert-True ($baselineReceipt.exitCode -eq 1 -and $baselineReceipt.state -eq 'complete') (
        'A complete baseline grade failure was treated as infrastructure failure.'
    )
    Assert-True ($skilledReceipt.state -eq 'complete') (
        'The skilled cell did not run after a complete baseline grade failure.'
    )
    $actorControl = Get-Content $runData.controller.actorControlPath -Raw |
        ConvertFrom-Json -Depth 100
    $executionControl = Get-Content $runData.controller.executionControlPath -Raw |
        ConvertFrom-Json -Depth 100
    Assert-True (
        $actorControl.captureSource -ceq 'prepare_investigate_issue_run.ps1' -and
        $actorControl.approvedManifestInvocation.approvedManifestHash -ceq $approvedHash -and
        $actorControl.approvedManifestInvocation.actualManifestHash -ceq $approvedHash -and
        @($actorControl.cells).Count -eq 2
    ) 'Run did not capture the approved-manifest invocation.'
    foreach ($cellControl in @($actorControl.cells)) {
        $manifestCell = @($runData.cells | Where-Object cellId -CEQ $cellControl.cellId)
        Assert-True (
            $manifestCell.Count -eq 1 -and
            $cellControl.hostInputHash -ceq $manifestCell[0].setupHash -and
            $cellControl.canonicalInputHash -ceq $manifestCell[0].canonicalInputHash -and
            $cellControl.effectiveInputHash -ceq $manifestCell[0].effectiveInputHash -and
            $cellControl.storagePermission -ceq $manifestCell[0].storageCase -and
            $cellControl.storagePath -ceq $manifestCell[0].destination -and
            $cellControl.scenarioControl.source -ceq 'frozen-case-input' -and
            $cellControl.scenarioControl.actualReproductionObservation -ceq 'unknown' -and
            $cellControl.nativeResultHash -ceq (
                Get-FileHash $cellControl.nativeResultPath -Algorithm SHA256
            ).Hash.ToLowerInvariant()
        ) "Run controller capture is not bound to '$($cellControl.cellId)'."
    }
    Assert-True (
        @($executionControl.cells | Where-Object {
            $_.topLevelProcessExited -eq $true -and
            $_.environmentRestored -eq $true -and
            $_.ownedDescendantsExited -ceq 'unknown' -and
            $_.unrelatedMarkersUnchanged -ceq 'unknown'
        }).Count -eq 2
    ) 'Run invented unsupported descendant or host cleanup observations.'
    $actorAssessmentPath = Join-Path $runOutput 'captured-actor-assessment.json'
    [ordered]@{
        schemaVersion = 1
        action = 'ActorTrace'
        manifest = (Resolve-Path $runManifest).Path
        manifestHash = $approvedHash
        hostControlReceipt = $runData.controller.actorControlPath
        hostControlReceiptHash = (
            Get-FileHash $runData.controller.actorControlPath -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        status = 'passed'
    } | ConvertTo-Json | Set-Content $actorAssessmentPath -Encoding utf8NoBOM
    $executionAssessmentPath = Join-Path $runOutput 'captured-execution-assessment.json'
    & $effectChecker ExecutionReceipt `
        -Manifest $runManifest `
        -Receipt $actorAssessmentPath `
        -HostControlReceipt $runData.controller.executionControlPath `
        -Output $executionAssessmentPath
    $executionAssessment = Get-Content $executionAssessmentPath -Raw | ConvertFrom-Json
    Assert-True (
        $executionAssessment.status -ceq 'partial' -and
        $executionAssessment.pendingGates -ccontains 'ExecutionReceiptMatchesToolsAndCleanup'
    ) 'Unknown descendant/host observations did not preserve partial acceptance.'
    Write-Host '  [OK] ControllerCaptureBindsSubmittedInputsResultsAndUnknownOutcomes'
    Write-Host '  [OK] NonzeroGradeExitPreservesCompleteBaselinePair launch'

    $regressionFailures = [Collections.Generic.List[string]]::new()
    $regressions = [ordered]@{
        PinnedRunnerProducesInventoriedWorkspaceInputs = {
            $module = Join-Path (
                Split-Path (Split-Path $realManifest.toolchain.cliEntry -Parent) -Parent
            ) '../vally/dist/index.js'
            $probeRoot = Join-Path $testRoot 'native-workspace-probe'
            $script = @'
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFile, readdir } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
const [modulePath, manifestPath, root] = process.argv.slice(1);
const { runEval } = await import(pathToFileURL(path.resolve(modulePath)));
const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
let invocations = 0;
for (const cell of manifest.cells.slice(0, 2)) {
  const workspace = path.join(root, cell.variant, cell.stimulus);
  const result = await runEval({
    prompt: "Model-free workspace-injection check; no issue investigation or model call.",
    skills: [],
    workDir: cell.cwd,
    workspace,
    environment: {
      skills: cell.variant === "skilled"
        ? [path.join(cell.stageRoot, ".github/skills/investigate-issue")]
        : [],
    },
    executor: {
      name: "model-free-input-observer",
      async execute(stimulus, options) {
        invocations++;
        assert.equal(options.workDir, workspace);
        assert.notEqual(options.workDir, cell.cwd);
        const inputs = [];
        async function visit(directory) {
          for (const entry of await readdir(directory, { withFileTypes: true })) {
            const file = path.join(directory, entry.name);
            if (entry.isDirectory()) {
              await visit(file);
            } else {
              inputs.push({
                relativePath: path.relative(workspace, file).split(path.sep).join("/"),
                sha256: createHash("sha256").update(await readFile(file)).digest("hex"),
              });
            }
          }
        }
        await visit(workspace);
        const byPath = (a, b) => a.relativePath.localeCompare(b.relativePath);
        assert.deepEqual(inputs.sort(byPath), [...cell.runnerInputs].sort(byPath));
        return { workDir: options.workDir, output: "", events: [], stimulus };
      },
    },
  });
  try {
    assert.equal(result.trajectory.workDir, workspace);
  } finally {
    await result.cleanup();
  }
}
assert.equal(invocations, 2);
'@
            & node --input-type=module -e $script $module (
                Join-Path $realProjectionOutput 'manifest.json'
            ) $probeRoot
            Assert-True ($LASTEXITCODE -eq 0) (
                'The pinned runner did not produce the declared input inventory and actor working directory.'
            )
        }
        PinnedSessionFilesystemRejectsExternalPersistence = {
            $provider = Join-Path (
                Split-Path (Split-Path $realManifest.toolchain.cliEntry -Parent) -Parent
            ) '../vally/dist/executor/local-session-fs-handler.js'
            $probeRoot = Join-Path $testRoot 'native-filesystem-probe'
            $script = @'
import assert from "node:assert/strict";
import { access, mkdir } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
const [modulePath, root] = process.argv.slice(1);
const { LocalSessionFsHandler } = await import(pathToFileURL(path.resolve(modulePath)));
const sessionRoot = path.join(root, "native-session");
const artifacts = path.join(root, "artifacts");
await mkdir(sessionRoot, { recursive: true });
await mkdir(artifacts, { recursive: true });
const provider = new LocalSessionFsHandler(sessionRoot);
const outside = path.join(artifacts, "issue-999024-investigation.md");
await assert.rejects(provider.writeFile(outside, "report"), /Session filesystem path escapes root/);
await assert.rejects(access(outside), { code: "ENOENT" });
const inside = path.join(sessionRoot, "control.txt");
await provider.writeFile(inside, "control");
assert.equal(await provider.readFile(inside), "control");
'@
            & node --input-type=module -e $script $provider $probeRoot
            Assert-True ($LASTEXITCODE -eq 0) (
                'The pinned native filesystem no longer has the observed external-path limitation.'
            )
        }
        PreparedRunnerInputsBindExactStagedBytes = {
            foreach ($cell in $realManifest.cells) {
                Assert-True ($cell.evidencePolicy -ceq 'frozen-input-only') (
                    "Cell '$($cell.cellId)' does not declare the frozen evidence boundary."
                )
                Assert-True ($cell.storageCapability -ceq 'unsupported-native-session-filesystem') (
                    "Cell '$($cell.cellId)' incorrectly claims an external native-storage capability."
                )
                if ($cell.variant -ceq 'baseline') {
                    Assert-True (@($cell.runnerInputs).Count -eq 0) 'Baseline grants skill inputs.'
                    continue
                }
                $skillRoot = Join-Path $cell.stageRoot '.github/skills/investigate-issue'
                $files = @(Get-ChildItem $skillRoot -File -Recurse -Force)
                Assert-True (@($cell.runnerInputs).Count -eq $files.Count) (
                    'Runner inputs do not enumerate the staged skill exactly.'
                )
                foreach ($file in $files) {
                    $relative = 'investigate-issue/' + [IO.Path]::GetRelativePath(
                        $skillRoot, $file.FullName
                    ).Replace('\', '/')
                    $entry = @($cell.runnerInputs | Where-Object relativePath -CEQ $relative)
                    Assert-True (
                        $entry.Count -eq 1 -and
                        $entry[0].sha256 -ceq (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                    ) "Runner input '$relative' is not bound to its exact staged bytes."
                }
            }
        }
        ControllerRecordsNativeWorkDirNotLauncherCwd = {
            foreach ($cellControl in $actorControl.cells) {
                $cell = @($runData.cells | Where-Object cellId -CEQ $cellControl.cellId)[0]
                $expected = Join-Path $cell.workspaceRoot "$($cell.variant)/$($cell.stimulus)"
                Assert-True (
                    $cellControl.actorWorkDir -ceq $expected -and
                    $cellControl.actorWorkDir -cne $cell.cwd
                ) 'The controller did not retain the native actor working directory.'
            }
        }
        UnsupportedNativeStorageBlocksBeforeAnyActorLaunch = {
            $blockedOutput = Join-Path $testRoot 'unsupported-native-storage'
            & $helper Prepare `
                -TrustedRoot $repoRoot `
                -CandidateRoot $repoRoot `
                -OutputRoot $blockedOutput `
                -CaseName ordinary-report-uses-trusted-session-storage `
                -Runs 1 `
                -ActorModel model `
                -JudgeModel judge `
                -ConfirmPrivateHost `
                -VallyCli $fakeVally `
                -ProjectionScript $fakeProjector
            $path = Join-Path $blockedOutput 'manifest.json'
            $rejected = $false
            try {
                & $helper Run -Manifest $path -ApprovedManifestSha256 (
                    Get-FileHash $path -Algorithm SHA256
                ).Hash.ToLowerInvariant()
            } catch {
                $rejected = $_.Exception.Message -like '*native session filesystem cannot grant*'
            }
            Assert-True $rejected 'Run accepted a manifest with unsupported native persistence.'
            Assert-True (
                @(Get-ChildItem $blockedOutput -Filter actor-invoked.txt -Recurse -Force).Count -eq 0
            ) 'The unsupported storage capability was checked after an actor launched.'
        }
    }
    foreach ($regression in $regressions.GetEnumerator()) {
        try {
            & $regression.Value
            Write-Host "  [OK] $($regression.Key)"
        } catch {
            $regressionFailures.Add("$($regression.Key): $($_.Exception.Message)")
            Write-Host "  [FAIL] $($regressionFailures[-1])"
        }
    }
    Assert-True ($regressionFailures.Count -eq 0) ($regressionFailures -join "`n")
    Write-Host 'Investigate-issue private preparation self-test passed.'
} finally {
    Remove-Item $testRoot -Recurse -Force
}
