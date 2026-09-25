// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import * as fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { checkPaths, exportTree, guideLinks, prepare, resolvePolicy, validateGuide } from '../scripts/prepare-review.mjs';

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

async function fixture(t, components = false)
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
    const valuePath = components ? 'src/Components/Value.cs' : 'src/Value.cs';
    const original = {
        [valuePath]: 'MERGE_VALUE\n',
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
        ...original, [valuePath]: 'HEAD_VALUE\n',
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
    await fs.mkdir(path.join(guidanceRoot, '.github'), { recursive: true });
    await fs.writeFile(path.join(guidanceRoot, '.github/copilot-instructions.md'),
        '# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n');
    git(guidanceRoot, ['init', '--quiet']);
    git(guidanceRoot, ['add', '.']);
    git(guidanceRoot, ['commit', '--quiet', '-m', 'Guidance']);
    await fs.writeFile(path.join(guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n## Overarching principles\n- DIRTY_GUIDANCE\n## Topics\n### Topic\n- Required clause.\n');
    const files = [
        { filename: valuePath, status: 'modified' },
        { filename: 'src/OldName.cs', status: 'removed' },
        { filename: 'src/Renamed.cs', status: 'added' },
        { filename: 'src/Deleted.cs', status: 'removed' },
        { filename: 'src/Mode.cs', status: 'modified' },
    ].map(file => ({ ...file, sha: git(repository, ['rev-parse', `${file.status === 'removed' ? mergeBase : head}:${file.filename}`]) }));
    const pull = {
        number: 42, state: 'open', changed_files: files.length,
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
        if (endpoint.includes('/comments?') || endpoint.includes('/reviews?'))
        {
            return [];
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
    const checkoutBefore = git(f.options.guidanceRoot, ['status', '--porcelain']);
    const manifest = await prepare(f.options, f.dependencies);
    assert.equal(git(f.options.guidanceRoot, ['status', '--porcelain']), checkoutBefore);
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
    assert.equal(manifest.exclusions.length, 2);
    assert.match(manifest.exclusions[1].body, /Do not review the excluded scope/);
    assert.deepEqual(JSON.parse(await fs.readFile(path.join(f.options.output, 'feedback.json'), 'utf8')),
        { comments: [], reviews: [], inline: [] });
    assert.match(await fs.readFile(path.join(f.options.output, 'guidance/docs/CrossCuttingGuidance.md.source'), 'utf8'), /DIRTY_GUIDANCE/);
    assert.equal((await prepare({ ...f.options, check: true }, f.dependencies)).ready, true);
});

for (const baseRef of ['main', 'release/11.0'])
{
    test(`keeps binding base-tip separate from merge base for ${baseRef}`, async t =>
    {
        const f = await fixture(t);
        f.state.pull.base.ref = baseRef;
        const manifest = await prepare(f.options, f.dependencies);
        assert.equal(manifest.target.baseRef, baseRef);
        assert.equal(manifest.target.baseTip, f.baseTip);
        assert.equal(manifest.target.mergeBase, f.mergeBase);
    });
}

test('includes exact required policy section from selected guidance snapshot', async t =>
{
    const f = await fixture(t);
    await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/Policy.md'),
        '# Policy\n## Required clause\n- Only this requirement.\n## Other clause\n- Unrelated.\n');
    await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#required-clause).\n' +
        '## Topics\n### Topic\n- Review changed code.\n');
    const manifest = await prepare(f.options, f.dependencies);
    assert.deepEqual(manifest.policies, [{
        path: 'docs/Policy.md', anchor: 'required-clause', guide: 'docs/CrossCuttingGuidance.md',
        body: '## Required clause\n- Only this requirement.',
    }]);
    manifest.policies = [];
    await fs.writeFile(path.join(f.options.output, 'manifest.json'), JSON.stringify(manifest));
    await assert.rejects(prepare({ ...f.options, check: true }, f.dependencies), /policy inputs are incomplete/);
});

test('missing delegated policy anchor fails before publishing readiness', async t =>
{
    const f = await fixture(t);
    await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/Policy.md'), '# Policy\n## Different\n- A rule.\n');
    await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n## Overarching principles\n- Follow [the requirement](Policy.md#missing).\n' +
        '## Topics\n### Topic\n- Review changed code.\n');
    await assert.rejects(prepare(f.options, f.dependencies), /Missing or ambiguous required policy/);
    await assert.rejects(fs.stat(path.join(f.options.output, 'manifest.json')), { code: 'ENOENT' });
});

