// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { join } from 'node:path';
import { test } from 'node:test';
import { command, pin, scratch } from './pin.mjs';
import { extractTranscript, outcomes, reconcilePrompt } from './outcomes.mjs';

test('PinPreservesExistingRefsAndReportsUnreachableCommits', () => scratch(dir =>
{
  const repo = join(dir, 'source');
  const mirror = join(dir, 'mirror.git');
  const git = (...args) =>
  {
    const result = command('git', ['--no-pager', ...args]);
    assert.equal(result.status, 0, result.stderr);
    return result.stdout.trim();
  };
  git('init', '--object-format=sha1', repo);
  const commit = message =>
  {
    git('-C', repo, '-c', 'user.name=L3', '-c', 'user.email=l3@example.invalid', '-c', 'commit.gpgsign=false',
      'commit', '--allow-empty', '-m', message);
    return git('-C', repo, 'rev-parse', 'HEAD');
  };
  const head = commit('first');
  const differentHead = commit('second');
  const unreachable = 'a'.repeat(40);
  const args = { repo, mirror, run: '1-1' };
  const first = pin({ ...args, commits: { head, guidance: unreachable } });
  assert.deepEqual(first.entries.map(entry => entry.status), ['pinned', 'unreachable']);
  assert.equal(first.offlineRestoreVerified, false);
  assert.throws(() => pin({ ...args, commits: { head: differentHead } }), /Refusing to overwrite/);
  assert.equal(git(`--git-dir=${mirror}`, 'rev-parse', 'refs/l3/1-1/head'), head);
  const retry = pin({ ...args, commits: { head } });
  assert.equal(retry.entries[0].status, 'already-pinned');
  assert.equal(retry.offlineRestoreVerified, true);
}));

test('PinPreservesForeignGitConfig', () => scratch(dir =>
{
  const repo = join(dir, 'source');
  const mirror = join(dir, 'mirror.git');
  const foreign = join(dir, 'foreign.git');
  const config = join(foreign, 'config');
  const git = (...args) =>
  {
    const result = command('git', ['--no-pager', ...args]);
    assert.equal(result.status, 0, result.stderr);
    return result.stdout.trim();
  };
  git('init', '--object-format=sha1', repo);
  git('-C', repo, '-c', 'user.name=L3', '-c', 'user.email=l3@example.invalid', '-c', 'commit.gpgsign=false',
    'commit', '--allow-empty', '-m', 'first');
  const head = git('-C', repo, 'rev-parse', 'HEAD');
  git('init', '--bare', foreign);
  git('config', '--file', config, 'remote.origin.url', repo);

  const previous = process.env.GIT_CONFIG;
  try
  {
    process.env.GIT_CONFIG = config;
    const result = pin({ repo, mirror, run: '1-1', commits: { head } });
    assert.equal(result.offlineRestoreVerified, true);
    assert.equal(result.entries[0].status, 'pinned');
    const remote = command('git', ['--no-pager', 'config', '--file', config, '--get', 'remote.origin.url']);
    assert.equal(remote.stdout.trim(), repo, 'Pin must preserve the foreign remote.origin URL.');
  }
  finally
  {
    if (previous === undefined) delete process.env.GIT_CONFIG;
    else process.env.GIT_CONFIG = previous;
  }
}));

