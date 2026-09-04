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

export function normalizeOptions(input = {}, fallback = {}) {
  const source = input.source ?? fallback.source ?? "fixture";
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

  let queue;
  try {
    queue = JSON.parse(stdout);
  } catch (error) {
    throw queueError("queue_json_invalid", `PR attention queue returned invalid JSON: ${error.message}`);
  }

  validateQueue(queue);
  return { options, queue };
}

export function summarizeQueue(queue) {
  const visibleItems = queue.items
    .filter((item) => item.shownInDigest)
    .map((item) => ({
      number: item.number,
      title: item.title,
      url: item.url,
      author: item.author,
      bucket: item.bucket,
      nextActor: item.nextActor,
      reasonCodes: item.reasonCodes,
      blockers: item.blockers,
      ageDays: item.ageDays,
      idleDays: item.idleDays,
    }));

  return {
    schemaVersion: queue.schemaVersion,
    generatedAt: queue.generatedAt,
    repository: queue.repository,
    filter: {
      name: queue.filter.name,
      description: queue.filter.description,
      selection: queue.filter.selection,
    },
    query: queue.query,
    census: queue.census,
    overflow: queue.overflow,
    caps: queue.caps,
    warnings: queue.warnings,
    visibleItems,
  };
}

function validateQueue(queue) {
  if (!queue || typeof queue !== "object") {
    throw queueError("queue_shape_invalid", "PR attention queue returned no object");
  }
  if (queue.query?.complete !== true) {
    throw queueError("queue_incomplete", "PR attention queue did not return a complete repository query");
  }
  if (!queue.filter || !queue.census || !Array.isArray(queue.items) || !Array.isArray(queue.warnings)) {
    throw queueError("queue_shape_invalid", "PR attention queue JSON is missing required fields");
  }

  for (const item of queue.items) {
    if (
      !Number.isInteger(item.number)
      || typeof item.bucket !== "string"
      || typeof item.nextActor !== "string"
      || !Array.isArray(item.reasonCodes)
      || !Array.isArray(item.blockers)
    ) {
      throw queueError("queue_item_invalid", "PR attention queue contains an invalid item");
    }
  }
}

function queueError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
