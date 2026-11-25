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
const retargetDiagnostic =
  'The PR head moved, the base branch changed, or the PR closed after review; safe outputs are blocked.';

assert.match(workflowSource, /bash:\s*false/);
assert.match(workflowSource, /edit:\s*false/);
assert.doesNotMatch(workflowSource, /memory:\s*8g/);
assert.match(workflowSource, /  roles: \[admin, maintainer, write\]/);
assert.doesNotMatch(workflowSource, /user-rate-limit/);
assert.match(workflowSource, /network:\r?\n  allowed:\r?\n    - defaults\r?\n    - github\r?\n    - node/);
assert.doesNotMatch(workflowSource, /builds\.dotnet\.microsoft\.com|pkgs\.dev\.azure\.com|playwright/);
assert.doesNotMatch(workflowSource, /DOTNET_CLI_TELEMETRY_OPTOUT/);
assert.match(workflowSource, /This adapter supports publication only through the native safe-output functions/);
assert.match(workflowSource, /Do not publish through the `safeoutputs` CLI or any shell command/);
assert.match(workflowSource, /If a required native safe-output function is unavailable, do not fall back to CLI or shell publication/);
assert.match(workflowSource, /id: prepare_review_bundle/);
assert.doesNotMatch(workflowSource, /\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowSource, /chmod -R a\+rX,a-w \/tmp\/gh-aw\/review-bundle/);
assert.match(workflowSource, /Perform source-only analysis of/);
assert.match(workflowSource, /Do not execute target code\./);
assert.doesNotMatch(workflowSource, /review_execution|review-execution|execution\.json/);
assert.equal(workflowSource.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowSource, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.doesNotMatch(workflowLock, /--allow-tool shell/);
assert.doesNotMatch(workflowLock, /--allow-tool write/);
assert.doesNotMatch(workflowLock, /--memory-limit 8g/);
assert.match(workflowLock, /GH_AW_TOOL_TIMEOUT: "?120"?/);
assert.doesNotMatch(workflowLock, /check_rate_limit|GH_AW_RATE_LIMIT|rate_limit_ok/);
assert.doesNotMatch(workflowLock, /builds\.dotnet\.microsoft\.com|pkgs\.dev\.azure\.com/);
assert.doesNotMatch(workflowLock, /\/tmp\/gh-aw\/review-worktree/);
assert.doesNotMatch(workflowLock, /review_execution|review-execution|review-build-sandbox/);
assert.equal(workflowLock.split(retargetDiagnostic).length - 1, 2);
assert.doesNotMatch(workflowLock, /The PR head moved or closed after review; safe outputs are blocked\./);

assert.match(workflowTests, /- '\.github\/agents\/\*\*'/);

const extractTranscriptCheck = source => source.match(
  /name: Verify single review transcript[\s\S]*?node - <<'NODE'\r?\n([\s\S]*?)\r?\n      NODE/)?.[1]
  .replace(/\r\n/g, '\n').replace(/^ {6}/gm, '');
const sourceCheck = extractTranscriptCheck(workflowSource);
assert.ok(sourceCheck, 'The single-review transcript check must be extractable.');
assert.equal(extractTranscriptCheck(workflowSource.replace(/\r?\n/g, '\r\n')), sourceCheck);
assert.match(workflowSource, /skills:\r?\n  - \.github\/skills\/code-review/);
assert.match(workflowSource, /skill\(skill="code-review"\)/);
assert.doesNotMatch(workflowSource, /## agent:|expected_worker_count|review-guide-worker|review-candidate-verifier/);

const start = (toolName, toolCallId, args = {}) => ({
  type: 'tool.execution_start',
  data: { toolName, toolCallId, arguments: args },
});
const skill = start('skill', '1', { skill: 'code-review' });
const loaded = { type: 'tool.execution_complete', data: { toolCallId: '1', success: true } };
const view = start('view', '2');
const glob = start('glob', '3');
const rg = start('rg', '4');
const noop = start('safeoutputs-noop', '5');
const comment = start('safeoutputs-create_pull_request_review_comment', '6');
const review = start('safeoutputs-submit_pull_request_review', '7');
const status = start('safeoutputs-add_comment', '8');
const incomplete = start('safeoutputs-report_incomplete', '9');
const cases = [
  ['source-only findings', [skill, loaded, view, glob, rg, comment, review], null],
  ['clean result', [skill, loaded, view, noop], null],
  ['stopped result', [skill, loaded, status, incomplete], null],
  ['subagent start', [skill, loaded, { type: 'subagent.started' }, noop], /must not start or use subagents/],
  ['nested read', [skill, loaded, { ...view, data: { ...view.data, parentToolCallId: 'child' } }, noop],
    /must not start or use subagents/],
  ['child completion', [skill, loaded, { type: 'tool.execution_complete', agentId: 'child' }, noop],
    /must not start or use subagents/],
  ['no skill', [view, noop], /must successfully invoke the code-review skill/],
  ['different skill', [start('skill', '1', { skill: 'different' }), loaded, noop],
    /must successfully invoke the code-review skill/],
  ['skill after read', [view, skill, loaded, noop], /must successfully invoke the code-review skill/],
  ['read before skill completion', [skill, view, loaded, noop], /must successfully invoke the code-review skill/],
  ['skill completion before start', [loaded, skill, noop], /must successfully invoke the code-review skill/],
  ['repeated skill', [skill, loaded, start('skill', '10', { skill: 'code-review' }), noop],
    /must successfully invoke the code-review skill/],
  ['failed skill', [skill, { ...loaded, data: { ...loaded.data, success: false } }, noop],
    /must successfully invoke the code-review skill/],
  ['duplicated skill completion', [skill, loaded, loaded, noop], /must successfully invoke the code-review skill/],
  ['no native publication', [skill, loaded, view], /No native safe-output publication call/],
  ['only missing-tool', [skill, loaded, start('safeoutputs-missing_tool', '10')],
    /No native safe-output publication call/],
  ...['bash', 'shell', 'powershell', 'create', 'edit', 'write', 'task', 'read_agent',
    'web_fetch', 'safeoutputs-add_labels'].map(tool => [
    `${tool} denied`, [skill, loaded, start(tool, '10'), noop], /Unexpected review tool/,
  ]),
];

const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'single-review-transcript-'));
try {
  const script = path.join(temp, 'verify.cjs');
  const session = path.join(temp, 'events.jsonl');
  fs.writeFileSync(script, sourceCheck);
  const execute = () => spawnSync(process.execPath, [script], {
    encoding: 'utf8',
    env: { ...process.env, REVIEW_WORKER_SESSION_PATH: session },
  });
  assert.notEqual(execute().status, 0, 'Missing transcript must fail closed.');
  fs.writeFileSync(session, '{malformed}\n');
  assert.notEqual(execute().status, 0, 'Malformed transcript must fail closed.');
  for (const [name, events, error] of cases) {
    fs.writeFileSync(session, events.map(event => JSON.stringify(event)).join('\r\n') + '\r\n');
    const result = execute();
    assert.equal(result.error, undefined, name);
    if (error) {
      assert.notEqual(result.status, 0, name);
      assert.match(result.stderr, error, name);
    } else {
      assert.equal(result.status, 0, `${name}: ${result.stderr}`);
    }
  }
} finally {
  fs.rmSync(temp, { recursive: true, force: true });
}

console.log(`Validated ${cases.length + 2} cases against the source transcript check.`);
const compiledStep = workflowLock.match(
  /name: Verify single review transcript\r?\n\s+run: ("(?:[^"\\]|\\.)*")\r?\n/)?.[1];
assert.ok(compiledStep, 'The compiled lock is stale: it has no single-review transcript step.');
const compiledCheck = JSON.parse(compiledStep).match(/node - <<'NODE'\n([\s\S]*?)\nNODE$/)?.[1];
assert.equal(compiledCheck, sourceCheck, 'The compiled single-review transcript check must match the source.');
assert.doesNotMatch(workflowLock, /review-guide-worker|review-candidate-verifier/);
