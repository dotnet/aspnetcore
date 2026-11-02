import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const modulePath = 'src/submodules/MessagePack-CSharp';
const repository = 'https://github.com/aspnet/MessagePack-CSharp.git';

export function prepareValidationDependencies(
  worktree,
  expectedHead,
  { fetch = execFileSync, allowFetch = true } = {}) {
  if (!/^[a-f0-9]{40}$/.test(expectedHead)) {
    throw new Error('Expected a full frozen validation head.');
  }
  const worktreeStat = fs.lstatSync(worktree, { throwIfNoEntry: false });
  if (!worktreeStat?.isDirectory() || worktreeStat.isSymbolicLink()) {
    throw new Error('Validation worktree must be a non-symlink directory.');
  }
  const resolvedWorktree = path.resolve(worktree);
  const scratch = fs.mkdtempSync(path.join(os.tmpdir(), 'review-dependencies-'));
  try {
    const hooks = path.join(scratch, 'disabled-hooks');
    fs.mkdirSync(hooks);
    const env = {
      PATH: process.env.PATH,
      HOME: scratch,
      GIT_CONFIG_GLOBAL: path.join(scratch, 'absent-config'),
      GIT_CONFIG_NOSYSTEM: '1',
      GIT_TERMINAL_PROMPT: '0',
    };
    for (const name of ['SystemRoot', 'TEMP', 'TMP']) {
      if (process.env[name]) {
        env[name] = process.env[name];
      }
    }
    const options = { env, encoding: 'utf8', timeout: 120_000, maxBuffer: 1024 * 1024 };
    let destination = resolvedWorktree;
    for (const component of modulePath.split('/')) {
      destination = path.join(destination, component);
      if (fs.lstatSync(destination, { throwIfNoEntry: false })?.isSymbolicLink()) {
        throw new Error('Validation dependency path must not traverse a symlink.');
      }
    }
    const prefix = [
      '-c', `core.hooksPath=${hooks}`,
      '-c', 'credential.helper=',
      '-c', `safe.directory=${resolvedWorktree}`,
      '-c', `safe.directory=${destination}`,
    ];
    const git = (...args) => execFileSync('git', [...prefix, ...args], options).trim();
    if (git('-C', resolvedWorktree, 'rev-parse', 'HEAD') !== expectedHead) {
      throw new Error('Validation worktree head differs from the frozen head.');
    }
    const entry = git('-C', resolvedWorktree, 'ls-tree', '-z', expectedHead, '--', modulePath);
    if (!entry) {
      return [];
    }
    const match = /^160000 commit ([a-f0-9]{40})\tsrc\/submodules\/MessagePack-CSharp\0$/.exec(entry);
    if (!match) {
      throw new Error('Expected the fixed MessagePack path to be a frozen gitlink.');
    }
    fs.mkdirSync(destination, { recursive: true });
    if (fs.readdirSync(destination).length !== 0) {
      const gitEntry = fs.lstatSync(path.join(destination, '.git'), { throwIfNoEntry: false });
      if (gitEntry && !gitEntry.isSymbolicLink()) {
        try {
          if (git('-C', destination, 'rev-parse', 'HEAD') === match[1] &&
              git('-C', destination, 'status', '--porcelain') === '') {
            return [{ path: modulePath, repository, commit: match[1] }];
          }
        } catch {
          // Fall through to reject an invalid or mismatched populated directory.
        }
      }
      throw new Error('Validation dependency destination must be empty or match the clean frozen gitlink.');
    }
    if (!allowFetch) {
      throw new Error('Validation dependency is missing and network fetch is disabled.');
    }
    const staged = path.join(scratch, 'staged');
    fs.mkdirSync(staged);
    git('init', '--quiet', staged);
    // Ignore target-controlled .gitmodules URLs; acquire only this frozen public dependency.
    fetch('git', [...prefix, '-C', staged, 'fetch', '--quiet', '--no-tags',
      '--no-recurse-submodules', '--depth=1', repository, match[1]], options);
    git('-C', staged, 'checkout', '--quiet', '--detach', 'FETCH_HEAD');
    if (git('-C', staged, 'rev-parse', 'HEAD') !== match[1]) {
      throw new Error('Validation dependency head differs from the frozen gitlink.');
    }
    try {
      for (const item of fs.readdirSync(staged)) {
        fs.cpSync(path.join(staged, item), path.join(destination, item), { recursive: true });
      }
    } catch (error) {
      for (const item of fs.readdirSync(destination)) {
        fs.rmSync(path.join(destination, item), { recursive: true, force: true });
      }
      throw error;
    }
    if (git('-C', destination, 'rev-parse', 'HEAD') !== match[1]) {
      throw new Error('Validation dependency head differs from the frozen gitlink.');
    }
    return [{ path: modulePath, repository, commit: match[1] }];
  } finally {
    fs.rmSync(scratch, { recursive: true, force: true });
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  if (process.argv.length !== 4) {
    throw new Error('Usage: prepare-validation-dependencies.mjs WORKTREE FROZEN_HEAD');
  }
  console.log(JSON.stringify(prepareValidationDependencies(process.argv[2], process.argv[3])));
}
