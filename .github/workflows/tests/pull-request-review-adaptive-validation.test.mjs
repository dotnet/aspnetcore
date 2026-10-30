import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '../../..');
const workflowSource = fs.readFileSync(
  path.join(repositoryRoot, '.github/workflows/pull-request-review.md'), 'utf8');
const workflowLock = fs.readFileSync(
  path.join(repositoryRoot, '.github/workflows/pull-request-review.lock.yml'), 'utf8');
const skill = fs.readFileSync(
  path.join(repositoryRoot, '.github/skills/review-pull-request/SKILL.md'), 'utf8');

assert.match(workflowSource, /bash:\s*\[":\*"\]/);
assert.match(workflowSource, /^\s+edit:\s*$/m);
assert.match(workflowSource, /memory:\s*8g/);
assert.match(workflowSource, /- dotnet/);
assert.match(workflowSource, /- "data\.nuget\.org"/);
assert.match(workflowSource, /- playwright/);
assert.match(workflowSource, /worktree=\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowSource, /update-ref refs\/review\/head "\$REVIEW_HEAD"/);
assert.match(workflowSource, /refs\/review\/merge-base:refs\/remotes\/review\/merge-base/);
assert.match(workflowSource, /--depth=1 "\$store"/);
assert.match(workflowSource, /chmod -R a\+rX,a-w \/tmp\/gh-aw\/review-bundle/);
assert.match(workflowSource, /REVIEW_WORKTREE_AVAILABLE=false/);
assert.match(workflowSource, /Only after collecting every required guide worker's actual result/);
assert.match(workflowSource, /write temporary\s+tests or repros/);
assert.doesNotMatch(workflowSource, /review_execution|review-execution|execution\.json/);

assert.match(workflowLock, /--allow-tool shell/);
assert.match(workflowLock, /--allow-tool write/);
assert.match(workflowLock, /--memory-limit 8g/);
assert.match(workflowLock, /GH_AW_TOOL_TIMEOUT: "?1800"?/);
assert.match(workflowLock, /worktree=\/tmp\/gh-aw\/review-worktree/);
assert.match(workflowLock, /refs\/review\/head:refs\/remotes\/review\/head/);
assert.match(workflowLock, /REVIEW_WORKTREE_AVAILABLE=false/);
assert.doesNotMatch(workflowLock, /review_execution|review-execution|review-build-sandbox/);

assert.match(skill, /write temporary\s+tests or repros/);
assert.match(skill, /Temporary validation edits are diagnostic only/);
assert.match(skill, /Workers remain source-only/);

console.log('Validated adaptive in-sandbox pull request review capabilities.');
