#!/usr/bin/env node

import { createHash } from "node:crypto";
import { readFile, realpath, writeFile } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

function parseArgs(argv) {
  const result = {};
  for (let i = 0; i < argv.length; i += 2) {
    const name = argv[i];
    const value = argv[i + 1];
    if (!name?.startsWith("--") || value === undefined) {
      throw new Error(`Invalid argument near '${name ?? "<end>"}'.`);
    }
    result[name.slice(2)] = value;
  }
  return result;
}

function sha256(value) {
  return createHash("sha256").update(value).digest("hex");
}

function modulePaths(cliEntry) {
  const cliRoot = path.dirname(path.dirname(path.resolve(cliEntry)));
  if (path.basename(cliRoot) !== "vally-cli") {
    throw new Error(`Vally CLI entry is not under an @microsoft/vally-cli package: ${cliEntry}`);
  }

  const microsoftRoot = path.dirname(cliRoot);
  const nodeModulesRoot = path.dirname(microsoftRoot);
  return {
    vally: path.join(microsoftRoot, "vally", "dist", "index.js"),
    yaml: path.join(nodeModulesRoot, "yaml", "dist", "index.js"),
  };
}

const args = parseArgs(process.argv.slice(2));
const command = args.command;
if (!command || !args.input || !args["vally-cli"]) {
  throw new Error("--command, --input, and --vally-cli are required.");
}

const modules = modulePaths(args["vally-cli"]);
const vally = await import(pathToFileURL(modules.vally));
const yaml = await import(pathToFileURL(modules.yaml));
const inputPath = path.resolve(args.input);
const { spec } = await vally.loadEvalWithParams(inputPath);
const caseMetadataPath = args["case-metadata"]
  ? path.resolve(args["case-metadata"])
  : path.join(path.dirname(inputPath), "fixtures", "private-case-metadata.json");
const privateCases = JSON.parse(await readFile(caseMetadataPath, "utf8"));
const validation = vally.validateEvalSpec(spec, {
  registry: vally.createDefaultGraderRegistry(),
  evalFilePath: inputPath,
  environments: {},
});
if (!validation.valid) {
  throw new Error(
    `Input eval failed pinned Vally validation: ${validation.diagnostics.map((d) => d.message).join("; ")}`,
  );
}

if (command === "list") {
  const content =
    JSON.stringify(
      spec.stimuli.map((stimulus, index) => ({
        name: stimulus.name,
        index,
        privateCase: privateCases[stimulus.name] ?? null,
      })),
    ) +
    "\n";
  if (args.metadata) {
    await writeFile(path.resolve(args.metadata), content, "utf8");
  } else {
    process.stdout.write(content);
  }
  process.exit(0);
}

if (command !== "project") {
  throw new Error(`Unknown command '${command}'.`);
}
if (
  !args.output ||
  !args.stimulus ||
  !args.metadata ||
  !args.model ||
  !args["judge-model"] ||
  !args.experiment ||
  !args.variant
) {
  throw new Error(
    "--output, --stimulus, --metadata, --model, --judge-model, --experiment, and --variant are required for project.",
  );
}

const matches = spec.stimuli.filter((stimulus) => stimulus.name === args.stimulus);
if (matches.length !== 1) {
  throw new Error(`Expected one stimulus named '${args.stimulus}'; found ${matches.length}.`);
}

const original = structuredClone(matches[0]);
const privateCase = privateCases[args.stimulus] ?? null;
if (privateCase) {
  const canonicalIssue = `https://github.com/dotnet/aspnetcore/issues/${privateCase.issueNumber}`;
  if (!original.prompt.includes(canonicalIssue)) {
    throw new Error(
      `Private case '${args.stimulus}' does not contain its canonical issue '${canonicalIssue}'.`,
    );
  }
  if (!original.prompt.includes(privateCase.promptDestination)) {
    throw new Error(
      `Private case '${args.stimulus}' does not contain its requested destination '${privateCase.promptDestination}'.`,
    );
  }
  if (privateCase.destinationFile !== `issue-${privateCase.issueNumber}-investigation.md`) {
    throw new Error(`Private case '${args.stimulus}' has an inconsistent destination file.`);
  }
}
const projectedStimulus = structuredClone(original);
const replacements = args.replacements
  ? JSON.parse(await readFile(path.resolve(args.replacements), "utf8"))
  : {};
