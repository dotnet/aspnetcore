const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');

const workflow = fs.readFileSync(path.join(__dirname, '..', 'pr-docs-check-merged.yml'), 'utf8');
const script = workflow.split(/          script: \|\r?\n/)[1].split(/\r?\n/)
  .map(line => line.replace(/^ {12}/, '')).join('\n');
const run = new Function('github', 'context', `return (async () => {${script}\n})();`);
const event = headRepository => ({
  eventName: 'pull_request_target',
  repo: { owner: 'dotnet', repo: 'aspnetcore' },
  payload: {
    action: 'closed',
    pull_request: {
      number: 42, merged: true,
      base: { ref: 'main' },
      head: { repo: { full_name: headRepository } },
    },
  },
});

for (const headRepository of ['contributor/aspnetcore', 'dotnet/aspnetcore']) {
  test(`merged-main event from ${headRepository} queues exact skip inputs`, async () => {
    const requests = [];
    await run({ rest: { actions: { createWorkflowDispatch: async request => requests.push(request) } } },
      event(headRepository));
    assert.deepEqual(requests, [{
      owner: 'dotnet', repo: 'aspnetcore', workflow_id: 'pr-docs-check.lock.yml', ref: 'main',
      inputs: { source_repository: 'dotnet/aspnetcore', pr_number: '42', existing_draft: 'skip' },
    }]);
  });
}

for (const [scenario, change] of [
  ['closed unmerged', context => context.payload.pull_request.merged = false],
  ['non-main', context => context.payload.pull_request.base.ref = 'release/11.0'],
  ['not closed', context => context.payload.action = 'opened'],
  ['other event', context => context.eventName = 'pull_request'],
  ['other repository', context => context.repo.repo = 'other'],
  ['fork repository', context => context.repo.owner = 'contributor'],
  ['missing PR', context => delete context.payload.pull_request],
]) {
  test(`${scenario} never dispatches analysis`, async () => {
    const context = event('contributor/aspnetcore');
    change(context);
    await run({ rest: { actions: { createWorkflowDispatch: async () => assert.fail('Must not dispatch') } } }, context);
  });
}

test('invalid event number and dispatch API failures are explicit', async () => {
  for (const number of [0, -1, '42', Number.MAX_SAFE_INTEGER + 1]) {
    const context = event('contributor/aspnetcore');
    context.payload.pull_request.number = number;
    await assert.rejects(run({}, context), /invalid source PR number/);
  }
  await assert.rejects(run({
    rest: { actions: { createWorkflowDispatch: async () => { throw new Error('API unavailable'); } } },
  }, event('contributor/aspnetcore')), /API unavailable/);
});
