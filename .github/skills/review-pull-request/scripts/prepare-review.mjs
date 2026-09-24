// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createHash } from 'node:crypto';
import { execFileSync, spawn } from 'node:child_process';
import { once } from 'node:events';
import * as fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

const script = fileURLToPath(import.meta.url);
const suffix = '.source';
const fullSha = /^[a-f0-9]{40}$/;
const repositoryName = /^[a-z0-9_.-]+\/[a-z0-9_.-]+$/i;
const hash = (bytes, algorithm = 'sha256') => createHash(algorithm).update(bytes).digest('hex');
const blobHash = bytes => createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex');

function requireValue(condition, message)
{
    if (!condition)
    {
        throw new Error(message);
    }
}

function run(command, args, options = {})
{
    try
    {
        return execFileSync(command, args, {
            maxBuffer: 64 * 1024 * 1024,
            windowsHide: true,
            ...options,
            env: { ...process.env, GIT_TERMINAL_PROMPT: '0', ...options.env },
        });
    }
    catch (error)
    {
        throw new Error(`${command} failed: ${error.stderr?.toString().trim() || error.message}`, { cause: error });
    }
}

function git(directory, ...args)
{
    return run('git', ['-C', directory, ...args]);
}

function objects(directory, ...args)
{
    return run('git', ['--git-dir', directory, ...args]);
}

export function checkPaths(names)
{
    const files = new Set();
    const directories = new Set();
    for (const name of names)
    {
        const parts = name.split('/');
        requireValue(parts.every(part => part && part !== '.' && part !== '..'
            && !/[<>:"\\|?*\x00-\x1f]/.test(part) && !/[ .]$/.test(part)
            && !/^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/i.test(part)),
        `Cannot export this path portably: ${name}`);
        const output = `${name}${suffix}`.normalize('NFC').toLowerCase();
        requireValue(!files.has(output) && !directories.has(output), `Export path collision: ${name}`);
        files.add(output);
        const parents = output.split('/');
        parents.pop();
        while (parents.length)
        {
            const parent = parents.join('/');
            requireValue(!files.has(parent), `Export file/directory collision: ${name}`);
            directories.add(parent);
            parents.pop();
        }
    }
}

async function write(directory, name, bytes)
{
    const destination = path.join(directory, name);
    await fs.mkdir(path.dirname(destination), { recursive: true });
    await fs.writeFile(destination, bytes, { flag: 'wx', mode: 0o600 });
}

async function walk(directory, prefix = '')
{
    const result = [];
    for (const entry of await fs.readdir(path.join(directory, prefix), { withFileTypes: true }))
    {
        const name = prefix ? `${prefix}/${entry.name}` : entry.name;
        requireValue(!entry.isSymbolicLink(), `Prepared input became a symlink: ${name}`);
        if (entry.isDirectory())
        {
            result.push(...await walk(directory, name));
        }
        else
        {
            requireValue(entry.isFile(), `Prepared input is not an ordinary file: ${name}`);
            result.push(name);
        }
    }
    return result.sort();
}

async function directoryDigest(directory)
{
    const digest = createHash('sha256');
    const names = await walk(directory);
    for (const name of names)
    {
        digest.update(`${name}\0${hash(await fs.readFile(path.join(directory, name)))}\n`);
    }
    return { files: names.length, sha256: digest.digest('hex') };
}

function treeEntries(store, commit)
{
    return objects(store, 'ls-tree', '-r', '-z', '--full-tree', commit).toString('utf8').split('\0')
        .filter(Boolean).map(line =>
        {
            const tab = line.indexOf('\t');
            const [mode, type, sha] = line.slice(0, tab).split(' ');
            requireValue(tab > 0 && fullSha.test(sha) && ['blob', 'commit'].includes(type),
                'Malformed Git tree entry.');
            return { mode, type, sha, name: line.slice(tab + 1) };
        });
}

export async function exportTree(store, commit, destination, markdownOnly = false)
{
    const entries = treeEntries(store, commit).filter(entry => !markdownOnly || entry.name.endsWith('.md'));
    checkPaths(entries.map(entry => entry.name));
    await fs.mkdir(destination);
    const blobs = entries.filter(entry => entry.type === 'blob');
    const child = spawn('git', ['--git-dir', store, 'cat-file', '--batch'], { windowsHide: true });
    const completed = once(child, 'close');
    let stderr = '';
    child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });
    const chunks = child.stdout[Symbol.asyncIterator]();
    let pending = Buffer.alloc(0);
    async function take(size)
    {
        while (pending.length < size)
        {
            const next = await chunks.next();
            requireValue(!next.done, `Incomplete Git blob stream: ${stderr}`);
            pending = Buffer.concat([pending, next.value]);
        }
        const result = pending.subarray(0, size);
        pending = pending.subarray(size);
        return result;
    }
    child.stdin.end(blobs.map(entry => `${entry.sha}\n`).join(''));
    const pointers = [];
    try
    {
        for (const entry of blobs)
        {
            let header = '';
            for (let byte; (byte = await take(1))[0] !== 10;)
            {
                header += byte.toString('ascii');
            }
            const [sha, type, size] = header.split(' ');
            requireValue(sha === entry.sha && type === 'blob' && /^\d+$/.test(size), 'Unexpected Git blob response.');
            const body = await take(Number(size));
            requireValue((await take(1))[0] === 10 && blobHash(body) === entry.sha, `Blob mismatch: ${entry.name}`);
            await write(destination, `${entry.name}${suffix}`, body);
            if (entry.mode === '120000' || body.subarray(0, 43).toString().startsWith('version https://git-lfs.github.com/spec/v1'))
            {
                pointers.push({ path: entry.name, kind: entry.mode === '120000' ? 'symlink-text' : 'lfs-pointer' });
            }
        }
        requireValue((await completed)[0] === 0, `Git blob export failed: ${stderr}`);
    }
    finally
    {
        if (child.exitCode === null)
        {
            child.kill();
        }
    }
    for (const entry of entries.filter(entry => entry.type === 'commit'))
    {
        await write(destination, `${entry.name}${suffix}`, `Unmaterialized submodule commit: ${entry.sha}\n`);
        pointers.push({ path: entry.name, kind: 'submodule', commit: entry.sha });
    }
    return { ...await directoryDigest(destination), pointers };
}