const prefix = args.prefix ? await readFile(path.resolve(args.prefix), "utf8") : "";
if (prefix) {
  projectedStimulus.prompt = `${prefix.trimEnd()}\n\n${projectedStimulus.prompt}`;
}
for (const [from, to] of Object.entries(replacements)) {
  projectedStimulus.prompt = projectedStimulus.prompt.split(from).join(to);
}
let requestedDestination = null;
if (privateCase?.storageCase === "writer-failure") {
  requestedDestination = replacements.__FAILING_ARTIFACT_PATH__;
} else if (privateCase) {
  const artifactRoot = replacements.__SESSION_ARTIFACT_ROOT__;
  if (!artifactRoot) {
    throw new Error(`Private case '${args.stimulus}' has no artifact-root replacement.`);
  }
  requestedDestination = path.join(artifactRoot, privateCase.destinationFile);
}
if (privateCase && (!requestedDestination || !projectedStimulus.prompt.includes(requestedDestination))) {
  throw new Error(
    `Projected private case '${args.stimulus}' does not request '${requestedDestination ?? "<missing>"}'.`,
  );
}

const projected = structuredClone(spec);
projected.defaults = {
  ...projected.defaults,
  runs: 1,
  model: args.model,
  judge_model: args["judge-model"],
};
projected.stimuli = [projectedStimulus];
if (projected.scoring?.weights) {
  const usedTypes = new Set((projectedStimulus.graders ?? []).map((grader) => grader.type));
  const selectedWeights = Object.fromEntries(
    Object.entries(projected.scoring.weights).filter(([type]) => usedTypes.has(type)),
  );
  const totalWeight = Object.values(selectedWeights).reduce((sum, value) => sum + value, 0);
  if (totalWeight <= 0) {
    throw new Error(`Selected stimulus '${args.stimulus}' has no positive scoring weight.`);
  }
  projected.scoring.weights = Object.fromEntries(
    Object.entries(selectedWeights).map(([type, value]) => [type, value / totalWeight]),
  );
}

const projectedValidation = vally.validateEvalSpec(projected, {
  registry: vally.createDefaultGraderRegistry(),
  evalFilePath: path.resolve(args.output),
  environments: {},
});
if (!projectedValidation.valid) {
  throw new Error(
    `Projected eval failed pinned Vally validation: ${projectedValidation.diagnostics.map((d) => d.message).join("; ")}`,
  );
}

const serialized = yaml.stringify(projected, { lineWidth: 0 });
await writeFile(path.resolve(args.output), serialized, { encoding: "utf8", flag: "w" });

const resolvedExperiment = await vally.resolveExperiment(path.resolve(args.experiment));
const outputRealPath = await realpath(path.resolve(args.output));
const matchingPlans = [];
for (const plan of resolvedExperiment.plans) {
  if (plan.variant === args.variant && (await realpath(plan.evalFile)) === outputRealPath) {
    matchingPlans.push(plan);
  }
}
const resolvedPlans = matchingPlans;
if (
  resolvedPlans.length !== 1 ||
  resolvedPlans[0].effectiveSpec.stimuli.length !== 1 ||
  resolvedPlans[0].effectiveSpec.defaults.runs !== 1
) {
  throw new Error(
    `Projected experiment did not resolve to one '${args.variant}' plan, one stimulus, and one run: ` +
      JSON.stringify(
        resolvedExperiment.plans.map((plan) => ({
          variant: plan.variant,
          evalFile: plan.evalFile,
          stimuli: plan.effectiveSpec.stimuli.length,
          runs: plan.effectiveSpec.defaults.runs,
        })),
      ),
  );
}

const stableStringify = vally.stableStringify ?? JSON.stringify;
const executorName = vally.resolveExecutorName(projected.defaults?.executor) ?? "copilot-sdk";
const metadata = {
  stimulus: args.stimulus,
  privateCase,
  requestedDestination,
  canonicalInputHash: sha256(stableStringify(original)),
  effectiveInputHash: sha256(stableStringify(projectedStimulus)),
  graderHash: sha256(stableStringify(original.graders ?? [])),
  scoringHash: sha256(stableStringify(projected.scoring ?? {})),
  threshold: projected.scoring?.threshold,
  evalHash: sha256(serialized),
  resolvedConfigHash: resolvedPlans[0].configHash,
  backendName: "local",
  executorName,
  judgeConfigHash: sha256(
    stableStringify({
      judgeModel: projected.defaults?.judge_model,
      graders: original.graders ?? [],
      scoring: projected.scoring ?? {},
    }),
  ),
};
metadata.frozenProjectionHash = sha256(stableStringify(metadata));
await writeFile(path.resolve(args.metadata), JSON.stringify(metadata, null, 2) + "\n", "utf8");