test('VerifierDiscardsRequireAttributableStructuredEvidenceAndDoNotStoreText', () =>
{
  const start = (toolCallId, args) =>
    ({ type: 'tool.execution_start', data: { toolCallId, arguments: args } });
  const complete = (toolCallId, content) =>
    ({ type: 'tool.execution_complete', data: { toolCallId, success: true, result: { content } } });
  const records = [
    start('task1', { agent_type: 'review-guide-worker', name: 'worker' }),
    { type: 'subagent.started', data: { toolCallId: 'task1', agentName: 'review-guide-worker' } },
    start('read1', { agent_id: 'worker' }),
    complete('read1', 'wrapper\n[Turn 0]\nPATH: per-guide-worker\nCANDIDATES\nNone.\nDISCARDED\n- Worker-only discard.'),
    start('task2', { agent_type: 'review-candidate-verifier', name: 'verifier' }),
    { type: 'subagent.started', data: { toolCallId: 'task2', agentName: 'review-candidate-verifier' } },
    start('read2', { agent_id: 'verifier' }),
  ];
  const extract = events => extractTranscript(events.map(event => JSON.stringify(event)).join('\n'), 'agent-session.jsonl');
  assert.equal(extract(records.slice(0, 4)).verifierStatus, 'absent-in-evidence');
  assert.equal(extract(records).verifierStatus, 'unknown');
  const result = extract([...records, complete('read2', 'wrapper\n[Turn 0]\n' +
    JSON.stringify({ candidateId: 'C1', decision: 'DROP', reason: 'Reason text must not be copied.' }))]);
  assert.equal(result.verifierStatus, 'recorded');
  assert.equal(result.verifierDispositions[0].disposition, 'discarded');
  assert.equal(result.verifierDispositions[0].reason.payloadJsonPointer, '/reason');
  assert.equal(result.verifierDispositions[0].reason.sha256Prefix,
    createHash('sha256').update(JSON.stringify('Reason text must not be copied.')).digest('hex').slice(0, 16));
  assert.doesNotMatch(JSON.stringify(result), /Reason text|Worker-only discard/);
});

test('WorkerEvidenceUsesRuntimeIdAndAnUncollectedWorkerIsUnknown', () =>
{
  const events = [
    { type: 'tool.execution_start', data: { toolCallId: 'task1', arguments: { agent_type: 'review-guide-worker', name: 'worker' } } },
    { type: 'subagent.started', data: { toolCallId: 'task1', agentName: 'review-guide-worker', agentId: 'agent-1' } },
    { type: 'tool.execution_start', data: { toolCallId: 'read1', arguments: { agent_id: 'agent-1' } } },
  ];
  const extract = () => extractTranscript(events.map(event => JSON.stringify(event)).join('\n'), 'agent-session.jsonl');
  assert.equal(extract().workerStatus, 'unknown');
  events.push({ type: 'tool.execution_complete', data: { toolCallId: 'read1', success: true,
    result: { content: 'wrapper\n[Turn 0]\nPATH: per-guide-worker\nCANDIDATES\n- One candidate.' } } });
  assert.equal(extract().workerStatus, 'recorded');
});

for (const shape of ['input', 'topLevelAgentId', 'topLevelResult'])
{
  test(`TranscriptAcceptsValidatorShape_${shape}`, () =>
  {
    const args = (toolCallId, value) =>
      ({ toolCallId, ...(shape === 'input' ? { input: value } : { arguments: value }) });
    const started = { type: 'subagent.started', data: { toolCallId: 'task1', agentName: 'review-candidate-verifier' } };
    if (shape === 'topLevelAgentId') started.agentId = 'agent-1';
    else started.data.agentId = 'agent-1';
    const content = 'wrapper\n[Turn 0]\n' + JSON.stringify({ candidateId: 'C1', decision: 'KEEP' });
    const complete = shape === 'topLevelResult'
      ? { type: 'tool.execution_complete', data: { toolCallId: 'read1', success: true }, result: { content } }
      : { type: 'tool.execution_complete', data: { toolCallId: 'read1', success: true, result: { content } } };
    const events = [
      { type: 'tool.execution_start', data: args('task1', { agent_type: 'review-candidate-verifier', name: 'verifier' }) },
      started,
      { type: 'tool.execution_start', data: args('read1', { agent_id: 'agent-1', wait: true }) },
      complete,
    ];
    const result = extractTranscript(events.map(event => JSON.stringify(event)).join('\n'), 'agent-session.jsonl');
    assert.equal(result.verifierStatus, 'recorded');
    assert.equal(result.verifierDispositions[0].disposition, 'kept');
    assert.equal(result.verifierDispositions[0].evidence.jsonPointer,
      shape === 'topLevelResult' ? '/result/content' : '/data/result/content');
  });
}

