import { execFile } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);
const extensionDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(extensionDirectory, "../../..");
const scriptPath = resolve(
  repositoryRoot,
  ".github/skills/pr-attention-queue/scripts/Get-PRAttentionQueue.ps1",
);
const fixturePath = resolve(
  repositoryRoot,
  ".github/skills/pr-attention-queue/tests/fixtures/pull-requests.json",
);

export const SUPPORTED_SCHEMA_VERSION = "1.0.0";
export const BUCKETS = [
  "ReviewNow",
  "NeedsRescue",
  "ReadyToMerge",
  "WaitingOnAuthor",
  "WaitingOnCI",
  "DesignDecision",
  "Draft",
  "Excluded",
];
export const SECONDARY_BUCKETS = [
  "WaitingOnAuthor",
  "WaitingOnCI",
  "DesignDecision",
  "Draft",
  "Excluded",
];
export function normalizeOptions(input = {}, fallback = {}) {
  const source = input.source ?? fallback.source ?? "live";
  const preset = input.preset ?? fallback.preset ?? "blazor";

  if (!["fixture", "live"].includes(source)) {
    throw queueError("invalid_source", "source must be fixture or live");
  }
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(preset)) {
    throw queueError("invalid_preset", "preset must be a simple named preset");
  }

  return { source, preset };
}

export async function loadQueue(input = {}) {
  const options = normalizeOptions(input);
  const args = [
    "-NoProfile",
    "-File",
    scriptPath,
    "-OutputFormat",
    "Json",
    "-Preset",
    options.preset,
  ];

  if (options.source === "fixture") {
    args.push("-InputPath", fixturePath);
  }

  let stdout;
  try {
    ({ stdout } = await execFileAsync("pwsh", args, {
      cwd: repositoryRoot,
      encoding: "utf8",
      maxBuffer: 8 * 1024 * 1024,
      timeout: options.source === "live" ? 180_000 : 30_000,
    }));
  } catch (error) {
    const detail = error.stderr?.trim() || error.message;
    throw queueError("queue_script_failed", `PR attention queue script failed: ${detail}`);
  }

  return {
    options,
    queue: parseQueueJson(stdout),
  };
}

export function parseQueueJson(stdout) {
  let queue;
  try {
    queue = JSON.parse(stdout);
  } catch (error) {
    throw queueError("queue_json_invalid", `PR attention queue returned invalid JSON: ${error.message}`);
  }

  return validateQueue(queue);
}

export function validateQueue(queue) {
  requireRecord(queue, "queue", "queue_shape_invalid");
  if (queue.schemaVersion !== SUPPORTED_SCHEMA_VERSION) {
    throw queueError(
      "queue_schema_unsupported",
      `Unsupported PR attention queue schema version: ${String(queue.schemaVersion)}`,
    );
  }

  requireString(queue.generatedAt, "generatedAt");
  if (Number.isNaN(Date.parse(queue.generatedAt))) {
    throw queueError("queue_shape_invalid", "generatedAt must be an ISO date");
  }
  requireRepository(queue.repository);

  requireRecord(queue.display, "display");
  requireRecord(queue.display.buckets, "display.buckets");
  requireRecord(queue.display.reasonCodes, "display.reasonCodes");
  for (const bucket of BUCKETS) {
    requireDisplayEntry(queue.display.buckets[bucket], `display.buckets.${bucket}`);
  }
  requireRecord(queue.filter, "filter");
  for (const field of ["name", "description", "coverage", "selection"]) {
    requireString(queue.filter[field], `filter.${field}`);
  }

  requireRecord(queue.query, "query");
  if (queue.query.complete !== true) {
    throw queueError("queue_incomplete", "PR attention queue did not return a complete repository query");
  }
  requireNonNegativeInteger(queue.query.openPullRequestCount, "query.openPullRequestCount");
  requireNonNegativeInteger(queue.query.returnedPullRequestCount, "query.returnedPullRequestCount");

  requireRecord(queue.census, "census");
  for (const field of [
    "openPullRequests",
    "matched",
    "labelOnly",
    "pathOnly",
    "labelAndPath",
    "incidentalPathExcluded",
    "unresolvedMergeable",
  ]) {
    requireNonNegativeInteger(queue.census[field], `census.${field}`);
  }
  requireRecord(queue.census.byBucket, "census.byBucket");
  for (const bucket of BUCKETS) {
    requireNonNegativeInteger(queue.census.byBucket[bucket], `census.byBucket.${bucket}`);
  }

  requireRecord(queue.overflow, "overflow");
  for (const field of ["reviewNow", "needsRescue", "readyToMerge"]) {
    requireNonNegativeInteger(queue.overflow[field], `overflow.${field}`);
  }

  requireRecord(queue.caps, "caps");
  for (const field of ["reviewNow", "reviewNowPerAuthor", "needsRescue", "readyToMerge"]) {
    requireNonNegativeInteger(queue.caps[field], `caps.${field}`);
  }

  requireStringArray(queue.warnings, "warnings");
  if (!Array.isArray(queue.items)) {
    throw queueError("queue_shape_invalid", "items must be an array");
  }
  for (const item of queue.items) {
    validateItem(queue, item);
  }

  return queue;
}

