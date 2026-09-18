const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const [actionsDir, workflowPath, lockPath] = process.argv.slice(2);
const workflow = fs.readFileSync(workflowPath, "utf8");
const lock = fs.readFileSync(lockPath, "utf8");
const onBlock = text => text.match(/^on:\r?\n((?:[ \t]+[^\r\n]*\r?\n|\r?\n)+)/m)?.[1] ?? "";
const sourceOn = onBlock(workflow);
const compiledOn = onBlock(lock);
const preActivation = lock.slice(lock.indexOf("\n  pre_activation:"), lock.indexOf("\n  safe_outputs:"));
const roles = preActivation.match(/GH_AW_REQUIRED_ROLES: "([^"]+)"/)?.[1];
const membershipScript = preActivation.match(/          script: \|\r?\n((?: {12}[^\r\n]*\r?\n)+)/)?.[1]
  .split(/\r?\n/).map(line => line.replace(/^ {12}/, "")).join("\n");
const failures = [];

async function test(name, action) {
  try {
    await action();
    console.log(`PASS ${name}`);
  } catch (error) {
    failures.push(name);
    console.error(`FAIL ${name}: ${error.message}`);
  }
}

async function main() {
  await test("DailySourceTrigger", () => {
    assert.equal((sourceOn.match(/^  schedule: daily\r?$/gm) ?? []).length, 1,
      "The source must request exactly one every-calendar-day schedule, including weekends.");
    assert.equal((sourceOn.match(/^  workflow_dispatch:\s*$/gm) ?? []).length, 1,
      "Manual dispatch must remain available.");
  });
  await test("DailyCompiledTrigger", () => {
    assert.deepEqual([...compiledOn.matchAll(/^  ([a-z_]+):/gm)].map(match => match[1]).sort(),
      ["schedule", "workflow_dispatch"]);
    const schedules = [...compiledOn.matchAll(/^    - cron: ['"]?(\d+) (\d+) \* \* \*['"]?\r?$/gm)];
    assert.equal(schedules.length, 1, "One compiled UTC cron must run every day of every month.");
    assert.ok(Number(schedules[0][1]) < 60 && Number(schedules[0][2]) < 24);
    assert.equal((compiledOn.match(/^\s+- cron:/gm) ?? []).length, 1);
    console.log(`Compiled daily UTC cron: ${schedules[0][1]} ${schedules[0][2]} * * *`);
  });
  await test("ScheduledActivationWiring", () => {
    assert.equal(roles, "admin,maintainer,write");
    assert.match(sourceOn, /^  roles: \[admin, maintainer, write\]\r?$/m);
    assert.ok(preActivation.includes("uses: github/gh-aw-actions/setup@v0.88.7"));
    assert.ok(preActivation.includes("activated: ${{ steps.check_membership.outputs.is_team_member == 'true' }}"));
    assert.ok(lock.includes("if: needs.pre_activation.outputs.activated == 'true'"));
    assert.ok(membershipScript?.includes("require(path.join(actionsDir, 'check_membership.cjs'))"));
  });

  for (const [name, eventName, actor, permission, expectedResult] of [
    ["scheduled-human", "schedule", "schedule-editor", "read", "safe_event"],
    ["scheduled-bot", "schedule", "github-actions[bot]", "read", "safe_event"],
    ["manual-admin", "workflow_dispatch", "operator", "admin", "authorized"],
    ["manual-maintainer", "workflow_dispatch", "operator", "maintain", "authorized"],
    ["manual-write", "workflow_dispatch", "operator", "write", "authorized"],
    ["manual-read", "workflow_dispatch", "operator", "read", "insufficient_permissions"],
  ]) {
    await test(`PinnedActivation/${name}`, async () => {
      const outputs = {};
      let permissionCalls = 0;
      const core = {
        info() {}, warning() {}, error() {}, debug() {},
        setOutput(key, value) { outputs[key] = value; },
        summary: { addRaw() { return this; }, async write() {} },
      };
      const context = {
        actor, eventName, ref: "refs/heads/main",
        repo: { owner: "dotnet", repo: "aspnetcore" },
        payload: { repository: { default_branch: "main" } },
      };
      const github = {
        hook: { before() {} },
        rest: { repos: { async getCollaboratorPermissionLevel(request) {
          permissionCalls++;
          assert.deepEqual(request, { owner: "dotnet", repo: "aspnetcore", username: actor });
          return { data: { permission } };
        } } },
      };
      process.env.GH_AW_REQUIRED_ROLES = roles;
      process.env.GH_AW_ALLOWED_BOTS = "";
      const runtimeBase = path.join(process.cwd(), "gh-aw", "actions");
      const runtimeRequire = module => require(module.startsWith(runtimeBase)
        ? path.join(actionsDir, path.basename(module)) : module);
      const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
      const invoke = new AsyncFunction("require", "process", "core", "github", "context", "exec", "io", "getOctokit", membershipScript);
      await invoke(runtimeRequire, { env: { RUNNER_TEMP: process.cwd() } }, core, github, context, {}, {}, () => github);
      assert.equal(outputs.result, expectedResult);
      assert.equal(outputs.is_team_member, expectedResult === "insufficient_permissions" ? "false" : "true");
      assert.equal(permissionCalls, eventName === "schedule" ? 0 : 1);
    });
  }
  assert.equal(failures.length, 0, "All Pulse schedule and pinned activation controls must pass.");
}

main().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
