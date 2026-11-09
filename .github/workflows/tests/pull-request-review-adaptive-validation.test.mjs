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
const localVerifierProfile = fs.readFileSync(
  path.join(repositoryRoot, '.github/agents/review-candidate-verifier.agent.md'), 'utf8');
const retargetDiagnostic =
  'The PR head moved, the base branch changed, or the PR closed after review; safe outputs are blocked.';
const nativePublicationDiagnostic =
  'No native safe-output publication call was recorded; CLI publication is not supported by this adapter.';
const normalizeLineEndings = value => value.replaceAll('\r\n', '\n');
const normalizeProfile = value => normalizeLineEndings(value).trimEnd();
const normalizeVerifier = value => normalizeLineEndings(value).replace(/^ {6}/gm, '');
const inlineWorkerProfile = workflowSource.match(
  /## agent: `review-guide-worker`\r?\n([\s\S]+?)\r?\n\r?\n## agent: `review-candidate-verifier`/);
const inlineVerifierProfile = workflowSource.match(
  /## agent: `review-candidate-verifier`\r?\n([\s\S]+)$/);

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

assert.ok(inlineVerifierProfile, 'The hosted review-candidate-verifier profile must be extractable.');
assert.doesNotMatch(inlineVerifierProfile[1], /user-invocable|strict-tools-list/);
assert.doesNotMatch(localVerifierProfile, /disable-model-invocation/);
assert.equal(
  normalizeProfile(localVerifierProfile),
  normalizeProfile(inlineVerifierProfile[1])
    .replace(
      'description: Tries to disprove only the coordinator\'s provisional findings against frozen bundle source',
      'description: Internal post-gate verifier for review-pull-request; not for direct use. Tries to disprove only the coordinator\'s provisional findings against frozen bundle source')
    .replace(
      'tools: ["read", "search"]',
      'tools: ["read", "search"]\nstrict-tools-list: true\nuser-invocable: false'),
  'The local review-candidate-verifier profile may differ only by its internal local flags and description.');

assert.match(workflowSource, /bash:\s*false/);
assert.match(workflowSource, /edit:\s*false/);
assert.doesNotMatch(workflowSource, /memory:\s*8g/);
assert.match(workflowSource, /  roles: \[admin, maintainer, write\]/);
assert.doesNotMatch(workflowSource, /user-rate-limit/);
assert.match(workflowSource, /network:\r?\n  allowed:\r?\n    - defaults\r?\n    - github\r?\n    - node/);
assert.doesNotMatch(workflowSource, /builds\.dotnet\.microsoft\.com|pkgs\.dev\.azure\.com|playwright/);
assert.doesNotMatch(workflowSource, /DOTNET_CLI_TELEMETRY_OPTOUT/);
assert.match(workflowSource, /name: Verify restricted review workers/);
assert.match(workflowSource, /expectedAgentName = 'review-guide-worker'/);
assert.match(workflowSource, /allowedTools = new Set\(\['glob', 'grep', 'read_file', 'rg', 'search', 'skill', 'view'\]\)/);
assert.match(workflowSource, /REVIEW_EXPECTED_WORKER_COUNT: \$\{\{ steps\.prepare_review_bundle\.outputs\.expected_worker_count \}\}/);
assert.match(workflowSource, /const expectedWorkerCount = Number\(expectedWorkerCountValue\)/);
assert.match(workflowSource, /event\.data\.toolName\.startsWith\('safeoutputs-'\)/);
assert.match(workflowSource, /event\.type === 'tool\.execution_complete'/);
assert.match(workflowSource, /const validStoppedOutput = hasValidStoppedOutput\(\)/);
assert.match(workflowSource, /This adapter supports publication only through the native safe-output functions/);
assert.match(workflowSource, /Do not publish through the `safeoutputs` CLI or any shell command/);
assert.match(workflowSource, /If a required native safe-output function is unavailable, do not fall back to CLI or shell publication/);
assert.ok(workflowSource.includes(nativePublicationDiagnostic));
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
assert.doesNotMatch(workflowSource, /\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowSource, /chmod -R a\+rX,a-w \/tmp\/gh-aw\/review-bundle/);
assert.match(workflowSource, /Perform source-only analysis of/);
assert.match(workflowSource, /Do not execute target code\./);
assert.match(workflowSource, /stay in your turn and collect each worker's report with `read_agent`/);
assert.doesNotMatch(workflowSource, /review_execution|review-execution|execution\.json/);
assert.equal(workflowSource.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowSource, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.doesNotMatch(workflowLock, /--allow-tool shell/);
assert.doesNotMatch(workflowLock, /--allow-tool write/);
assert.doesNotMatch(workflowLock, /--memory-limit 8g/);
assert.match(workflowLock, /GH_AW_TOOL_TIMEOUT: "?120"?/);
assert.doesNotMatch(workflowLock, /check_rate_limit|GH_AW_RATE_LIMIT|rate_limit_ok/);
assert.match(workflowLock, /Restore inline sub-agents from activation artifact/);
assert.match(workflowLock, /Verify restricted review workers/);
assert.doesNotMatch(workflowLock, /builds\.dotnet\.microsoft\.com|pkgs\.dev\.azure\.com/);
assert.doesNotMatch(workflowLock, /\/tmp\/gh-aw\/review-worktree/);
assert.doesNotMatch(workflowLock, /review_execution|review-execution|review-build-sandbox/);
assert.equal(workflowLock.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowLock, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.match(workflowTests, /- '\.github\/agents\/\*\*'/);

assert.match(skill, /Hosted runs and workers must not execute target code, build, test/);
assert.match(skill, /stay in your turn and\r?\ncollect each worker's actual report with `read_agent` using `wait: true`/);
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
const sourceVerifier = normalizeVerifier(verifierMatch[1]);
assert.equal(
  normalizeVerifier(
    normalizeLineEndings(verifierMatch[1]).replaceAll('\n', '\r\n')),
  sourceVerifier,
  'The workflow source verifier must be stable in a CRLF checkout.');
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
  const writeStarted = (toolCallId, target, inputOnly = false) => ({
    type: 'tool.execution_start',
    data: {
      toolCallId,
      toolName: 'write_agent',
      ...toolArguments({ ...target, message: 'Clarify the disputed evidence.' }, inputOnly),
    },
  });
  const writeCompleted = (toolCallId, success = true) => ({
    type: 'tool.execution_complete',
    data: { toolCallId, success, result: { content: 'Message delivered.' } },
  });
  const clarification = (toolCallId, target = { agent_id: 'guide-one' }) => [
    writeStarted(toolCallId, target),
    writeCompleted(toolCallId),
  ];
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
  const hostedCandidateVerifier = (
    toolCallId = 'verifier-one',
    name = 'candidate-verifier',
    decision = 'KEEP') => [
    taskStarted(toolCallId, name, { agentType: 'review-candidate-verifier' }),
    workerStarted(toolCallId, toolCallId, 'review-candidate-verifier'),
    taskSpawned(toolCallId, name),
    workerTool(toolCallId, 'view'),
    workerTool(toolCallId, 'grep'),
    {
      type: 'subagent.completed',
      agentId: toolCallId,
      data: { toolCallId, agentName: 'review-candidate-verifier' },
    },
    readStarted(`read-${toolCallId}`, name),
    readCompleted(
      `read-${toolCallId}`,
      `{"candidateId":"C1","decision":"${decision}"}`),
  ];
  const sameIdWorker = (id = 'worker-same-id') => [
    taskStarted(id, id, { inputOnly: true }),
    workerStarted(id),
    workerCompleted(id),
    readStarted(`read-${id}`, id, { inputOnly: true }),
    readCompleted(`read-${id}`),
  ];

  const publicationTransports = [
    {
      name: 'Findings',
      prePublicationEvents: hostedCandidateVerifier(),
      events: [
        ...safeOutputCall('finding', 'safeoutputs-create_pull_request_review_comment', {
          path: 'example.cs', line: 1, body: 'A validated finding.',
        }),
        ...safeOutputCall('review', 'safeoutputs-submit_pull_request_review', {
          event: 'COMMENT', body: 'One validated finding.',
        }),
      ],
    },
    { name: 'Noop', prePublicationEvents: [], events: publication() },
    { name: 'Stopped', prePublicationEvents: [], events: stopped() },
  ];
  for (const { name, prePublicationEvents, events } of publicationTransports) {
    const nativeName = `Native${name}Accepted`;
    const native = runVerifier([...hostedWorker(), ...prePublicationEvents, ...events], '1');
    assert.equal(native.status, 0, `${nativeName}: ${native.stderr}`);
    console.log(`Validated ${nativeName}.`);

    const cliName = `CliOnly${name}Rejected`;
    const cliEvents = events.map(event => event.type === 'tool.execution_start'
      ? {
        ...event,
        data: {
          toolCallId: event.data.toolCallId,
          toolName: 'shell',
          arguments: {
            command: `safeoutputs ${event.data.toolName.slice('safeoutputs-'.length)} '${JSON.stringify(event.data.arguments)}'`,
          },
        },
      }
      : event);
    const cli = runVerifier([...hostedWorker(), ...cliEvents], '1');
    assert.notEqual(cli.status, 0, cliName);
    assert.ok(cli.stderr.includes(nativePublicationDiagnostic), `${cliName}: ${cli.stderr}`);
    assert.doesNotMatch(cli.stderr, /Expected .*worker|was not collected/, cliName);
    console.log(`Validated ${cliName}.`);
  }

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

  const initialReport = hostedWorker('task-one', 'guide-one');
  const secondInitialReport = hostedWorker('task-two', 'guide-two');
  const followUpReport = [
    readStarted('follow-up', 'guide-one', { sinceTurn: 1 }),
    readCompleted('follow-up', 'Clarification evidence without a repeated path marker.'),
  ];
  const missingFollowUp = /write_agent follow-up that was not collected before publication/;
  const unsuccessfulWrite = /write_agent.*did not complete successfully/;
  const invalidWriteTarget = /write_agent.*explicit known worker targets/;
  const rejectedClarifications = [
    {
      name: 'UncollectedClarification',
      events: [...initialReport, ...clarification('write-one'), ...publication()],
      diagnostic: missingFollowUp,
    },
    {
      name: 'StaleInFlightRead',
      events: [
        ...initialReport, followUpReport[0], ...clarification('write-one'),
        followUpReport[1], ...publication(),
      ],
      diagnostic: missingFollowUp,
    },
    {
      name: 'UncollectedSecondClarification',
      events: [
        ...initialReport, ...clarification('write-one'), ...followUpReport,
        ...clarification('write-two'), ...publication(),
      ],
      diagnostic: missingFollowUp,
    },
    {
      name: 'OverlappingWriteCompletesAfterReadStarts',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'guide-one' }),
        ...clarification('write-two'), followUpReport[0], writeCompleted('write-one'),
        followUpReport[1], ...publication(),
      ],
      diagnostic: missingFollowUp,
    },
    {
      name: 'WrongWorkerFollowUp',
      events: [
        ...initialReport, ...secondInitialReport, ...clarification('write-one'),
        readStarted('follow-up', 'guide-two'), readCompleted('follow-up'), ...publication(),
      ],
      workerCount: '2',
      diagnostic: missingFollowUp,
    },
    {
      name: 'OnePluralRecipientUncollected',
      events: [
        ...initialReport, ...secondInitialReport,
        ...clarification('write-one', { agent_ids: ['guide-one', 'task-two'] }),
        ...followUpReport, ...publication(),
      ],
      workerCount: '2',
      diagnostic: /guide-two.*write_agent follow-up that was not collected/,
    },
    {
      name: 'EmptyFollowUpReport',
      events: [
        ...initialReport, ...clarification('write-one'), followUpReport[0],
        readCompleted('follow-up', ' \n\t'), ...publication(),
      ],
      diagnostic: missingFollowUp,
    },
    {
      name: 'MissingFollowUpContent',
      events: [
        ...initialReport, ...clarification('write-one'), followUpReport[0],
        { type: 'tool.execution_complete', data: { toolCallId: 'follow-up', success: true } },
        ...publication(),
      ],
      diagnostic: missingFollowUp,
    },
    {
      name: 'NonWaitingFollowUp',
      events: [
        ...initialReport, ...clarification('write-one'),
        readStarted('follow-up', 'guide-one', { wait: false }), followUpReport[1],
        ...publication(),
      ],
      diagnostic: /wait: true/,
    },
    {
      name: 'InvalidFollowUpCursor',
      events: [
        ...initialReport, ...clarification('write-one'),
        readStarted('follow-up', 'guide-one', { sinceTurn: -1 }), followUpReport[1],
        ...publication(),
      ],
      diagnostic: /invalid since_turn/,
    },
    {
      name: 'FailedFollowUpRead',
      events: [
        ...initialReport, ...clarification('write-one'), followUpReport[0],
        readCompleted('follow-up', 'Report unavailable.', false), ...publication(),
      ],
      diagnostic: /read_agent.*did not complete successfully/,
    },
    {
      name: 'UncompletedFollowUpRead',
      events: [
        ...initialReport, ...clarification('write-one'), followUpReport[0], ...publication(),
      ],
      diagnostic: /read_agent.*did not complete successfully/,
    },
    {
      name: 'FollowUpReadAfterPublication',
      events: [
        ...initialReport, ...clarification('write-one'), ...publication(), ...followUpReport,
      ],
      diagnostic: /read_agent.*did not complete before publication/,
    },
    {
      name: 'FailedClarificationSend',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'guide-one' }),
        writeCompleted('write-one', false), ...followUpReport, ...publication(),
      ],
      diagnostic: unsuccessfulWrite,
    },
    {
      name: 'UncompletedClarificationSend',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'guide-one' }),
        ...followUpReport, ...publication(),
      ],
      diagnostic: unsuccessfulWrite,
    },
    {
      name: 'DuplicateClarificationCompletion',
      events: [
        ...initialReport, ...clarification('write-one'), writeCompleted('write-one'),
        ...followUpReport, ...publication(),
      ],
      diagnostic: unsuccessfulWrite,
    },
    {
      name: 'ClarificationCompletionBeforeStart',
      events: [
        ...initialReport, writeCompleted('write-one'),
        writeStarted('write-one', { agent_id: 'guide-one' }),
        ...followUpReport, ...publication(),
      ],
      diagnostic: unsuccessfulWrite,
    },
    {
      name: 'ClarificationBeforeWorkerStart',
      events: [
        writeStarted('write-one', { agent_id: 'guide-one' }), ...initialReport,
        writeCompleted('write-one'), ...followUpReport, ...publication(),
      ],
      diagnostic: /write_agent.*before the worker started/,
    },
    {
      name: 'ClarificationAfterPublication',
      events: [
        ...initialReport, ...publication(), ...clarification('write-one'), ...followUpReport,
      ],
      diagnostic: /write_agent.*did not complete before publication/,
    },
    ...[
      ['UnknownClarificationTarget', { agent_id: 'unknown' }],
      ['UnknownPluralClarificationTarget', { agent_ids: ['guide-one', 'unknown'] }],
      ['EmptyPluralClarificationTarget', { agent_ids: [] }],
      ['MissingClarificationTarget', {}],
      ['ConflictingClarificationTargets', { agent_id: 'guide-one', agent_ids: ['guide-one'] }],
      ['ScopedChildrenClarification', { scope: 'children' }],
      ['ScopedSiblingsClarification', { scope: 'siblings' }],
      ['ConflictingScopeClarification', { agent_id: 'guide-one', scope: 'children' }],
    ].map(([name, target]) => ({
      name,
      events: [
        ...initialReport, ...clarification('write-one', target), ...followUpReport,
        ...publication(),
      ],
      diagnostic: invalidWriteTarget,
    })),
  ];
  for (const { name, events, diagnostic, workerCount = '1' } of rejectedClarifications) {
    const result = runVerifier(events, workerCount);
    assert.notEqual(result.status, 0, name);
    assert.match(result.stderr, diagnostic, name);
  }

  const acceptedClarifications = [
    ...[undefined, 0, 1].map(sinceTurn => ({
      name: sinceTurn === undefined ? 'FullFollowUpRead' : `FollowUpReadFromTurn${sinceTurn}`,
      events: [
        ...initialReport, ...clarification('write-one'),
        readStarted('follow-up', 'guide-one', { sinceTurn }), followUpReport[1],
        ...publication(),
      ],
    })),
    {
      name: 'CoalescedClarifications',
      events: [
        ...initialReport, ...clarification('write-one'), ...clarification('write-two'),
        ...followUpReport, ...publication(),
      ],
    },
    {
      name: 'SequentialCollectedClarifications',
      events: [
        ...initialReport, ...clarification('write-one'), ...followUpReport,
        ...clarification('write-two'), readStarted('second-follow-up', 'task-one'),
        readCompleted('second-follow-up'), ...publication(),
      ],
    },
    {
      name: 'PluralRecipientsCollected',
      events: [
        ...initialReport, ...secondInitialReport,
        ...clarification('write-one', { agent_ids: ['guide-one', 'task-two'] }),
        ...followUpReport, readStarted('second-follow-up', 'guide-two'),
        readCompleted('second-follow-up', 'Second worker clarification.'), ...publication(),
      ],
      workerCount: '2',
    },
    {
      name: 'WorkerIdAndInputOnlyClarification',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'task-one' }, true),
        writeCompleted('write-one'), ...followUpReport, ...publication(),
      ],
    },
    {
      name: 'StoppedAfterUnreturnedClarification',
      events: [...initialReport, ...clarification('write-one'), ...stopped('INCOMPLETE')],
    },
    {
      name: 'StoppedAfterFailedClarification',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'guide-one' }),
        writeCompleted('write-one', false), ...stopped('INCOMPLETE'),
      ],
    },
    {
      name: 'StoppedAfterUncompletedClarification',
      events: [
        ...initialReport, writeStarted('write-one', { agent_id: 'guide-one' }),
        ...stopped('INCOMPLETE'),
      ],
    },
  ];
  for (const { name, events, workerCount = '1' } of acceptedClarifications) {
    const result = runVerifier(events, workerCount);
    assert.equal(result.status, 0, `${name}: ${result.stderr}`);
  }
  console.log(
    `Validated ${rejectedClarifications.length + acceptedClarifications.length} clarification collection cases.`);

  const acceptedVerifierRun = runVerifier([
    ...initialReport,
    ...hostedCandidateVerifier(),
    ...publication(),
  ], '1');
  assert.equal(acceptedVerifierRun.status, 0, acceptedVerifierRun.stderr);

  const findingsWithoutVerifierRejected = runVerifier([
    ...initialReport,
    ...safeOutputCall('finding', 'safeoutputs-create_pull_request_review_comment', {
      path: 'example.cs', line: 1, body: 'A validated finding.\n\nconfidence: verified',
    }),
    ...safeOutputCall('review', 'safeoutputs-submit_pull_request_review', {
      event: 'COMMENT', body: 'One validated finding.',
    }),
  ], '1');
  assert.notEqual(findingsWithoutVerifierRejected.status, 0);
  assert.match(
    findingsWithoutVerifierRejected.stderr,
    /Findings publication requires 1 'review-candidate-verifier' subagent/);

  const mixedFindingsWithoutVerifierRejected = runVerifier([
    ...initialReport,
    ...safeOutputCall('finding-discarded', 'safeoutputs-create_pull_request_review_comment', {
      path: 'example.cs',
      line: 1,
      body: '- [DISCARDED] Pre-existing behavior.\n  - confidence: discarded\n  - Why discarded: Unchanged in merge-base.',
    }),
    ...safeOutputCall('finding-verified', 'safeoutputs-create_pull_request_review_comment', {
      path: 'example.cs',
      line: 2,
      body: '- Verified regression.\n  - confidence: verified',
    }),
    ...safeOutputCall('review', 'safeoutputs-submit_pull_request_review', {
      event: 'COMMENT', body: 'Two findings.',
    }),
  ], '1');
  assert.notEqual(mixedFindingsWithoutVerifierRejected.status, 0);
  assert.match(
    mixedFindingsWithoutVerifierRejected.stderr,
    /Findings publication requires 1 'review-candidate-verifier' subagent/);

  const reviewOnlyWithoutVerifierRejected = runVerifier([
    ...initialReport,
    ...safeOutputCall('review', 'safeoutputs-submit_pull_request_review', {
      event: 'COMMENT', body: 'Review body without inline comments.',
    }),
  ], '1');
  assert.notEqual(reviewOnlyWithoutVerifierRejected.status, 0);
  assert.match(
    reviewOnlyWithoutVerifierRejected.stderr,
    /Findings publication requires 1 'review-candidate-verifier' subagent/);

  const discardedOnlyFindingsWithoutVerifierAccepted = runVerifier([
    ...initialReport,
    ...safeOutputCall('finding-discarded', 'safeoutputs-create_pull_request_review_comment', {
      path: 'example.cs',
      line: 1,
      body: '- [DISCARDED] Pre-existing behavior.\n  - confidence: discarded\n  - Why discarded: Unchanged in merge-base.',
    }),
    ...safeOutputCall('review', 'safeoutputs-submit_pull_request_review', {
      event: 'COMMENT', body: 'One pre-verification discarded candidate.',
    }),
  ], '1');
  assert.equal(
    discardedOnlyFindingsWithoutVerifierAccepted.status,
    0,
    discardedOnlyFindingsWithoutVerifierAccepted.stderr);

  const verifierDisallowedSkill = runVerifier([
    ...initialReport,
    taskStarted('verifier-skill', 'candidate-verifier', { agentType: 'review-candidate-verifier' }),
    workerStarted('verifier-skill', 'verifier-skill', 'review-candidate-verifier'),
    workerTool('verifier-skill', 'skill'),
    readStarted('read-verifier-skill', 'candidate-verifier'),
    readCompleted('read-verifier-skill', '{"candidateId":"C1","decision":"KEEP"}'),
    ...publication(),
  ], '1');
  assert.notEqual(verifierDisallowedSkill.status, 0);
  assert.match(verifierDisallowedSkill.stderr, /review-candidate-verifier.*disallowed tool 'skill'/);

  const excessVerifier = runVerifier([
    ...initialReport,
    ...hostedCandidateVerifier('verifier-one', 'candidate-verifier-1'),
    ...hostedCandidateVerifier('verifier-two', 'candidate-verifier-2'),
    ...publication(),
  ], '1');
  assert.notEqual(excessVerifier.status, 0);
  assert.match(excessVerifier.stderr, /Expected at most 1 'review-candidate-verifier'/);

  const verifierBeforeWorkerCollection = runVerifier([
    taskStarted('task-one', 'guide-one'),
    workerStarted('task-one'),
    ...hostedCandidateVerifier('verifier-early', 'candidate-verifier'),
    readStarted('read-one', 'guide-one'),
    readCompleted('read-one'),
    ...publication(),
  ], '1');
  assert.notEqual(verifierBeforeWorkerCollection.status, 0);
  assert.match(
    verifierBeforeWorkerCollection.stderr,
    /started before every review worker was collected/);

  const uncollectedVerifier = runVerifier([
    ...initialReport,
    taskStarted('verifier-uncollected', 'candidate-verifier', { agentType: 'review-candidate-verifier' }),
    workerStarted('verifier-uncollected', 'verifier-uncollected', 'review-candidate-verifier'),
    ...publication(),
  ], '1');
  assert.notEqual(uncollectedVerifier.status, 0);
  assert.match(uncollectedVerifier.stderr, /Candidate verifier 'candidate-verifier' was not collected/);

  const writeToVerifier = runVerifier([
    ...initialReport,
    ...hostedCandidateVerifier('verifier-write', 'candidate-verifier'),
    ...clarification('write-verifier', { agent_id: 'candidate-verifier' }),
    ...publication(),
  ], '1');
  assert.notEqual(writeToVerifier.status, 0);
  assert.match(writeToVerifier.stderr, /write_agent must not target 'review-candidate-verifier'/);

  const stoppedWriteToVerifier = runVerifier([
    ...initialReport,
    ...hostedCandidateVerifier('verifier-stopped-write', 'candidate-verifier'),
    ...clarification('write-verifier', { agent_id: 'candidate-verifier' }),
    ...stopped('INCOMPLETE'),
  ], '1');
  assert.notEqual(stoppedWriteToVerifier.status, 0);
  assert.match(stoppedWriteToVerifier.stderr, /write_agent must not target 'review-candidate-verifier'/);

  const verifierBeforeClarificationCollection = runVerifier([
    ...initialReport,
    ...clarification('write-one'),
    ...hostedCandidateVerifier('verifier-before-followup', 'candidate-verifier'),
    ...followUpReport,
    ...publication(),
  ], '1');
  assert.notEqual(verifierBeforeClarificationCollection.status, 0);
  assert.match(
    verifierBeforeClarificationCollection.stderr,
    /started before every review worker was collected/);

  const verifierTaskWithoutStart = runVerifier([
    ...initialReport,
    taskStarted('verifier-orphan', 'candidate-verifier', { agentType: 'review-candidate-verifier' }),
    ...publication(),
  ], '1');
  assert.notEqual(verifierTaskWithoutStart.status, 0);
  assert.match(
    verifierTaskWithoutStart.stderr,
    /Expected 1 started 'review-candidate-verifier' subagent\(s\), but recorded 0/);

  const verifierWithoutDecision = runVerifier([
    ...initialReport,
    taskStarted('verifier-empty', 'candidate-verifier', { agentType: 'review-candidate-verifier' }),
    workerStarted('verifier-empty', 'verifier-empty', 'review-candidate-verifier'),
    readStarted('read-verifier-empty', 'candidate-verifier'),
    readCompleted('read-verifier-empty', 'The verifier mentions KEEP in prose without decision field.'),
    ...publication(),
  ], '1');
  assert.notEqual(verifierWithoutDecision.status, 0);
  assert.match(
    verifierWithoutDecision.stderr,
    /Candidate verifier 'candidate-verifier' was not collected/);

  const runtimeIdleReadWorker = runVerifier([
    taskStarted('call_task1', 'guide-one'),
    workerStarted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    workerCompleted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: '192396e8-4f94-4f67-b926-2c8de862d775',
        toolName: 'read_agent',
        arguments: { agent_id: '23871449-d358-43e9-9d8f-6ce940dbb4ad', since_turn: 0 },
      },
    },
    readCompleted(
      '192396e8-4f94-4f67-b926-2c8de862d775',
      'Agent is idle.\n\nPATH: per-guide-worker\nGUIDE: docs/CrossCuttingGuidance.md\nRESULT: complete'),
    ...publication(),
  ], '1');
  assert.equal(runtimeIdleReadWorker.status, 0, runtimeIdleReadWorker.stderr);

  const runtimeIdleReadBeforeCompletedRejected = runVerifier([
    taskStarted('call_task1', 'guide-one'),
    workerStarted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: '192396e8-4f94-4f67-b926-2c8de862d775',
        toolName: 'read_agent',
        arguments: { agent_id: '23871449-d358-43e9-9d8f-6ce940dbb4ad', since_turn: 0 },
      },
    },
    readCompleted(
      '192396e8-4f94-4f67-b926-2c8de862d775',
      'Agent is idle.\n\nPATH: per-guide-worker\nRESULT: complete'),
    ...publication(),
    workerCompleted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
  ], '1');
  assert.notEqual(runtimeIdleReadBeforeCompletedRejected.status, 0);
  assert.match(runtimeIdleReadBeforeCompletedRejected.stderr, /wait: true/);

  const runtimeIdleReadAfterClarificationBeforeSecondCompletedRejected = runVerifier([
    ...initialReport,
    ...clarification('write-one'),
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: '192396e8-4f94-4f67-b926-2c8de862d775',
        toolName: 'read_agent',
        arguments: { agent_id: 'guide-one', since_turn: 0 },
      },
    },
    readCompleted(
      '192396e8-4f94-4f67-b926-2c8de862d775',
      'Follow-up reply before second subagent.completed.'),
    ...publication(),
  ], '1');
  assert.notEqual(runtimeIdleReadAfterClarificationBeforeSecondCompletedRejected.status, 0);
  assert.match(runtimeIdleReadAfterClarificationBeforeSecondCompletedRejected.stderr, /wait: true/);

  const runtimeIdleReadWithoutSinceTurnZeroRejected = runVerifier([
    taskStarted('call_task1', 'guide-one'),
    workerStarted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    workerCompleted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: '192396e8-4f94-4f67-b926-2c8de862d775',
        toolName: 'read_agent',
        arguments: { agent_id: '23871449-d358-43e9-9d8f-6ce940dbb4ad' },
      },
    },
    readCompleted(
      '192396e8-4f94-4f67-b926-2c8de862d775',
      'Agent is idle.\n\nPATH: per-guide-worker\nRESULT: complete'),
    ...publication(),
  ], '1');
  assert.notEqual(runtimeIdleReadWithoutSinceTurnZeroRejected.status, 0);
  assert.match(runtimeIdleReadWithoutSinceTurnZeroRejected.stderr, /wait: true/);

  const nonUuidMissingWaitRejected = runVerifier([
    taskStarted('call_task1', 'guide-one'),
    workerStarted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    workerCompleted('call_task1', '23871449-d358-43e9-9d8f-6ce940dbb4ad'),
    {
      type: 'tool.execution_start',
      data: {
        toolCallId: 'call_model_omitted_wait',
        toolName: 'read_agent',
        arguments: { agent_id: '23871449-d358-43e9-9d8f-6ce940dbb4ad', since_turn: 0 },
      },
    },
    readCompleted('call_model_omitted_wait'),
    ...publication(),
  ], '1');
  assert.notEqual(nonUuidMissingWaitRejected.status, 0);
  assert.match(nonUuidMissingWaitRejected.stderr, /wait: true/);
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}

console.log('Validated source-only hosted pull request review capabilities.');