test('classifies every repository-relative Markdown link in each routed guide', async () =>
{
    for (const name of ['CrossCuttingGuidance.md', 'BlazorComponentsGuidance.md'])
    {
        const body = await fs.readFile(path.join('docs', name), 'utf8');
        const classified = guideLinks(body, `docs/${name}`, true);
        const count = [...body.matchAll(/\[[^\]]+\]\((?:\.\.?\/)*[^)\s]+\.md(?:#[^)\s]*)?\)/g)].length;
        assert.equal(classified.included.length + classified.context.length + classified.skipped.length, count, name);
        assert.ok(classified.context.every(link => link.role === 'context' && link.guide === `docs/${name}`));
        assert.ok(classified.skipped.every(link => link.reason && link.guide === `docs/${name}`));
    }
    const architecture = guideLinks(await fs.readFile('docs/BlazorComponentsGuidance.md', 'utf8'),
        'docs/BlazorComponentsGuidance.md', true);
    assert.deepEqual(architecture.context, [{
        path: 'src/Components/ARCHITECTURE.md', guide: 'docs/BlazorComponentsGuidance.md', role: 'context',
    }]);
    const sample = '- Apply [binding](Policy.md#binding).\n' +
        '- Orient with [architecture](../src/Components/ARCHITECTURE.md).\n' +
        '- Read [design](<../src/Components/DESIGN.md> "Context").\n' +
        '- External [docs](https://example.com/Policy.md) are not repository-relative.\n' +
        '- Supplemental implementation/test references: [example](Example.md#sample).\n' +
        '- For Components APIs follow [API](../src/Components/AGENTS.md#code-clarity-and-durable-knowledge); generic JSInterop differs.\n';
    const links = guideLinks(sample, 'docs/Guide.md', false);
    assert.deepEqual(links.included.map(link => link.anchor), ['binding']);
    assert.deepEqual(links.context.map(link => link.path),
        ['src/Components/ARCHITECTURE.md', 'src/Components/DESIGN.md']);
    assert.deepEqual(links.skipped.map(link => link.anchor), ['sample', 'code-clarity-and-durable-knowledge']);
    const mixed = (await fs.readFile('docs/BlazorComponentsGuidance.md', 'utf8')).split('\n')
        .find(line => line.includes('For Components E2E work'));
    const jsInterop = guideLinks(mixed, 'docs/BlazorComponentsGuidance.md', false);
    assert.deepEqual(jsInterop.included.map(link => `${link.path}#${link.anchor}`), [
        'CONTRIBUTING.md#tests',
        '.github/copilot-instructions.md#running-tests',
    ]);
    assert.deepEqual(jsInterop.skipped.map(link => `${link.path}#${link.anchor}`), [
        'src/Components/AGENTS.md#creating-e2e-tests',
    ]);
    assert.throws(() => guideLinks('[bad](Policy.md#)', 'docs/Guide.md', true), /Invalid required policy anchor/);
});

for (const area of ['Components', 'JSInterop'])
{
    test(`routes a rename out of ${area} using its previous path`, async t =>
    {
        const f = await fixture(t);
        const oldPath = `src/${area}/Old.cs`;
        const newPath = 'docs/Renamed.cs';
        const base = f.commit({ [oldPath]: 'UNCHANGED_VALUE\n' });
        const head = f.commit({ [newPath]: 'UNCHANGED_VALUE\n' }, base);
        f.state.pull.base.ref = 'main';
        f.state.pull.changed_files = 1;
        f.state.pull.head.sha = head;
        f.state.baseTip = base;
        f.state.mergeBase = base;
        f.state.diff = execFileSync('git', ['--git-dir', f.repository, 'diff', '--binary', base, head]);
        f.state.files = [{
            filename: newPath, previous_filename: oldPath, status: 'renamed',
            sha: git(f.repository, ['rev-parse', `${head}:${newPath}`]),
        }];
        await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/BlazorComponentsGuidance.md'),
            '# Components\n## Overarching principles\n- A rule.\n' +
            '## Topics\n### Tests\n- Follow [Components E2E](../src/Components/AGENTS.md#creating-e2e-tests).\n');
        await fs.mkdir(path.join(f.options.guidanceRoot, 'src/Components'), { recursive: true });
        await fs.writeFile(path.join(f.options.guidanceRoot, 'src/Components/AGENTS.md'),
            '# Components\n## Creating E2E Tests\n- Validate the behavior.\n');
        const manifest = await prepare(f.options, f.dependencies);
        assert.deepEqual(manifest.guides.map(guide => guide.path),
            ['docs/CrossCuttingGuidance.md', 'docs/BlazorComponentsGuidance.md']);
        assert.deepEqual(manifest.policies.map(policy => policy.anchor),
            area === 'Components' ? ['creating-e2e-tests'] : []);
        assert.deepEqual(manifest.skippedLinks.map(link => link.anchor),
            area === 'JSInterop' ? ['creating-e2e-tests'] : []);
        assert.equal((await prepare({ ...f.options, check: true }, f.dependencies)).ready, true);
        const filename = path.join(f.options.output, 'files.json');
        const changed = JSON.parse(await fs.readFile(filename, 'utf8'));
        delete changed[0].previous_filename;
        const bytes = Buffer.from(JSON.stringify(changed, null, 2) + '\n');
        await fs.writeFile(filename, bytes);
        manifest.artifacts['files.json'] = createHash('sha256').update(bytes).digest('hex');
        await fs.writeFile(path.join(f.options.output, 'manifest.json'), JSON.stringify(manifest));
        await assert.rejects(prepare({ ...f.options, check: true }, f.dependencies), /guide routing is incomplete/);
    });
}

for (const baseRef of ['main', 'release/11.0'])
{
    test(`includes readable Components architecture context from selected guidance for ${baseRef}`, async t =>
    {
        const f = await fixture(t, true);
        f.state.pull.base.ref = baseRef;
        await fs.mkdir(path.join(f.options.guidanceRoot, 'src/Components'), { recursive: true });
        await fs.writeFile(path.join(f.options.guidanceRoot, 'src/Components/ARCHITECTURE.md'),
            'REVIEWER_WORKING_TREE_ARCHITECTURE\n');
        await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/BlazorComponentsGuidance.md'),
            '# Components\n[Architecture](../src/Components/ARCHITECTURE.md)\n' +
            '## Overarching principles\n- Apply the full guide.\n## Topics\n### Forms\n- Review binding.\n');
        let options = f.options;
        let architecture = 'REVIEWER_WORKING_TREE_ARCHITECTURE\n';
        if (baseRef === 'release/11.0')
        {
            architecture = 'IMMUTABLE_REVIEWER_ARCHITECTURE\n';
            const commit = f.commit({
                'docs/CrossCuttingGuidance.md':
                    '# Guidance\n## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n',
                'docs/BlazorComponentsGuidance.md':
                    '# Components\n[Architecture](../src/Components/ARCHITECTURE.md)\n' +
                    '## Overarching principles\n- A principle.\n## Topics\n### Forms\n- A rule.\n',
                'src/Components/ARCHITECTURE.md': architecture,
                '.github/copilot-instructions.md':
                    '# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n',
            });
            options = { ...f.options, guidanceRoot: undefined, guidance: `reviewer/guidance@${commit}` };
        }
        const manifest = await prepare(options, f.dependencies);
        assert.deepEqual(manifest.guides.map(guide => guide.path),
            ['docs/CrossCuttingGuidance.md', 'docs/BlazorComponentsGuidance.md']);
        const bytes = await fs.readFile(path.join(options.output,
            'guidance/src/Components/ARCHITECTURE.md.source'));
        assert.equal(bytes.toString(), architecture);
        assert.equal(manifest.guidance.mode, baseRef === 'main' ? 'local' : 'remote');
        if (baseRef === 'release/11.0')
        {
            await assert.rejects(fs.stat(path.join(options.output, manifest.sources.baseTip.root,
                'docs/BlazorComponentsGuidance.md.source')), { code: 'ENOENT' });
        }
        assert.deepEqual(manifest.context, [{
            path: 'src/Components/ARCHITECTURE.md', guide: 'docs/BlazorComponentsGuidance.md',
            role: 'context', sha256: createHash('sha256').update(bytes).digest('hex'),
            status: 'readable',
        }]);
        assert.equal((await prepare({ ...options, check: true }, f.dependencies)).ready, true);
    });
}

