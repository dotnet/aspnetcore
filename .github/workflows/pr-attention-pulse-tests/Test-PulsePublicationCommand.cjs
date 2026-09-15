const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { spawnSync } = require("node:child_process");

const [actionsDir, workflowPath, testRoot] = process.argv.slice(2);
const dashboardIssueNumber = 69123;
const workflow = fs.readFileSync(workflowPath, "utf8");
const script = workflow.match(/      script: \|\r?\n([\s\S]*?)\r?\n  - name: Protect canonical Pulse artifacts/)[1]
  .split(/\r?\n/).map(line => line.replace(/^        /, "")).join("\n");
const command = workflow.match(/^cat \.pr-attention-pulse\/pulse-request\.json \| safeoutputs update_issue \.$/m)?.[0];

const { hasStdinJsonPayload, parseToolArgs } = require(path.join(actionsDir, "mcp_cli_bridge.cjs"));
assert.equal(hasStdinJsonPayload([".", "."]), false, "Two sentinels do not select JSON stdin in the pinned CLI.");
assert.equal(hasStdinJsonPayload(["."]), true);

const originalDirectory = process.cwd();
const root = path.join(testRoot, "publication-command");
fs.mkdirSync(path.join(root, ".pr-attention-pulse"), { recursive: true });
try {
  process.chdir(root);
  const failures = [];
  for (const [name, body] of [
    ["quoted-lf", "# Dashboard\n\nQuotes: \"value\", backslash: \\, dollar: $(), backtick: `.\n"],
    ["unicode-crlf", "# Dashboard\r\n\r\nUnicode: caf\u00e9 \u6771\u4eac\r\n"],
    ["long-body", "# Dashboard\n\n" + "A long report row.\n".repeat(3000)],
  ]) {
    try {
      fs.rmSync(".pr-attention-pulse/pulse-request.json", { force: true });
      fs.writeFileSync(".pr-attention-pulse/pulse-body.md", body);
      const quietCore = { info() {}, warning() {}, error() {} };
      global.core = quietCore;
      vm.runInNewContext(script, {
        require: module => require(module.startsWith(path.join(root, "gh-aw", "actions"))
          ? path.join(actionsDir, path.basename(module)) : module),
        process: {
          env: {
            RUNNER_TEMP: root,
            PR_ATTENTION_PULSE_ISSUE_NUMBER: String(dashboardIssueNumber),
          },
        },
        core: quietCore, github: { rest: {}, hook: { before() {} } }, context: {}, exec: {}, io: {}, getOctokit() {},
      });
      const expectedBody = fs.readFileSync(".pr-attention-pulse/pulse-body.md", "utf8");
      const request = fs.readFileSync(".pr-attention-pulse/pulse-request.json", "utf8");
      assert.deepEqual(parseToolArgs(["."], {}, request).args, {
        issue_number: dashboardIssueNumber, operation: "replace", body: expectedBody,
      });

      // Exercise the pinned stdin reader and argument parser through an actual pipe.
      const reader = `
        const { readStdinSync, parseToolArgs } = require(process.argv[1]);
        process.stdout.write(JSON.stringify(parseToolArgs(["."], {}, readStdinSync()).args));
      `;
      const result = spawnSync(process.execPath, ["-e", reader, path.join(actionsDir, "mcp_cli_bridge.cjs")], {
        input: request, encoding: "utf8",
      });
      assert.equal(result.status, 0, result.stderr);
      assert.deepEqual(JSON.parse(result.stdout), { issue_number: dashboardIssueNumber, operation: "replace", body: expectedBody });
      console.log(`PASS ${name}`);
    } catch (error) {
      console.error(`FAIL ${name}: ${error.message}`);
      failures.push(error);
    }
  }
  for (const [name, value, message] of [
    ["missing-target", "", /positive decimal integer/],
    ["zero-target", "0", /positive decimal integer/],
    ["nonnumeric-target", "not-a-number", /positive decimal integer/],
    ["out-of-range-target", "2147483648", /supported issue-number range/],
  ]) {
    fs.rmSync(".pr-attention-pulse/pulse-request.json", { force: true });
    fs.writeFileSync(".pr-attention-pulse/pulse-body.md", "# Dashboard\n\nValid body.\n");
    assert.throws(() => vm.runInNewContext(script, {
      require: module => require(module.startsWith(path.join(root, "gh-aw", "actions"))
        ? path.join(actionsDir, path.basename(module)) : module),
      process: { env: { RUNNER_TEMP: root, PR_ATTENTION_PULSE_ISSUE_NUMBER: value } },
      core: { info() {}, warning() {}, error() {} },
      github: { rest: {}, hook: { before() {} } },
      context: {},
      exec: {},
      io: {},
      getOctokit() {},
    }), message);
    assert.equal(fs.existsSync(".pr-attention-pulse/pulse-request.json"), false, `${name} must not create a request.`);
    console.log(`PASS ${name}`);
  }
  assert.equal(failures.length, 0, "Every serialization case must pass.");
  assert.ok(command, "The prompt must specify one supported JSON-stdin invocation using only already-allowed commands.");
  console.log("Pinned Pulse JSON-stdin serialization tests passed (not a Copilot permission-service test).");
} finally {
  process.chdir(originalDirectory);
  fs.rmSync(root, { recursive: true, force: true });
}
