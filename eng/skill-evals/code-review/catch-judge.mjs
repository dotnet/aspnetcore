import assert from "node:assert/strict";
import { createHash, randomUUID } from "node:crypto";
import { appendFileSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { ReadonlyClient } from "./readonly-executor.mjs";
import { complete, parseMatches, rubricsFor } from "./results.mjs";

const prompt = readFileSync(new URL("./rubric-matcher.txt", import.meta.url), "utf8");
const sha = value => createHash("sha256").update(value).digest("hex");

export async function judge(runDir, cases, baseline, { model, cliVersion }) {
    const { trials, cliVersion: reviewedCli, model: reviewedModel } = JSON.parse(readFileSync(join(runDir, "trials.json"), "utf8"));
    assert.ok(model && model === reviewedModel && cliVersion === reviewedCli, "Match the recorded reviewer model and CLI");
    const byId = new Map(cases.filter(item => !item.excluded).map(item => [item.id, item]));
    const home = join(runDir, "judge-home");
    mkdirSync(home, { mode: 0o700 });
    const client = new ReadonlyClient({ ...process.env, COPILOT_HOME: home, REVIEW_EXPECTED_CLI_VERSION: cliVersion }, []);
    const receipts = [];
    let started = false;
    try {
        for (const trial of trials) {
            const item = byId.get(trial.caseId);
            assert.ok(item, `Unknown or excluded case: ${trial.caseId}`);
            const rubrics = rubricsFor(item, baseline);
            if (!complete(trial) || !trial.findings.length || !rubrics.length) {
                continue;
            }
            const findings = trial.findings.map(({ path, line, confidence, claim, body }) => ({
                file: path.replaceAll("\\", "/"), line, confidence,
                text: `${claim}\n\n${body}`.replace(/https?:\/\/\S+|\b[A-Z]:[\\/]\S+/gi, "external reference"),
            }));
            const packet = { reviewId: randomUUID(), findings, rubrics };
            const packetJson = JSON.stringify(packet);
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
                const response = await session.sendAndWait(`${prompt}\n${packetJson}`, 120_000);
                assert.ok(response?.data.content, `${trial.id}: judge returned no response`);
                const matches = parseMatches(response.data.content, packet);
                const receipt = { id: trial.id, sessionId: session.sessionId, packetSha256: sha(packetJson), matches };
                appendFileSync(join(runDir, "catch-receipts.jsonl"), `${JSON.stringify(receipt)}\n`, { mode: 0o600 });
                receipts.push(receipt);
            } finally {
                await session.disconnect();
            }
        }
    } finally {
        if (started) {
            await client.stop();
        }
    }

    const charges = new Map();
    if (receipts.length) {
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
        assert.ok(charges.has(sessionId), `Missing native billing receipt for judge session ${sessionId}`);
        return sum + charges.get(sessionId);
    }, 0);
    writeFileSync(join(runDir, "judgments.json"), `${JSON.stringify({ receipts, judgeNanoAiu }, null, 2)}\n`, { flag: "wx" });
    console.log(`Judged ${receipts.length} trials for ${judgeNanoAiu / 1e9} AI credits`);
}