test('records missing optional context but rejects an unclassified context on reuse', async t =>
{
    const f = await fixture(t);
    await fs.writeFile(path.join(f.options.guidanceRoot, 'docs/CrossCuttingGuidance.md'),
        '# Guidance\n[Orientation](Missing.md)\n' +
        '## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n');
    const manifest = await prepare(f.options, f.dependencies);
    assert.deepEqual(manifest.context, [{
        path: 'docs/Missing.md', guide: 'docs/CrossCuttingGuidance.md', role: 'context',
        sha256: null, status: 'missing', reason: 'ENOENT',
    }]);
    assert.equal((await prepare({ ...f.options, check: true }, f.dependencies)).ready, true);
    manifest.context = [];
    await fs.writeFile(path.join(f.options.output, 'manifest.json'), JSON.stringify(manifest));
    await assert.rejects(prepare({ ...f.options, check: true }, f.dependencies), /context is incompletely classified/);
});

test('records a guidance symlink as unreadable context rather than treating link text as a document', async t =>
{
    const f = await fixture(t);
    const commit = f.commit({
        'docs/CrossCuttingGuidance.md':
            '# Guidance\n[Architecture](Architecture.md)\n' +
            '## Overarching principles\n- A principle.\n## Topics\n### Topic\n- A rule.\n',
        'docs/Architecture.md': ['120000', 'Other.md'],
        '.github/copilot-instructions.md':
            '# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n',
    });
    const options = { ...f.options, guidanceRoot: undefined, guidance: `reviewer/guidance@${commit}` };
    const manifest = await prepare(options, f.dependencies);
    assert.deepEqual(manifest.context, [{
        path: 'docs/Architecture.md', guide: 'docs/CrossCuttingGuidance.md',
        role: 'context', sha256: null, status: 'unreadable', reason: 'symlink-text',
    }]);
    assert.equal((await prepare({ ...options, check: true }, f.dependencies)).ready, true);
});