async function localGuidance(root)
{
    root = git(root, 'rev-parse', '--show-toplevel').toString().trim();
    const names = [...new Set(git(root, 'ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', '*.md')
        .toString('utf8').split('\0').filter(Boolean))].sort();
    const files = [];
    for (const name of names)
    {
        let stat;
        try
        {
            stat = await fs.lstat(path.join(root, name));
        }
        catch (error)
        {
            if (error.code === 'ENOENT')
            {
                continue; // A tracked deletion is part of the selected working tree.
            }
            throw error;
        }
        requireValue(stat.isFile(), `Guidance must be an ordinary file: ${name}`);
        files.push({ name, body: await fs.readFile(path.join(root, name)) });
    }
    checkPaths(files.map(file => file.name));
    const digest = createHash('sha256');
    for (const name of files.map(file => `${file.name}${suffix}`).sort())
    {
        const file = files.find(file => `${file.name}${suffix}` === name);
        digest.update(`${name}\0${hash(file.body)}\n`);
    }
    return {
        mode: 'local', originalRoot: root,
        checkoutCommit: git(root, 'rev-parse', 'HEAD').toString().trim(),
        workingTreeChanges: git(root, 'status', '--porcelain', '--untracked-files=all', '--', '*.md').length > 0,
        sha256: digest.digest('hex'), files,
    };
}

