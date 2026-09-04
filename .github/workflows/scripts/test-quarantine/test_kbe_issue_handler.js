#!/usr/bin/env node

const assert = require("node:assert/strict");
const crypto = require("node:crypto");
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");

const workflow = fs.readFileSync(
  path.resolve(__dirname, "../../test-quarantine.md"),
  "utf8",
);
const frontmatter = workflow.slice(0, workflow.indexOf("\n---\n", 4));
assert.doesNotMatch(frontmatter, /\n  create-issue:/);
assert.match(frontmatter, /\n  scripts:\n    create-quarantine-issue:/);
const match = workflow.match(
  /\/\/ --- BEGIN quarantine-kbe-handler ---([\s\S]*?)\/\/ --- END quarantine-kbe-handler ---/,
);
assert.ok(match, "The shipped quarantine KBE handler was not found.");

const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
const executeHandler = new AsyncFunction(
  "item",
  "github",
  "context",
  "core",
  "sanitizeContent",
  "temporaryIdMap",
  "process",
  "require",
  "URL",
  match[1],
);

const stateKey = Symbol.for("aspnetcore.test-quarantine.kbe-handler");
const testName = "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse";
const otherTestName = "Microsoft.AspNetCore.Tests.OtherTests.ReturnsExpectedResponse";

function createEvidence(overrides = {}) {
  return {
    generated_utc: "2026-08-17T00:00:00Z",
    builds: {
      "101": {
        def: 83,
        startedUtc: "2026-08-16T10:00:00Z",
        finishedUtc: "2026-08-16T10:10:00Z",
        sourceVersion: "abc",
        pr: null,
      },
      "102": {
        def: 83,
        startedUtc: "2026-08-17T10:00:00Z",
        finishedUtc: "2026-08-17T10:10:00Z",
        sourceVersion: "def",
        pr: null,
      },
    },
    source_a: {
      [testName]: {
        count: 2,
        assembly: "Sample.Tests--net11.0",
        builds: [101, 102],
        evidence_build: 102,
        run_id: 2001,
        result_id: 3001,
        leg: "Linux_Test",
        error: "Expected response body to contain stable-marker-123 but it was empty.",
        stack: "at Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse()",
        is_consistent_regression: false,
      },
    },
    source_b: {},
    source_c: [],
    source_c_truncated: false,
    ...overrides,
  };
}

function createItem(overrides = {}) {
  return {
    temporary_id: "aw_sample",
    test_name: testName,
    matcher_kind: "literal",
    matcher: "Expected response body to contain stable-marker-123 but it was empty.",
    duplicate_status: "none",
    duplicate_summary: "No matching open or recently closed issue.",
    ...overrides,
  };
}

function createEligibility(evidenceText, evidence, overrides = {}) {
  const tests = {};
  for (const [name, record] of Object.entries(evidence.source_a ?? {})) {
    tests[name] = {
      status: "eligible",
      originating_case: "case-a",
      source_resolution: {
        status: "exact",
        path: "src/Sample.Tests/SampleTests.cs",
        type: name.slice(0, name.lastIndexOf(".")),
        method: name.slice(name.lastIndexOf(".") + 1),
      },
      current_quarantine_state: "not-quarantined",
      latest_quarantine_transition: "none",
      is_consistent_regression: false,
      raw_failure_builds: record.builds,
      excluded_builds: [],
      cutoff: {
        utc: "2026-08-01T00:00:00Z",
        reason: "latest-test-file-change",
        commit: "source-commit",
      },
      eligible_failure_builds: record.builds,
      evidence: {
        build: record.evidence_build,
        run_id: record.run_id,
        result_id: record.result_id,
      },
      reasons: [],
    };
  }
  if (overrides.testName && overrides.record) {
    tests[overrides.testName] = {
      ...(tests[overrides.testName] ?? {}),
      ...overrides.record,
    };
  }
  return {
    schema_version: 1,
    part1_sha256: crypto.createHash("sha256").update(evidenceText).digest("hex"),
    repository: "dotnet/aspnetcore",
    ref: "refs/heads/main",
    commit: "workflow-commit",
    history_ref: "refs/remotes/origin/main",
    history_commit: "a".repeat(40),
    tests,
    ...overrides.root,
  };
}