test('rejects an oversized source body rather than exporting an incomplete snapshot', async t =>
{
    const f = await fixture(t);
    const commit = f.commit({ 'src/Oversized.cs': 'x'.repeat(17 * 1024 * 1024) });
    await assert.rejects(exportTree(f.repository, commit, path.join(f.root, 'oversized')), /exceeds 16 MiB/);
});

test('supports an immutable remote guidance selection without selecting the local checkout', async t =>
{
    const f = await fixture(t);
    const commit = f.commit({
        'docs/CrossCuttingGuidance.md':
            '# REMOTE_GUIDANCE\n[Architecture](Architecture.md)\n' +
            '## Overarching principles\n- A rule.\n## Topics\n### Topic\n- Another rule.\n',
        'docs/Architecture.md': 'REMOTE_ARCHITECTURE\n',
        '.github/copilot-instructions.md':
            '# Instructions\n## Security Concerns Are Out of Scope\nDo not review the excluded scope.\n',
    });
    const options = { ...f.options, guidanceRoot: undefined, guidance: `reviewer/guidance@${commit}` };
    const manifest = await prepare(options, f.dependencies);
    assert.equal(manifest.guidance.mode, 'remote');
    assert.equal(manifest.guidance.commit, commit);
    assert.match(await fs.readFile(path.join(options.output, 'guidance/docs/CrossCuttingGuidance.md.source'), 'utf8'), /REMOTE_GUIDANCE/);
    assert.deepEqual(manifest.context, [{
        path: 'docs/Architecture.md', guide: 'docs/CrossCuttingGuidance.md', role: 'context',
        sha256: createHash('sha256').update('REMOTE_ARCHITECTURE\n').digest('hex'), status: 'readable',
    }]);
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
    ['missing maintained exclusion', async f =>
    {
        const filename = path.join(f.options.output, 'manifest.json');
        const manifest = JSON.parse(await fs.readFile(filename));
        manifest.exclusions = [];
        await fs.writeFile(filename, JSON.stringify(manifest));
    }],
    ['missing routed guide', async f =>
    {
        const filename = path.join(f.options.output, 'manifest.json');
        const manifest = JSON.parse(await fs.readFile(filename));
        manifest.guides = [];
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

test('rejects malformed guide topics and unresolved policy anchors', () =>
{
    for (const guide of [
        '# Guidance\n## Topics\n### Topic\n- A rule.\n',
        '# Guidance\n## Overarching principles\n- A rule.\n## Topics\n### Topic\nNo bullets.\n',
        '# Guidance\n## Overarching principles\n- A rule.\n## Topics\n### Topic\n- A rule.\n### Topic\n- A rule.\n',
    ])
    {
        assert.throws(() => validateGuide(guide, 'docs/Guide.md'));
    }
    assert.throws(() => resolvePolicy('# Policy\n## Existing\n- A rule.\n', 'missing', 'docs/Policy.md'));
});

for (const [name, mutate] of [
    ['truncated diff', f => { f.state.diff = Buffer.alloc(0); }],
    ['incomplete file list', f => { f.state.files = f.state.files.slice(1); }],
    ['wrong file identity', f => { f.state.files[0].sha = '1'.repeat(40); }],
    ['unavailable feedback', f =>
    {
        const api = f.dependencies.api;
        f.dependencies.api = (endpoint, accept) =>
        {
            if (endpoint.includes('/reviews?'))
            {
                throw new Error('Review feedback unavailable');
            }
            return api(endpoint, accept);
        };
    }],
    ['unavailable Git fetch', f => { f.dependencies.fetch = () => { throw new Error('Git fetch failed'); }; }],
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
