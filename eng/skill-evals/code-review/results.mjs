import assert from "node:assert/strict";

const shaPattern = /^[a-f0-9]{40}$/;
const digestPattern = /^[a-f0-9]{64}$/;
const nonempty = value => typeof value === "string" && value.trim().length > 0;
export const quickCaseIds = ["case-01", "case-03", "case-07", "case-09", "case-20", "case-21", "case-34",
    "case-04", "case-10", "case-17"];

export function validateCohort(cases) {
    assert.ok(Array.isArray(cases) && cases.some(item => item.rubrics?.length) &&
        cases.some(item => item.rubrics?.length === 0), "Expected must-find and control cases");
    assert.equal(new Set(cases.map(item => item.id)).size, cases.length);
    assert.equal(new Set(cases.map(item => item.pr)).size, cases.length);
    for (const item of cases) {
        assert.match(item.id, /^case-\d{2,}$/);
        assert.ok(Number.isInteger(item.pr) && item.pr > 0);
        for (const key of ["head", "baseTip", "mergeBase"]) {
            assert.match(item[key], shaPattern);
        }
        if (item.fixCommit !== undefined) {
            assert.match(item.fixCommit, shaPattern);
        }
        assert.match(item.diffSha256, digestPattern);
        assert.ok(Array.isArray(item.rubrics) && item.rubrics.every(nonempty));
        assert.ok(item.exploratory === undefined || (item.exploratory === true && item.rubrics.length));
        assert.ok(item.mustNotRaise === undefined || (!item.rubrics.length && nonempty(item.mustNotRaise)));
    }
}

export function parseReview(output) {
    const match = /^(STATUS: (?:FINDINGS|NO_FINDINGS|BLOCKED|INCOMPLETE))\r?\n([\s\S]+)$/.exec(output);
    assert.ok(match, "Missing review status or JSON");
    const [, status, json] = match;
    const data = JSON.parse(json);
    assert.equal(data.path, "single-reviewer");
    for (const key of ["newFindings", "existingFeedbackCoverage", "unresolved", "discarded"]) {
        assert.ok(Array.isArray(data[key]), `Missing review array: ${key}`);
    }
    const findings = data.newFindings;
    assert.ok(status === "STATUS: FINDINGS" ? findings.length >= 1 && findings.length <= 5 : findings.length === 0);
    for (const { path, line, severity, confidence, claim, body } of findings) {
        assert.ok(nonempty(path) && !/^(?:[/\\]|[A-Za-z]:)|(?:^|[/\\])\.\.(?:[/\\]|$)/.test(path));
        assert.ok(Number.isInteger(line) && line > 0);
        assert.ok(["verified", "question", "unresolved"].includes(confidence));
        assert.ok([severity, claim, body].every(nonempty));
    }
    assert.ok(status === "STATUS: FINDINGS" || status === "STATUS: NO_FINDINGS", status);
    return findings;
}

export function auditReview(events, cliVersion, reviewTools) {
    const starts = events.filter(event => event.type === "session.start");
    assert.equal(starts.length, 1, "Expected one native reviewer session");
    assert.equal(starts[0].data.copilotVersion, cliVersion);
    assert.ok(!events.some(event => event.type.startsWith("subagent.")), "Unexpected subagent");
    const tools = events.filter(event => event.type === "tool.execution_start").map(event => event.data);
    assert.ok(tools.every(tool => reviewTools.includes(tool.toolName)), "Unexpected executed tool");
    const skills = tools.filter(tool => tool.toolName === "skill");
    assert.equal(skills.length, 1, "Expected one native skill invocation");
    assert.equal(skills[0].arguments.skill, "code-review");
    assert.ok(events.some(event => event.type === "tool.execution_complete" &&
        event.data.toolCallId === skills[0].toolCallId && event.data.success === true), "Skill did not load");
    const usage = events.filter(event => ["session.usage_checkpoint", "session.shutdown"].includes(event.type))
        .map(event => event.data.totalNanoAiu).filter(Number.isSafeInteger);
    assert.ok(usage.length, "Missing native reviewer usage");
    return usage.at(-1);
}

export function parseMatches(response, packet) {
    const { reviewId, matches, ...extra } = JSON.parse(response);
    assert.deepEqual(extra, {}, "Unexpected judge response fields");
    assert.equal(reviewId, packet.reviewId);
    assert.ok(Array.isArray(matches) && matches.length === packet.rubrics.length, "Invalid rubric result count");
    for (const positions of matches) {
        assert.ok(Array.isArray(positions) && new Set(positions).size === positions.length);
        assert.ok(positions.every(index => Number.isInteger(index) && index > 0 && index <= packet.findings.length));
    }
    return matches;
}
