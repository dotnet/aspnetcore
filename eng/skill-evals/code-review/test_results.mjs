import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { auditReview, parseMatches, parseReview, quickCaseIds, validateCohort } from "./results.mjs";
import { reviewProfile, reviewSpec } from "./workflow.mjs";

test("offline review uses the shipped prompt, model, CLI and analysis tools", () => {
    const workflow = readFileSync(new URL("../../../.github/workflows/pull-request-review.md", import.meta.url), "utf8")
        .replaceAll("\r\n", "\n");
    const profile = reviewProfile(workflow);
    const spec = reviewSpec({ name: "test", runs: 1, ids: ["case-01"] }, profile);
    const { model } = spec.defaults;
    const { REVIEW_EXPECTED_CLI_VERSION: cliVersion, REVIEW_TOOLS } = spec.agent_environment.env;
    const tools = REVIEW_TOOLS.split(",");
    const [{ prompt }] = spec.stimuli;
    assert.ok(workflow.includes(`\nmodel: ${model}\n`));
    assert.ok(workflow.includes(`\n  version: "${cliVersion}"\n`));
    const shippedTools = workflow.split("--available-tools=")[1].split("\n")[0].split(",");
    assert.deepEqual(tools, shippedTools.filter(tool => !tool.startsWith("safeoutputs-")).sort());
    const shared = [
        ["Review the supplied version-2 bundle", "Wait for native invocation to succeed; reading a file is not an invocation."],
        ["Use `manifest.json`", "Do not assess vulnerabilities."],
        ["First line: exactly", "use any file create/edit/write tool at any point in the hosted run."],
    ];
    for (const [first, last] of shared) {
        const start = workflow.indexOf(first);
        const end = workflow.indexOf(last, start);
        assert.ok(start >= 0 && end > start);
        assert.ok(prompt.includes(workflow.slice(start, end + last.length)));
    }
    assert.match(prompt, /`bundle\/manifest\.json`/);
    assert.doesNotMatch(prompt, /gh-aw\/review-bundle|All GitHub-visible output|safeoutputs-/);
    assert.throws(() => reviewProfile(workflow.replace("--available-tools=view,", "--available-tools=bash,view,")),
        /offline tool boundary/);
});

test("cohort has the public revision pins and separates controls and exploratory cases", () => {
    const cases = JSON.parse(readFileSync(new URL("./cases.json", import.meta.url), "utf8"));
    validateCohort(cases);
    assert.equal(cases.filter(item => item.rubrics.length && !item.exploratory).length, 23);
    assert.equal(cases.filter(item => item.exploratory).length, 2);
    assert.equal(cases.filter(item => !item.rubrics.length).length, 11);
    assert.throws(() => validateCohort([...cases, cases[0]]));
    const quick = quickCaseIds.map(id => cases.find(item => item.id === id));
    assert.equal(new Set(quickCaseIds).size, 10);
    assert.equal(quick.filter(item => item.rubrics.length && !item.exploratory).length, 7);
    assert.equal(quick.filter(item => !item.rubrics.length).length, 3);
});

test("an incomplete review cannot count as a clean control", () => {
    const payload = JSON.stringify({ path: "single-reviewer", newFindings: [], existingFeedbackCoverage: [], unresolved: [], discarded: [] });
    assert.deepEqual(parseReview(`STATUS: NO_FINDINGS\n${payload}`), []);
    assert.throws(() => parseReview(`STATUS: BLOCKED\n${payload}`), /STATUS: BLOCKED/);
    assert.throws(() => parseReview(`STATUS: INCOMPLETE\n${payload}`), /STATUS: INCOMPLETE/);
});

test("a reviewer must load the skill successfully and use only read tools", () => {
    const start = { type: "session.start", data: { copilotVersion: "1.2.3" } };
    const load = { type: "tool.execution_start", data: { toolName: "skill", toolCallId: "skill-call", arguments: { skill: "code-review" } } };
    const done = { type: "tool.execution_complete", data: { toolCallId: "skill-call", success: true } };
    const usage = { type: "session.shutdown", data: { totalNanoAiu: 123 } };
    const tools = ["glob", "rg", "skill", "view"];
    assert.equal(auditReview([start, load, done, usage], "1.2.3", tools), 123);
    assert.throws(() => auditReview([start, load, usage], "1.2.3", tools), /Skill did not load/);
    const unexpected = { type: "tool.execution_start", data: { toolName: "edit" } };
    assert.throws(() => auditReview([start, load, done, unexpected, usage], "1.2.3", tools), /Unexpected executed tool/);
});

test("the catch judge must return valid finding positions for the assigned review", () => {
    const packet = { reviewId: "review", rubrics: ["first", "second"], findings: [{}] };
    assert.deepEqual(parseMatches('{"reviewId":"review","matches":[[1],[]]}', packet), [[1], []]);
    assert.throws(() => parseMatches('{"reviewId":"review","matches":[[2],[]]}', packet));
    assert.throws(() => parseMatches('{"reviewId":"other","matches":[[1],[]]}', packet));
});
