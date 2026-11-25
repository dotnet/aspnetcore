import assert from "node:assert/strict";

const shaPattern = /^[a-f0-9]{40}$/;
const digestPattern = /^[a-f0-9]{64}$/;
const nonempty = value => typeof value === "string" && value.trim().length > 0;
export const complete = trial => ["findings", "no-findings"].includes(trial?.status);

export function reviewGroups(cases, baseline, selection) {
    const active = cases.filter(item => !item.excluded);
    const controls = active.filter(item => !item.rubrics.length).map(item => item.id);
    if (selection.length === 1 && selection[0] === "--full") {
        return [
            { name: "code-review-must-find", runs: 3, ids: active.filter(item => item.rubrics.length).map(item => item.id) },
            { name: "code-review-controls", runs: 1, ids: controls },
        ];
    }
    if (selection.length === 1 && selection[0] === "--quick") {
        const always = active.filter(item => item.rubrics.length && baseline.catches[item.id] === baseline.mustFindTrials)
            .map(item => item.id);
        assert.equal(always.length, 7, "Expected seven Run A always-caught cases");
        return [{ name: "code-review-quick", runs: 1, ids: [...always, ...controls] }];
    }
    if (selection[0] === "--replay") {
        assert.ok(selection.length === 2 && active.some(item => item.id === selection[1] && item.rubrics.length),
            "Replay requires one active must-find case");
        return [{ name: "code-review-replay", runs: 3, ids: [selection[1]] }];
    }
    assert.ok(selection.length >= 1 && selection.length <= 20 && new Set(selection).size === selection.length,
        "Select up to 20 distinct case IDs, or explicitly opt in with --quick or --full");
    for (const id of selection) {
        assert.ok(active.some(item => item.id === id), `Unknown or excluded case: ${id}`);
    }
    return [{ name: "code-review-sample", runs: 1, ids: selection }];
}

export function validateCohort(cases) {
    assert.ok(Array.isArray(cases) && cases.some(item => item.rubrics?.length) &&
        cases.some(item => item.rubrics?.length === 0), "Expected must-find and control cases");
    assert.equal(new Set(cases.map(item => item.id)).size, cases.length);
    assert.equal(new Set(cases.map(item => `${item.pr}/${item.head}`)).size, cases.length, "Duplicate PR and head pair");
    for (const item of cases) {
        assert.match(item.id, /^case-\d{2,}$/);
        assert.ok(Number.isInteger(item.pr) && item.pr > 0);
        for (const key of ["head", "baseTip", "mergeBase"]) {
            assert.match(item[key], shaPattern);
        }
        assert.match(item.diffSha256, digestPattern);
        assert.ok(Array.isArray(item.rubrics) && item.rubrics.every(nonempty));
        if (item.rubrics.length || item.fixCommit !== undefined) {
            assert.match(item.fixCommit, shaPattern, `Invalid or missing fix commit for ${item.id}`);
        }
        assert.ok(item.excluded === undefined || item.excluded === true);
        assert.ok(["fresh batch", "added for a rule"].includes(item.label), `Invalid label for ${item.id}`);
        assert.ok(item.mustNotRaise === undefined || (!item.rubrics.length && nonempty(item.mustNotRaise)));
    }
}

export function parseReview(output) {
    const match = typeof output === "string" && /^STATUS: (FINDINGS|NO_FINDINGS|BLOCKED|INCOMPLETE)(?:\r?\n|$)/.exec(output);
    if (!match) {
        return { status: "malformed", findings: [], reason: "Missing review status" };
    }
    const status = match[1].toLowerCase().replaceAll("_", "-");
    const json = output.slice(match[0].length);
    if (status === "blocked" || status === "incomplete") {
        return { status, findings: [], reason: json.trim() };
    }
    try {
        return { status, findings: parseFindings(json, status) };
    } catch (error) {
        return { status: "malformed", findings: [], reason: error.message };
    }
}

function parseFindings(json, status) {
    const data = JSON.parse(json);
    assert.equal(data.path, "single-reviewer");
    for (const key of ["newFindings", "existingFeedbackCoverage", "unresolved", "discarded"]) {
        assert.ok(Array.isArray(data[key]), `Missing review array: ${key}`);
    }
    const findings = data.newFindings;
    assert.ok(status === "findings" ? findings.length >= 1 && findings.length <= 5 : findings.length === 0);
    for (const { path, line, severity, confidence, claim, body } of findings) {
        assert.ok(nonempty(path) && !/^(?:[/\\]|[A-Za-z]:)|(?:^|[/\\])\.\.(?:[/\\]|$)/.test(path));
        assert.ok(Number.isInteger(line) && line > 0);
        assert.ok(["verified", "question", "unresolved"].includes(confidence));
        assert.ok([severity, claim, body].every(nonempty));
    }
    return findings;
}

