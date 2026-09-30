const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const [actionsDir, lockPath, bodyPath, snapshotManifestPath] = process.argv.slice(2);
const dashboardIssueNumber = 69328;
const lock = fs.readFileSync(lockPath, "utf8");
const job = lock.slice(lock.indexOf("\n  safe_outputs:"));
const step = job.slice(job.indexOf("      - name: Process Safe Outputs"), job.indexOf("      - name:", job.indexOf("      - name: Process Safe Outputs") + 10));
const serializedConfig = JSON.parse(step.match(/GH_AW_SAFE_OUTPUTS_HANDLER_CONFIG: (.+)/)[1]);
const config = JSON.parse(serializedConfig).update_issue;
assert.equal(config.target, String(dashboardIssueNumber));
const jobWorkflowId = JSON.parse(job.match(/^\s+GH_AW_WORKFLOW_ID: (.+)$/m)[1]);
process.env.GH_AW_WORKFLOW_ID = jobWorkflowId;
const preparation = job.match(/name: Preserve canonical Pulse body on publication\r?\n[\s\S]*?script: (.+)/)?.[1];
if (preparation) {
  vm.runInNewContext(preparation, {
    core: {
      exportVariable(name, value) {
        assert.equal(name, "GH_AW_WORKFLOW_ID");
        assert.equal(value, "");
        process.env[name] = value;
      },
    },
  });
}
process.env.GH_AW_SAFE_OUTPUTS_URLS = "allowed-or-code-region";
process.env.GH_AW_ALLOWED_GITHUB_REFS = "dotnet/aspnetcore";
process.env.GH_AW_DETECTION_CONCLUSION = "success";

global.core = { info() {}, warning() {}, error() {}, debug() {} };
global.context = {
  repo: { owner: "dotnet", repo: "aspnetcore" },
  serverUrl: "https://github.com", runId: 1, eventName: "workflow_dispatch", payload: {},
};
const { main } = require(path.join(actionsDir, "update_issue.cjs"));
const body = fs.readFileSync(bodyPath, "utf8");
const issue = {
  number: dashboardIssueNumber, title: "[pr-attention-pulse] ASP.NET Core PR Attention Pulse",
  state: "open", labels: [], assignees: [], html_url: `https://github.com/dotnet/aspnetcore/issues/${dashboardIssueNumber}`,
};

async function publish(currentBody, title = issue.title, target = dashboardIssueNumber) {
  const writes = [];
  global.github = {
    rest: {
      issues: {
        get: async args => {
          assert.deepEqual(args, { owner: "dotnet", repo: "aspnetcore", issue_number: dashboardIssueNumber });
          return { data: { ...issue, title, body: currentBody } };
        },
        update: async args => {
          writes.push(args);
          return { data: { ...issue, body: args.body } };
        },
      },
    },
  };
  const handler = await main(config);
  const result = await handler({ type: "update_issue", issue_number: target, operation: "replace", body });
  return { result, writes, handler };
}

