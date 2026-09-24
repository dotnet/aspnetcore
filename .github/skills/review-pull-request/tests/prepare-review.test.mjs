// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import * as fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { checkPaths, prepare } from '../scripts/prepare-review.mjs';

const identity = {
    GIT_AUTHOR_NAME: 'Preparation test', GIT_AUTHOR_EMAIL: 'preparation@example.invalid',
    GIT_COMMITTER_NAME: 'Preparation test', GIT_COMMITTER_EMAIL: 'preparation@example.invalid',
};

function git(root, args, input)
{
    const location = root.endsWith('guidance-checkout') ? ['-C', root] : ['--git-dir', root];
    return execFileSync('git', [...location, '-c', 'commit.gpgsign=false', ...args], {
        input, env: { ...process.env, ...identity }, windowsHide: true,
    }).toString().trim();
}

async function fixture(t)
{
    const root = await fs.mkdtemp(path.join(os.tmpdir(), 'review-preparation-'));
    t.after(() => fs.rm(root, { recursive: true, force: true }));
    const repository = path.join(root, 'repository');
    await fs.mkdir(repository);
    git(repository, ['init', '--bare', '--quiet']);
    function commit(files, parent)
    {
        git(repository, ['read-tree', '--empty']);
        for (const [name, entry] of Object.entries(files))
        {
            const [mode, body] = Array.isArray(entry) ? entry : ['100644', entry];
            const sha = git(repository, ['hash-object', '-w', '--stdin'], body);
            git(repository, ['update-index', '--add', '--cacheinfo', `${mode},${sha},${name}`]);
        }
        return git(repository, ['commit-tree', git(repository, ['write-tree']), ...(parent ? ['-p', parent] : []), '-m', 'Fixture']);
    }
    const original = {
        'src/Value.cs': 'MERGE_VALUE\n',
        'src/Unchanged.cs': 'MERGE_DEPENDENCY\n',
        'src/OldName.cs': 'RENAMED_BYTES\n',
        'src/Deleted.cs': 'DELETED_BYTES\n',
        'src/Mode.cs': 'REGULAR_BYTES\n',
        'src/Large.cs': `${'unchanged padding\n'.repeat(2500)}RELEVANT_IMPLEMENTATION\n`,
        'AGENTS.md': 'TARGET_INSTRUCTION_SENTINEL\n',
        '.github/copilot-instructions.md': 'TARGET_ROOT_INSTRUCTION_SENTINEL\n',
        '.github/instructions/product.instructions.md': 'TARGET_NESTED_INSTRUCTION_SENTINEL\n',
    };
    const mergeBase = commit(original);
    const headFiles = {
        ...original, 'src/Value.cs': 'HEAD_VALUE\n',
        'src/Renamed.cs': original['src/OldName.cs'], 'src/Mode.cs': ['120000', 'Value.cs'],
    };
    delete headFiles['src/OldName.cs'];
    delete headFiles['src/Deleted.cs'];
    const head = commit(headFiles, mergeBase);
    const baseTip = commit({ ...original, 'src/Unchanged.cs': 'BASE_TIP_DEPENDENCY\n' }, mergeBase);
    const guidanceRoot = path.join(root, 'guidance-checkout');
    await fs.mkdir(path.join(guidanceRoot, 'docs'), { recursive: true });
    await fs.writeFile(path.join(guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n## Overarching principles\n- ORIGINAL_GUIDANCE\n## Topics\n### Topic\n- Required clause.\n');
    git(guidanceRoot, ['init', '--quiet']);
    git(guidanceRoot, ['add', '.']);
    git(guidanceRoot, ['commit', '--quiet', '-m', 'Guidance']);
    await fs.writeFile(path.join(guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n## Overarching principles\n- DIRTY_GUIDANCE\n## Topics\n### Topic\n- Required clause.\n');
    const files = [
        { filename: 'src/Value.cs', status: 'modified' },
        { filename: 'src/OldName.cs', status: 'removed' },
        { filename: 'src/Renamed.cs', status: 'added' },
        { filename: 'src/Deleted.cs', status: 'removed' },
        { filename: 'src/Mode.cs', status: 'modified' },
    ].map(file => ({ ...file, sha: git(repository, ['rev-parse', `${file.status === 'removed' ? mergeBase : head}:${file.filename}`]) }));
    const pull = {
        number: 42, changed_files: files.length,
        head: { sha: head, repo: { id: 2, full_name: 'contributor/product' } },
        base: { ref: 'release/test', repo: { id: 1, full_name: 'owner/product' } },
    };
    const diff = execFileSync('git', ['--git-dir', repository, 'diff', '--binary', '--no-ext-diff', mergeBase, head]);
    const state = { pull, files, diff, baseTip, mergeBase };
    const api = (endpoint, accept) =>
    {
        if (endpoint === 'repos/owner/product')
        {
            return { id: 1, full_name: 'owner/product' };
        }
        if (endpoint === 'repos/reviewer/guidance')
        {
            return { id: 3, full_name: 'reviewer/guidance' };
        }
        if (accept)
        {
            return state.diff;
        }
        if (endpoint.includes('/files?'))
        {
            return state.files;
        }
        if (endpoint.includes('/git/ref/heads/'))
        {
            return { object: { sha: state.baseTip } };
        }
        if (endpoint.includes('/compare/'))
        {
            return { base_commit: { sha: state.baseTip }, merge_base_commit: { sha: state.mergeBase } };
        }
        if (endpoint.endsWith('/pulls/42'))
        {
            return structuredClone(state.pull);
        }
        throw new Error(`Unexpected API request: ${endpoint}`);
    };
    const dependencies = {
        api,
        fetch: (_repo, commits, store) => git(store, ['fetch', '--quiet', '--no-tags', repository, ...commits]),
    };
    const options = { repo: 'owner/product', pr: 42, output: path.join(root, 'prepared'), guidanceRoot };
    return { root, repository, commit, original, head, mergeBase, baseTip, options, dependencies, state };
}

test('prepares distinct complete sides, inert target instructions, large files and dirty guidance', async t =>
{
    const f = await fixture(t);
    const manifest = await prepare(f.options, f.dependencies);
    assert.equal(manifest.target.head, f.head);
    assert.equal(manifest.target.mergeBase, f.mergeBase);
    assert.equal(manifest.target.baseTip, f.baseTip);
    assert.notEqual(f.head, f.baseTip);
    assert.notEqual(f.baseTip, f.mergeBase);
    const source = async (role, name) => fs.readFile(path.join(f.options.output, manifest.sources[role].root, `${name}.source`), 'utf8');
    assert.equal(await source('head', 'src/Value.cs'), 'HEAD_VALUE\n');
    assert.equal(await source('mergeBase', 'src/Unchanged.cs'), 'MERGE_DEPENDENCY\n');
    assert.equal(await source('baseTip', 'src/Unchanged.cs'), 'BASE_TIP_DEPENDENCY\n');
    assert.equal(await source('mergeBase', 'src/Deleted.cs'), 'DELETED_BYTES\n');
    assert.equal(await source('head', 'src/Renamed.cs'), 'RENAMED_BYTES\n');
    assert.equal(await source('head', 'src/Mode.cs'), 'Value.cs');
    assert.equal(await source('head', 'AGENTS.md'), 'TARGET_INSTRUCTION_SENTINEL\n');
    assert.equal(await source('head', '.github/copilot-instructions.md'), 'TARGET_ROOT_INSTRUCTION_SENTINEL\n');
    assert.match(await source('head', 'src/Large.cs'), /RELEVANT_IMPLEMENTATION/);
    assert.equal((await fs.stat(path.join(f.options.output, manifest.sources.head.root, 'src/Mode.cs.source'))).isFile(), true);
    await assert.rejects(fs.stat(path.join(f.options.output, manifest.sources.head.root, 'AGENTS.md')), { code: 'ENOENT' });
    assert.equal(manifest.guidance.workingTreeChanges, true);
    assert.match(await fs.readFile(path.join(f.options.output, 'guidance/docs/CrossCuttingGuidance.md.source'), 'utf8'), /DIRTY_GUIDANCE/);
    assert.equal((await prepare({ ...f.options, check: true }, f.dependencies)).ready, true);
});

test('supports an immutable remote guidance selection without selecting the local checkout', async t =>
{
    const f = await fixture(t);
    const commit = f.commit({ 'docs/CrossCuttingGuidance.md': '# REMOTE_GUIDANCE\n' });
    const options = { ...f.options, guidanceRoot: undefined, guidance: `reviewer/guidance@${commit}` };
    const manifest = await prepare(options, f.dependencies);
    assert.equal(manifest.guidance.mode, 'remote');
    assert.equal(manifest.guidance.commit, commit);
    assert.match(await fs.readFile(path.join(options.output, 'guidance/docs/CrossCuttingGuidance.md.source'), 'utf8'), /REMOTE_GUIDANCE/);
    assert.equal((await prepare({ ...options, check: true }, f.dependencies)).ready, true);
});

for (const [name, mutate] of [
    ['changed head', f => { f.state.pull.head.sha = f.baseTip; }],
    ['changed base-tip', f => { f.state.baseTip = f.mergeBase; }],
    ['changed guidance', f => fs.appendFile(path.join(f.options.guidanceRoot, 'docs/CrossCuttingGuidance.md'), '\nCHANGED\n')],
    ['changed source', async (f, m) => fs.appendFile(path.join(f.options.output, m.sources.head.root, 'src/Value.cs.source'), 'MUTATED')],
    ['missing diff', f => fs.unlink(path.join(f.options.output, 'diff.patch'))],
    ['partial manifest', async f =>
    {
        const filename = path.join(f.options.output, 'manifest.json');
        const manifest = JSON.parse(await fs.readFile(filename));
        delete manifest.sources.mergeBase;
        await fs.writeFile(filename, JSON.stringify(manifest));
    }],
    ['wrong source role', async f =>
    {
        const filename = path.join(f.options.output, 'manifest.json');
        const manifest = JSON.parse(await fs.readFile(filename));
        manifest.sources.head = manifest.sources.baseTip;
        await fs.writeFile(filename, JSON.stringify(manifest));
    }],
])
{
    test(`rejects reuse with ${name}`, async t =>
    {
        const f = await fixture(t);
        const manifest = await prepare(f.options, f.dependencies);
        await mutate(f, manifest);
        await assert.rejects(prepare({ ...f.options, check: true }, f.dependencies));
    });
}

for (const [name, mutate] of [
    ['truncated diff', f => { f.state.diff = Buffer.alloc(0); }],
    ['incomplete file list', f => { f.state.files = f.state.files.slice(1); }],
    ['wrong file identity', f => { f.state.files[0].sha = '1'.repeat(40); }],
    ['unavailable GitHub evidence', f => { f.dependencies.api = () => { throw new Error('HTTP 503'); }; }],
])
{
    test(`never writes readiness after ${name}`, async t =>
    {
        const f = await fixture(t);
        mutate(f);
        await assert.rejects(prepare(f.options, f.dependencies));
        await assert.rejects(fs.stat(path.join(f.options.output, 'manifest.json')), { code: 'ENOENT' });
    });
}

test('rejects an interrupted directory and an explicit wrong target head', async t =>
{
    const f = await fixture(t);
    await fs.mkdir(f.options.output);
    await assert.rejects(prepare(f.options, f.dependencies));
    await assert.rejects(prepare({ ...f.options, check: true }, f.dependencies));
    await assert.rejects(prepare({ ...f.options, head: f.baseTip }, f.dependencies), /expected frozen head/);
});

test('rejects suffix, directory, case and Windows filename aliases', () =>
{
    for (const names of [
        ['x', 'x.source/child'], ['x.source/child', 'x'],
        ['Path.cs', 'path.cs'], ['src/CON.cs'], ['src/a:stream'], ['../escape'],
    ])
    {
        assert.throws(() => checkPaths(names));
    }
    assert.doesNotThrow(() => checkPaths(['src/File.cs', 'src/AGENTS.md', '.github/copilot-instructions.md']));
});
