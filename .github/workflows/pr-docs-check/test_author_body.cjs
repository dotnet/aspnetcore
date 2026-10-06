const assert = require('node:assert/strict');
const { test } = require('node:test');
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { authorBody } = require('./author_body.cjs');

const original = 'Source: dotnet/aspnetcore#42\n\nGenerated explanation\n\nHuman edits\n\n> gh-aw attribution\n';
const author = { type: 'User', login: 'contributor' };
const update = (body, user = author) => authorBody(body, user, 'dotnet/aspnetcore', 42);

test('new drafts retain the whole generated description and gh-aw attribution', () => {
  const body = update(original);
  assert.ok(body.startsWith(original));
  assert.ok(body.includes('@contributor, this draft documents your source change'));
  assert.ok(body.includes('Please inspect this draft'));
});

test('repeated attribution is byte-for-byte idempotent', () => {
  const body = update(original);
  assert.equal(update(body), body);
  assert.equal(body.split('@contributor').length - 1, 1);
});

test('updated drafts preserve human edits before and after the managed section', () => {
  const body = `${update(original)}\n\nLater human note`;
  const updated = update(body, { type: 'User', login: 'another-author' });
  assert.ok(updated.startsWith(original));
  assert.ok(updated.endsWith('\n\nLater human note'));
  assert.ok(updated.includes('@another-author'));
  assert.ok(!updated.includes('@contributor'));
  assert.equal(update(updated, { type: 'User', login: 'another-author' }), updated);
});

test('duplicate complete managed sections collapse without deleting adjacent human text', () => {
  const section = update('').slice(2);
  const body = `${original}${section}\nHuman note\n${section}\nHuman tail`;
  const updated = update(body);
  assert.equal(updated.split('@contributor').length - 1, 1);
  assert.ok(updated.includes('\nHuman note\n'));
  assert.ok(updated.endsWith('\nHuman tail'));
});

test('bot and missing authors are not mentioned', () => {
  for (const user of [
    { type: 'Bot', login: 'service[bot]' },
    { type: 'Bot', login: 'service' },
    { type: 'User', login: 'service[bot]' },
    null,
  ]) {
    assert.equal(update(original, user), original);
  }
});

test('malformed sections and invalid identities fail without replacing human text', () => {
  for (const body of [
    `${original}<!-- aspnetcore-pr-docs-check-author -->`,
    `${original}<!-- /aspnetcore-pr-docs-check-author -->`,
    `${original}<!-- /aspnetcore-pr-docs-check-author --><!-- aspnetcore-pr-docs-check-author --><!-- /aspnetcore-pr-docs-check-author -->`,
    `${original}<!-- aspnetcore-pr-docs-check-author --><!-- aspnetcore-pr-docs-check-author --><!-- /aspnetcore-pr-docs-check-author -->`,
  ]) {
    assert.throws(() => update(body), /managed author section/);
  }
  assert.throws(() => update(original, { type: 'User', login: 'not a login' }), /Invalid GitHub author/);
  assert.throws(() => authorBody(original, author, 'other/repo', 42), /Invalid source identity/);
});

for (const action of ['created', 'updated']) {
  test(`trusted notification step updates ${action} draft descriptions, not comments`, async () => {
    const workflow = process.env.WORKFLOW_BASELINE_REF
      ? execFileSync('git', ['show', `${process.env.WORKFLOW_BASELINE_REF}:.github/workflows/pr-docs-check.md`], { encoding: 'utf8' })
      : fs.readFileSync(path.join(__dirname, '..', 'pr-docs-check.md'), 'utf8');
    const step = workflow.split('- name: Notify source author on docs pull request')[1].split('\npre-agent-steps:')[0];
    const script = step.split('script: |\n')[1].split('\n').map(line => line.replace(/^ {14}/, '')).join('\n');
    const run = new Function('github', 'core', 'require', `return (async () => {${script}\n})();`);
    const saved = { ...process.env };
    try {
      process.env.EXPECTED_SOURCE_REPOSITORY = 'dotnet/aspnetcore';
      process.env.EXPECTED_SOURCE_PR_NUMBER = '42';
      process.env.SOURCE_AUTHOR = 'contributor';
      process.env.CANONICAL_OUTCOME_PATH = 'outcome.json';
      const outcome = { render_kind: 'drafted', docs_pr_number: 9, docs_pr_action: action };
      const metadata = {
        number: 9,
        state: 'open',
        draft: true,
        body: original,
        title: '[docs] Update guidance',
        user: { login: 'aspnetcore-docs-bot[bot]' },
        base: { ref: 'main', repo: { full_name: 'dotnet/AspNetCore.Docs' } },
        head: { ref: 'docs/aspnetcore-pr-42', repo: { full_name: 'dotnet/AspNetCore.Docs.Automation' } },
        labels: [{ name: 'documentation' }],
      };
      const updates = [];
      const comments = [];
      const github = {
        paginate: async () => [],
        rest: {
          pulls: {
            get: async () => ({ data: { ...metadata, body: `${metadata.body}\nLatest human edit` } }),
            update: async request => updates.push(request),
          },
          issues: {
            createComment: async request => comments.push(request),
            updateComment: async request => comments.push(request),
          },
        },
      };
      const mockRequire = name => name === 'fs'
        ? { readFileSync: file => JSON.stringify(file === 'outcome.json' ? outcome : metadata) }
        : { authorBody };
      await run(github, {}, mockRequire);
      assert.equal(updates.length, 1, `${action} must update the description`);
      assert.equal(comments.length, 0, `${action} must not add an author comment`);
      assert.ok(updates[0].body.includes('@contributor'));
      assert.ok(updates[0].body.startsWith(original));
      assert.ok(updates[0].body.includes('Latest human edit'));
      metadata.body = updates[0].body;
      github.rest.pulls.get = async () => ({ data: metadata });
      await run(github, {}, mockRequire);
      assert.equal(updates.length, 1, `${action} repeated attribution must not write again`);
      github.rest.pulls.get = async () => ({ data: { ...metadata, draft: false } });
      await assert.rejects(run(github, {}, mockRequire), /identity changed after outcome validation/);
      assert.equal(updates.length, 1, `${action} changed PR identity must not be mutated`);
      process.env.SOURCE_AUTHOR = '';
      github.rest.pulls.get = async () => assert.fail('Bot authors must not read or mutate the docs PR');
      await run(github, {}, mockRequire);
      assert.equal(updates.length, 1);
    } finally {
      process.env = saved;
    }
  });
}
