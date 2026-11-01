const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');

const root = path.join(__dirname, '..');
const workflow = fs.readFileSync(path.join(root, 'pr-docs-check.md'), 'utf8');
const compiled = fs.readFileSync(path.join(root, 'pr-docs-check.lock.yml'), 'utf8');
const sourceGate = workflow.match(/^if: \$\{\{ (.+) \}\}$/m)[1];
const compiledGate = compiled.match(/  pre_activation:\r?\n    name: pre_activation\r?\n    if: >\r?\n([\s\S]+?)    runs-on:/)[1].trim();
const repositoryExpression = "github.event_name == 'pull_request_target' && github.repository || inputs.source_repository";
const numberExpression = "github.event_name == 'pull_request_target' && github.event.pull_request.number || inputs.pr_number";

function evaluate(expression, github, inputs)
{
  return new Function('github', 'inputs', `return (${expression});`)(github, inputs);
}

function context(head)
{
  return {
    event_name: 'pull_request_target',
    repository: 'dotnet/aspnetcore',
    event: {
      action: 'closed',
      pull_request: {
        number: 42,
        merged: true,
        base: { ref: 'main' },
        head: { repo: { full_name: head } },
      },
    },
  };
}

for (const head of ['contributor/aspnetcore', 'dotnet/aspnetcore']) {
  test(`actual source and compiled activation expressions accept merged-main from ${head}`, () => {
    const github = context(head);
    const inputs = { pr_number: '999', source_repository: 'other/repo', existing_draft: 'refresh' };
    for (const gate of [sourceGate, compiledGate]) {
      assert.equal(evaluate(gate, github, inputs), true);
    }
    assert.equal(evaluate(repositoryExpression, github, inputs), 'dotnet/aspnetcore');
    assert.equal(evaluate(numberExpression, github, inputs), 42);
  });
}

for (const [name, change] of [
  ['closed unmerged', github => github.event.pull_request.merged = false],
  ['non-main', github => github.event.pull_request.base.ref = 'release/11.0'],
  ['not closed', github => github.event.action = 'opened'],
  ['other repository', github => github.repository = 'other/aspnetcore'],
  ['ordinary pull_request', github => github.event_name = 'pull_request'],
]) {
  test(`actual activation expressions exclude ${name}`, () => {
    const github = context('contributor/aspnetcore');
    change(github);
    for (const gate of [sourceGate, compiledGate]) {
      assert.equal(evaluate(gate, github, {}), false);
    }
  });
}

for (const mode of [undefined, 'skip', 'refresh']) {
  test(`manual dispatch retains source identity with existing_draft=${mode}`, () => {
    const github = { event_name: 'workflow_dispatch', repository: 'dotnet/aspnetcore', event: {} };
    const inputs = { source_repository: 'dotnet/aspnetcore', pr_number: '42', existing_draft: mode };
    for (const gate of [sourceGate, compiledGate]) {
      assert.equal(evaluate(gate, github, inputs), true);
    }
    assert.equal(evaluate(repositoryExpression, github, inputs), 'dotnet/aspnetcore');
    assert.equal(evaluate(numberExpression, github, inputs), '42');
  });
}

test('compiled concurrency and notifications consume the same event-or-dispatch expressions', () => {
  const group = `pr-docs-check-\${{ ${repositoryExpression} }}-\${{ ${numberExpression} }}`;
  assert.ok(compiled.includes(`group: ${group}\n`));
  assert.equal(compiled.split(`EXPECTED_SOURCE_REPOSITORY: \${{ ${repositoryExpression} }}`).length - 1, 3);
  assert.equal(compiled.split(`EXPECTED_SOURCE_PR_NUMBER: \${{ ${numberExpression} }}`).length - 1, 3);
  assert.ok(compiled.includes('EXPECTED_SOURCE_PR_NUMBER: ${{ needs.docs_context.outputs.source_pr_number }}'));
  assert.ok(!compiled.includes('github.event.inputs.source_repository'));
  assert.ok(!compiled.includes('github.event.inputs.pr_number'));
  const triggers = compiled.split('\non:\n', 2)[1].split('\npermissions:', 1)[0].replace(/^\s*#.*$/gm, '');
  assert.match(triggers, /pull_request_target:\s+branches:\s+- main\s+types:\s+- closed/);
  assert.ok(triggers.includes('workflow_dispatch:'));
  assert.ok(!triggers.includes('acknowledge-risk'));
  assert.ok(!triggers.includes('allowed-checkouts'));
});

for (const event of ['workflow_dispatch', 'pull_request_target']) {
  test(`all helper checkouts follow the workflow execution revision for ${event}`, () => {
    const github = {
      ...context('contributor/aspnetcore'),
      event_name: event,
      workflow_sha: 'a'.repeat(40),
      sha: 'b'.repeat(40),
    };
    github.event.pull_request.head.sha = 'c'.repeat(40);
    github.event.pull_request.merge_commit_sha = github.sha;
    for (const text of [workflow, compiled]) {
      const checkouts = [...text.matchAll(/- name: (Check out (?:trusted workflow helpers|trusted outcome validator|safe-output preflight validator))\r?\n([\s\S]*?)(?=\r?\n\s*- name:)/g)];
      assert.equal(checkouts.length, 3);
      for (const [, name, step] of checkouts) {
        const ref = step.match(/^\s+ref: (.+)$/m)[1].trim();
        assert.equal(ref, "${{ github.event_name == 'workflow_dispatch' && github.workflow_sha || '' }}", name);
        assert.equal(evaluate(ref.slice(3, -2).trim(), github, {}),
          event === 'workflow_dispatch' ? github.workflow_sha : '', name);
        assert.ok(!step.includes('repository:'), name);
      }
    }
  });
}
