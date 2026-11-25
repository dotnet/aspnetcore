import assert from "node:assert/strict";
import { execFileSync, spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, realpathSync, rmSync, unlinkSync, writeFileSync } from "node:fs";
import { basename, dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { auditReview, parseReview, reviewGroups, validateCohort } from "./results.mjs";
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
assert.ok(["prepare", "review", "judge"].includes(command) && requestedDir,
    "Usage: run.mjs cli-version | prepare <new-run-dir> <case-id> [case-id] | prepare <new-run-dir> --quick|--full | review <run-dir> | judge <run-dir>");
assert.ok(process.platform === "linux" && process.getuid() > 0, "Use Linux or WSL as an unprivileged user");
const runDir = command === "prepare"
    ? join(realpathSync(dirname(resolve(requestedDir))), basename(resolve(requestedDir)))
    : realpathSync(requestedDir);
const within = relative(repo, runDir);
assert.ok(within.startsWith(`..${sep}`) || within === ".." || isAbsolute(within), "Keep run data outside the checkout");
const specDir = join(runDir, "spec");
const cases = load(join(here, "cases.json"));
validateCohort(cases);

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
    const groups = reviewGroups(cases, selection);
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
    save(join(runDir, "plan.json"), { groups, quick: selection[0] === "--quick", profileHash, skillSha256: sha(skill),
        producerSha256: sha(producer), guidance });
}

function review(plan) {
    const { model, cliVersion, tools: reviewTools } = profile;
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
    execFileSync(join(tools, "node_modules/.bin/vally"), ["eval", ...specs,
        "--executor-plugin", join(here, "readonly-executor.mjs"), "--backend", "local", "--workers", "1",
        "--max-retries", "0", "--require-pass", "--workspace", join(runDir, "workspaces"), "--output-dir", output],
    { cwd: specDir, env: { ...process.env, COPILOT_HOME: join(runDir, "review-home") }, stdio: "inherit" });
    const dirs = readdirSync(output, { withFileTypes: true }).filter(entry => entry.isDirectory());
    assert.equal(dirs.length, 1, "Expected a single Vally result directory");
    const results = join(output, dirs[0].name);
    const expected = new Map(plan.groups.flatMap(group => group.ids.map(id => [id, group])));
    const counts = new Map([...expected.keys()].map(id => [id, 0]));
    const seen = new Set();
    const trials = jsonLines(join(results, "results.jsonl")).filter(row => row.type === "trial-result").map(row => {
        const { stimulus: id, trajectory, status, itemId, evalName, workspacePath } = row;
        const index = Number(/::trial-(\d+)$/.exec(itemId)?.[1]);
        const trial = `${id}/${index + 1}`;
        const group = expected.get(id);
        assert.ok(group && evalName === group.name && Number.isInteger(index) && index >= 0 && index < group.runs && !seen.has(trial),
            `Unexpected or duplicate trial: ${itemId}`);
        seen.add(trial);
        counts.set(id, counts.get(id) + 1);
        assert.equal(row.model, model, `${trial}: unexpected reviewer model`);
        assert.equal(status, "success", `${trial}: Vally trial failed`);
        assert.equal(trajectory.endReason, "completed", `${trial}: review did not complete`);
        assert.equal(sha(join(workspacePath, ".github/skills/code-review/SKILL.md")), plan.skillSha256,
            `${trial}: staged skill differs from the prepared skill`);
        const events = jsonLines(join(results, group.name, id, model, String(index), "events.jsonl"));
        const nanoAiu = auditReview(events, cliVersion, reviewTools);
        const cost = trajectory.metrics.tokenUsage.cost;
        assert.equal(cost.unit, "nano-aiu");
        assert.equal(cost.amount, nanoAiu, `${trial}: native usage differs from Vally`);
        return { id: trial, caseId: id, nanoAiu, findings: parseReview(trajectory.output) };
    });
    assert.ok([...counts].every(([id, count]) => count === expected.get(id).runs), "Missing reviewer trials");
    save(join(runDir, "trials.json"), { cliVersion, model, trials });
    console.log(`Reviewed ${trials.length} trials for ${trials.reduce((sum, item) => sum + item.nanoAiu, 0) / 1e9} AI credits`);
}

if (command === "prepare") {
    prepare();
} else {
    assert.equal(selection.length, 0);
    const plan = load(join(runDir, "plan.json"));
    assert.equal(plan.profileHash, profileHash, "The /review profile changed after preparation; prepare a new run");
    if (command === "review") {
        review(plan);
    } else {
        assert.ok(!existsSync(join(runDir, "summary.json")), "This run has already been judged");
        const { judge } = await import("./catch-judge.mjs");
        await judge(runDir, cases, profile, plan.quick);
    }
}
