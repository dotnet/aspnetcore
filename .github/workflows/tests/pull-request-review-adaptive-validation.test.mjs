import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '../../..');
const workflowSource = fs.readFileSync(
  path.join(repositoryRoot, '.github/workflows/pull-request-review.md'), 'utf8');
const workflowLock = fs.readFileSync(
  path.join(repositoryRoot, '.github/workflows/pull-request-review.lock.yml'), 'utf8');
const workflowTests = fs.readFileSync(
  path.join(repositoryRoot, '.github/workflows/workflow-tests.yml'), 'utf8');
const skill = fs.readFileSync(
  path.join(repositoryRoot, '.github/skills/review-pull-request/SKILL.md'), 'utf8');
const localWorkerProfile = fs.readFileSync(
  path.join(repositoryRoot, '.github/agents/review-guide-worker.agent.md'), 'utf8');
const retargetDiagnostic =
  'The PR head moved, the base branch changed, or the PR closed after review; safe outputs are blocked.';
const normalizeProfile = value => value.replaceAll('\r\n', '\n').trimEnd();
const inlineWorkerProfile = workflowSource.match(
  /## agent: `review-guide-worker`\r?\n([\s\S]+)$/);

assert.ok(inlineWorkerProfile, 'The hosted review-guide-worker profile must be extractable.');
assert.doesNotMatch(inlineWorkerProfile[1], /user-invocable|strict-tools-list/);
assert.doesNotMatch(localWorkerProfile, /disable-model-invocation/);
assert.equal(
  normalizeProfile(localWorkerProfile),
  normalizeProfile(inlineWorkerProfile[1])
    .replace(
      'description: Reviews one routed ASP.NET Core guide using only frozen source and assigned guidance',
      'description: Internal per-guide worker for review-pull-request; not for direct use. Reviews one routed ASP.NET Core guide using only frozen source and assigned guidance')
    .replace(
      'tools: ["read", "search", "skill"]',
      'tools: ["read", "search", "skill"]\nstrict-tools-list: true\nuser-invocable: false'),
  'The local review-guide-worker profile may differ only by its internal local flags and description.');

assert.match(workflowSource, /bash:\s*\[":\*"\]/);
assert.match(workflowSource, /^\s+edit:\s*$/m);
assert.match(workflowSource, /memory:\s*8g/);
assert.doesNotMatch(workflowSource, /^\s+- (?:containers|defaults|dotnet|github|java|node|powershell|python)\s*$/m);
assert.match(workflowSource, /- "builds\.dotnet\.microsoft\.com"/);
assert.match(workflowSource, /- "ci\.dot\.net"/);
assert.match(workflowSource, /- "pkgs\.dev\.azure\.com"/);
assert.match(workflowSource, /- "\*\.vsblob\.vsassets\.io"/);
assert.match(workflowSource, /- "data\.nuget\.org"/);
assert.match(workflowSource, /- "oneocsp\.microsoft\.com"/);
assert.match(workflowSource, /- "www\.microsoft\.com"/);
assert.match(workflowSource, /- playwright/);
assert.doesNotMatch(workflowSource, /- "(?:github\.com|release-assets\.githubusercontent\.com|storage\.googleapis\.com)"/);
assert.match(workflowSource, /DOTNET_CLI_TELEMETRY_OPTOUT:\s*"1"/);
assert.match(workflowSource, /name: Verify restricted review workers/);
assert.match(workflowSource, /expectedAgentName = 'review-guide-worker'/);
assert.match(workflowSource, /allowedTools = new Set\(\['glob', 'grep', 'read_file', 'rg', 'search', 'skill', 'view'\]\)/);
assert.match(workflowSource, /REVIEW_EXPECTED_WORKER_COUNT: \$\{\{ steps\.prepare_review_bundle\.outputs\.expected_worker_count \}\}/);
assert.match(workflowSource, /const expectedWorkerCount = Number\(expectedWorkerCountValue\)/);
assert.match(workflowSource, /event\.data\.toolName\.startsWith\('safeoutputs-'\)/);
assert.match(workflowSource, /event\.type === 'tool\.execution_complete'/);
assert.match(workflowSource, /const validStoppedOutput = hasValidStoppedOutput\(\)/);
assert.match(workflowSource, /const missingWorkersAllowed = validStoppedOutput && workers\.size < expectedWorkerCount/);
assert.match(workflowSource, /event\.data\?\.toolName !== 'read_agent'/);
assert.match(workflowSource, /input\.wait !== true/);
assert.match(workflowSource, /const isInitialCollection = sinceTurn === undefined \|\| sinceTurn === 0/);
assert.match(workflowSource, /PATH:\[ \\t\]\*per-guide-worker/);
assert.match(workflowSource, /completion\.index >= firstPublicationIndex/);
assert.match(workflowSource, /id: prepare_review_bundle/);
assert.match(workflowSource, /printf 'expected_worker_count=%s\\n' "\$expected_worker_count" >> "\$GITHUB_OUTPUT"/);
assert.match(workflowSource, /Ready review manifest has no routed guides/);
assert.match(
  workflowSource,
  /if \(workers\.size !== expectedWorkerCount && !missingWorkersAllowed\)/);
assert.match(workflowSource, /if \(event\.data\?\.parentToolCallId\)/);
assert.match(workflowSource, /## agent: `review-guide-worker`/);
assert.match(workflowSource, /tools: \["read", "search", "skill"\]/);
assert.match(workflowSource, /worktree=\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowSource, /update-ref refs\/review\/head "\$REVIEW_HEAD"/);
assert.match(workflowSource, /refs\/review\/merge-base:refs\/remotes\/review\/merge-base/);
assert.match(workflowSource, /--depth=1 "\$store"/);
assert.match(workflowSource, /status --porcelain\)"; then\s+:\s+else/);
assert.match(workflowSource, /chmod -R a\+rX,a-w \/tmp\/gh-aw\/review-bundle/);
assert.match(workflowSource, /Only after collecting every required guide worker's actual result/);
assert.match(workflowSource, /write temporary\s+tests or repros/);
assert.match(workflowSource, /PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1/);
assert.match(workflowSource, /existing `Microsoft\.Playwright` NuGet dependency/);
assert.match(workflowSource, /Do not inject package-feed credentials/);
assert.match(workflowSource, /configured npm feed requires authentication/);
assert.match(workflowSource, /explicitly\s+select an already-installed system Chrome or Chromium executable/);
assert.doesNotMatch(workflowSource, /review_execution|review-execution|execution\.json/);
assert.equal(workflowSource.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowSource, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.match(workflowLock, /--allow-tool shell/);
assert.match(workflowLock, /--allow-tool write/);
assert.match(workflowLock, /--memory-limit 8g/);
assert.match(workflowLock, /GH_AW_TOOL_TIMEOUT: "?1800"?/);
assert.match(workflowLock, /Restore inline sub-agents from activation artifact/);
assert.match(workflowLock, /Verify restricted review workers/);
assert.match(workflowLock, /builds\.dotnet\.microsoft\.com/);
assert.match(workflowLock, /pkgs\.dev\.azure\.com/);
assert.doesNotMatch(workflowLock, /release-assets\.githubusercontent\.com/);
assert.doesNotMatch(workflowLock, /storage\.googleapis\.com/);
assert.doesNotMatch(workflowLock, /api\.nuget\.org/);
assert.doesNotMatch(workflowLock, /registry\.npmjs\.org/);
assert.doesNotMatch(workflowLock, /pypi\.org/);
assert.match(workflowLock, /worktree=\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowLock, /refs\/review\/head:refs\/remotes\/review\/head/);
assert.match(workflowLock, /status --porcelain\)\\"; then\\n\s+:\\nelse/);
assert.doesNotMatch(workflowLock, /review_execution|review-execution|review-build-sandbox/);
assert.equal(workflowLock.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowLock, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.match(workflowTests, /- '\.github\/agents\/\*\*'/);

assert.match(skill, /write temporary\s+tests or repros/);
assert.match(skill, /Temporary validation edits are diagnostic only/);
assert.match(skill, /Workers remain source-only/);
assert.match(skill, /`review-guide-worker` custom agent/);
assert.doesNotMatch(skill, /full-capability\s+`general-purpose` agent/);

const verifierMatch = workflowSource.match(
  /name: Verify restricted review workers[\s\S]*?node - <<'NODE'\r?\n([\s\S]*?)\r?\n      NODE/);
assert.ok(verifierMatch, 'The restricted-worker verifier script must be extractable.');
const lockVerifierStep = workflowLock.match(
  /name: Verify restricted review workers\r?\n\s+run: ("(?:[^"\\]|\\.)*")\r?\n/);
assert.ok(lockVerifierStep, 'The compiled restricted-worker verifier step must be extractable.');
const lockVerifierRun = JSON.parse(lockVerifierStep[1]);
const lockVerifierMatch = lockVerifierRun.match(/node - <<'NODE'\n([\s\S]*?)\nNODE$/);
assert.ok(lockVerifierMatch, 'The compiled restricted-worker verifier script must be extractable.');
const sourceVerifier = verifierMatch[1].replace(/^ {6}/gm, '');
assert.equal(
  lockVerifierMatch[1],
  sourceVerifier,
  'The compiled restricted-worker verifier must match the workflow source.');

const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'review-worker-verifier-'));
try {
  const verifierPath = path.join(tempRoot, 'verify.cjs');
  fs.writeFileSync(verifierPath, sourceVerifier);

  const runVerifier = (events, expectedWorkerCount) => {
    const sessionPath = path.join(tempRoot, 'agent-session.jsonl');
    fs.writeFileSync(sessionPath, events.map(event => JSON.stringify(event)).join('\n'));

    return spawnSync(process.execPath, [verifierPath], {
      encoding: 'utf8',
      env: {
        ...process.env,
        REVIEW_WORKER_SESSION_PATH: sessionPath,
        REVIEW_EXPECTED_WORKER_COUNT: expectedWorkerCount,
      },
    });
  };

  const toolArguments = (arguments_, inputOnly) => inputOnly
    ? { input: arguments_ }
    : { arguments: arguments_, input: arguments_ };
  const taskStarted = (
    toolCallId,
    name,
    { agentType = 'review-guide-worker', inputOnly = false } = {}) => ({
    type: 'tool.execution_start',
    data: {
      toolCallId,
      toolName: 'task',
      ...toolArguments({ agent_type: agentType, name, mode: 'background' }, inputOnly),
    },
  });
  const taskSpawned = (toolCallId, handle, success = true) => ({
    type: 'tool.execution_complete',
    data: {
      toolCallId,
      toolName: 'task',
      success,
      result: {
        content: `Agent started in background with agent_id: ${handle}.`,
      },
    },
  });
  const workerStarted = (toolCallId, agentId = toolCallId, agentName = 'review-guide-worker') => ({
    type: 'subagent.started',
    agentId,
    data: { toolCallId, agentName },
  });
  const workerCompleted = (toolCallId, agentId = toolCallId, data = {}) => ({
    type: 'subagent.completed',
    agentId,
    data: { toolCallId, agentName: 'review-guide-worker', ...data },
  });
  const workerFailed = (toolCallId, agentId = toolCallId) => ({
    type: 'subagent.failed',
    agentId,
    data: { toolCallId, agentName: 'review-guide-worker', success: false, error: 'worker failed' },
  });
  const workerTool = (agentId, toolName) => ({
    type: 'tool.execution_start',
    agentId,
    data: { toolName },
  });
  const readStarted = (
    toolCallId,
    agentId,
    { wait = true, inputOnly = false, sinceTurn } = {}) => ({
    type: 'tool.execution_start',
    data: {
      toolCallId,
      toolName: 'read_agent',
      ...toolArguments({
        agent_id: agentId,
        wait,
        ...(sinceTurn === undefined ? {} : { since_turn: sinceTurn }),
      }, inputOnly),
    },
  });
  const readCompleted = (
    toolCallId,
    content = 'Agent is idle. status: idle\n\nPATH: per-guide-worker\nRESULT: complete',
    success = true,
    includeToolName = true) => ({
    type: 'tool.execution_complete',
    data: {
      toolCallId,
      ...(includeToolName ? { toolName: 'read_agent' } : {}),
      success,
      result: { content },
    },
  });
  const safeOutputCall = (toolCallId, toolName, arguments_, success = true) => [
    {
      type: 'tool.execution_start',
      data: { toolCallId, toolName, arguments: arguments_ },
    },
    {
      type: 'tool.execution_complete',
      data: { toolCallId, success },
    },
  ];
  const stopped = (status = 'BLOCKED', reason = 'The review could not complete.') => [
    ...safeOutputCall('incomplete', 'safeoutputs-report_incomplete', { reason }),
    ...safeOutputCall('comment', 'safeoutputs-add_comment', {
      body: `Review not published (${status}): ${reason}\n\nNo partial findings were published.`,
    }),
  ];
  const publication = (toolCallId = 'publication') =>
    safeOutputCall(toolCallId, 'safeoutputs-noop', { message: 'No findings.' });
  const hostedWorker = (
    toolCallId = 'task-hosted',
    name = 'guide-hosted') => [
    taskStarted(toolCallId, name),
    workerStarted(toolCallId),
    taskSpawned(toolCallId, name),
    workerTool(toolCallId, 'skill'),
    workerTool(toolCallId, 'view'),
    workerCompleted(toolCallId),
    readStarted(`read-${toolCallId}`, name),
    readCompleted(`read-${toolCallId}`),
  ];
  const sameIdWorker = (id = 'worker-same-id') => [
    taskStarted(id, id, { inputOnly: true }),
    workerStarted(id),
    workerCompleted(id),
    readStarted(`read-${id}`, id, { inputOnly: true }),
    readCompleted(`read-${id}`),
  ];

  assert.equal(runVerifier([
    ...hostedWorker('task-hosted', 'guide-hosted'),
    ...publication(),
  ], '1').status, 0);
  assert.equal(runVerifier([
    ...hostedWorker('task-named', 'guide-named'),
    readStarted('read-follow-up', 'guide-named', { sinceTurn: 1 }),
    readCompleted('read-follow-up', 'Follow-up details without a repeated contract marker.'),
    ...publication(),
  ], '1').status, 0);
  assert.equal(runVerifier([...sameIdWorker(), ...publication()], '1').status, 0);
  assert.equal(runVerifier([
    taskStarted('task-newer', 'guide-newer'),
    workerStarted('task-newer', 'worker-newer'),
    readStarted('read-newer', 'worker-newer'),
    readCompleted(
      'read-newer',
      'Agent is idle. status: idle\n\nPATH: per-guide-worker\nRESULT: complete',
      true,
      false),
    ...publication(),
  ], '1').status, 0);
  assert.notEqual(runVerifier([], '0').status, 0);
  assert.equal(runVerifier(stopped(), '0').status, 0);
  assert.equal(runVerifier([
    ...hostedWorker('task-one', 'guide-one'),
    ...sameIdWorker('worker-two'),
    ...publication(),
  ], '2').status, 0);

  const invalidWorkerRequirement = runVerifier([], '');
  assert.notEqual(invalidWorkerRequirement.status, 0);
  assert.match(invalidWorkerRequirement.stderr, /Trusted worker count is invalid/);

  const missingWorker = runVerifier([], '1');
  assert.notEqual(missingWorker.status, 0);
  assert.match(missingWorker.stderr, /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingSecondWorker = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
  ], '2');
  assert.notEqual(missingSecondWorker.status, 0);
  assert.match(missingSecondWorker.stderr, /Expected 2 per-guide review worker\(s\), but recorded 1/);

  assert.equal(runVerifier(stopped(), '1').status, 0);
  assert.equal(runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerFailed('task-one'),
    ...stopped('INCOMPLETE'),
  ], '2').status, 0);
  assert.equal(runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerFailed('task-one'),
    taskStarted('task-two', 'guide-two'),
    workerStarted('task-two'),
    taskSpawned('task-two', 'guide-two'),
    workerCompleted('task-two', 'task-two', { cancelled: true }),
    ...stopped('INCOMPLETE'),
  ], '2').status, 0);

  const stoppedWithExcessWorker = runVerifier(
    [
      taskStarted('task-one', 'guide-one'),
      workerStarted('task-one'),
      taskStarted('task-two', 'guide-two'),
      workerStarted('task-two'),
      ...stopped(),
    ],
    '1');
  assert.notEqual(stoppedWithExcessWorker.status, 0);
  assert.match(stoppedWithExcessWorker.stderr, /Expected 1 per-guide review worker\(s\), but recorded 2/);

  const missingWorkerWithNoop = runVerifier(
    safeOutputCall('noop', 'safeoutputs-noop', { message: 'No findings.' }),
    '1');
  assert.notEqual(missingWorkerWithNoop.status, 0);
  assert.match(missingWorkerWithNoop.stderr, /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithMalformedStoppedOutput = runVerifier([
    ...safeOutputCall('incomplete', 'safeoutputs-report_incomplete', { reason: 'Reason' }),
    ...safeOutputCall('comment', 'safeoutputs-add_comment', {
      body: 'Review not published (BLOCKED): Different reason',
    }),
  ], '1');
  assert.notEqual(missingWorkerWithMalformedStoppedOutput.status, 0);
  assert.match(
    missingWorkerWithMalformedStoppedOutput.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithFailedStoppedOutput = runVerifier([
    ...safeOutputCall(
      'incomplete',
      'safeoutputs-report_incomplete',
      { reason: 'The review could not complete.' },
      false),
    ...safeOutputCall('comment', 'safeoutputs-add_comment', {
      body: 'Review not published (BLOCKED): The review could not complete.\n\nNo partial findings were published.',
    }),
  ], '1');
  assert.notEqual(missingWorkerWithFailedStoppedOutput.status, 0);
  assert.match(
    missingWorkerWithFailedStoppedOutput.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithAdditionalSafeOutput = runVerifier([
    ...stopped(),
    ...safeOutputCall('extra', 'safeoutputs-noop', { message: 'No findings.' }),
  ], '1');
  assert.notEqual(missingWorkerWithAdditionalSafeOutput.status, 0);
  assert.match(
    missingWorkerWithAdditionalSafeOutput.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithMissingCallIds = runVerifier([
    {
      type: 'tool.execution_start',
      data: {
        toolName: 'safeoutputs-report_incomplete',
        arguments: { reason: 'The review could not complete.' },
      },
    },
    {
      type: 'tool.execution_start',
      data: {
        toolName: 'safeoutputs-add_comment',
        arguments: {
          body: 'Review not published (BLOCKED): The review could not complete.\n\nNo partial findings were published.',
        },
      },
    },
    { type: 'tool.execution_complete', data: { success: true } },
  ], '1');
  assert.notEqual(missingWorkerWithMissingCallIds.status, 0);
  assert.match(
    missingWorkerWithMissingCallIds.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithDuplicateCallIds = runVerifier([
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: 'duplicate',
        toolName: 'safeoutputs-report_incomplete',
        arguments: { reason: 'The review could not complete.' },
      },
    },
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: 'duplicate',
        toolName: 'safeoutputs-add_comment',
        arguments: {
          body: 'Review not published (BLOCKED): The review could not complete.\n\nNo partial findings were published.',
        },
      },
    },
    {
      type: 'tool.execution_complete',
      data: { toolCallId: 'duplicate', success: true },
    },
  ], '1');
  assert.notEqual(missingWorkerWithDuplicateCallIds.status, 0);
  assert.match(
    missingWorkerWithDuplicateCallIds.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingWorkerWithScopedCompletion = stopped();
  missingWorkerWithScopedCompletion[1].data.parentToolCallId = 'worker-1';
  const scopedCompletionResult = runVerifier(missingWorkerWithScopedCompletion, '1');
  assert.notEqual(scopedCompletionResult.status, 0);
  assert.match(
    scopedCompletionResult.stderr,
    /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const wrongWorker = runVerifier([
    taskStarted('task-one', 'guide-one', { agentType: 'general-purpose' }),
    workerStarted('task-one', 'task-one', 'general-purpose'),
    ...stopped(),
  ], '1');
  assert.notEqual(wrongWorker.status, 0);
  assert.match(wrongWorker.stderr, /general-purpose/);

  const disallowedTool = runVerifier(
    [
      taskStarted('task-one', 'guide-one'),
      workerStarted('task-one'),
      workerTool('task-one', 'bash'),
      ...stopped(),
    ],
    '1');
  assert.notEqual(disallowedTool.status, 0);
  assert.match(disallowedTool.stderr, /used disallowed tool 'bash'/);

  const unattributedWorkerTool = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    {
      type: 'tool.execution_start',
      data: { toolName: 'bash', parentToolCallId: 'worker-1' },
    },
    ...stopped(),
  ], '1');
  assert.notEqual(unattributedWorkerTool.status, 0);
  assert.match(unattributedWorkerTool.stderr, /is missing an agent ID/);

  const startOnly = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    ...publication(),
  ], '1');
  assert.notEqual(startOnly.status, 0);
  assert.match(startOnly.stderr, /was not collected/);

  const excessTaskStart = runVerifier([
    ...hostedWorker('task-one', 'guide-one'),
    taskStarted('task-two', 'guide-two'),
    ...publication(),
  ], '1');
  assert.notEqual(excessTaskStart.status, 0);
  assert.match(
    excessTaskStart.stderr,
    /Expected at most 1 review worker task start\(s\), but recorded 2/);

  const cancelledWorker = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one', 'task-one', { cancelled: true }),
    ...publication(),
  ], '1');
  assert.notEqual(cancelledWorker.status, 0);
  assert.match(cancelledWorker.stderr, /was not collected/);

  const completedButUncollected = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    ...publication(),
  ], '1');
  assert.notEqual(completedButUncollected.status, 0);
  assert.match(completedButUncollected.stderr, /was not collected/);

  const wrongWorkerCollection = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    readStarted('read-one', 'guide-other'),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.notEqual(wrongWorkerCollection.status, 0);
  assert.match(wrongWorkerCollection.stderr, /unknown worker 'guide-other'/);

  const nonWaitingCollection = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    readStarted('read-one', 'guide-one', { wait: false }),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.notEqual(nonWaitingCollection.status, 0);
  assert.match(nonWaitingCollection.stderr, /wait: true/);

  const lifecycleEventsIgnored = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    workerCompleted('task-one'),
    workerCompleted('task-one', 'task-one', { cancelled: true }),
    readStarted('read-one', 'guide-one'),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.equal(lifecycleEventsIgnored.status, 0);

  const collectionAfterPublication = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    ...publication(),
    readStarted('read-one', 'guide-one'),
    readCompleted('read-one'),
  ], '1');
  assert.notEqual(collectionAfterPublication.status, 0);
  assert.match(collectionAfterPublication.stderr, /before publication/);

  const lifecycleWordsInReport = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    readStarted('read-one', 'guide-one'),
    readCompleted(
      'read-one',
      'Agent status: failed\n\nPATH: per-guide-worker\nRESULT: complete'),
    ...publication(),
  ], '1');
  assert.equal(lifecycleWordsInReport.status, 0);

  const laterTurnOnlyCollection = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    readStarted('read-one', 'guide-one', { sinceTurn: 1 }),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.notEqual(laterTurnOnlyCollection.status, 0);
  assert.match(laterTurnOnlyCollection.stderr, /since_turn absent or 0/);

  const missingPathMarker = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    taskSpawned('task-one', 'guide-one'),
    workerCompleted('task-one'),
    readStarted('read-one', 'guide-one', { sinceTurn: 0 }),
    readCompleted('read-one', 'Agent is idle. status: idle\n\nRESULT: complete'),
    ...publication(),
  ], '1');
  assert.notEqual(missingPathMarker.status, 0);
  assert.match(missingPathMarker.stderr, /PATH: per-guide-worker/);

  const failedReadCompletion = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    readStarted('read-one', 'guide-one'),
    readCompleted('read-one', undefined, false),
    ...publication(),
  ], '1');
  assert.notEqual(failedReadCompletion.status, 0);
  assert.match(failedReadCompletion.stderr, /did not complete successfully/);

  const missingReadCompletion = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    readStarted('read-one', 'guide-one'),
    ...publication(),
  ], '1');
  assert.notEqual(missingReadCompletion.status, 0);
  assert.match(missingReadCompletion.stderr, /did not complete successfully/);

  const nestedTask = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    {
      type: 'tool.execution_start',
      agentId: 'task-one',
      data: {
        toolCallId: 'nested-task',
        toolName: 'task',
        arguments: {
          agent_type: 'review-guide-worker',
          name: 'nested-guide',
          mode: 'background',
        },
      },
    },
    readStarted('read-one', 'guide-one'),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.notEqual(nestedTask.status, 0);
  assert.match(nestedTask.stderr, /used disallowed tool 'task'/);

  const aliasCollision = runVerifier([
    taskStarted('task-one', 'shared-guide'),
    workerStarted('task-one'),
    taskStarted('task-two', 'shared-guide'),
    workerStarted('task-two'),
    readStarted('read-one', 'task-one'),
    readCompleted('read-one'),
    readStarted('read-two', 'task-two'),
    readCompleted('read-two'),
    ...publication(),
  ], '2');
  assert.notEqual(aliasCollision.status, 0);
  assert.match(aliasCollision.stderr, /task name 'shared-guide' is not unique|identifies more than one task/);

  const oneMissingCollection = runVerifier([
    ...hostedWorker('task-one', 'guide-one'),
    taskStarted('task-two', 'guide-two'),
    workerStarted('task-two'),
    taskSpawned('task-two', 'guide-two'),
    workerCompleted('task-two'),
    ...publication(),
  ], '2');
  assert.notEqual(oneMissingCollection.status, 0);
  assert.match(oneMissingCollection.stderr, /guide-two.*was not collected/);
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}

console.log('Validated adaptive in-sandbox pull request review capabilities.');