async function runSnapshotPublications() {
  const manifest = JSON.parse(fs.readFileSync(snapshotManifestPath, "utf8"));
  const firstBody = fs.readFileSync(manifest.first.bodyPath, "utf8");
  const secondBody = fs.readFileSync(manifest.second.bodyPath, "utf8");
  const firstOutput = JSON.parse(fs.readFileSync(manifest.first.outputPath, "utf8"));
  const secondOutput = JSON.parse(fs.readFileSync(manifest.second.outputPath, "utf8"));
  assert.notEqual(firstBody, secondBody);
  assert.notEqual(firstBody.slice(firstBody.indexOf("## Snapshot")), secondBody.slice(secondBody.indexOf("## Snapshot")));
  assert.notEqual(firstBody.slice(0, firstBody.indexOf("## Snapshot")), secondBody.slice(0, secondBody.indexOf("## Snapshot")));
  const { processMessages } = require(path.join(actionsDir, "safe_output_handler_manager.cjs"));
  let currentBody = "Previous report and reference.";
  let currentTitle = issue.title;
  const writes = [];
  global.github = {
    rest: {
      issues: {
        get: async args => {
          assert.deepEqual(args, { owner: "dotnet", repo: "aspnetcore", issue_number: dashboardIssueNumber });
          return { data: { ...issue, title: currentTitle, body: currentBody } };
        },
        update: async args => {
          assert.deepEqual(Object.keys(args).sort(), ["body", "issue_number", "owner", "repo"]);
          assert.equal(args.owner, "dotnet");
          assert.equal(args.repo, "aspnetcore");
          assert.equal(args.issue_number, dashboardIssueNumber);
          writes.push(args);
          currentBody = args.body;
          return { data: { ...issue, body: currentBody } };
        },
      },
    },
  };
  async function dispatch(output, handler = null) {
    handler ??= await main(config);
    const result = await processMessages(new Map([["update_issue", handler]]), output.items);
    return { result, handler };
  }

  const first = await dispatch(firstOutput);
  assert.equal(writes.length, 1);
  assert.equal(currentBody, firstBody);
  console.log("PASS SnapshotPublication/first-report-and-reference");

  const limited = await dispatch(secondOutput, first.handler);
  assert.equal(writes.length, 1);
  assert.equal(currentBody, firstBody);
  assert.equal(limited.result.results[0].success, false);
  assert.match(limited.result.results[0].error, /Max count of 1 reached/);
  console.log("PASS SnapshotPublication/one-item-guard-retains-first");

  currentTitle = "Unrelated issue";
  await dispatch(secondOutput);
  assert.equal(writes.length, 1);
  assert.equal(currentBody, firstBody);
  currentTitle = issue.title;
  console.log("PASS SnapshotPublication/title-guard-retains-first");

  const wrongRepository = structuredClone(secondOutput);
  wrongRepository.items[0].repo = "dotnet/runtime";
  await dispatch(wrongRepository);
  assert.equal(writes.length, 1);
  assert.equal(currentBody, firstBody);
  console.log("PASS SnapshotPublication/repository-guard-retains-first");

  for (const rejected of manifest.rejected) {
    const output = JSON.parse(fs.readFileSync(rejected.outputPath, "utf8"));
    assert.deepEqual(output, { items: [], errors: [] }, "The native validator must have disarmed the collected output.");
    const { result } = await dispatch(output);
    assert.equal(result.results.length, 0, "Invoke the real dispatcher even for quarantined publication attempts.");
    assert.equal(writes.length, 1);
    assert.equal(currentBody, firstBody);
    console.log(`PASS SnapshotPublication/rejected-${rejected.name}-retains-first`);
  }

  await dispatch(secondOutput);
  assert.equal(writes.length, 2, "Each successful report is a single body-only API write.");
  assert.equal(writes[0].body, firstBody);
  assert.equal(writes[1].body, secondBody);
  assert.equal(currentBody, secondBody, "Report and reference must be replaced together, not appended independently.");
  console.log("PASS SnapshotPublication/second-report-and-reference");
}

async function run() {
  const failures = [];
  for (const [name, currentBody] of [
    ["first-publication", "Phase 1 implementation tracking placeholder."],
    ["replace-previous-dashboard", "Old report\n\n<!-- gh-aw-workflow-id: pr-attention-pulse -->"],
  ]) {
    try {
      const { result, writes, handler } = await publish(currentBody);
      assert.equal(result.success, true);
      assert.equal(writes.length, 1);
      assert.deepEqual(Object.keys(writes[0]).sort(), ["body", "issue_number", "owner", "repo"]);
      assert.equal(writes[0].body, body, "The real pinned issue handler must send the canonical body unchanged.");
      const repeated = await handler({ type: "update_issue", issue_number: dashboardIssueNumber, operation: "replace", body });
      assert.equal(repeated.success, false);
      assert.equal(writes.length, 1, "The one-item limit must still prevent another mutation.");
      console.log(`PASS ${name}`);
    } catch (error) {
      console.error(`FAIL ${name}: ${error.message.split("\n")[0]}`);
      failures.push(error);
    }
  }
  const rejectedTitle = await publish("Unrelated content", "Unrelated issue");
  assert.notEqual(rejectedTitle.result.success, true);
  assert.equal(rejectedTitle.writes.length, 0);
  console.log("PASS wrong-title");
  const fixedTarget = await publish("Old report", issue.title, dashboardIssueNumber + 1);
  assert.equal(fixedTarget.writes.length, 1);
  assert.equal(fixedTarget.writes[0].issue_number, dashboardIssueNumber, "The handler's configured target must override the supplied target.");
  console.log("PASS fixed-target");
  assert.equal(failures.length, 0, "Every API-body equality case must pass.");
  if (snapshotManifestPath) {
    await runSnapshotPublications();
  }
}
run().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