export async function prepare(options, dependencies = {})
{
    requireValue(Number(process.versions.node.split('.')[0]) >= 22, 'Node.js 22 or newer is required.');
    requireValue(repositoryName.test(options.repo || '') && /^[1-9]\d*$/.test(String(options.pr)),
        'Specify --repo OWNER/REPO and --pr NUMBER; the invocation branch is not a review target.');
    requireValue(options.output, 'Specify a new --output directory, or --check an existing prepared directory.');
    requireValue(!options.head || fullSha.test(options.head), '--head must be a full immutable commit.');
    requireValue(!options.guidance || !options.guidanceRoot, 'Select either --guidance or --guidance-root.');
    const host = options.hostname || 'github.com';
    requireValue(/^[a-z0-9.-]+$/i.test(host), 'Invalid GitHub hostname.');
    run('git', ['--version']);
    run('gh', ['--version']);
    const api = dependencies.api || ((endpoint, accept) =>
    {
        const args = ['api', '--hostname', host, endpoint];
        if (accept)
        {
            args.push('-H', `Accept: ${accept}`);
        }
        const bytes = run('gh', args);
        return accept ? bytes : JSON.parse(bytes);
    });
    const repository = await api(`repos/${options.repo}`);
    requireValue(repositoryName.test(repository.full_name) && Number.isSafeInteger(repository.id), 'Invalid target repository metadata.');
    const endpoint = `repos/${repository.full_name}`;
    async function freeze()
    {
        const pull = await api(`${endpoint}/pulls/${options.pr}`);
        requireValue(pull.number === Number(options.pr) && pull.base?.repo?.id === repository.id
            && repositoryName.test(pull.head?.repo?.full_name || '') && fullSha.test(pull.head.sha),
        'GitHub did not identify the requested PR and its head repository.');
        requireValue(!options.head || options.head === pull.head.sha, 'The live PR head differs from the expected frozen head.');
        const base = await api(`${endpoint}/git/ref/heads/${encodeURIComponent(pull.base.ref)}`);
        requireValue(fullSha.test(base.object?.sha || ''), 'The base branch did not resolve to a full commit.');
        const comparison = await api(`${endpoint}/compare/${base.object.sha}...${pull.head.sha}`);
        requireValue(comparison.base_commit?.sha === base.object.sha && fullSha.test(comparison.merge_base_commit?.sha || ''),
            'GitHub did not return the expected immutable comparison identities.');
        return {
            identity: {
                hostname: host, repository: repository.full_name, repositoryId: repository.id, pr: pull.number,
                headRepository: pull.head.repo.full_name, head: pull.head.sha,
                baseRepository: pull.base.repo.full_name, baseRef: pull.base.ref,
                baseTip: base.object.sha, mergeBase: comparison.merge_base_commit.sha,
            },
            pull,
        };
    }
    const frozen = await freeze();
    const output = path.resolve(options.output);
    const producer = hash(await fs.readFile(script));
    let guidance;
    if (options.guidance)
    {
        const [repo, commit, extra] = options.guidance.split('@');
        requireValue(!extra && repositoryName.test(repo || '') && fullSha.test(commit || ''), '--guidance requires OWNER/REPO@FULL_COMMIT.');
        const selected = await api(`repos/${repo}`);
        requireValue(repositoryName.test(selected.full_name), 'Invalid guidance repository.');
        guidance = { mode: 'remote', repository: selected.full_name, commit };
    }
    else
    {
        guidance = await localGuidance(options.guidanceRoot || process.cwd());
    }
    if (options.check)
    {
        const manifest = JSON.parse(await fs.readFile(path.join(output, 'manifest.json'), 'utf8'));
        requireValue(manifest.version === 1 && manifest.ready === true && manifest.producer === producer
            && manifest.suffix === suffix && JSON.stringify(manifest.target) === JSON.stringify(frozen.identity),
        'Prepared input is stale, mismatched, or from a different preparation version.');
        requireValue(JSON.stringify(Object.keys(manifest.sources || {}).sort()) === JSON.stringify(['baseTip', 'head', 'mergeBase'])
            && manifest.guidance?.root === 'guidance'
            && JSON.stringify(Object.keys(manifest.artifacts || {}).sort()) === JSON.stringify(['diff.patch', 'files.json', 'pull.json']),
        'Prepared manifest omits required inputs.');
        for (const role of ['head', 'mergeBase', 'baseTip'])
        {
            requireValue(manifest.sources[role].commit === frozen.identity[role]
                && manifest.sources[role].root === `source/${frozen.identity[role]}`, `Prepared source has the wrong role: ${role}`);
        }
        for (const key of guidance.mode === 'local'
            ? ['mode', 'originalRoot', 'checkoutCommit', 'workingTreeChanges', 'sha256']
            : ['mode', 'repository', 'commit'])
        {
            requireValue(manifest.guidance[key] === guidance[key], `Prepared guidance mismatch: ${key}`);
        }
        for (const item of [...Object.values(manifest.sources), manifest.guidance])
        {
            requireValue(!path.isAbsolute(item.root) && !item.root.split('/').includes('..'), 'Invalid prepared root.');
            const actual = await directoryDigest(path.join(output, item.root));
            requireValue(actual.sha256 === item.sha256 && actual.files === item.files, `Incomplete or modified prepared source: ${item.root}`);
        }
        for (const [name, digest] of Object.entries(manifest.artifacts))
        {
            requireValue(!name.includes('/') && hash(await fs.readFile(path.join(output, name))) === digest, `Incomplete or modified input: ${name}`);
        }
        return manifest;
    }
    await fs.mkdir(output);
    const store = path.join(output, '.objects');
    run('git', ['init', '--bare', '--quiet', '--object-format=sha1', store]);
    objects(store, 'config', 'core.hooksPath', path.join(store, 'disabled-hooks'));
    const fetch = dependencies.fetch || ((repo, commits) =>
        objects(store, '-c', 'credential.helper=', '-c', 'credential.helper=!gh auth git-credential',
            'fetch', '--quiet', '--no-tags', '--depth=1', `https://${host}/${repo}.git`, ...commits));
    const groups = new Map();
    for (const [repo, commit] of [
        [frozen.identity.headRepository, frozen.identity.head],
        [frozen.identity.baseRepository, frozen.identity.baseTip],
        [frozen.identity.baseRepository, frozen.identity.mergeBase],
        ...(guidance.mode === 'remote' ? [[guidance.repository, guidance.commit]] : []),
    ])
    {
        groups.set(repo, [...new Set([...(groups.get(repo) || []), commit])]);
    }
    for (const [repo, commits] of groups)
    {
        await fetch(repo, commits, store);
    }
    const files = [];
    for (let page = 1;; page++)
    {
        const batch = await api(`${endpoint}/pulls/${options.pr}/files?per_page=100&page=${page}`);
        requireValue(Array.isArray(batch), 'GitHub returned an invalid file list.');
        files.push(...batch);
        if (batch.length < 100)
        {
            break;
        }
    }
    requireValue(files.length === frozen.pull.changed_files && new Set(files.map(file => file.filename)).size === files.length,
        'GitHub returned an incomplete or duplicate changed-file list.');
    const changedPaths = objects(store, 'diff', '--no-ext-diff', '--no-textconv', '--no-renames', '--name-only', '-z',
        frozen.identity.mergeBase, frozen.identity.head).toString('utf8').split('\0').filter(Boolean).sort();
    const listedPaths = [...new Set(files.flatMap(file => [file.filename, ...(file.previous_filename ? [file.previous_filename] : [])]))].sort();
    requireValue(JSON.stringify(changedPaths) === JSON.stringify(listedPaths), 'GitHub file list does not match the frozen trees.');
    for (const file of files)
    {
        const side = file.status === 'removed' ? frozen.identity.mergeBase : frozen.identity.head;
        requireValue(objects(store, 'rev-parse', `${side}:${file.filename}`).toString().trim() === file.sha,
            `GitHub file identity does not match the frozen tree: ${file.filename}`);
    }
    const diff = await api(`${endpoint}/pulls/${options.pr}`, 'application/vnd.github.diff');
    requireValue(Buffer.isBuffer(diff), 'GitHub did not return the authoritative diff bytes.');
    await write(output, 'diff.patch', diff);
    objects(store, 'read-tree', frozen.identity.mergeBase);
    if (diff.length)
    {
        objects(store, 'apply', '--cached', '--binary', '--whitespace=nowarn', path.join(output, 'diff.patch'));
    }
    requireValue(objects(store, 'write-tree').toString().trim()
        === objects(store, 'rev-parse', `${frozen.identity.head}^{tree}`).toString().trim(),
    'The authoritative diff does not reconstruct the frozen head; incomplete or unsupported diff.');
    await write(output, 'files.json', JSON.stringify(files, null, 2) + '\n');
    await write(output, 'pull.json', JSON.stringify(frozen.pull, null, 2) + '\n');
    await fs.mkdir(path.join(output, 'source'));
    const sources = {};
    const exported = new Map();
    for (const role of ['head', 'mergeBase', 'baseTip'])
    {
        const commit = frozen.identity[role];
        if (!exported.has(commit))
        {
            const root = `source/${commit}`;
            exported.set(commit, {
                root, commit, tree: objects(store, 'rev-parse', `${commit}^{tree}`).toString().trim(),
                ...await exportTree(store, commit, path.join(output, root)),
            });
        }
        sources[role] = exported.get(commit);
    }
    if (guidance.mode === 'remote')
    {
        guidance = { ...guidance, root: 'guidance', ...await exportTree(store, guidance.commit, path.join(output, 'guidance'), true) };
    }
    else
    {
        const { files: selected, ...provenance } = guidance;
        await fs.mkdir(path.join(output, 'guidance'));
        for (const file of selected)
        {
            await write(path.join(output, 'guidance'), `${file.name}${suffix}`, file.body);
        }
        guidance = { ...provenance, root: 'guidance', ...await directoryDigest(path.join(output, 'guidance')) };
        requireValue((await localGuidance(provenance.originalRoot)).sha256 === guidance.sha256, 'Working-tree guidance changed during preparation.');
    }
    for (const name of ['docs/CrossCuttingGuidance.md', ...(files.some(file => /^src\/(Components|JSInterop)\//.test(file.filename))
        ? ['docs/BlazorComponentsGuidance.md'] : [])])
    {
        requireValue((await fs.readFile(path.join(output, 'guidance', `${name}${suffix}`), 'utf8')).trim(),
            `Required guidance is empty: ${name}`);
    }
    requireValue(JSON.stringify((await freeze()).identity) === JSON.stringify(frozen.identity),
        'The target or base branch moved during preparation; no ready manifest was written.');
    const artifacts = {};
    for (const name of ['diff.patch', 'files.json', 'pull.json'])
    {
        artifacts[name] = hash(await fs.readFile(path.join(output, name)));
    }
    const manifest = {
        version: 1, ready: true, producer, target: frozen.identity, suffix, sources, guidance, artifacts,
        limitations: 'Tracked Git bytes only. Symlinks, submodules and LFS pointers are data, not dereferenced content. Required outside evidence remains incomplete until explicitly sourced.',
    };
    await write(output, 'manifest.pending', JSON.stringify(manifest, null, 2) + '\n');
    await fs.rename(path.join(output, 'manifest.pending'), path.join(output, 'manifest.json'));
    return manifest;
}

if (process.argv[1] && path.resolve(process.argv[1]) === script)
{
    try
    {
        const { values } = parseArgs({ options: {
            repo: { type: 'string' }, pr: { type: 'string' }, output: { type: 'string' },
            head: { type: 'string' }, hostname: { type: 'string' }, guidance: { type: 'string' },
            'guidance-root': { type: 'string' }, check: { type: 'boolean' },
        } });
        const result = await prepare({ ...values, guidanceRoot: values['guidance-root'] });
        console.log(JSON.stringify({ manifest: path.join(path.resolve(values.output), 'manifest.json'), target: result.target, ready: true }));
    }
    catch (error)
    {
        console.error(`BLOCKED: ${error.message}`);
        process.exitCode = 1;
    }
}