export function rubricsFor(item, baseline) {
    if (item.rubrics.length) {
        return item.rubrics;
    }
    return [...(baseline.controls[item.id]?.findings ?? []), ...(item.mustNotRaise ? [item.mustNotRaise] : [])];
}

export function scoreTrials(cases, baseline, trials, receipts, quick) {
    const byId = new Map(cases.filter(item => !item.excluded).map(item => [item.id, item]));
    const matchesById = new Map(receipts.map(receipt => [receipt.id, receipt]));
    const scores = new Map();
    const newControlFindings = [];
    const unjudgedTrials = [];
    for (const trial of trials) {
        const item = byId.get(trial.caseId);
        assert.ok(item, `Unknown or excluded case: ${trial.caseId}`);
        const { id, label, rubrics, mustNotRaise } = item;
        const score = scores.get(id) ?? { id, label, kind: rubrics.length ? "must-find" : mustNotRaise ? "must-not-raise" : "control",
            trials: 0, statuses: {}, ...(rubrics.length ? { caught: 0 } : { findings: 0, forbidden: 0 }) };
        scores.set(id, score);
        score.trials++;
        score.statuses[trial.status] = (score.statuses[trial.status] ?? 0) + 1;
        if (!complete(trial)) {
            continue;
        }
        const targets = rubricsFor(item, baseline);
        let matches = targets.map(() => []);
        if (trial.findings.length && targets.length) {
            const receipt = matchesById.get(trial.id);
            try {
                assert.ok(receipt, "Missing judge receipt");
                matches = parseMatches(JSON.stringify({ reviewId: trial.id, matches: receipt.matches }),
                    { reviewId: trial.id, rubrics: targets, findings: trial.findings });
            } catch (error) {
                unjudgedTrials.push({ id: trial.id, reason: error.message });
                score.unjudged = (score.unjudged ?? 0) + 1;
                continue;
            }
        }
        if (rubrics.length) {
            if (matches.every(positions => positions.length)) {
                score.caught++;
            }
            continue;
        }
        const forbidden = new Set(mustNotRaise ? matches.at(-1) : []);
        const known = new Set((mustNotRaise ? matches.slice(0, -1) : matches).flat());
        score.forbidden += forbidden.size;
        trial.findings.forEach((finding, index) => {
            if (!forbidden.has(index + 1)) {
                score.findings++;
            }
            if (!known.has(index + 1) || forbidden.has(index + 1)) {
                newControlFindings.push({ id, trial: trial.id, ...finding });
            }
        });
    }
    for (const score of scores.values()) {
        if (score.kind === "must-find" && score.trials === 3 && !score.unjudged) {
            score.bucket = score.caught === 3 ? "always" : score.caught ? "sometimes" : "never";
            score.lost = baseline.catches[score.id] === baseline.mustFindTrials && score.caught <= 1;
        }
    }
    const groups = Object.fromEntries(["fresh batch", "added for a rule"].map(label =>
        [label, [...scores.values()].filter(score => score.label === label)]));
    const controls = [...scores.values()].filter(score => score.kind !== "must-find");
    const controlFindings = controls.reduce((sum, score) => sum + score.findings, 0);
    const mustNotRaisePassed = controls.every(score => score.forbidden === 0);
    const incompleteControls = new Set(trials.filter(trial => !byId.get(trial.caseId).rubrics.length && !complete(trial))
        .map(trial => trial.caseId));
    const controlsComplete = incompleteControls.size === 0;
    const result = { groups, controlFindings, newControlFindings, mustNotRaisePassed, controlsComplete, unjudgedTrials };
    if (quick) {
        const catches = [...scores.values()].filter(score => score.caught > 0).length;
        const historicallyComplete = controls.filter(score => complete(baseline.controls[score.id]));
        const paired = historicallyComplete.filter(score => !incompleteControls.has(score.id) && !score.unjudged);
        const pairedIds = new Set(paired.map(score => score.id));
        const pairedFindings = paired.reduce((sum, score) => sum + score.findings, 0);
        const controlLimit = paired.reduce((sum, score) => sum + baseline.controls[score.id].findings.length, 1);
        const pairedCoverageComplete = paired.length === historicallyComplete.length;
        const unpairedControls = controls.filter(score => !pairedIds.has(score.id)).map(score => ({
            id: score.id, baselineStatus: baseline.controls[score.id]?.status ?? "missing",
            statuses: score.statuses, findings: score.findings,
        }));
        result.quickGate = { catches, controlLimit, pairedFindings, unpairedControls,
            passed: catches >= 6 && pairedFindings <= controlLimit && pairedCoverageComplete &&
                mustNotRaisePassed && unjudgedTrials.length === 0,
            noNewControlFindings: pairedCoverageComplete && unjudgedTrials.length === 0 && newControlFindings.length === 0 };
    }
    return result;
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
