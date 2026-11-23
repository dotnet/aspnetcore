// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { spawnSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { existsSync, mkdirSync, rmSync } from 'node:fs';
import { dirname, isAbsolute, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const shaPattern = /^[0-9a-f]{40}$/;
export const numberPattern = /^[1-9]\d*$/;
export const repoPattern = /^[\w.-]+\/[\w.-]+$/;
export const roles = ['head', 'baseTip', 'mergeBase', 'workflow', 'guidance'];

export function command(exe, args, input, env)
{
  const result = spawnSync(exe, args, { encoding: 'utf8', input, env, maxBuffer: 64 * 1024 * 1024, windowsHide: true });
  if (result.error) throw result.error;
  return result;
}

// Keep only fetch authentication, transport and tracing variables; repository routing
// (GIT_DIR, GIT_COMMON_DIR, GIT_CONFIG*, GIT_OBJECT_DIRECTORY, ...) must not redirect a call.
const transport = /^GIT_(TERMINAL_PROMPT|ASKPASS|SSH|SSH_COMMAND|SSH_VARIANT|SSL_\w+|HTTP_\w+|PROXY_\w+|CURL_\w+|CONFIG_GLOBAL|CONFIG_SYSTEM|CONFIG_NOSYSTEM|TRACE\w*)$/i;

// Runs git with repository-routing variables removed. Offline calls also ignore user and system config.
export function git(args, { input, offline = false } = {})
{
  const env = Object.fromEntries(Object.entries(process.env)
    .filter(([name]) => !/^GIT_/i.test(name) || (offline ? /^GIT_TERMINAL_PROMPT$/i : transport).test(name)));
  if (offline)
  {
    env.GIT_CONFIG_NOSYSTEM = '1';
    env.GIT_CONFIG_GLOBAL = process.platform === 'win32' ? 'NUL' : '/dev/null';
    env.GIT_ATTR_NOSYSTEM = '1';
  }
  return command('git', ['--no-pager', ...args], input, env);
}

export function scratch(action)
{
  const dir = join(dirname(fileURLToPath(import.meta.url)), `.l3-${randomUUID()}`);
  mkdirSync(dir);
  try { return action(dir); }
  finally { rmSync(dir, { recursive: true, force: true, maxRetries: 5 }); }
}

export function pin({ repo, mirror, run, pr, commits })
{
  if (!/^[1-9]\d*-[1-9]\d*$/.test(run)) throw new Error('Expected --run RUN_ID-ATTEMPT.');
  if (pr !== undefined && !numberPattern.test(pr)) throw new Error('Expected a positive --pr number.');
  const source = isAbsolute(repo) && existsSync(repo) ? repo :
    repoPattern.test(repo) ? `https://github.com/${repo}.git` : undefined;
  if (!source) throw new Error('Expected --repo OWNER/REPO or an absolute local Git repository path.');
  if (!Object.keys(commits).length || Object.entries(commits).some(([role, sha]) => !roles.includes(role) || !shaPattern.test(sha)))
    throw new Error('Supply at least one role with a full lowercase commit SHA.');

  mirror = resolve(mirror);
  const store = (...args) => git(['--no-replace-objects', `--git-dir=${mirror}`, ...args]);
  function check(result, operation)
  {
    if (result.status !== 0) throw new Error(`${operation} failed: ${result.stderr.trim()}`);
    return result.stdout.trim();
  }
  if (!existsSync(mirror))
  {
    mkdirSync(dirname(mirror), { recursive: true });
    check(git(['init', '--bare', '--template=', '--object-format=sha1', '--', mirror]), 'Bare mirror init');
  }
  if (check(store('rev-parse', '--is-bare-repository'), 'Bare mirror check') !== 'true' ||
      check(store('rev-parse', '--is-shallow-repository'), 'Shallow check') !== 'false' ||
      existsSync(join(mirror, 'objects', 'info', 'alternates')))
    throw new Error('The mirror must be bare, non-shallow, and have no object alternates.');

  const entries = Object.entries(commits).map(([role, sha]) => ({ role, sha, ref: `refs/l3/${run}/${role}` }));
  for (const entry of entries)
  {
    if (store('symbolic-ref', '-q', entry.ref).status === 0)
      throw new Error(`Refusing symbolic pin ${entry.ref}.`);
    const old = store('rev-parse', '--verify', '--quiet', entry.ref);
    if (old.status === 0 && old.stdout.trim() !== entry.sha)
      throw new Error(`Refusing to overwrite ${entry.ref}: ${old.stdout.trim()} != ${entry.sha}.`);
    entry.status = old.status === 0 ? 'already-pinned' : 'pinned';
  }

  const isCommit = sha => store('-c', 'protocol.allow=never', 'cat-file', '-t', sha).stdout.trim() === 'commit';
  const attempts = new Map();
  const fetch = ref =>
  {
    if (!attempts.has(ref))
    {
      // Include objects fetched earlier in this run, even though its pins are not yet visible.
      const tips = [...new Set(entries.map(entry => entry.sha))].filter(isCommit);
      attempts.set(ref, store('fetch', '--no-tags', '--no-write-fetch-head', '--no-auto-maintenance',
        ...tips.map(tip => `--negotiation-tip=${tip}`), '--', source, ref));
    }
    return attempts.get(ref);
  };
  for (const entry of [...entries].sort((a, b) => Number(b.role === 'baseTip') - Number(a.role === 'baseTip')))
  {
    if (isCommit(entry.sha)) continue;
    const tried = [fetch(entry.sha)];
    if (!isCommit(entry.sha) && entry.role === 'head' && pr !== undefined)
      tried.push(fetch(`refs/pull/${pr}/head`));
    if (!isCommit(entry.sha))
    {
      if (entry.status === 'already-pinned') throw new Error(`Existing pin ${entry.ref} has no commit object.`);
      entry.status = 'unreachable';
      entry.fetchExitCodes = tried.map(result => result.status);
    }
  }

  const reachable = entries.filter(entry => entry.status !== 'unreachable');
  if (reachable.length)
  {
    scratch(dir =>
    {
      const restore = join(dir, 'restore.git');
      check(git(['-c', 'protocol.allow=never', '-c', 'protocol.file.allow=always',
        'clone', '--local', '--mirror', '--template=', '--', mirror, restore], { offline: true }), 'Local restore clone');
      const offline = (...args) => git(['--no-replace-objects',
        '-c', 'protocol.allow=never', `--git-dir=${restore}`, ...args], { offline: true });
      check(offline('config', '--file', join(restore, 'config'), '--remove-section', 'remote.origin'), 'Remove restore remote');
      const shas = [...new Set(reachable.map(entry => entry.sha))];
      check(offline('fsck', '--connectivity-only', '--no-dangling', ...shas), 'Offline object closure');
      for (const sha of shas)
        check(offline('cat-file', '-e', `${sha}^{tree}`), `Offline tree ${sha}`);
    });

    // A concurrent writer cannot replace an existing pin or bypass the preflight check.
    const updates = reachable.map(({ status, ref, sha }) =>
      `${status === 'pinned' ? 'create' : 'verify'} ${ref} ${sha}\n`).join('');
    check(git(['--no-replace-objects', `--git-dir=${mirror}`, 'update-ref', '--no-deref', '--stdin'], { input: updates }), 'Pin refs transaction');
  }
  return { run, mirror, offlineRestoreVerified: reachable.length === entries.length, entries };
}
