import assert from "node:assert/strict";
import { createHash, randomUUID } from "node:crypto";
import { appendFileSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { ReadonlyClient } from "./readonly-executor.mjs";
import { parseMatches } from "./results.mjs";

const prompt = readFileSync(new URL("./rubric-matcher.txt", import.meta.url), "utf8");
const sha = value => createHash("sha256").update(value).digest("hex");

export async function judge(runDir, cases, { model, cliVersion }, quick) {
    const { trials, cliVersion: reviewedCli, model: reviewedModel } = JSON.parse(readFileSync(join(runDir, "trials.json"), "utf8"));
    assert.ok(model && model === reviewedModel && cliVersion === reviewedCli, "Match the recorded reviewer model and CLI");
    const byId = new Map(cases.map(item => [item.id, item]));
    const home = join(runDir, "judge-home");
    mkdirSync(home, { mode: 0o700 });
    const client = new ReadonlyClient({ ...process.env, COPILOT_HOME: home, REVIEW_EXPECTED_CLI_VERSION: cliVersion }, []);
    const receipts = [];
    let started = false;
    try {
        for (const trial of trials) {
            const { rubrics } = byId.get(trial.caseId);
            if (!rubrics.length) {
                continue;
            }
            const findings = trial.findings.map(({ path, line, confidence, claim, body }) => ({
                file: path.replaceAll("\\", "/"), line, confidence,
                text: `${claim}\n\n${body}`.replace(/https?:\/\/\S+|\b[A-Z]:[\\/]\S+/gi, "external reference"),
            }));
            const packet = { reviewId: randomUUID(), findings, rubrics };
            const packetJson = JSON.stringify(packet);
            let matches = rubrics.map(() => []);
            let sessionId = null;
            if (findings.length) {
                if (!started) {
                    await client.start();
                    started = true;
                }
                const work = join(home, packet.reviewId);
                mkdirSync(work, { mode: 0o700 });
                const session = await client.createSession({
                    model, workingDirectory: work, configDirectory: home, enableConfigDiscovery: false,
                    onPermissionRequest: () => ({ kind: "denied-by-rules" }),
                });
                try {
                    sessionId = session.sessionId;
                    const response = await session.sendAndWait(`${prompt}\n${packetJson}`, 120_000);
                    assert.ok(response?.data.content, `${trial.id}: judge returned no response`);
                    matches = parseMatches(response.data.content, packet);
                } finally {
                    await session.disconnect();
                }
            }
            const receipt = { id: trial.id, sessionId, packetSha256: sha(packetJson), matches };
            appendFileSync(join(runDir, "catch-receipts.jsonl"), `${JSON.stringify(receipt)}\n`, { mode: 0o600 });
            receipts.push(receipt);
        }
    } finally {
        if (started) {
            await client.stop();
        }
    }

    const charges = new Map();
    if (receipts.some(receipt => receipt.sessionId)) {
        for (const entry of readdirSync(home, { recursive: true, withFileTypes: true })) {
            if (entry.name !== "events.jsonl" || !entry.isFile()) {
                continue;
            }
            const events = readFileSync(join(entry.parentPath, entry.name), "utf8").trim().split(/\r?\n/).map(line => JSON.parse(line));
            const start = events.find(event => event.type === "session.start");
            const usage = events.filter(event => ["session.usage_checkpoint", "session.shutdown"].includes(event.type))
                .map(event => event.data.totalNanoAiu).filter(Number.isSafeInteger);
            if (start && usage.length) {
                charges.set(start.data.sessionId, usage.at(-1));
            }
        }
    }
    const judgeNanoAiu = receipts.reduce((sum, { sessionId }) => {
        if (!sessionId) {
            return sum;
        }
        assert.ok(charges.has(sessionId), `Missing native billing receipt for judge session ${sessionId}`);
        return sum + charges.get(sessionId);
    }, 0);
    const caughtByTrial = new Map(receipts.map(({ id, matches }) => [id, matches.every(positions => positions.length > 0)]));
    const scores = [...new Set(trials.map(trial => trial.caseId))].map(id => {
        const rows = trials.filter(trial => trial.caseId === id);
        const item = byId.get(id);
        if (!item.rubrics.length) {
            return { id, kind: "control", findings: rows.reduce((sum, row) => sum + row.findings.length, 0),
                trials: rows.length, mustNotRaise: item.mustNotRaise };
        }
        const caught = rows.filter(row => caughtByTrial.get(row.id)).length;
        const bucket = rows.length === 1 ? undefined : caught === rows.length ? "always" : caught ? "sometimes" : "never";
        return { id, kind: item.exploratory ? "exploratory" : "must-find", caught, trials: rows.length, bucket };
    });
    const reviewNanoAiu = trials.reduce((sum, row) => sum + row.nanoAiu, 0);
    const summary = { model, cliVersion, reviewNanoAiu, judgeNanoAiu, scores };
    if (quick) {
        summary.quickCatchGatePassed = scores.filter(item => item.kind === "must-find" && item.caught).length >= 6;
        summary.quickControlGate = "Compare the recorded control findings to the baseline before accepting this run.";
    }
    writeFileSync(join(runDir, "summary.json"), `${JSON.stringify(summary, null, 2)}\n`, { flag: "wx" });
    console.log(`Review and judge cost: ${(reviewNanoAiu + judgeNanoAiu) / 1e9} AI credits`);
    if (summary.quickCatchGatePassed === false) {
        console.error("Quick check did not catch at least six must-find cases; see summary.json");
        process.exitCode = 1;
    }
}
