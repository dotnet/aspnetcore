// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { cpSync, mkdirSync, mkdtempSync, readFileSync, rmSync, unlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { parse } from '../evaluation-tools/node_modules/yaml/dist/index.js';
import { runEval } from '../evaluation-tools/node_modules/@microsoft/vally/dist/pipeline/run.js';
import { MockExecutor } from '../evaluation-tools/node_modules/@microsoft/vally/dist/executor/mock-executor.js';
import { DiffEmptyGrader } from '../evaluation-tools/node_modules/@microsoft/vally/dist/graders/static/diff-empty-grader.js';
import { ToolCallGrader } from '../evaluation-tools/node_modules/@microsoft/vally/dist/graders/static/tool-call-grader.js';
import { OutputContainsGrader } from '../evaluation-tools/node_modules/@microsoft/vally/dist/graders/static/output-contains-grader.js';
import { OutputMatchesGrader } from '../evaluation-tools/node_modules/@microsoft/vally/dist/graders/static/output-matches-grader.js';

const baseDir = fileURLToPath(new URL('.', import.meta.url));
const spec = parse(readFileSync(path.join(baseDir, 'guidance-source.vally.yaml'), 'utf8'));
const guide = 'docs/CrossCuttingGuidance.md';
const api = '.github/skills/review-public-api/SKILL.md';

test('The skill freezes local criteria before retrieving PR evidence', () => {
    const skill = readFileSync(path.resolve(baseDir, '../../../.github/skills/review-pull-request/SKILL.md'), 'utf8');
    const step1 = skill.indexOf('## Step 1');
    const freeze = skill.indexOf('`git rev-parse --show-toplevel`');
    const prEvidence = skill.indexOf('1. the **exact head SHA**');
    assert.ok(step1 < freeze && freeze < prEvidence, 'Freeze the local repository in Step 1 before PR retrieval');
});

async function acceptsAssertions(caseName, commands, output) {
    const stimulus = spec.stimuli.find(item => item.name === caseName);
    const classes = commands
        ? { 'tool-calls': ToolCallGrader }
        : { 'output-contains': OutputContainsGrader, 'output-matches': OutputMatchesGrader };
    const trajectory = {
        output,
        events: (commands ?? []).flatMap((command, index) => [
            { type: 'tool_call', data: { toolName: 'bash', toolCallId: `${index}`, arguments: { command } } },
            { type: 'tool_result', data: { toolName: 'bash', toolCallId: `${index}`, result: 'Completed command' } },
        ]),
    };
    const configs = stimulus.graders.filter(grader => classes[grader.type]);
    assert.ok(configs.length > 0);
    const grades = await Promise.all(configs.map(config =>
        new classes[config.type]().grade({ trajectory, config: config.config })));

    return grades.every(grade => grade.passed);
}

const sha = '1'.repeat(40);
for (const [caseName, files] of [
    ['committed-main-api-criteria', [guide, api]],
    ['committed-release-criteria', [guide, 'docs/BlazorComponentsGuidance.md']],
    ['missing-committed-guide', [guide]],
]) {
    const pinned = files.map(file => `git -C "/fixture with spaces" show '${sha}:${file}'`);
    test(`${caseName}: recorded literal-SHA reads of the expected paths pass`, async () => {
        assert.equal(await acceptsAssertions(caseName, pinned), true);
    });
    test(`${caseName}: an unrelated hash cannot disguise recorded HEAD reads`, async () => {
        const commands = files.map(file => `printf '%s\\n' ${sha}; git show HEAD:${file}`);
        assert.equal(await acceptsAssertions(caseName, commands), false);
    });
    test(`${caseName}: recorded reads of different paths fail`, async () => {
        assert.equal(await acceptsAssertions(caseName, pinned.map(command =>
            command.replace(".md'", ".md.bak'"))), false);
    });
    test(`${caseName}: an additional recorded HEAD read fails`, async () => {
        assert.equal(await acceptsAssertions(caseName, [...pinned, `git show HEAD:${files[0]}`]), false);
    });
    if (files.length > 1) {
        for (const [index, file] of files.entries()) {
            test(`${caseName}: omitting the recorded ${file} read fails`, async () => {
                assert.equal(await acceptsAssertions(caseName, pinned.filter((_, item) => item !== index)), false);
            });
        }
    }
}

test('The missing-guide assertions accept a leading blocked status and reason', async () => {
    assert.equal(await acceptsAssertions('missing-committed-guide', undefined,
        `**BLOCKED:** ${guide} is missing from committed revision ${sha}.`), true);
});

for (const [name, output] of [
    ['READY with a negated block', `READY - not BLOCKED: ${guide} is missing.`],
    ['formatted READY', `**READY:** not BLOCKED: ${guide} is missing.`],
    ['a READY heading', `# READY\nBLOCKED: ${guide} is missing.`],
    ['a later READY status', `BLOCKED: ${guide} is missing.\nREADY`],
    ['a non-status BLOCKED mention', `${guide} is missing but the review is not BLOCKED.`],
    ['no blocking reason', `BLOCKED: ${guide}`],
]) {
    test(`The missing-guide assertions reject ${name}`, async () => {
        assert.equal(await acceptsAssertions('missing-committed-guide', undefined, output), false);
    });
}

