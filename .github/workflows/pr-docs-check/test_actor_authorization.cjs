const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { after, before, test } = require('node:test');

const root = path.join(__dirname, '..');
const compiled = fs.readFileSync(path.join(root, 'pr-docs-check.lock.yml'), 'utf8');
const preActivation = compiled.split('\n  pre_activation:\n')[1].split('\n  safe_outputs:\n')[0];
const setupSha = preActivation.match(/uses: github\/gh-aw-actions\/setup@([a-f0-9]{40}) # v0\.91\.6/)[1];
const requiredRoles = JSON.parse(preActivation.match(/GH_AW_REQUIRED_ROLES: (.+)/)[1]);
const allowedBots = preActivation.match(/GH_AW_ALLOWED_BOTS: (.+)/)?.[1];
const runtimeRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pr-docs-check-membership-'));
let main;
let isAllowedBot;

before(() => {
  // Exercise the setup action's pinned producer, not a copy of its authorization logic.
  const git = (...args) => execFileSync('git', args, { cwd: runtimeRoot, encoding: 'utf8', timeout: 120000 });
  git('init', '--quiet');
  git('remote', 'add', 'origin', 'https://github.com/github/gh-aw-actions.git');
  git('fetch', '--quiet', '--depth=1', 'origin', setupSha);
  git('checkout', '--quiet', '--detach', 'FETCH_HEAD');
  assert.equal(git('rev-parse', 'HEAD').trim(), setupSha);
  ({ main } = require(path.join(runtimeRoot, 'setup', 'js', 'check_membership.cjs')));
  ({ isAllowedBot } = require(path.join(runtimeRoot, 'setup', 'js', 'check_permissions_utils.cjs')));
});

after(() => fs.rmSync(runtimeRoot, { recursive: true, force: true }));

async function authorize(actor, role, { bots = allowedBots, active = true } = {})
{
  const saved = { ...process.env };
  const outputs = {};
  const requests = [];
  const summaries = [];
  const logs = [];
  const summary = {
    addRaw(content) {
      summaries.push(content);
      return summary;
    },
    async write() {},
  };
  try {
    process.env.GH_AW_REQUIRED_ROLES = requiredRoles;
    if (bots === undefined || bots === null) {
      delete process.env.GH_AW_ALLOWED_BOTS;
    } else {
      process.env.GH_AW_ALLOWED_BOTS = JSON.parse(bots);
    }
    global.core = {
      info: message => logs.push(message),
      warning: message => logs.push(message),
      debug() {},
      setOutput: (name, value) => outputs[name] = value,
      summary,
    };
    global.context = {
      actor,
      eventName: 'pull_request_target',
      repo: { owner: 'dotnet', repo: 'aspnetcore' },
      payload: {
        action: 'closed',
        pull_request: {
          merged: true,
          user: { login: 'contributor' },
          base: { ref: 'main', repo: { full_name: 'dotnet/aspnetcore' } },
          head: { repo: { full_name: 'contributor/aspnetcore' } },
        },
      },
    };
    global.github = {
      rest: {
        repos: {
          async getCollaboratorPermissionLevel(request) {
            assert.equal(request.owner, 'dotnet');
            assert.equal(request.repo, 'aspnetcore');
            requests.push(request.username);
            if (!active) {
              throw Object.assign(new Error('Not Found'), { status: 404 });
            }
            return { data: { permission: role, role_name: role === 'none' ? '' : role } };
          },
        },
      },
    };
    await main();
    return { outputs, requests, summaries, logs };
  } finally {
    process.env = saved;
    delete global.core;
    delete global.context;
    delete global.github;
  }
}

test('only the production merge bot is allowlisted and existing human roles are retained', () => {
  const workflow = fs.readFileSync(path.join(root, 'pr-docs-check.md'), 'utf8');
  const trigger = workflow.split('\non:\n')[1].split('\nif:')[0];
  assert.equal(requiredRoles, 'admin,maintainer,write');
  assert.equal(allowedBots, '"dotnet-policy-service[bot]"');
  assert.match(trigger, /^  roles: \[admin, maintainer, write\]$/m);
  assert.match(trigger, /^  bots: \["dotnet-policy-service\[bot\]"\]$/m);
  assert.equal((trigger.match(/^  bots:/gm) || []).length, 1);
});

test('real membership producer rejects the production bot without the allowlist', async () => {
  const result = await authorize('dotnet-policy-service[bot]', 'none', { bots: null });
  assert.equal(result.outputs.is_team_member, 'false');
  assert.equal(result.outputs.result, 'insufficient_permissions');
  assert.match(result.outputs.error_message, /Required permissions: admin, maintainer, write/);
  assert.ok(result.summaries[0].includes('on.bots:'));
  assert.deepEqual(result.requests, ['dotnet-policy-service[bot]']);
});

test('real membership producer admits the active production merge bot with no human role', async () => {
  const result = await authorize('dotnet-policy-service[bot]', 'none');
  assert.equal(result.outputs.is_team_member, 'true');
  assert.equal(result.outputs.result, 'authorized_bot');
  assert.equal(result.outputs.user_permission, 'bot');
  assert.deepEqual(result.requests, ['dotnet-policy-service[bot]']);
  assert.ok(result.logs.some(message => message.includes('matched the allowed bots list')));
  assert.deepEqual(result.summaries, []);
});

test('pinned runtime matches the exact bot login and equivalent App slug, not other identities', () => {
  for (const identifier of ['dotnet-policy-service[bot]', 'dotnet-policy-service']) {
    assert.equal(isAllowedBot('dotnet-policy-service[bot]', [identifier]), true);
  }
  for (const actor of ['github-actions[bot]', 'dotnet-policy-service-other[bot]', 'contributor']) {
    assert.equal(isAllowedBot(actor, ['dotnet-policy-service[bot]']), false);
  }
});

test('allowlisted bot still requires an active repository installation', async () => {
  const result = await authorize('dotnet-policy-service[bot]', 'none', { active: false });
  assert.equal(result.outputs.is_team_member, 'false');
  assert.equal(result.outputs.result, 'bot_not_active');
  assert.deepEqual(result.requests, ['dotnet-policy-service[bot]', 'dotnet-policy-service']);
});

for (const actor of ['contributor', 'github-actions[bot]', 'unlisted-service[bot]']) {
  test(`real membership producer still rejects ${actor} without a required role`, async () => {
    const result = await authorize(actor, 'none');
    assert.equal(result.outputs.is_team_member, 'false');
    assert.equal(result.outputs.result, 'insufficient_permissions');
    assert.deepEqual(result.requests, [actor]);
  });
}

for (const role of ['admin', 'maintain', 'write']) {
  test(`real membership producer still admits a human with ${role} access`, async () => {
    const result = await authorize('DeagleGross', role);
    assert.equal(result.outputs.is_team_member, 'true');
    assert.equal(result.outputs.result, 'authorized');
    assert.equal(result.outputs.user_permission, role);
    assert.deepEqual(result.requests, ['DeagleGross']);
  });
}
