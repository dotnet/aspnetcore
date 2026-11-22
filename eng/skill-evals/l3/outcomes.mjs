// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { command, numberPattern, repoPattern, scratch, shaPattern } from './pin.mjs';

const recorded = 'recorded';
const absent = 'absent-in-evidence';
const expired = 'artifact-expired';
const unknown = 'unknown';
const hash = text => createHash('sha256').update(text).digest('hex').slice(0, 16);
const lines = text => text.split(/\r?\n/);
const json = text => { try { return JSON.parse(text); } catch { return undefined; } };
const selector = (file, raw, position) => ({ file, ...position, sha256Prefix: hash(raw) });
const id = value => numberPattern.test(String(value)) && Number.isSafeInteger(Number(value));

export function extractTranscript(text, file)
{
  const entries = lines(text).flatMap((raw, index) => raw.trim() ? [{ event: json(raw), raw, line: index + 1 }] : []);
  const tasks = new Map();
  const pendingWorkers = new Set();
  const agents = new Map();
  const reads = new Map();
  const workerCandidates = [];
  const verifierDispositions = [];
  let unparsedVerifier = false;
  let unparsedWorker = false;
  for (const { event, raw, line } of entries)
  {
    const data = event?.data ?? {};
    // Match the workflow validator: arguments or input, and top-level or data agent IDs and results.
    const input = data.arguments ?? data.input;
    const agentId = event?.agentId ?? data.agentId;
    if (event?.type === 'tool.execution_start' &&
        ['review-guide-worker', 'review-candidate-verifier'].includes(input?.agent_type))
    {
      tasks.set(data.toolCallId, input);
      if (input.agent_type === 'review-guide-worker') pendingWorkers.add(data.toolCallId);
    }
    if (event?.type === 'subagent.started' && tasks.get(data.toolCallId)?.agent_type === data.agentName)
    {
      const { name } = tasks.get(data.toolCallId);
      const agent = { type: data.agentName, taskId: data.toolCallId };
      agents.set(name, agent);
      if (typeof agentId === 'string') agents.set(agentId, agent);
    }
    if (event?.type === 'tool.execution_start' && typeof input?.agent_id === 'string' &&
        Object.keys(input).every(key => ['agent_id', 'wait', 'timeout', 'since_turn'].includes(key)))
      reads.set(data.toolCallId, agents.get(input.agent_id));
    const agent = reads.get(data.toolCallId);
    if (event?.type !== 'tool.execution_complete' || !data.success || !agent) continue;

    const nested = data.result?.content !== undefined;
    const content = nested ? data.result.content : event.result?.content;
    const evidence = selector(file, raw, { line, jsonPointer: nested ? '/data/result/content' : '/result/content' });
    const turns = typeof content === 'string' ? content.split(/^\[Turn \d+\][ \t]*\r?\n/gm).slice(1) : [];
    if (agent.type === 'review-guide-worker')
    {
      let found = false;
      for (const [turn, body] of turns.entries())
      {
        if (!/^PATH:\s*per-guide-worker\s*$/m.test(body)) continue;
        const rows = lines(body);
        const start = rows.findIndex(row => /^(?:#{1,6}\s+)?(?:\*\*)?CANDIDATES:?(?:\*\*)?:?\s*$/i.test(row.trim()));
        if (start < 0) continue;
        found = true;
        let end = rows.findIndex((row, i) => i > start &&
          /^(?:#{1,6}\s+|\*\*)?(?:DISCARDED|UNRESOLVED|EXISTING_FEEDBACK_COVERAGE)\b/i.test(row.trim()));
        if (end < 0) end = rows.length;
        const section = rows.slice(start + 1, end).join('\n').trim();
        if (section && !/^(?:[-*]\s+)?None\b[^\r\n]*$/i.test(section))
          workerCandidates.push({ ...evidence, turn, contentLines: [start + 2, end], contentSha256Prefix: hash(section) });
      }
      if (found) pendingWorkers.delete(agent.taskId);
      unparsedWorker ||= !found;
      continue;
    }

    let found = false;
    for (const [turn, body] of turns.entries())
    {
      const payload = json(body.trim().replace(/^```(?:json)?\r?\n|\r?\n```$/gi, ''));
      const rows = Array.isArray(payload) ? payload : Array.isArray(payload?.results) ? payload.results : [payload];
      for (const [index, row] of rows.entries())
      {
        if (typeof row?.candidateId !== 'string' || !['KEEP', 'DROP', 'DOWNGRADE'].includes(row.decision)) continue;
        found = true;
        const jsonPointer = Array.isArray(payload) ? `/${index}` : Array.isArray(payload?.results) ? `/results/${index}` : '';
        verifierDispositions.push({
          candidateIdHash: hash(row.candidateId),
          decision: row.decision,
          disposition: { KEEP: 'kept', DROP: 'discarded', DOWNGRADE: 'downgraded' }[row.decision],
          evidence: { ...evidence, turn, payloadJsonPointer: jsonPointer },
          reason: typeof row.reason === 'string' && row.reason.trim()
            ? { completeness: recorded, payloadJsonPointer: `${jsonPointer}/reason`, sha256Prefix: hash(row.reason) }
            : { completeness: absent },
        });
      }
    }
    unparsedVerifier ||= !found;
  }
  const malformed = entries.some(({ event }) => !event);
  return {
    workerCandidates, verifierDispositions,
    workerStatus: workerCandidates.length ? recorded :
      malformed || unparsedWorker || pendingWorkers.size || !entries.length ? unknown : absent,
    verifierStatus: verifierDispositions.length ? recorded :
      malformed || unparsedVerifier || !entries.length || [...tasks.values()].some(task => task.agent_type === 'review-candidate-verifier')
        ? unknown : absent,
  };
}

export function reconcilePrompt(prompt, file, repo, loggedHead)
{
  const repoLiteral = repo.replaceAll('.', '\\.');
  const matches = [...(prompt ?? '').matchAll(new RegExp(
    `^[ \\t]*\`${repoLiteral}#([1-9]\\d*)\`\\s+at\\s+the\\s+trusted\\s+frozen\\s+head\\s+\`([a-f0-9]{40})\`\\.\\s+Never\\s+take\\s+the\\s+repository,`, 'gim'))];
  if (!matches.length) return { head: loggedHead };
  const [match] = matches;
  if (matches.some(other => other[1] !== match[1] || other[2] !== match[2])) return { head: undefined };
  const line = lines(prompt.slice(0, match.index)).length;
  const lineCount = lines(match[0]).length;
  const evidence = selector(file, lines(prompt).slice(line - 1, line - 1 + lineCount).join('\n'), { line, lineCount });
  return {
    pr: { value: Number(match[1]), evidence },
    head: loggedHead ? (loggedHead.value === match[2] ? loggedHead : undefined) : { value: match[2], evidence },
  };
}

function logValue(text, file, pattern, convert = value => value)
{
  const matches = lines(text).flatMap((raw, index) =>
  {
    const match = raw.replace(/\x1b\[[0-9;]*m/g, '').replace(/^\S+Z\s+/, '').match(pattern);
    return match ? [{ value: convert(match[1]), evidence: selector(file, raw, { line: index + 1 }) }] : [];
  });
  return new Set(matches.map(match => match.value)).size === 1 ? matches[0] : undefined;
}

export function outcomes({ repo, run, attempt }, request = (...args) => command('gh', ['api', ...args]))
{
  if (!repoPattern.test(repo) || !numberPattern.test(run) || !numberPattern.test(attempt))
    throw new Error('Expected OWNER/REPO, run ID and attempt.');
  const api = `repos/${repo}/actions/runs/${run}`;
  function get(path, paginate = false)
  {
    const result = request(path, ...(paginate ? ['--paginate', '--slurp'] : []));
    if (result.status !== 0) throw new Error(`Could not read ${path} (gh exit ${result.status}).`);
    return JSON.parse(result.stdout);
  }
  const resolved = get(`${api}/attempts/${attempt}`);
  if (String(resolved.id) !== run || String(resolved.run_attempt) !== attempt ||
      resolved.path?.split('@')[0] !== '.github/workflows/pull-request-review.lock.yml')
    throw new Error('The requested run attempt is not the PR reviewer workflow.');
  const jobs = get(`${api}/attempts/${attempt}/jobs?per_page=100`, true).flatMap(page => page.jobs);
  const agentJob = jobs.find(job => job.name === 'agent');
  const log = agentJob && request(`repos/${repo}/actions/jobs/${agentJob.id}/logs`, '--allow-escape-sequences');
  const logFile = `actions/jobs/${agentJob?.id}/logs`;
  const agentLog = !agentJob ? { status: absent } : log.status === 0 ? { status: recorded } :
    { status: 'fetch-failed', ghExit: log.status ?? null };
  const logField = (pattern, convert) => log?.status === 0 ? logValue(log.stdout, logFile, pattern, convert) : undefined;
  const repoLiteral = repo.replaceAll('.', '\\.');
  const evidence = {
    head: logField(/^\s*REVIEW_HEAD: ([a-f0-9]{40})\s*$/),
    baseTip: logField(/^\s*REVIEW_BASE: ([a-f0-9]{40})\s*$/),
    mergeBase: logField(/^\s*REVIEW_MERGE_BASE: ([a-f0-9]{40})\s*$/),
    guidance: logField(new RegExp(`^\\s*GUIDANCE_SOURCE: (?:immutable commit |${repoLiteral}@)([a-f0-9]{40})\\s*$`, 'i')),
    workflow: resolved.event === 'issue_comment' && shaPattern.test(resolved.head_sha) ? {
      value: resolved.head_sha,
      evidence: selector(`actions/runs/${run}/attempts/${attempt}`, resolved.head_sha, { jsonPointer: '/head_sha' }),
    } : undefined,
  };
  const reportedPr = logField(new RegExp(`^\\s*PR: ${repoLiteral}#([1-9]\\d*)\\s*$`, 'i'), Number);

  return scratch(dir =>
  {
    const attemptSafe = () => attempt === '1' && resolved.status === 'completed' && get(api).run_attempt === 1;
    const bindArtifacts = attemptSafe();
    const inventory = bindArtifacts ? get(`${api}/artifacts?per_page=100`, true).flatMap(page => page.artifacts) : [];
    const artifacts = inventory.map(({ name, id, digest, expired }) =>
      ({ name, id, digest: digest ?? null, expired, digestCompleteness: digest ? recorded : absent }));
    const states = new Map();
    for (const name of ['agent', 'agent-output-fallback', 'safe-outputs-items'])
    {
      const matching = inventory.filter(item => item.name === name);
      if (matching.length !== 1) continue;
      const item = matching[0];
      if (item.expired)
      {
        states.set(name, expired);
        continue;
      }
      const dest = join(dir, name);
      mkdirSync(dest);
      const download = command('gh', ['run', 'download', run, '-R', repo, '-n', name, '-D', dest]);
      if (download.status === 0) states.set(name, recorded);
    }
    // The artifact API is run-scoped, not attempt-scoped. Don't mix provenance across retries.
    const bound = bindArtifacts && attemptSafe();
    const state = name => bound ? states.get(name) ?? unknown : unknown;
    const read = (name, filename) =>
    {
      if (state(name) !== recorded) return undefined;
      try { return readFileSync(join(dir, name, filename), 'utf8'); }
      catch (error) { if (error.code !== 'ENOENT') throw error; }
    };
    const missing = (name, text) => state(name) === recorded && text === undefined ? absent :
      state(name) === expired ? expired : unknown;

    const output = ['agent', 'agent-output-fallback'].map(name =>
    {
      const raw = read(name, 'agent_output.json');
      return { file: `${name}/agent_output.json`, value: json(raw), missing: missing(name, raw) };
    });
    const selected = output.find(({ value }) => Array.isArray(value?.items) &&
      (!Object.hasOwn(value, 'errors') || (Array.isArray(value.errors) && !value.errors.length)) &&
      value.items.every(item => typeof item?.type === 'string'));
    const items = selected?.value.items ?? [];
    const itemsStatus = selected ? absent : output.some(entry => entry.missing === expired) && !output.some(entry => entry.value)
      ? expired : output.every(entry => entry.missing === absent) ? absent : unknown;
    const inlineRequests = items.flatMap((item, index) => item.type === 'create_pull_request_review_comment' ?
      [selector(selected.file, JSON.stringify(item), { jsonPointer: `/items/${index}` })] : []);
    const noopIndex = items.findIndex(item => item.type === 'noop');
    const noop = noopIndex < 0 ? null : selector(selected.file, JSON.stringify(items[noopIndex]), { jsonPointer: `/items/${noopIndex}` });

    const transcriptFile = 'agent/agent-session.jsonl';
    const transcriptText = read('agent', 'agent-session.jsonl');
    const transcript = transcriptText === undefined ? undefined : extractTranscript(transcriptText, transcriptFile);
    const prompt = read('agent', 'aw-prompts/user.txt');
    const promptFile = 'agent/aw-prompts/user.txt';
    const { pr: promptPr, head } = reconcilePrompt(prompt, promptFile, repo, evidence.head);
    evidence.head = head;
    const safeText = read('safe-outputs-items', 'safe-output-items.jsonl');
    const safe = lines(safeText ?? '').flatMap((raw, index) => raw.trim() ?
      [{ item: json(raw), raw, line: index + 1 }] : []);
    const correctRepo = safe.filter(({ item }) => typeof item?.repo === 'string' &&
      item.repo.toLowerCase() === repo.toLowerCase() && id(item.number));
    const received = correctRepo.filter(({ item }) => item.type === 'submit_pull_request_review' && id(item.id) &&
      Number(item.metadata?.review_id) === Number(item.id));
    const safeEvidence = entry => selector('safe-outputs-items/safe-output-items.jsonl', entry.raw, { line: entry.line });
    const reported = new Set([reportedPr?.value, promptPr?.value, ...correctRepo.map(({ item }) => Number(item.number))].filter(Boolean));
    const prNumber = reported.size === 1 ? [...reported][0] : null;
    const prEvidence = prNumber === null ? null : reportedPr?.evidence ?? promptPr?.evidence ?? safeEvidence(correctRepo[0]);
    const review = received.length === 1 && Number(received[0].item.number) === prNumber ? received[0] : undefined;
    const publishedReview = review ? { id: Number(review.item.id), evidence: safeEvidence(review) } : null;
    const inlineComments = correctRepo.flatMap(entry =>
      publishedReview && entry.item.type === 'create_pull_request_review_comment' && id(entry.item.id) &&
      Number(entry.item.number) === prNumber && Number(entry.item.metadata?.review_id) === publishedReview.id
        ? [{ id: Number(entry.item.id), evidence: safeEvidence(entry) }] : []);
    const missingPublications = safeText === undefined ? missing('safe-outputs-items', safeText) :
      safe.some(entry => !entry.item) || !noop || inlineRequests.length ? unknown : absent;
    return {
      schemaVersion: 1, repo, runId: run, attempt: Number(attempt), headSha: evidence.head?.value ?? null,
      prNumber, prEvidence, agentLog,
      sourceCommits: Object.fromEntries(Object.entries(evidence).map(([role, entry]) => [role, entry ?? null])),
      artifacts: bound ? artifacts : [], workerCandidateSections: transcript?.workerCandidates ?? [],
      inlineRequests, verifierDispositions: transcript?.verifierDispositions ?? [], noop, publishedReview, inlineComments,
      completeness: {
        runId: recorded, attempt: recorded, agentLog: agentLog.status, headSha: evidence.head ? recorded : unknown, prNumber: prNumber ? recorded : unknown,
        sourceCommits: Object.fromEntries(Object.entries(evidence).map(([role, entry]) => [role, entry ? recorded : unknown])),
        artifacts: bound && artifacts.length ? recorded : unknown,
        workerCandidateSections: transcript?.workerStatus ?? missing('agent', transcriptText),
        inlineRequests: inlineRequests.length ? recorded : itemsStatus,
        verifierDispositions: transcript?.verifierStatus ?? missing('agent', transcriptText),
        noop: noop ? recorded : itemsStatus,
        publishedReview: publishedReview ? recorded : missingPublications,
        inlineComments: inlineComments.length ? recorded : missingPublications,
      },
    };
  });
}
