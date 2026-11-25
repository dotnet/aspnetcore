import assert from "node:assert/strict";
import { parseReview } from "./results.mjs";

export function collectTrials(groups, lines, audit) {
    const expected = new Map(groups.flatMap(group => group.ids.flatMap(caseId =>
        Array.from({ length: group.runs }, (_, index) => [`${caseId}/${index + 1}`, { group, caseId, index }]))));
    const rows = new Map();
    const warnings = [];
    for (const [index, line] of lines.entries()) {
        if (!line.trim()) {
            continue;
        }
        let row;
        try {
            row = JSON.parse(line);
        } catch {
            warnings.push(`Invalid JSON on Vally results line ${index + 1}`);
            continue;
        }
        if (row?.type !== "trial-result") {
            continue;
        }
        const trialIndex = Number(/::trial-(\d+)$/.exec(row.itemId)?.[1]);
        const id = `${row.stimulus}/${trialIndex + 1}`;
        if (!expected.has(id)) {
            warnings.push(`Unexpected Vally trial: ${id}`);
            continue;
        }
        const records = rows.get(id) ?? [];
        records.push(row);
        rows.set(id, records);
    }

    const trials = [...expected].map(([id, { group, caseId, index }]) => {
        const records = rows.get(id) ?? [];
        const trial = { id, caseId };
        let nanoAiu = null;
        try {
            assert.equal(records.length, 1, records.length ? "Duplicate Vally trial" : "Missing Vally trial");
            const [row] = records;
            const cost = row.trajectory?.metrics?.tokenUsage?.cost;
            if (cost?.unit === "nano-aiu" && Number.isSafeInteger(cost.amount)) {
                nanoAiu = cost.amount;
            }
            assert.equal(row.evalName, group.name, "Unexpected Vally eval");
            assert.equal(row.status, "success", `Vally trial status: ${row.status}`);
            assert.equal(row.trajectory?.endReason, "completed", "Review trajectory did not complete");
            const parsed = parseReview(row.trajectory.output);
            if (["blocked", "incomplete", "malformed"].includes(parsed.status)) {
                return { ...trial, nanoAiu, ...parsed };
            }
            audit(row, group, index);
            return { ...trial, nanoAiu, ...parsed };
        } catch (error) {
            return { ...trial, nanoAiu, status: "error", reason: error.message, findings: [] };
        }
    });
    return { trials, warnings };
}
