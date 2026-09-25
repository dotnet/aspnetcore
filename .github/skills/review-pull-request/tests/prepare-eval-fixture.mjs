// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import * as fs from 'node:fs/promises';
import path from 'node:path';
import { parseArgs } from 'node:util';
import { contextLinks, exportTree, guideLinks } from '../scripts/prepare-review.mjs';

const { values } = parseArgs({ options: {
    source: { type: 'string' },
    output: { type: 'string' },
    'branch-head': { type: 'string' },
    'base-sha': { type: 'string' },
    repository: { type: 'string' },
    'base-ref': { type: 'string' },
} });
if (!values.source || !values.output)
{
    throw new Error('Specify an existing complete --source bundle and a new --output directory.');
}
const source = path.resolve(values.source);
const output = path.resolve(values.output);
const manifest = JSON.parse(await fs.readFile(path.join(source, 'manifest.json'), 'utf8'));
if (manifest.version !== 2 || manifest.ready !== true ||
    output === source || output.startsWith(`${source}${path.sep}`))
{
    throw new Error('The source must be a ready version-2 bundle, distinct from the output.');
}
const branch = values['branch-head'];
if (branch && (!/^[a-f0-9]{40}$/.test(branch) || !/^[a-f0-9]{40}$/.test(values['base-sha'] || '') ||
    !/^[a-z0-9_.-]+\/[a-z0-9_.-]+$/i.test(values.repository || '') ||
    !/^release\/[a-z0-9._-]+$/i.test(values['base-ref'] || '') ||
    manifest.guidance.mode !== 'remote' || !/^[a-f0-9]{40}$/.test(manifest.guidance.commit)))
{
    throw new Error('Offline branch fixtures require immutable head, base, repository, release ref, and remote guidance.');
}
if (!branch && [values['base-sha'], values.repository, values['base-ref']].some(Boolean))
{
    throw new Error('Offline branch options require --branch-head.');
}
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
function git(store, ...args)
{
    return execFileSync('git', ['--git-dir', store, ...args], {
        maxBuffer: 64 * 1024 * 1024,
        env: { ...process.env, GIT_TERMINAL_PROMPT: '0' },
    });
}

async function linkTree(relative)
{
    await fs.mkdir(path.join(output, relative), { recursive: true });
    for (const entry of await fs.readdir(path.join(source, relative), { withFileTypes: true }))
    {
        const name = path.join(relative, entry.name);
        if (entry.isDirectory())
        {
            await linkTree(name);
        }
        else if (entry.isFile())
        {
            await fs.link(path.join(source, name), path.join(output, name));
        }
        else
        {
            throw new Error(`Unsupported evaluation source entry: ${name}`);
        }
    }
}

await fs.mkdir(output);
await linkTree('guidance');
let diff;
let files;
let pull;
if (branch)
{
    const base = values['base-sha'];
    const store = path.join(output, '.objects');
    execFileSync('git', ['init', '--bare', '--quiet', store]);
    git(store, '-c', 'credential.helper=', 'fetch', '--quiet', '--no-tags', '--depth=1',
        `https://github.com/${values.repository}.git`, branch, base);
    await fs.mkdir(path.join(output, 'source'));
    const baseSource = {
        root: `source/${base}`, commit: base,
        tree: git(store, 'rev-parse', `${base}^{tree}`).toString().trim(),
        ...await exportTree(store, base, path.join(output, 'source', base)),
    };
    const headSource = {
        root: `source/${branch}`, commit: branch,
        tree: git(store, 'rev-parse', `${branch}^{tree}`).toString().trim(),
        ...await exportTree(store, branch, path.join(output, 'source', branch)),
    };
    const entries = git(store, 'diff', '--no-renames', '--name-status', '-z', base, branch)
        .toString('utf8').split('\0').filter(Boolean);
    if (entries.length === 0 || entries.length % 2 !== 0)
    {
        throw new Error('Offline release branch has no valid changed-file list.');
    }
    files = [];
    for (let i = 0; i < entries.length; i += 2)
    {
        const [status, filename] = entries.slice(i, i + 2);
        if (!['A', 'M', 'D'].includes(status))
        {
            throw new Error(`Unsupported offline change status: ${status}`);
        }
        const commit = status === 'D' ? base : branch;
        files.push({
            filename, status: { A: 'added', M: 'modified', D: 'removed' }[status],
            sha: git(store, 'rev-parse', `${commit}:${filename}`).toString().trim(),
        });
    }
    diff = git(store, 'diff', '--binary', '--no-renames', '--no-ext-diff', base, branch);
    manifest.sources = { head: headSource, mergeBase: baseSource, baseTip: baseSource };
    manifest.target = {
        kind: 'offline-branch', hostname: 'github.com', repository: values.repository, pr: null,
        headRepository: values.repository, head: branch, baseRepository: values.repository,
        baseRef: values['base-ref'], baseTip: base, mergeBase: base,
    };
    pull = {
        number: null, state: 'offline-branch', changed_files: files.length,
        title: 'Input formatting adjustment', body: '',
        head: { sha: branch, repo: { full_name: values.repository } },
        base: { ref: values['base-ref'], sha: base, repo: { full_name: values.repository } },
    };
    manifest.producer = `offline-eval/${hash(await fs.readFile(new URL(import.meta.url)))}`;
    manifest.evaluation = { mode: 'offline-branch', feedback: 'neutralized', title: 'neutralized',
        source: 'verified immutable Git objects; no live PR or GitHub-authoritative PR file list' };
}
else
{
    await linkTree('source');
    diff = await fs.readFile(path.join(source, 'diff.patch'));
    files = JSON.parse(await fs.readFile(path.join(source, 'files.json')));
    pull = JSON.parse(await fs.readFile(path.join(source, 'pull.json'), 'utf8'));
    pull.title = 'Input formatting adjustment';
    pull.body = '';
    pull.comments = 0;
    pull.review_comments = 0;
    manifest.evaluation = { feedback: 'neutralized', title: 'neutralized' };
}
manifest.context = [];
for (const guide of manifest.guides)
{
    const body = await fs.readFile(path.join(output, 'guidance', `${guide.path}.source`), 'utf8');
    const links = guideLinks(body, guide.path, files.some(file => /^src\/Components\//.test(file.filename)));
    manifest.context.push(...await contextLinks(links.context, path.join(output, 'guidance'), manifest.guidance.pointers));
}
const artifacts = {
    'diff.patch': diff,
    'files.json': Buffer.from(JSON.stringify(files, null, 2) + '\n'),
    'pull.json': Buffer.from(JSON.stringify(pull, null, 2) + '\n'),
    'feedback.json': Buffer.from(JSON.stringify({ comments: [], reviews: [], inline: [] }, null, 2) + '\n'),
};
for (const [name, bytes] of Object.entries(artifacts))
{
    await fs.writeFile(path.join(output, name), bytes, { flag: 'wx' });
    manifest.artifacts[name] = hash(bytes);
}
if (branch)
{
    const store = path.join(output, '.objects');
    git(store, 'read-tree', values['base-sha']);
    git(store, 'apply', '--cached', '--binary', '--whitespace=nowarn', path.join(output, 'diff.patch'));
    if (git(store, 'write-tree').toString().trim() !== manifest.sources.head.tree)
    {
        throw new Error('Offline diff does not reconstruct the frozen head tree.');
    }
}
await fs.writeFile(path.join(output, 'manifest.json'), JSON.stringify(manifest, null, 2) + '\n', { flag: 'wx' });
console.log(path.join(output, 'manifest.json'));
