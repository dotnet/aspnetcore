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
assert.equal(
  normalizeProfile(localWorkerProfile),
  normalizeProfile(inlineWorkerProfile[1]).replace(
    'tools: ["read", "search", "skill"]',
    'tools: ["read", "search", "skill"]\nstrict-tools-list: true'),
  'The local review-guide-worker profile may differ only by its strict local tool-list enforcement.');

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
assert.match(workflowSource, /id: prepare_review_bundle/);
assert.match(workflowSource, /printf 'expected_worker_count=%s\\n' "\$expected_worker_count" >> "\$GITHUB_OUTPUT"/);
assert.match(workflowSource, /Ready review manifest has no routed guides/);
assert.match(workflowSource, /if \(subagents\.size !== expectedWorkerCount\)/);
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

const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'review-worker-verifier-'));
try {
  const verifierPath = path.join(tempRoot, 'verify.cjs');
  fs.writeFileSync(verifierPath, verifierMatch[1]);

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

  const workerStarted = {
    type: 'subagent.started',
    agentId: 'worker-1',
    data: { agentName: 'review-guide-worker' },
  };
  const workerTool = toolName => ({
    type: 'tool.execution_start',
    agentId: 'worker-1',
    data: { toolName },
  });
  const secondWorkerStarted = {
    ...workerStarted,
    agentId: 'worker-2',
  };
  assert.equal(runVerifier([workerStarted, workerTool('skill'), workerTool('view')], '1').status, 0);
  assert.equal(runVerifier([], '0').status, 0);
  assert.equal(runVerifier([workerStarted, secondWorkerStarted], '2').status, 0);

  const invalidWorkerRequirement = runVerifier([], '');
  assert.notEqual(invalidWorkerRequirement.status, 0);
  assert.match(invalidWorkerRequirement.stderr, /Trusted worker count is invalid/);

  const missingWorker = runVerifier([], '1');
  assert.notEqual(missingWorker.status, 0);
  assert.match(missingWorker.stderr, /Expected 1 per-guide review worker\(s\), but recorded 0/);

  const missingSecondWorker = runVerifier([workerStarted], '2');
  assert.notEqual(missingSecondWorker.status, 0);
  assert.match(missingSecondWorker.stderr, /Expected 2 per-guide review worker\(s\), but recorded 1/);

  const wrongWorker = runVerifier([
    { ...workerStarted, data: { agentName: 'general-purpose' } },
  ], '1');
  assert.notEqual(wrongWorker.status, 0);
  assert.match(wrongWorker.stderr, /Unexpected subagent 'general-purpose'/);

  const disallowedTool = runVerifier([workerStarted, workerTool('bash')], '1');
  assert.notEqual(disallowedTool.status, 0);
  assert.match(disallowedTool.stderr, /used disallowed tool 'bash'/);

  const unattributedWorkerTool = runVerifier([
    workerStarted,
    {
      type: 'tool.execution_start',
      data: { toolName: 'bash', parentToolCallId: 'worker-1' },
    },
  ], '1');
  assert.notEqual(unattributedWorkerTool.status, 0);
  assert.match(unattributedWorkerTool.stderr, /is missing an agent ID/);
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}

console.log('Validated adaptive in-sandbox pull request review capabilities.');