async function run(item = createItem(), options = {}) {
  delete globalThis[stateKey];
  return runWithoutReset(item, options);
}

async function runWithoutReset(item = createItem(), options = {}) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "test-quarantine-kbe-"));
  const evidenceDirectory = path.join(root, "test-quarantine-evidence");
  fs.mkdirSync(evidenceDirectory);
  const evidence = options.evidence ?? createEvidence();
  const evidenceText = JSON.stringify(evidence);
  if (!options.missingEvidence) {
    fs.writeFileSync(
      path.join(evidenceDirectory, "test-quarantine-part1.json"),
      evidenceText,
    );
  }
  if (!options.missingEligibility) {
    const eligibility = options.eligibility ?? createEligibility(
      evidenceText,
      evidence,
      {
        testName: item.test_name,
        record: options.eligibilityRecord,
        root: options.eligibilityRoot,
      },
    );
    fs.writeFileSync(
      path.join(evidenceDirectory, "test-quarantine-case-a-eligibility.json"),
      JSON.stringify(eligibility),
    );
  }

  const calls = {
    create: [],
    paginate: [],
    summary: [],
    errors: [],
    warnings: [],
    info: [],
  };
  const summary = {
    addHeading(value) {
      calls.summary.push(["heading", value]);
      return this;
    },
    addRaw(value) {
      calls.summary.push(["raw", value]);
      return this;
    },
    addEOL() {
      calls.summary.push(["eol"]);
      return this;
    },
    async write() {
      calls.summary.push(["write"]);
      return this;
    },
  };
  const core = {
    summary,
    error: value => calls.errors.push(String(value)),
    warning: value => calls.warnings.push(String(value)),
    info: value => calls.info.push(String(value)),
  };
  const existingIssues = options.existingIssues ?? [];
  const issuePages = options.existingIssuePages ?? [existingIssues];
  const listForRepo = async () => {
    throw new Error(
      "github.rest.issues.listForRepo must be invoked through github.paginate",
    );
  };
  const github = {
    paginate: async (endpoint, parameters) => {
      calls.paginate.push({ endpoint, parameters });
      assert.equal(
        endpoint,
        listForRepo,
        "The handler must paginate github.rest.issues.listForRepo.",
      );
      assert.deepEqual(parameters, {
        owner: "dotnet",
        repo: "aspnetcore",
        state: "open",
        labels: "test-failure",
        per_page: 100,
      });
      return issuePages.flat();
    },
    rest: {
      issues: {
        listForRepo,
        create: async request => {
          calls.create.push(request);
          return { data: { number: options.issueNumber ?? 70001 } };
        },
      },
      search: {
        issuesAndPullRequests: async () => {
          throw new Error(
            "github.rest.search.issuesAndPullRequests is eventually consistent and must not be used",
          );
        },
      },
    },
  };
  const context = {
    repo: { owner: "dotnet", repo: "aspnetcore" },
    runId: 123456,
  };
  const env = {
    ...process.env,
    RUNNER_TEMP: root,
    TEST_QUARANTINE_ENABLE_KBE: options.enableKbe ? "true" : "false",
    GH_AW_SAFE_OUTPUTS_STAGED: options.staged ? "true" : "false",
    GH_AW_DETECTION_CONCLUSION: options.threatConclusion ?? "success",
    GITHUB_REF: "refs/heads/main",
    GITHUB_SHA: "workflow-commit",
  };
  const localProcess = { env };
  const temporaryIdMap = options.temporaryIdMap ?? new Map();

  try {
    const result = await executeHandler(
      item,
      github,
      context,
      core,
      value => String(value),
      temporaryIdMap,
      localProcess,
      require,
      URL,
    );
    for (const call of calls.paginate) {
      assert.equal(
        call.endpoint,
        listForRepo,
        "The handler must paginate github.rest.issues.listForRepo.",
      );
      assert.deepEqual(call.parameters, {
        owner: "dotnet",
        repo: "aspnetcore",
        state: "open",
        labels: "test-failure",
        per_page: 100,
      });
    }
    return { result, calls };
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
}

