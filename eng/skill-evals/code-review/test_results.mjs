import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { collectTrials } from "./collection.mjs";
import { auditReview, parseMatches, parseReview, reviewGroups, rubricsFor, scoreTrials, validateCohort } from "./results.mjs";
import { reviewProfile, reviewSpec } from "./workflow.mjs";

const cases = JSON.parse(readFileSync(new URL("./cases.json", import.meta.url), "utf8"));
const baseline = JSON.parse(readFileSync(new URL("./baseline.json", import.meta.url), "utf8"));
const finding = { path: "src/Example.cs", line: 42, severity: "medium", confidence: "verified", claim: "A distinct claim", body: "The complete supporting text." };
const payload = findings => JSON.stringify({
    path: "single-reviewer", newFindings: findings, existingFeedbackCoverage: [], unresolved: [], discarded: [],
});
const workflow = readFileSync(new URL("../../../.github/workflows/pull-request-review.md", import.meta.url), "utf8")
    .replaceAll("\r\n", "\n");

test("offline review uses the shipped prompt, model, CLI and analysis tools", () => {
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
    const [grader] = spec.stimuli[0].graders;
    const disallowed = new RegExp(grader.config.disallowed[0].name);
    assert.equal(disallowed.test("grep"), false);
    assert.equal(disallowed.test("rg"), false);
    assert.equal(disallowed.test("bash"), true);
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

test("the shared eval lockfile pins the same CLI as /review", () => {
    const { packages } = JSON.parse(readFileSync(new URL("../evaluation-tools/package-lock.json", import.meta.url), "utf8"));
    const { cliVersion } = reviewProfile(workflow);
    assert.equal(packages["node_modules/@github/copilot"].version, cliVersion);
    for (const arch of ["x64", "arm64"]) {
        assert.equal(packages[`node_modules/@github/copilot-linux-${arch}`].version, cliVersion);
    }
});

test("changing the hosted review step timeout changes the eval timeout and plan hash", () => {
    const [setting, minutes] = /^timeout-minutes: (\d+)$/m.exec(workflow);
    const changedMinutes = Number(minutes) + 1;
    const profile = reviewProfile(workflow);
    const changedProfile = reviewProfile(workflow.replace(setting, `timeout-minutes: ${changedMinutes}`));
    const group = { name: "test", runs: 1, ids: ["case-01"] };
    const hash = value => createHash("sha256").update(JSON.stringify(value)).digest("hex");

    assert.equal(reviewSpec(group, profile).defaults.timeout, `${Number(minutes) * 60}s`);
    assert.equal(reviewSpec(group, changedProfile).defaults.timeout, `${changedMinutes * 60}s`);
    assert.notEqual(hash(profile), hash(changedProfile));
    assert.throws(() => reviewProfile(workflow.replace(setting, "")), /timeout/i);
});

test("the Run A cohort keeps both exclusions out of every tier", () => {
    validateCohort(cases);
    assert.throws(() => validateCohort([...cases, cases[0]]));
    assert.throws(() => validateCohort([{ ...cases[0], label: undefined }, ...cases.slice(1)]), /Invalid label/);
    const [quickGroup] = reviewGroups(cases, baseline, ["--quick"]);
    assert.equal(quickGroup.runs, 1);
    const [mustFind, controls] = reviewGroups(cases, baseline, ["--full"]);
    assert.deepEqual(quickGroup.ids, ["case-01", "case-03", "case-07", "case-09", "case-20", "case-21", "case-34", ...controls.ids]);
    assert.equal(mustFind.runs, 3);
    assert.equal(controls.runs, 1);
    assert.deepEqual(mustFind.ids.filter(id => id in baseline.catches), Object.keys(baseline.catches));
    assert.deepEqual(controls.ids.filter(id => id in baseline.controls), Object.keys(baseline.controls));
    assert.deepEqual(cases.filter(item => item.excluded).map(item => item.id), ["case-25", "case-27"]);
    for (const id of Object.keys(baseline.excluded)) {
        assert.ok(!quickGroup.ids.includes(id) && !mustFind.ids.includes(id) && !controls.ids.includes(id));
        assert.throws(() => reviewGroups(cases, baseline, [id]), /excluded case/);
        assert.throws(() => reviewGroups(cases, baseline, ["--replay", id]), /active must-find case/);
    }
    assert.deepEqual(reviewGroups(cases, baseline, ["--replay", "case-01"])[0].runs, 3);
    const freshBatch = [...mustFind.ids.slice(0, 15), ...controls.ids.slice(0, 5)];
    assert.deepEqual(reviewGroups(cases, baseline, freshBatch), [{ name: "code-review-sample", runs: 1, ids: freshBatch }]);
    assert.throws(() => reviewGroups(cases, baseline, []), /explicitly opt in/);
});

test("the same PR can have separate cases at different commits", () => {
    const original = cases.find(item => item.id === "case-01");
    const later = { ...original, id: "case-99", head: original.fixCommit, rubrics: [], mustNotRaise: original.rubrics[0] };
    validateCohort([...cases, later]);
    assert.throws(() => validateCohort([...cases, { ...later, head: original.head }]), /Duplicate PR and head pair/);
    assert.throws(() => validateCohort([{ ...original, fixCommit: undefined }, cases[1]]), /missing fix commit/);
});

test("saved Vally output preserves completed trials alongside failed and missing trials", () => {
    const group = { name: "test", runs: 1, ids: cases.slice(0, 9).map(item => item.id) };
    const row = (stimulus, output, status = "success") => JSON.stringify({
        type: "trial-result", evalName: group.name, stimulus, itemId: `${stimulus}::trial-0`, status,
        trajectory: { output, endReason: "completed", metrics: { tokenUsage: { cost: { unit: "nano-aiu", amount: 123 } } } },
    });
    const duplicate = row("case-08", `STATUS: NO_FINDINGS\n${payload([])}`);
    const lines = [
        row("case-01", `STATUS: FINDINGS\n${payload([finding])}`),
        row("case-02", "STATUS: BLOCKED\nNo bundle"),
        row("case-03", "STATUS: INCOMPLETE\nCould not finish"),
        row("case-04", "STATUS: NO_FINDINGS\nnot json"),
        row("case-05", `STATUS: NO_FINDINGS\n${payload([])}`, "error"),
        "{truncated",
        row("case-07", `STATUS: NO_FINDINGS\n${payload([])}`),
        duplicate, duplicate,
        row("case-09", `STATUS: NO_FINDINGS\n${payload([])}`),
    ];
    const audit = result => assert.notEqual(result.stimulus, "case-09", "Missing native trace");
    const { trials, warnings } = collectTrials([group], lines, audit);
    assert.deepEqual(trials.map(trial => trial.status),
        ["findings", "blocked", "incomplete", "malformed", "error", "error", "no-findings", "error", "error"]);
    assert.deepEqual(trials[0].findings, [finding]);
    assert.equal(trials[1].reason, "No bundle");
    assert.equal(trials[2].reason, "Could not finish");
    for (const [index, reason] of [[4, "Vally trial status"], [5, "Missing Vally trial"], [7, "Duplicate Vally trial"], [8, "Missing native trace"]]) {
        assert.ok(trials[index].reason.includes(reason));
    }
    assert.equal(trials[4].nanoAiu, 123);
    assert.equal(trials[5].nanoAiu, null);
    assert.deepEqual(warnings, ["Invalid JSON on Vally results line 6"]);
    assert.deepEqual(collectTrials([group], lines, audit), { trials, warnings });
    assert.equal(parseReview("missing status").status, "malformed");
    assert.equal(parseReview(`STATUS: NO_FINDINGS\n${payload([finding])}`).status, "malformed");
});

test("quick gate applies both thresholds, names new findings, and rejects an unfinished control", () => {
    const [{ ids }] = reviewGroups(cases, baseline, ["--quick"]);
    const trials = ids.map(id => ({ id: `${id}/1`, caseId: id, status: "no-findings", findings: [] }));
    const receipts = [];
    function report(id, findings, matches) {
        const trial = trials.find(row => row.caseId === id);
        Object.assign(trial, { status: "findings", findings });
        if (matches) {
            receipts.push({ id: trial.id, matches });
        }
    }
    for (const id of ids.slice(0, 6)) {
        const item = cases.find(item => item.id === id);
        report(id, [finding], rubricsFor(item, baseline).map(() => [1]));
    }
    report("case-02", [finding], [[1]]);
    report("case-17", [finding], [[1]]);
    report("case-04", [finding]);
    const score = () => scoreTrials(cases, baseline, trials, receipts, true);
    const summary = score();
    assert.deepEqual(summary.quickGate, { catches: 6, controlLimit: 3, pairedFindings: 3, passed: true, noNewControlFindings: false,
        unpairedControls: [{ id: "case-05", baselineStatus: "blocked", statuses: { "no-findings": 1 }, findings: 0 }] });
    assert.deepEqual(summary.newControlFindings, [{ id: "case-04", trial: "case-04/1", ...finding }]);
    report("case-04", [finding, finding]);
    assert.equal(score().quickGate.passed, false);
    Object.assign(trials.find(row => row.caseId === "case-04"), { status: "no-findings", findings: [] });
    assert.equal(score().quickGate.noNewControlFindings, true);
    Object.assign(trials.find(row => row.caseId === "case-05"), { status: "blocked" });
    assert.equal(score().quickGate.passed, true);
    assert.deepEqual(score().groups["fresh batch"].find(item => item.id === "case-05").statuses, { blocked: 1 });
    report("case-05", [finding, finding, finding, finding]);
    const unpaired = score();
    assert.equal(unpaired.controlFindings, 6);
    assert.equal(unpaired.quickGate.pairedFindings, 2);
    assert.equal(unpaired.quickGate.passed, true);
    assert.equal(unpaired.quickGate.noNewControlFindings, false);
    assert.deepEqual(unpaired.quickGate.unpairedControls, [
        { id: "case-05", baselineStatus: "blocked", statuses: { findings: 1 }, findings: 4 },
    ]);
    assert.deepEqual(unpaired.newControlFindings.map(item => item.id), Array(4).fill("case-05"));
    const formerlyComplete = trials.find(row => row.caseId === "case-02");
    Object.assign(formerlyComplete, { status: "incomplete", findings: [] });
    const incompletePair = score().quickGate;
    assert.equal(incompletePair.pairedFindings, 1);
    assert.equal(incompletePair.controlLimit, 2);
    assert.equal(incompletePair.passed, false);
    Object.assign(formerlyComplete, { status: "findings", findings: [finding] });
    Object.assign(trials[0], { status: "incomplete", findings: [] });
    assert.equal(score().quickGate.catches, 5);
    assert.equal(score().quickGate.passed, false);
});

test("scoring separates labels, reports loss after three trials, and gates any forbidden match", () => {
    const mustFind = cases.find(item => item.id === "case-01");
    const control = { id: "case-99", label: "added for a rule", rubrics: [], mustNotRaise: "A forbidden claim" };
    const trials = [
        { id: "case-01/1", caseId: "case-01", status: "findings", findings: [finding] },
        { id: "case-01/2", caseId: "case-01", status: "blocked", findings: [] },
        { id: "case-01/3", caseId: "case-01", status: "malformed", findings: [] },
        { id: "case-99/1", caseId: "case-99", status: "findings", findings: [finding] },
        { id: "case-99/2", caseId: "case-99", status: "findings", findings: [finding, finding] },
    ];
    const receipts = [
        { id: "case-01/1", matches: mustFind.rubrics.map(() => [1]) },
        { id: "case-99/1", matches: [[]] },
        { id: "case-99/2", matches: [[2]] },
    ];
    const score = results => scoreTrials([mustFind, control], baseline, trials, results, false);
    const summary = score(receipts);
    assert.deepEqual(summary.groups["fresh batch"].map(item => [item.caught, item.lost]), [[1, true]]);
    assert.deepEqual(summary.groups["added for a rule"].map(item => [item.findings, item.forbidden]), [[2, 1]]);
    assert.equal(summary.mustNotRaisePassed, false);
    assert.equal(summary.controlFindings, 2);
    assert.equal(summary.newControlFindings.length, 3);
    const partial = score(receipts.slice(1));
    assert.deepEqual(partial.unjudgedTrials, [{ id: "case-01/1", reason: "Missing judge receipt" }]);
    assert.equal(partial.groups["fresh batch"][0].lost, undefined);
    assert.equal(partial.controlFindings, 2);
});

test("collect and score replay an interrupted review without starting Vally or a judge", t => {
    const runDir = mkdtempSync(join(tmpdir(), "aspnetcore-review-replay-"));
    t.after(() => rmSync(runDir, { recursive: true, force: true }));
    const sha = value => createHash("sha256").update(value).digest("hex");
    const write = (path, data) => writeFileSync(path, `${JSON.stringify(data)}\n`);
    const profile = reviewProfile(workflow);
    const selection = ["case-04", "case-05"];
    const groups = reviewGroups(cases, baseline, selection);
    const [{ name }] = groups;
    const hashFile = name => sha(readFileSync(new URL(name, import.meta.url)));
    const skill = new URL("../../../.github/skills/code-review/SKILL.md", import.meta.url);
    write(join(runDir, "plan.json"), { selection, groups, profileHash: sha(JSON.stringify(profile)),
        corpusHash: sha(hashFile("./cases.json") + hashFile("./baseline.json")), skillSha256: sha(readFileSync(skill)) });

    const workspacePath = join(runDir, "workspaces", "case-04");
    const skillDir = join(workspacePath, ".github", "skills", "code-review");
    mkdirSync(skillDir, { recursive: true });
    writeFileSync(join(skillDir, "SKILL.md"), readFileSync(skill));
    const results = join(runDir, "reviews", "saved");
    const trace = join(results, name, "case-04", profile.model, "0");
    mkdirSync(trace, { recursive: true });
    writeFileSync(join(trace, "events.jsonl"), [
        { type: "session.start", data: { copilotVersion: profile.cliVersion } },
        { type: "tool.execution_start", data: { toolName: "skill", toolCallId: "load", arguments: { skill: "code-review" } } },
        { type: "tool.execution_complete", data: { toolCallId: "load", success: true } },
        { type: "session.shutdown", data: { totalNanoAiu: 123 } },
    ].map(event => JSON.stringify(event)).join("\n"));
    write(join(results, "results.jsonl"), {
        type: "trial-result", stimulus: "case-04", itemId: "item::trial-0", evalName: name,
        model: profile.model, workspacePath, status: "success",
        trajectory: { output: `STATUS: FINDINGS\n${payload([finding])}`, endReason: "completed",
            metrics: { tokenUsage: { cost: { unit: "nano-aiu", amount: 123 } } } },
    });

    for (const command of ["collect", "score", "collect", "score"]) {
        const result = spawnSync(process.execPath, [fileURLToPath(new URL("./run.mjs", import.meta.url)), command, runDir],
            { encoding: "utf8" });
        assert.equal(result.status, 0, result.stderr);
        if (command === "score") {
            assert.ok(result.stdout.includes(`${finding.claim}\n${finding.body}`));
        }
    }
    const summary = JSON.parse(readFileSync(join(runDir, "summary.json"), "utf8"));
    assert.equal(summary.controlsComplete, false);
    assert.deepEqual(summary.groups["fresh batch"].map(item => item.statuses), [{ findings: 1 }, { error: 1 }]);
    assert.equal(summary.controlFindings, 1);
    assert.equal(summary.reviewNanoAiu, 123);
});

test("a reviewer must load the skill successfully and use only read tools", () => {
    const start = { type: "session.start", data: { copilotVersion: "1.2.3" } };
    const load = { type: "tool.execution_start", data: { toolName: "skill", toolCallId: "skill-call", arguments: { skill: "code-review" } } };
    const done = { type: "tool.execution_complete", data: { toolCallId: "skill-call", success: true } };
    const usage = { type: "session.shutdown", data: { totalNanoAiu: 123 } };
    const tools = ["glob", "rg", "skill", "view"];
    assert.equal(auditReview([start, load, done, usage], "1.2.3", tools), 123);
    const grep = { type: "tool.execution_start", data: { toolName: "grep" } };
    assert.equal(auditReview([start, load, done, grep, usage], "1.2.3", tools), 123);
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
