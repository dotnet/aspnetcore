import assert from "node:assert/strict";
import { execFileSync, spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, realpathSync, rmSync, unlinkSync, writeFileSync } from "node:fs";
import { basename, dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { auditReview, complete, reviewGroups, scoreTrials, validateCohort } from "./results.mjs";
import { collectTrials } from "./collection.mjs";
import { reviewProfile, reviewSpec } from "./workflow.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const repo = realpathSync(resolve(here, "../../.."));
const publicRepo = "https://github.com/dotnet/aspnetcore.git";
const guidance = "cbf730f5cd0bf8c04bf94e4f3869d6898065f37a";
const load = path => JSON.parse(readFileSync(path, "utf8"));
const save = (path, data) => writeFileSync(path, `${JSON.stringify(data, null, 2)}\n`, { flag: "wx" });
const jsonLines = path => readFileSync(path, "utf8").trim().split(/\r?\n/).map(line => JSON.parse(line));
const sha = path => createHash("sha256").update(readFileSync(path)).digest("hex");
const git = (cwd, ...args) => execFileSync("git", args, { cwd, encoding: "utf8" }).trim();
const profile = reviewProfile(readFileSync(join(repo, ".github/workflows/pull-request-review.md"), "utf8"));
const profileHash = createHash("sha256").update(JSON.stringify(profile)).digest("hex");
const [command, requestedDir, ...selection] = process.argv.slice(2);
if (command === "cli-version") {
    console.log(profile.cliVersion);
    process.exit(0);
}
assert.ok(["prepare", "review", "collect", "judge", "score"].includes(command) && requestedDir,
    "Usage: run.mjs cli-version | prepare <new-run-dir> <case-id>... | prepare <new-run-dir> --quick|--full|--replay <case-id> | review|collect|judge|score <run-dir>");
if (["prepare", "review", "judge"].includes(command)) {
    assert.ok(process.platform === "linux" && process.getuid() > 0, "Use Linux or WSL as an unprivileged user");
}
const runDir = command === "prepare"
    ? join(realpathSync(dirname(resolve(requestedDir))), basename(resolve(requestedDir)))
    : realpathSync(requestedDir);
const within = relative(repo, runDir);
assert.ok(within.startsWith(`..${sep}`) || within === ".." || isAbsolute(within), "Keep run data outside the checkout");
const specDir = join(runDir, "spec");
const casePath = join(here, "cases.json");
const baselinePath = join(here, "baseline.json");
const cases = load(casePath);
const baseline = load(baselinePath);
validateCohort(cases);
const corpusHash = createHash("sha256").update(sha(casePath)).update(sha(baselinePath)).digest("hex");

function stripGuides(bundle) {
    rmSync(join(bundle, ".objects"), { recursive: true });
    const guide = /(?:^|\/)[^/]*guidance[^/]*\.md[^/]*$|(?:^|\/)\.github\/(?:instructions\/|copilot-instructions\.md[^/]*$|skills\/(?:code-review|review-pull-request)\/|agents\/review-[^/]*$)|(?:^|\/)required\/guides\/[^/]+\/policies\//i;
    for (const entry of readdirSync(bundle, { recursive: true, withFileTypes: true })) {
        const path = join(entry.parentPath, entry.name);
        if (entry.isFile() && guide.test(relative(bundle, path))) {
            unlinkSync(path);
        }
    }
}

function prepare() {
    const groups = reviewGroups(cases, baseline, selection);
    const selected = groups.flatMap(group => group.ids).map(id => {
        const item = cases.find(candidate => candidate.id === id);
        assert.ok(item, `Unknown case: ${id}`);
        return item;
    });
    assert.ok(process.env.DOTNET_ROOT, "Activate the repository SDK with source ./activate.sh");
    const donor = realpathSync(process.env.REVIEW_OBJECT_STORE || git(repo, "rev-parse", "--path-format=absolute", "--git-common-dir"));
    const donorGit = (...args) => git(repo, `--git-dir=${donor}`, ...args);
    assert.equal(donorGit("rev-parse", "--is-shallow-repository"), "false", "Use a non-shallow object store");
    mkdirSync(runDir, { mode: 0o700 });
    mkdirSync(join(specDir, "bundles"), { recursive: true });
    const producer = join(repo, ".github/skills/review-pull-request/scripts/prepare-review.cs");
    const skill = join(repo, ".github/skills/code-review/SKILL.md");
    copyFileSync(skill, join(specDir, "SKILL.md"));
    function ensureCommit(commit) {
        const args = [`--git-dir=${donor}`, "cat-file", "-e", `${commit}^{commit}`];
        if (spawnSync("git", args, { cwd: repo, stdio: "ignore" }).status !== 0) {
            if (process.env.REVIEW_OBJECT_STORE) {
                throw new Error(`Provided object store is missing frozen commit ${commit}`);
            }
            console.log(`Fetching frozen commit ${commit}`);
            execFileSync("git", [`--git-dir=${donor}`, "fetch", "--no-tags", "--no-write-fetch-head", publicRepo, commit],
                { cwd: repo, stdio: "inherit" });
        }
    }
    ensureCommit(guidance);
    for (const item of selected) {
        const { id, head, baseTip, mergeBase } = item;
        console.log(`Preparing ${id}`);
        for (const commit of [head, baseTip, mergeBase]) {
            ensureCommit(commit);
        }
        if (!process.env.REVIEW_OBJECT_STORE) {
            assert.equal(donorGit("merge-base", "--all", head, baseTip), mergeBase, `${id}: merge base changed`);
        }
        const bundle = join(specDir, "bundles", id);
        const output = execFileSync("dotnet", ["run", producer, "--", "--code-only", "--repo", "dotnet/aspnetcore",
            "--head", head, "--base-tip", baseTip, "--merge-base", mergeBase, "--base-ref", "main",
            "--guidance", `dotnet/aspnetcore@${guidance}`, "--objects", donor, "--output", bundle],
        { cwd: repo, encoding: "utf8" });
        const verdict = JSON.parse(output.trim().split(/\r?\n/).at(-1));
        assert.equal(verdict.verdict, "reconstructed", `${id}: ${output}`);
        const manifest = load(join(bundle, "manifest.json"));
        assert.ok(manifest.version === 2 && manifest.ready);
        assert.equal(manifest.target.pr, null);
        assert.equal(manifest.guidance.commit, guidance);
        assert.equal(sha(join(bundle, "diff.patch")), item.diffSha256, `${id}: frozen diff changed`);
        stripGuides(bundle);
    }
    for (const group of groups) {
        save(join(specDir, `${group.name}.vally.yaml`), reviewSpec(group, profile));
    }
    save(join(runDir, "plan.json"), { groups, selection, profileHash, corpusHash, skillSha256: sha(skill),
        producerSha256: sha(producer), guidance });
}

function review(plan) {
    const tools = process.env.VALLY_TOOLS;
    assert.ok(tools && process.env.COPILOT_CLI_PATH, "Set VALLY_TOOLS and COPILOT_CLI_PATH");
    const output = join(runDir, "reviews");
    assert.equal(sha(join(specDir, "SKILL.md")), plan.skillSha256, "Prepared skill changed");
    const specs = plan.groups.flatMap(group => {
        const path = join(specDir, `${group.name}.vally.yaml`);
        assert.deepEqual(load(path), reviewSpec(group, profile), `Prepared eval spec changed: ${group.name}`);
        return ["-e", path];
    });
    mkdirSync(output);
    const result = spawnSync(join(tools, "node_modules/.bin/vally"), ["eval", ...specs,
        "--executor-plugin", join(here, "readonly-executor.mjs"), "--backend", "local", "--workers", "1",
        "--max-retries", "0", "--workspace", join(runDir, "workspaces"), "--output-dir", output],
        { cwd: specDir, env: { ...process.env, COPILOT_HOME: join(runDir, "review-home") }, stdio: "inherit" });
    if (result.error) {
        throw result.error;
    }
    if (result.status !== 0) {
        console.error(`Vally exited with ${result.signal ?? result.status}; use collect to recover saved trials without rerunning`);
        process.exitCode = 1;
    }
}

function collect(plan) {
    const { cliVersion, model, tools: reviewTools } = profile;
    const output = join(runDir, "reviews");
    const dirs = readdirSync(output, { withFileTypes: true }).filter(entry => entry.isDirectory());
    assert.ok(dirs.length <= 1, "Expected at most one Vally result directory");
    const results = dirs.length ? join(output, dirs[0].name) : output;
    const raw = join(results, "results.jsonl");
    const lines = existsSync(raw) ? readFileSync(raw, "utf8").split(/\r?\n/) : [];
    const { trials, warnings } = collectTrials(plan.groups, lines, (row, group, index) => {
        const { stimulus: id, trajectory, workspacePath } = row;
        assert.equal(row.model, model, "Unexpected reviewer model");
        assert.equal(sha(join(workspacePath, ".github/skills/code-review/SKILL.md")), plan.skillSha256,
            "Staged skill differs from the prepared skill");
        const events = jsonLines(join(results, group.name, id, model, String(index), "events.jsonl"));
        const nanoAiu = auditReview(events, cliVersion, reviewTools);
        const cost = trajectory.metrics.tokenUsage.cost;
        assert.equal(cost.unit, "nano-aiu");
        assert.equal(cost.amount, nanoAiu, "Native usage differs from Vally");
    });
    const data = { cliVersion, model, trials, warnings };
    const path = join(runDir, "trials.json");
    if (existsSync(path)) {
        assert.deepEqual(load(path), data, "Saved reviewer data changed; do not score using stale judge receipts");
    } else {
        save(path, data);
    }
    for (const warning of warnings) {
        console.error(warning);
    }
    const missingCosts = trials.filter(trial => trial.nanoAiu === null).length;
    const unfinished = trials.filter(trial => !complete(trial));
    for (const { id, status, reason } of unfinished) {
        console.error(`${id}: ${status}${reason ? `: ${reason}` : ""}`);
    }
    console.log(`Collected ${trials.length} trials (${unfinished.length} unfinished); ${missingCosts} without a Vally usage receipt`);
}

function score(plan) {
    const { trials, cliVersion, model, warnings } = load(join(runDir, "trials.json"));
    const path = join(runDir, "judgments.json");
    const partialPath = join(runDir, "catch-receipts.jsonl");
    const { receipts, judgeNanoAiu } = existsSync(path) ? load(path) : {
        receipts: existsSync(partialPath) ? jsonLines(partialPath) : [],
        judgeNanoAiu: null,
    };
    const metrics = scoreTrials(cases, baseline, trials, receipts, plan.selection[0] === "--quick");
    const reviewNanoAiu = trials.reduce((sum, trial) => sum + (trial.nanoAiu ?? 0), 0);
    const summary = { model, cliVersion, baseline: baseline.configuration, reviewNanoAiu, judgeNanoAiu, warnings, ...metrics };
    writeFileSync(join(runDir, "summary.json"), `${JSON.stringify(summary, null, 2)}\n`);
    for (const { id, path: file, line, claim, body } of summary.newControlFindings) {
        console.log(`${id} ${file}:${line}\n${claim}\n${body}`);
    }
    console.log(`Recorded reviewer usage: ${reviewNanoAiu / 1e9} AI credits; judge usage: ${judgeNanoAiu === null ? "not recorded" : `${judgeNanoAiu / 1e9} AI credits`}`);
    if (summary.quickGate) {
        const { passed, catches, pairedFindings, controlLimit, unpairedControls } = summary.quickGate;
        console.log(`Quick gate: ${passed ? "passed" : "failed"} (${catches}/7 catches, ${pairedFindings}/${controlLimit} paired control findings)`);
        for (const { id, baselineStatus, statuses, findings } of unpairedControls) {
            const candidate = Object.entries(statuses).map(([status, count]) => `${status}: ${count}`).join(", ");
            console.log(`Unpaired control ${id}: baseline ${baselineStatus}; candidate ${candidate}; ${findings} ordinary findings (not compared)`);
        }
    }
    if (summary.unjudgedTrials.length) {
        console.error(`${summary.unjudgedTrials.length} trial(s) lack usable judge receipts; see summary.json`);
    }
    if (summary.quickGate?.passed === false || !summary.mustNotRaisePassed || summary.unjudgedTrials.length) {
        process.exitCode = 1;
    }
}

if (command === "prepare") {
    prepare();
} else {
    assert.equal(selection.length, 0);
    const plan = load(join(runDir, "plan.json"));
    assert.equal(plan.profileHash, profileHash, "The /review profile changed after preparation; prepare a new run");
    assert.equal(plan.corpusHash, corpusHash, "The corpus changed after preparation; use the original checkout for this run");
    assert.deepEqual(plan.groups, reviewGroups(cases, baseline, plan.selection), "Prepared trial groups changed");
    if (command === "review") {
        review(plan);
    } else if (command === "collect") {
        collect(plan);
    } else if (command === "judge") {
        collect(plan);
        const { judge } = await import("./catch-judge.mjs");
        await judge(runDir, cases, baseline, profile);
    } else {
        score(plan);
    }
}
