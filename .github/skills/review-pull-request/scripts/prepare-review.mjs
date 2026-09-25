// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createHash, randomUUID } from 'node:crypto';
import { execFileSync, spawn } from 'node:child_process';
import { once } from 'node:events';
import * as fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

const script = fileURLToPath(import.meta.url);
const suffix = '.source';
const maximumBlobBytes = 16 * 1024 * 1024;
const fullSha = /^[a-f0-9]{40}$/;
const repositoryName = /^[a-z0-9_.-]+\/[a-z0-9_.-]+$/i;
const componentsOnlyPolicies = new Set([
    'src/Components/AGENTS.md#code-clarity-and-durable-knowledge',
    'src/Components/AGENTS.md#cross-runtime-design-checkpoint',
    'src/Components/AGENTS.md#creating-e2e-tests',
]);
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

export function gitArguments(...args)
{
    return ['-c', 'core.longpaths=true', ...args];
}

function git(directory, ...args)
{
    return run('git', gitArguments('-C', directory, ...args));
}

function objects(directory, ...args)
{
    return run('git', gitArguments('--git-dir', directory, ...args));
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

function sections(markdown, level)
{
    const expression = new RegExp(`^${'#'.repeat(level)} (.+)$`, 'gm');
    const matches = [...markdown.matchAll(expression)];
    return matches.map((match, index) => ({
        name: match[1].trim(),
        body: markdown.slice(match.index, matches[index + 1]?.index ?? markdown.length).trim(),
    }));
}

function anchorFor(title)
{
    return title.toLowerCase().replace(/<[^>]*>/g, '').replace(/[^a-z0-9 _-]/g, '').replace(/ /g, '-');
}

function changedIn(files, expression)
{
    return files.some(file => [file.filename, file.previous_filename]
        .some(name => typeof name === 'string' && expression.test(name)));
}

export function validateGuide(markdown, name)
{
    const groups = sections(markdown, 2);
    for (const heading of ['Overarching principles', 'Topics'])
    {
        requireValue(groups.filter(group => group.name === heading).length === 1,
            `Required guide ${name} needs exactly one ## ${heading} section.`);
    }
    const principles = groups.find(group => group.name === 'Overarching principles').body;
    const topics = groups.find(group => group.name === 'Topics').body;
    requireValue(/^[-*] \S/m.test(principles), `Required guide ${name} has empty overarching principles.`);
    const entries = sections(topics, 3);
    requireValue(entries.length > 0 && new Set(entries.map(entry => entry.name)).size === entries.length
        && entries.every(entry => entry.name && /^[-*] \S/m.test(entry.body)),
    `Required guide ${name} has missing, duplicate, or empty topics.`);
    return { principles, topics: entries.map(entry => entry.name), body: topics };
}

export function guideLinks(text, guidePath, components)
{
    const included = [];
    const context = [];
    const skipped = [];
    for (const line of text.split('\n'))
    {
        for (const match of line.matchAll(/\[[^\]]+\]\(\s*(?:<([^>]+)>|([^\s)]+))(?:\s+(?:"[^"]*"|'[^']*'))?\s*\)/g))
        {
            const destination = match[1] || match[2];
            if (!/\.md(?:#.*)?$/.test(destination))
            {
                continue;
            }
            const [relative, anchor] = destination.split('#');
            if (/^[a-z][a-z0-9+.-]*:/i.test(relative) || relative.startsWith('/'))
            {
                continue;
            }
            const resolved = path.posix.normalize(path.posix.join(path.posix.dirname(guidePath), relative));
            checkPaths([resolved]);
            requireValue(anchor === undefined || /^[a-z0-9-]+$/.test(anchor), `Invalid required policy anchor: ${destination}`);
            const link = { path: resolved, ...(anchor ? { anchor } : {}), guide: guidePath };
            if (/\b(supplemental|implementation\/test references)\b/i.test(line))
            {
                skipped.push({ ...link, reason: 'Supporting source example, not a delegated criterion.' });
            }
            else if (!components && anchor && componentsOnlyPolicies.has(`${resolved}#${anchor}`))
            {
                skipped.push({ ...link, reason: 'Components-only criterion is not applicable to JSInterop-only paths.' });
            }
            else if (anchor)
            {
                included.push(link);
            }
            else
            {
                context.push({ ...link, role: 'context' });
            }
        }
    }
    return { included, context, skipped };
}

export async function contextLinks(links, guidanceRoot, pointers = [])
{
    const context = [];
    for (const link of links)
    {
        const pointer = pointers.find(item => item.path === link.path);
        if (pointer)
        {
            context.push({ ...link, sha256: null, status: 'unreadable', reason: pointer.kind });
            continue;
        }
        try
        {
            const bytes = await fs.readFile(path.join(guidanceRoot, `${link.path}${suffix}`));
            context.push({ ...link, sha256: hash(bytes), status: 'readable' });
        }
        catch (error)
        {
            if (!['ENOENT', 'EACCES', 'EPERM', 'EISDIR'].includes(error.code))
            {
                throw error;
            }
            context.push({
                ...link, sha256: null, status: error.code === 'ENOENT' ? 'missing' : 'unreadable',
                reason: error.code,
            });
        }
    }
    return context;
}

export function resolvePolicy(markdown, anchor, name)
{
    const headings = [...markdown.matchAll(/^(#{1,6}) (.+)$/gm)];
    const matches = headings.filter(match => anchorFor(match[2]) === anchor);
    requireValue(matches.length === 1, `Missing or ambiguous required policy ${name}#${anchor}.`);
    const start = matches[0];
    const end = headings.find(match => match.index > start.index && match[1].length <= start[1].length);
    const body = markdown.slice(start.index, end?.index ?? markdown.length).trim();
    requireValue(body.length > start[0].length, `Empty required policy ${name}#${anchor}.`);
    return body;
}

export async function exportTree(store, commit, destination, selection = () => true)
{
    const entries = treeEntries(store, commit).filter(entry => selection(entry.name));
    checkPaths(entries.map(entry => entry.name));
    await fs.mkdir(destination);
    const blobs = entries.filter(entry => entry.type === 'blob');
    const child = spawn('git', gitArguments('--git-dir', store, 'cat-file', '--batch'), { windowsHide: true });
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
            requireValue(Number(size) <= maximumBlobBytes, `Source blob exceeds 16 MiB: ${entry.name}`);
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
    const names = [...new Set(git(root, 'ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', '*.md', 'AGENTS.md')
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
        workingTreeChanges: git(root, 'status', '--porcelain', '--untracked-files=all', '--', '*.md', 'AGENTS.md').length > 0,
        sha256: digest.digest('hex'), files,
    };
}

export async function prepare(options, dependencies = {})
{
    requireValue(Number(process.versions.node.split('.')[0]) >= 22, 'Node.js 22 or newer is required.');
    requireValue(repositoryName.test(options.repo || '') && /^[1-9]\d*$/.test(String(options.pr)),
        'Cannot resolve a single target repository; specify --repo OWNER/REPO and --pr NUMBER.');
    requireValue(options.output, 'Specify a new --output directory, or --check an existing prepared directory.');
    requireValue(!options.head || fullSha.test(options.head), '--head must be a full immutable commit.');
    requireValue(!options.guidance || !options.guidanceRoot, 'Select either --guidance or --guidance-root.');
    const host = options.hostname || 'github.com';
    requireValue(/^[a-z0-9.-]+$/i.test(host), 'Invalid GitHub hostname.');
    run('git', gitArguments('--version'));
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
        requireValue(pull.number === Number(options.pr) && pull.state === 'open' && pull.base?.repo?.id === repository.id
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
        requireValue(manifest.version === 2 && manifest.ready === true && manifest.producer === producer
            && manifest.suffix === suffix && JSON.stringify(manifest.target) === JSON.stringify(frozen.identity),
        'Prepared input is stale, mismatched, or from a different preparation version.');
        requireValue(JSON.stringify(Object.keys(manifest.sources || {}).sort()) === JSON.stringify(['baseTip', 'head', 'mergeBase'])
            && manifest.guidance?.root === 'guidance'
            && JSON.stringify(Object.keys(manifest.artifacts || {}).sort()) === JSON.stringify(['diff.patch', 'feedback.json', 'files.json', 'pull.json'])
            && Array.isArray(manifest.guides) && Array.isArray(manifest.policies)
            && Array.isArray(manifest.context) && Array.isArray(manifest.exclusions)
            && Array.isArray(manifest.skippedLinks),
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
        const changed = JSON.parse(await fs.readFile(path.join(output, 'files.json'), 'utf8'));
        const required = ['docs/CrossCuttingGuidance.md', ...(changedIn(changed,
            /^src\/(Components|JSInterop)\//) ? ['docs/BlazorComponentsGuidance.md'] : [])];
        requireValue(JSON.stringify(manifest.guides.map(guide => guide.path)) === JSON.stringify(required),
            'Prepared guide routing is incomplete.');
        const included = [];
        const context = [];
        const skipped = [];
        for (const guide of manifest.guides)
        {
            const body = await fs.readFile(path.join(output, 'guidance', `${guide.path}${suffix}`), 'utf8');
            const actual = validateGuide(body, guide.path);
            requireValue(JSON.stringify(actual.topics) === JSON.stringify(guide.topics), `Prepared guide topics changed: ${guide.path}`);
            const links = guideLinks(body, guide.path, changedIn(changed, /^src\/Components\//));
            included.push(...links.included);
            context.push(...links.context);
            skipped.push(...links.skipped);
        }
        requireValue(JSON.stringify(skipped) === JSON.stringify(manifest.skippedLinks),
            'Prepared guidance links are incompletely classified.');
        requireValue(JSON.stringify(included.map(link => JSON.stringify(link))) ===
            JSON.stringify(manifest.policies.map(({ path: policyPath, anchor, guide }) =>
                JSON.stringify({ path: policyPath, anchor, guide }))),
        'Prepared required policy inputs are incomplete.');
        requireValue(JSON.stringify(await contextLinks(context, path.join(output, 'guidance'), manifest.guidance.pointers)) ===
            JSON.stringify(manifest.context), 'Prepared guidance context is incompletely classified or changed.');
        for (const policy of manifest.policies)
        {
            const actual = resolvePolicy(await fs.readFile(path.join(output, 'guidance', `${policy.path}${suffix}`), 'utf8'),
                policy.anchor, policy.path);
            requireValue(actual === policy.body, `Prepared policy clauses changed: ${policy.path}#${policy.anchor}`);
        }
        const instructions = '.github/copilot-instructions.md';
        requireValue(manifest.exclusions.length === 2 &&
            manifest.exclusions[1].body === resolvePolicy(
                await fs.readFile(path.join(output, 'guidance', `${instructions}${suffix}`), 'utf8'),
                'security-concerns-are-out-of-scope', instructions),
        'Prepared exclusions do not match the trusted instruction snapshot.');
        return manifest;
    }
    await fs.mkdir(output);
    const store = path.join(output, '.objects');
    run('git', gitArguments('init', '--bare', '--quiet', '--object-format=sha1', store));
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
    async function paginate(uri)
    {
        const items = [];
        for (let page = 1;; page++)
        {
            const batch = await api(`${endpoint}/${uri}?per_page=100&page=${page}`);
            requireValue(Array.isArray(batch), `Invalid review feedback at ${uri}.`);
            items.push(...batch);
            if (batch.length < 100)
            {
                return items;
            }
        }
    }
    const feedback = {
        comments: await paginate(`issues/${options.pr}/comments`),
        reviews: await paginate(`pulls/${options.pr}/reviews`),
        inline: await paginate(`pulls/${options.pr}/comments`),
    };
    await write(output, 'feedback.json', JSON.stringify(feedback, null, 2) + '\n');
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
        guidance = { ...guidance, root: 'guidance', ...await exportTree(store, guidance.commit, path.join(output, 'guidance'),
            name => name.endsWith('.md')) };
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
    const guides = [];
    const policies = [];
    const context = [];
    const components = changedIn(files, /^src\/Components\//);
    const exclusions = [
        { scope: 'Running PR code, tests, CI, browser workflows, or implementation samples',
            reason: 'This is a source-only review; assess changed tests and contracts from source.' },
    ];
    const instructionPath = '.github/copilot-instructions.md';
    const instruction = await fs.readFile(path.join(output, 'guidance', `${instructionPath}${suffix}`), 'utf8');
    exclusions.push({
        source: `${instructionPath}#security-concerns-are-out-of-scope`,
        body: resolvePolicy(instruction, 'security-concerns-are-out-of-scope', instructionPath),
    });
    const skippedLinks = [];
    for (const name of ['docs/CrossCuttingGuidance.md', ...(changedIn(files, /^src\/(Components|JSInterop)\//)
        ? ['docs/BlazorComponentsGuidance.md'] : [])])
    {
        const body = await fs.readFile(path.join(output, 'guidance', `${name}${suffix}`), 'utf8');
        const parsed = validateGuide(body, name);
        guides.push({ path: name, topics: parsed.topics });
        const links = guideLinks(body, name, components);
        skippedLinks.push(...links.skipped);
        context.push(...await contextLinks(links.context, path.join(output, 'guidance'), guidance.pointers));
        for (const link of links.included)
        {
            const target = await fs.readFile(path.join(output, 'guidance', `${link.path}${suffix}`), 'utf8');
            policies.push({ ...link, body: resolvePolicy(target, link.anchor, link.path) });
        }
    }
    requireValue(JSON.stringify((await freeze()).identity) === JSON.stringify(frozen.identity),
        'The target or base branch moved during preparation; no ready manifest was written.');
    const artifacts = {};
    for (const name of ['diff.patch', 'files.json', 'pull.json', 'feedback.json'])
    {
        artifacts[name] = hash(await fs.readFile(path.join(output, name)));
    }
    const manifest = {
        version: 2, ready: true, producer, target: frozen.identity, suffix, sources, guidance,
        guides, policies, context, skippedLinks, exclusions, artifacts,
        limitations: 'Tracked Git bytes only. Symlinks, submodules and LFS pointers are inert data and cannot establish their target behavior.',
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
        const resolved = { ...values, guidanceRoot: values['guidance-root'] };
        if (!resolved.repo)
        {
            const remote = run('git', gitArguments('remote', 'get-url', 'origin'), { cwd: process.cwd() }).toString('utf8').trim();
            const match = remote.match(/^(?:https:\/\/github\.com\/|git@github\.com:)([a-z0-9_.-]+\/[a-z0-9_.-]+?)(?:\.git)?$/i);
            requireValue(match, 'Ambiguous or unavailable checkout repository; specify --repo OWNER/REPO.');
            resolved.repo = match[1];
        }
        resolved.output ||= path.join(os.tmpdir(), `review-bundle-${randomUUID()}`);
        const result = await prepare(resolved);
        console.log(JSON.stringify({ manifest: path.join(path.resolve(resolved.output), 'manifest.json'), target: result.target, ready: true }));
    }
    catch (error)
    {
        console.error(`BLOCKED: ${error.message}`);
        process.exitCode = 1;
    }
}
