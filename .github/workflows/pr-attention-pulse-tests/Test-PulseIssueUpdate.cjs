const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const [actionsDir, lockPath, bodyPath] = process.argv.slice(2);
const lock = fs.readFileSync(lockPath, "utf8");
const job = lock.slice(lock.indexOf("\n  safe_outputs:"));
const step = job.slice(job.indexOf("      - name: Process Safe Outputs"), job.indexOf("      - name:", job.indexOf("      - name: Process Safe Outputs") + 10));
const config = JSON.parse(JSON.parse(step.match(/GH_AW_SAFE_OUTPUTS_HANDLER_CONFIG: (.+)/)[1])).update_issue;
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
process.env.GH_AW_ALLOWED_GITHUB_REFS = "";
process.env.GH_AW_DETECTION_CONCLUSION = "success";

global.core = { info() {}, warning() {}, error() {}, debug() {} };
global.context = {
  repo: { owner: "PureWeen", repo: "aspnetcore" },
  serverUrl: "https://github.com", runId: 1, eventName: "workflow_dispatch", payload: {},
};
const { main } = require(path.join(actionsDir, "update_issue.cjs"));
const body = fs.readFileSync(bodyPath, "utf8");
const issue = {
  number: 58, title: "[pr-attention-pulse] ASP.NET Core PR Attention Pulse",
  state: "open", labels: [], assignees: [], html_url: "https://github.com/PureWeen/aspnetcore/issues/58",
};

async function publish(currentBody, title = issue.title, target = 58) {
  const writes = [];
  global.github = {
    rest: {
      issues: {
        get: async args => {
          assert.deepEqual(args, { owner: "PureWeen", repo: "aspnetcore", issue_number: 58 });
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
      const repeated = await handler({ type: "update_issue", issue_number: 58, operation: "replace", body });
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
  const fixedTarget = await publish("Old report", issue.title, 59);
  assert.equal(fixedTarget.writes.length, 1);
  assert.equal(fixedTarget.writes[0].issue_number, 58, "The handler's fixed target must override the supplied target.");
  console.log("PASS fixed-target");
  assert.equal(failures.length, 0, "Every API-body equality case must pass.");
}
run().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