function validateItem(queue, item) {
  requireRecord(item, "item");
  requirePositiveInteger(item.number, "item.number");
  for (const field of ["title", "author", "bucket", "nextActor", "scopeMatch", "headSha"]) {
    requireString(item[field], `item.${field}`);
  }
  if (!BUCKETS.includes(item.bucket)) {
    throw queueError("queue_item_invalid", `Unknown item bucket: ${item.bucket}`);
  }
  if (typeof item.shownInDigest !== "boolean") {
    throw queueError("queue_item_invalid", "item.shownInDigest must be a boolean");
  }
  requireNonNegativeInteger(item.ageDays, "item.ageDays");
  requireNonNegativeInteger(item.idleDays, "item.idleDays");
  requireNonNegativeInteger(item.changedFiles, "item.changedFiles");
  requireStringArray(item.reasonCodes, "item.reasonCodes");
  requireStringArray(item.blockers, "item.blockers");

  for (const reasonCode of item.reasonCodes) {
    requireDisplayEntry(
      queue.display.reasonCodes[reasonCode],
      `display.reasonCodes.${reasonCode}`,
    );
  }

  const expectedUrl = `https://github.com/${queue.repository}/pull/${item.number}`;
  if (item.url !== expectedUrl) {
    throw queueError("queue_item_invalid", `item.url must match ${expectedUrl}`);
  }
}

function requireDisplayEntry(value, path) {
  requireRecord(value, path, "queue_display_invalid");
  requireString(value.label, `${path}.label`, "queue_display_invalid");
  requireString(value.description, `${path}.description`, "queue_display_invalid");
}

function requireRepository(value) {
  if (typeof value !== "string" || !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(value)) {
    throw queueError("queue_shape_invalid", "repository must be in owner/name form");
  }
}

function requireRecord(value, path, code = "queue_shape_invalid") {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw queueError(code, `${path} must be an object`);
  }
}

function requireString(value, path, code = "queue_shape_invalid") {
  if (typeof value !== "string" || !value.trim()) {
    throw queueError(code, `${path} must be a non-empty string`);
  }
}

function requireStringArray(value, path) {
  if (!Array.isArray(value) || value.some((entry) => typeof entry !== "string")) {
    throw queueError("queue_shape_invalid", `${path} must be an array of strings`);
  }
}

function requirePositiveInteger(value, path) {
  if (!Number.isInteger(value) || value < 1) {
    throw queueError("queue_item_invalid", `${path} must be a positive integer`);
  }
}

function requireNonNegativeInteger(value, path) {
  if (!Number.isInteger(value) || value < 0) {
    throw queueError("queue_shape_invalid", `${path} must be a non-negative integer`);
  }
}

function queueError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
