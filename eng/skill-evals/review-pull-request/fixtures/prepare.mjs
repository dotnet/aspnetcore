// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';

const guide = 'docs/CrossCuttingGuidance.md';
const api = '.github/skills/review-public-api/SKILL.md';
const mode = process.argv[2];

function git(...args) {
    return execFileSync('git', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'] });
}

function commit(message) {
    git('-c', 'core.hooksPath=fixtures/no-hooks', '-c', 'commit.gpgsign=false', 'commit', '-qm', message);
}

if (process.argv.length !== 3 || !['prepare', 'missing-guide'].includes(mode)) {
    throw new Error('Expected prepare or missing-guide fixture mode.');
}

if (mode === 'prepare') {
    if (existsSync('.git')) {
        throw new Error('Prepare requires an isolated workspace without an existing Git repository.');
    }

    const markedFiles = [
        [guide, '## Overarching principles', 'COMMITTED-CRITERION'],
        [api, '## The rules reviewers apply most (highlights)', 'COMMITTED-API-CRITERION'],
    ].map(([file, heading, marker]) => {
        const content = readFileSync(file, 'utf8');
        const newline = content.includes('\r\n') ? '\r\n' : '\n';
        const anchor = heading + newline;
        if (content.split(anchor).length !== 2 || content.includes(marker)) {
            throw new Error(`${file}: expected exactly one '${heading}' heading and no fixture marker.`);
        }

        return [file, content.replace(anchor, `${anchor}${newline}- ${marker}${newline}`)];
    });

    git('init', '-q', '-b', 'eval-review');
    git('config', 'user.name', 'Skill eval');
    git('config', 'user.email', 'skill-eval@example.invalid');
    for (const [file, content] of markedFiles) {
        writeFileSync(file, content);
    }
    git('add', 'docs', '.github', 'src', 'CONTRIBUTING.md');
    commit('Fixture');
    for (const [file, content] of markedFiles) {
        writeFileSync(file, content.replace('COMMITTED-', 'UNCOMMITTED-'));
    }
} else {
    const committed = git('show', `HEAD:${guide}`);
    if (!committed.includes('COMMITTED-CRITERION') ||
        !readFileSync(guide, 'utf8').includes('UNCOMMITTED-CRITERION')) {
        throw new Error('Missing-guide mode requires the prepared committed and dirty guide markers.');
    }

    git('rm', '-qf', guide);
    commit('Remove committed guide');
    writeFileSync(guide, committed);
}