function createFixture(context, newline = '\n') {
    const workDir = mkdtempSync(path.join(tmpdir(), 'reviewer-fixture-'));
    context.after(() => rmSync(workDir, { recursive: true, force: true }));
    for (const file of spec.environment.files) {
        const destination = path.join(workDir, file.dest);
        mkdirSync(path.dirname(destination), { recursive: true });
        cpSync(path.resolve(baseDir, file.src), destination);
    }
    for (const file of [guide, api]) {
        const destination = path.join(workDir, file);
        writeFileSync(destination, readFileSync(destination, 'utf8').replace(/\r?\n/g, newline));
    }

    return workDir;
}

function prepare(workDir, mode) {
    execFileSync(process.execPath, ['fixtures/prepare.mjs', mode], { cwd: workDir, stdio: 'pipe' });
}

for (const [name, newline] of [['LF', '\n'], ['CRLF', '\r\n']]) {
    test(`Fixture preparation preserves ${name} markers and missing-guide state`, context => {
        const workDir = createFixture(context, newline);
        prepare(workDir, 'prepare');
        for (const [file, marker] of [[guide, 'CRITERION'], [api, 'API-CRITERION']]) {
            const content = readFileSync(path.join(workDir, file), 'utf8');
            assert.ok(content.includes(`${newline}- UNCOMMITTED-${marker}${newline}`));
            const committed = execFileSync('git', ['show', `HEAD:${file}`], { cwd: workDir, encoding: 'utf8' });
            assert.ok(committed.includes(`COMMITTED-${marker}`));
            assert.doesNotMatch(committed, /UNCOMMITTED-/);
        }
        prepare(workDir, 'missing-guide');
        assert.match(readFileSync(path.join(workDir, guide), 'utf8'), /COMMITTED-CRITERION/);
        assert.equal(execFileSync('git', ['ls-files', '--', guide], { cwd: workDir, encoding: 'utf8' }), '');
    });
}

for (const [name, replacement] of [
    ['missing heading', '## Missing principles'],
    ['duplicate heading', '## Overarching principles\n## Overarching principles'],
    ['existing marker', '## Overarching principles\nCOMMITTED-CRITERION'],
]) {
    test(`Fixture preparation rejects ${name}`, context => {
        const workDir = createFixture(context);
        const file = path.join(workDir, guide);
        writeFileSync(file, readFileSync(file, 'utf8').replace('## Overarching principles', replacement));
        assert.throws(() => prepare(workDir, 'prepare'), /expected exactly one .* heading and no fixture marker/);
    });
}

test('Fixture preparation rejects an unsupported mode', context => {
    const workDir = createFixture(context);
    assert.throws(() => prepare(workDir, 'invalid'), /Expected prepare or missing-guide fixture mode/);
});

async function checkWorkspace(stimulus, mutate, expected) {
    assert.ok(stimulus.graders.some(grader => grader.type === 'diff-empty'),
        `${stimulus.name} must check workspace preservation`);
    const executor = new MockExecutor();
    const execute = executor.execute.bind(executor);
    executor.execute = async (current, options) => {
        assert.match(readFileSync(path.join(options.workDir, api), 'utf8'), /UNCOMMITTED-API-CRITERION/);
        await mutate(options.workDir);
        return execute(current, options);
    };
    const result = await runEval({
        prompt: stimulus.prompt,
        stimulus,
        skills: [],
        workDir: baseDir,
        executor,
        baseDir,
        environment: {
            ...spec.environment,
            commands: [...spec.environment.commands, ...(stimulus.environment?.commands ?? [])],
        },
    });
    try {
        assert.equal(typeof result.trajectory.diff, 'string', 'The real pipeline must capture the workspace diff');
        const grade = await new DiffEmptyGrader().grade({ trajectory: result.trajectory });
        assert.equal(grade.passed, expected, grade.evidence);
    } finally {
        await result.cleanup();
    }
}

for (const stimulus of spec.stimuli) {
    test(`${stimulus.name}: unchanged post-setup workspace passes`, async () => {
        await checkWorkspace(stimulus, workDir => {
            const content = readFileSync(path.join(workDir, guide), 'utf8');
            if (stimulus.name === 'missing-committed-guide') {
                assert.match(content, /COMMITTED-CRITERION/);
                const tracked = execFileSync('git', ['ls-files', '--', guide], { cwd: workDir, encoding: 'utf8' });
                assert.equal(tracked, '');
            } else {
                assert.match(content, /UNCOMMITTED-CRITERION/);
            }
        }, true);
    });
}

test('Restoring dirty criteria after baseline capture fails', async () => {
    await checkWorkspace(spec.stimuli[0], workDir => {
        execFileSync('git', ['-C', workDir, 'restore', guide, api]);
        assert.doesNotMatch(readFileSync(path.join(workDir, guide), 'utf8'), /UNCOMMITTED-/);
        assert.doesNotMatch(readFileSync(path.join(workDir, api), 'utf8'), /UNCOMMITTED-/);
    }, false);
});

test('Deleting the initially untracked guide after baseline capture fails', async () => {
    const stimulus = spec.stimuli.find(item => item.name === 'missing-committed-guide');
    await checkWorkspace(stimulus, workDir => unlinkSync(path.join(workDir, guide)), false);
});