function createdIssue(runResult) {
  assert.equal(runResult.calls.create.length, 1);
  return runResult.calls.create[0];
}

async function main() {
  {
    const output = await run();
    const issue = createdIssue(output);
    assert.deepEqual(issue.labels, ["test-failure"]);
    assert.match(issue.body, /"ErrorMessage": "Expected response body/);
    assert.match(issue.body, /"BuildRetry": false/);
    assert.match(issue.body, /"ExcludeConsoleLog": true/);
    assert.equal((issue.body.match(/```json/g) ?? []).length, 1);
    assert.ok(issue.body.endsWith("```"));
    assert.equal(output.result.temporaryId, "aw_sample");
    assert.equal(output.result.number, 70001);
  }

  {
    const output = await run(createItem(), { enableKbe: true });
    assert.deepEqual(createdIssue(output).labels, ["test-failure", "Known Build Error"]);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      eligibilityRecord: {
        status: "ineligible",
        eligible_failure_builds: [102],
        reasons: ["fewer-than-two-post-cutoff-failures"],
      },
    });
    const issue = createdIssue(output);
    assert.deepEqual(issue.labels, ["test-failure"]);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /minimum Case A eligibility invariants/);
  }

  {
    const evidence = createEvidence();
    evidence.source_a[testName].is_consistent_regression = true;
    const output = await run(createItem(), {
      evidence,
      enableKbe: true,
      eligibilityRecord: {
        status: "ineligible",
        is_consistent_regression: true,
        reasons: ["consistent-regression-or-unproven"],
      },
    });
    assert.deepEqual(createdIssue(output).labels, ["test-failure"]);
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      eligibilityRecord: {
        cutoff: {
          utc: "2026-08-18T00:00:00Z",
          reason: "latest-test-file-change",
          commit: "source-commit",
        },
      },
    });
    assert.deepEqual(createdIssue(output).labels, ["test-failure"]);
    assert.doesNotMatch(createdIssue(output).body, /```json/);
    assert.match(createdIssue(output).body, /invalid post-cutoff failure build/);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      eligibilityRecord: {
        status: "ineligible",
        originating_case: "case-b",
        current_quarantine_state: "not-quarantined",
        latest_quarantine_transition: "removed",
      },
    });
    assert.deepEqual(createdIssue(output).labels, ["test-failure"]);
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      eligibilityRecord: {
        status: "eligible",
        current_quarantine_state: "method-quarantined",
        reasons: ["already-quarantined"],
      },
    });
    const issue = createdIssue(output);
    assert.deepEqual(issue.labels, ["test-failure"]);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /minimum Case A eligibility invariants/);
    assert.doesNotMatch(issue.body, /matcher does not match/);
  }

  for (const eligibilityRecord of [
    {
      status: "eligible",
      source_resolution: {
        status: "ambiguous",
        path: "",
        type: "",
        method: "",
      },
      reasons: ["ambiguous-source-resolution"],
    },
    {
      status: "eligible",
      latest_quarantine_transition: "ambiguous",
      reasons: ["ambiguous-quarantine-history"],
    },
  ]) {
    const output = await run(createItem(), { enableKbe: true, eligibilityRecord });
    const issue = createdIssue(output);
    assert.deepEqual(issue.labels, ["test-failure"], JSON.stringify(eligibilityRecord.reasons));
    assert.doesNotMatch(issue.body, /```json/, JSON.stringify(eligibilityRecord.reasons));
    assert.match(issue.body, /minimum Case A eligibility invariants/);
    assert.doesNotMatch(issue.body, /matcher does not match/);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      missingEligibility: true,
    });
    assert.deepEqual(createdIssue(output).labels, ["test-failure"]);
    assert.doesNotMatch(createdIssue(output).body, /```json/);
    assert.match(createdIssue(output).body, /unable to read deterministic Case A eligibility/);
  }

  {
    const output = await run(createItem(), {
      enableKbe: true,
      eligibilityRoot: { part1_sha256: "tampered" },
    });
    assert.deepEqual(createdIssue(output).labels, ["test-failure"]);
    assert.doesNotMatch(createdIssue(output).body, /```json/);
    assert.match(createdIssue(output).body, /receipt identity does not match/);
  }

  {
    const evidence = createEvidence();
    evidence.source_a[testName].error = [
      "Expected response body to contain stable-marker-123 but it was empty.",
      "Actual response body: <empty>",
    ].join("\n");
    const output = await run(createItem({
      matcher_kind: "literal-array",
      matcher: JSON.stringify([
        "Expected response body to contain stable-marker-123 but it was empty.",
        "Actual response body: <empty>",
      ]),
    }), { evidence });
    assert.match(createdIssue(output).body, /"ErrorMessage": \[/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "literal-array",
      matcher: JSON.stringify([
        "Expected response body to contain stable-marker-123 but it was empty.",
        "at Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpectedResponse()",
      ]),
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /does not match this test's deterministic error or stack evidence/);
  }

  {
    const evidence = createEvidence();
    evidence.source_a[testName].error = "RequestFailed stablemarker status 503";
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^RequestFailed[^\\n]*stablemarker[^\\n]*status 503",
    }), { evidence });
    assert.match(createdIssue(output).body, /"ErrorPattern": "\^RequestFailed/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "incomplete",
      matcher: "",
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /KBE activation incomplete/);
  }

  {
    const output = await run(createItem({
      matcher: "Assert.True() Failure",
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /too broad/);
  }

  {
    const output = await run(createItem({
      matcher: "Assert.NotNull() Failure",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      matcher: "Microsoft.AspNetCore.Tests.SampleTests.ReturnsExpected",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const evidence = createEvidence();
    evidence.source_b[otherTestName] = {
      count: 1,
      assembly: "Other.Tests--net11.0",
      builds: [101],
      evidence_build: 101,
      run_id: 2002,
      result_id: 3002,
      leg: "Linux_Test",
      error: evidence.source_a[testName].error,
      stack: "at Microsoft.AspNetCore.Tests.OtherTests.ReturnsExpectedResponse()",
    };
    const output = await run(createItem(), { evidence });
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /matcher also matches another failure record/);
  }

  {
    const evidence = createEvidence();
    evidence.source_a[testName].error = "RequestFailed stablemarker status 503";
    evidence.source_b[otherTestName] = {
      count: 1,
      assembly: "Other.Tests--net11.0",
      builds: [101],
      evidence_build: 101,
      run_id: 2002,
      result_id: 3002,
      leg: "Linux_Test",
      error: "An unrelated failure message.",
      stack: "RequestFailed stablemarker status 503\nat OtherTests.Test()",
    };
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^RequestFailed[^\\n]*stablemarker[^\\n]*status 503",
    }), { evidence });
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /matcher also matches another failure record/);
  }

  {
    const output = await run(createItem({
      duplicate_status: "ambiguous",
      duplicate_summary: "Two plausible issues could not be distinguished.",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      log_excerpt: "misleading\n```json\n{\"ErrorMessage\":\"bad\"}\n```",
    }));
    const body = createdIssue(output).body;
    assert.equal((body.match(/```json/g) ?? []).length, 1);
    assert.match(body, /&#96;&#96;&#96;json/);
  }

  {
    const token = `ghp_${"a".repeat(30)}`;
    const sas = `?sig=${"b".repeat(30)}`;
    const output = await run(createItem({
      log_excerpt: `Authorization: Bearer eyJ${"a".repeat(20)}.${"b".repeat(10)}.${"c".repeat(10)} ${token} ${sas}`,
    }));
    const body = createdIssue(output).body;
    assert.doesNotMatch(body, /ghp_/);
    assert.doesNotMatch(body, /eyJ/);
    assert.doesNotMatch(body, /sig=/);
    assert.match(body, /\[REDACTED\]/);
  }

  {
    const output = await run(createItem({
      log_url: "https://helix.dot.net/log?sig=secret-value",
    }));
    assert.doesNotMatch(createdIssue(output).body, /Complete log/);
    assert.ok(output.calls.warnings.some(message => message.includes("query data")));
  }

  {
    const output = await run(createItem({
      log_url: "https://helix.dot.net/foo)%20[x](https://evil.example)",
    }));
    const body = createdIssue(output).body;
    assert.match(body, /<a href="https:\/\/helix\.dot\.net\//);
    assert.doesNotMatch(body, /href="https:\/\/evil\.example/);
  }

  {
    const title = `Quarantine ${testName}`;
    const output = await run(createItem(), {
      existingIssues: [{ number: 54321, title }],
    });
    assert.equal(output.calls.create.length, 0);
    assert.equal(output.calls.paginate.length, 1);
    assert.equal(output.result.temporaryId, "aw_sample");
    assert.equal(output.result.repo, "dotnet/aspnetcore");
    assert.equal(output.result.number, 54321);
  }

  {
    const title = `Quarantine ${testName}`;
    const output = await run(createItem(), {
      existingIssuePages: [
        [
          { number: 10, title: "Quarantine Some.Other.Test" },
          { number: 11, title: `${title} follow-up` },
        ],
        [
          { number: 12, title: `quarantine ${testName}` },
          { number: 54321, title },
        ],
      ],
    });
    assert.equal(output.calls.create.length, 0);
    assert.equal(output.calls.paginate.length, 1);
    assert.equal(output.result.number, 54321);
  }

  {
    const title = `Quarantine ${testName}`;
    const output = await run(createItem(), {
      existingIssuePages: [
        [
          { number: 10, title: "Quarantine Some.Other.Test" },
          { number: 11, title, pull_request: { url: "https://api.github.com/pulls/11" } },
        ],
        [
          { number: 12, title: ` ${title}` },
          { number: 13, title: `${title} `, pull_request: { url: "https://api.github.com/pulls/13" } },
        ],
      ],
    });
    const issue = createdIssue(output);
    assert.equal(issue.title, title);
    assert.equal(output.calls.paginate.length, 1);
    assert.equal(output.result.number, 70001);
  }

  {
    const secondEvidence = createEvidence();
    secondEvidence.source_a[otherTestName] = {
      ...secondEvidence.source_a[testName],
      stack: `at ${otherTestName}()`,
    };
    delete secondEvidence.source_a[testName];
    const secondItem = createItem({
      temporary_id: "aw_second",
      test_name: otherTestName,
    });

    delete globalThis[stateKey];
    const standalone = await runWithoutReset(secondItem, { evidence: secondEvidence });
    assert.equal(standalone.result.success, true, "The second payload must be valid on its own.");

    delete globalThis[stateKey];
    const first = await runWithoutReset(createItem());
    assert.equal(first.result.success, true);
    const second = await runWithoutReset(secondItem, { evidence: secondEvidence });
    assert.equal(second.result.success, false);
    assert.match(second.result.error, /per-run limit of 1 call/);
    assert.equal(second.calls.create.length, 0);
    assert.equal(second.calls.paginate.length, 0);
  }

  {
    globalThis[stateKey] = { calls: 0, claimedTests: new Set([testName]) };
    const output = await runWithoutReset(createItem({
      temporary_id: "aw_duplicate",
    }));
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /already claimed by this run/);
    assert.equal(output.calls.create.length, 0);
    delete globalThis[stateKey];
  }

  {
    const live = await run(createItem(), { enableKbe: true });
    const liveIssue = createdIssue(live);
    const output = await run(createItem(), { staged: true, enableKbe: true });
    assert.equal(output.calls.create.length, 0);
    assert.equal(output.result.staged, true);
    assert.deepEqual(output.calls.summary[0], ["heading", liveIssue.title]);
    assert.deepEqual(output.calls.summary[1], ["raw", `Labels: ${liveIssue.labels.join(", ")}`]);
    assert.ok(output.calls.summary.some(entry => entry[0] === "raw" && entry[1] === liveIssue.body));
    const preview = output.calls.summary
      .filter(entry => entry[0] === "raw")
      .map(entry => entry[1])
      .join("\n");
    assert.match(preview, /Labels: test-failure, Known Build Error/);
    assert.match(preview, /## Error Message/);
  }

  {
    const output = await run(createItem({ temporary_id: "invalid" }));
    assert.equal(output.result.success, false);
    assert.equal(output.calls.create.length, 0);
  }

  {
    const output = await run(createItem({ test_name: otherTestName }));
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /absent from deterministic Part 1 evidence/);
  }

  {
    const evidence = createEvidence({
      source_a: {},
      source_b: {},
      source_c: [{
        workitem: "Sample.Tests",
        build: 101,
        job: "helix-job",
        fail_block_count: 1,
        fail_blocks: `${testName} [FAIL]\nExpected response body to contain stable-marker-123 but it was empty.`,
      }],
    });
    const output = await run(createItem({
      matcher_kind: "incomplete",
      matcher: "",
    }), { evidence });
    const issue = createdIssue(output);
    assert.match(issue.body, /Sample\.Tests/);
    assert.doesNotMatch(issue.body, /```json/);
  }

  {
    const evidence = createEvidence({
      source_a: {},
      source_b: {},
      source_c: [{
        workitem: "Sample.Tests",
        build: 101,
        job: "helix-job",
        fail_block_count: 1,
        fail_blocks: `${testName} [FAIL]\nExpected response body to contain stable-marker-123 but it was empty.`,
      }],
    });
    const output = await run(createItem(), { evidence });
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /absent from the deterministic Case A eligibility receipt/);
  }

  {
    const evidence = createEvidence();
    evidence.source_a[testName].evidence_build = 999;
    const output = await run(createItem(), { evidence });
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /Evidence build is absent/);
  }

  {
    const evidence = createEvidence();
    delete evidence.source_a[testName].result_id;
    const output = await run(createItem(), { evidence });
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /lacks a bound eligible evidence result/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "literal-array",
      matcher: "{not-json",
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /not valid JSON/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "RequestFailed.*stablemarker",
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /unanchored, broad, unsupported/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123|Assert\\.NotNull\\(\\) Failure",
    }));
    const issue = createdIssue(output);
    assert.doesNotMatch(issue.body, /```json/);
    assert.match(issue.body, /unanchored, broad, unsupported/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123[^]+",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123[\\q]+",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123\\w+",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123x{0,2147483648}",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem({
      matcher_kind: "regex",
      matcher: "^stablemarker123}",
    }));
    assert.doesNotMatch(createdIssue(output).body, /```json/);
  }

  {
    const output = await run(createItem(), { missingEvidence: true });
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /Unable to read deterministic Part 1 evidence/);
  }

  {
    const output = await run(createItem(), { threatConclusion: "failure" });
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /Threat detection did not succeed/);
  }

  {
    const output = await run(createItem(), {
      temporaryIdMap: new Map([["aw_sample", { repo: "dotnet/aspnetcore", number: 1 }]]),
    });
    assert.equal(output.result.success, false);
    assert.match(output.result.error, /already resolved/);
  }

  {
    delete globalThis[stateKey];
    const output = await runWithoutReset(createItem());
    assert.equal(output.result.success, true);
    assert.equal(globalThis[stateKey].calls, 1);
    delete globalThis[stateKey];
  }

  console.log("All quarantine KBE handler tests passed.");
}

main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