for (const agentType of ['review-candidate-verifier', 'review-guide-worker'])
{
  test(`HeaderlessReadAgentResultIsOneTurn_${agentType}`, () =>
  {
    const content = agentType === 'review-guide-worker'
      ? 'PATH: per-guide-worker\nCANDIDATES\n- One candidate.'
      : JSON.stringify({ candidateId: 'C1', decision: 'KEEP' });
    const events = [
      { type: 'tool.execution_start', data: { toolCallId: 'task1', arguments: { agent_type: agentType, name: 'agent' } } },
      { type: 'subagent.started', data: { toolCallId: 'task1', agentName: agentType } },
      { type: 'tool.execution_start', data: { toolCallId: 'read1', arguments: { agent_id: 'agent' } } },
      { type: 'tool.execution_complete', data: { toolCallId: 'read1', success: true, result: { content } } },
    ];
    const result = extractTranscript(events.map(event => JSON.stringify(event)).join('\n'), 'agent-session.jsonl');
    if (agentType === 'review-guide-worker')
    {
      assert.equal(result.workerStatus, 'recorded');
      assert.equal(result.workerCandidates[0].turn, null);
    }
    else
    {
      assert.equal(result.verifierStatus, 'recorded');
      assert.equal(result.verifierDispositions[0].evidence.turn, null);
    }
  });
}

test('WorkflowHeadSelectorHashesTheSerializedJsonValue', () =>
{
  const repo = 'dotnet/aspnetcore';
  const api = `repos/${repo}/actions/runs/5`;
  const sha = 'c'.repeat(40);
  const request = path =>
  {
    if (path === `${api}/attempts/2`)
      return { status: 0, stdout: JSON.stringify({ id: 5, run_attempt: 2, status: 'completed', event: 'issue_comment', head_sha: sha,
        path: '.github/workflows/pull-request-review.lock.yml@refs/heads/main' }) };
    if (path.startsWith(`${api}/attempts/2/jobs`))
      return { status: 0, stdout: JSON.stringify([{ jobs: [] }]) };
    throw new Error(`Unexpected request: ${path}`);
  };
  const { workflow } = outcomes({ repo, run: '5', attempt: '2' }, request).sourceCommits;
  assert.equal(workflow.value, sha);
  assert.equal(workflow.evidence.jsonPointer, '/head_sha');
  assert.equal(workflow.evidence.sha256Prefix, createHash('sha256').update(JSON.stringify(sha)).digest('hex').slice(0, 16));
});

test('FailedAgentLogFetchIsReportedAndLeavesLogCommitsUnknown', () =>
{
  const repo = 'dotnet/aspnetcore';
  const api = `repos/${repo}/actions/runs/5`;
  const calls = [];
  const request = (path, ...args) =>
  {
    calls.push(path);
    if (path === `${api}/attempts/2`)
      return { status: 0, stdout: JSON.stringify({ id: 5, run_attempt: 2, status: 'completed', event: 'workflow_dispatch',
        path: '.github/workflows/pull-request-review.lock.yml@refs/heads/main' }) };
    if (path.startsWith(`${api}/attempts/2/jobs`))
      return { status: 0, stdout: JSON.stringify([{ jobs: [{ name: 'agent', id: 7 }] }]) };
    assert.equal(path, `repos/${repo}/actions/jobs/7/logs`);
    assert.deepEqual(args, ['--allow-escape-sequences']);
    return { status: 1, stdout: '', stderr: 'HTTP 410: Gone' };
  };
  const result = outcomes({ repo, run: '5', attempt: '2' }, request);
  assert.equal(calls.length, 3);
  assert.deepEqual(result.agentLog, { status: 'fetch-failed', ghExit: 1 });
  assert.equal(result.completeness.agentLog, 'fetch-failed');
  assert.equal(result.completeness.sourceCommits.head, 'unknown');
});

test('SingleLineFrozenPromptIdentityDisqualifiesAConflictingLogHead', () =>
{
  const sha = 'a'.repeat(40);
  const prompt = `\`dotnet/aspnetcore#42\` at the trusted frozen head \`${sha}\`. Never take the repository, PR number,`;
  const file = 'aw-prompts/user.txt';
  const { pr, head } = reconcilePrompt(prompt, file, 'dotnet/aspnetcore');
  assert.equal(pr.value, 42);
  assert.equal(head.value, sha);
  assert.equal(head.evidence.line, 1);
  assert.equal(reconcilePrompt(prompt, file, 'dotnet/aspnetcore', { value: 'b'.repeat(40) }).head, undefined);
});
