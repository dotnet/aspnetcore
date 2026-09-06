import { randomUUID } from "node:crypto";
import { mkdir, readFile, rename, rm, writeFile } from "node:fs/promises";
import { dirname, isAbsolute, join, relative, resolve } from "node:path";

import { requireConfiguredInvestigationModel } from "./investigation.mjs";
import { REPOSITORY, normalizeArea } from "./taxonomy.mjs";

export const MODEL_SETTINGS_FILE = "model-settings.json";
export const DIAGNOSTIC_SETTINGS_FILE = "diagnostic-settings.json";
export const DIAGNOSTIC_DIRECTORY = "diagnostics";
export const DIAGNOSTIC_SCHEMA_VERSION = "1.0.0";
export const MAX_DIAGNOSTIC_BYTES = 2 * 1024 * 1024;
const MAX_DIAGNOSTIC_STRING_LENGTH = 512 * 1024;

export function createReportStore(workspacePath, repositoryRoot) {
  requireArtifactWorkspace(workspacePath, repositoryRoot);
  const artifactRoot = workspacePath
    ? join(workspacePath, "files", "aspnetcore-issue-triage")
    : null;
  const root = artifactRoot ? join(artifactRoot, "reports") : null;

  function pathFor(issueNumber) {
    if (!Number.isInteger(issueNumber) || issueNumber < 1) {
      throw storageError("invalid_issue", "issueNumber must be a positive integer");
    }
    if (!root) {
      throw storageError(
        "artifact_storage_unavailable",
        "The current session does not expose a durable artifact workspace.",
      );
    }
    return join(root, `${REPOSITORY.replace("/", "-")}-issue-${issueNumber}.json`);
  }

  async function read(issueNumber) {
    let filePath;
    try {
      filePath = pathFor(issueNumber);
      return validateStoredReport(JSON.parse(await readFile(filePath, "utf8")), issueNumber);
    } catch (error) {
      if (error?.code === "ENOENT" || error?.code === "artifact_storage_unavailable") {
        return null;
      }
      throw error;
    }
  }

  async function write(report, isCurrent = () => true) {
    const normalized = validateStoredReport(report, report?.issueNumber);
    const filePath = pathFor(normalized.issueNumber);
    const temporaryPath = `${filePath}.${randomUUID()}.tmp`;
    await mkdir(dirname(filePath), { recursive: true });
    await writeFile(temporaryPath, `${JSON.stringify(normalized, null, 2)}\n`, "utf8");
    if (!isCurrent()) {
      await rm(temporaryPath, { force: true });
      throw storageError("artifact_write_cancelled", "The report write belongs to a closed canvas instance.");
    }
    await rename(temporaryPath, filePath);
    return normalized;
  }

  async function readArea() {
    if (!root) {
      return null;
    }
    try {
      const preferences = JSON.parse(
        await readFile(join(dirname(root), "preferences.json"), "utf8"),
      );
      return normalizeArea(preferences.area);
    } catch (error) {
      if (error?.code === "ENOENT" || error instanceof SyntaxError) {
        return null;
      }
      throw error;
    }
  }

  async function writeArea(area, isCurrent = () => true) {
    if (!root) {
      return normalizeArea(area);
    }
    const normalizedArea = normalizeArea(area);
    const filePath = join(dirname(root), "preferences.json");
    const temporaryPath = `${filePath}.${randomUUID()}.tmp`;
    await mkdir(dirname(filePath), { recursive: true });
    await writeFile(
      temporaryPath,
      `${JSON.stringify({ schemaVersion: "1.0.0", area: normalizedArea }, null, 2)}\n`,
      "utf8",
    );
    if (!isCurrent()) {
      await rm(temporaryPath, { force: true });
      throw storageError("artifact_write_cancelled", "The area write belongs to a closed canvas instance.");
    }
    await rename(temporaryPath, filePath);
    return normalizedArea;
  }

  async function readModel() {
    if (!artifactRoot) {
      return null;
    }
    try {
      const settings = JSON.parse(
        await readFile(join(artifactRoot, MODEL_SETTINGS_FILE), "utf8"),
      );
      if (
        !settings
        || typeof settings !== "object"
        || Array.isArray(settings)
        || settings.schemaVersion !== "1.0.0"
        || Object.keys(settings).length !== 2
        || !Object.hasOwn(settings, "model")
      ) {
        throw storageError(
          "model_settings_invalid",
          "The session-local investigation model setting has an invalid shape.",
        );
      }
      return settings.model;
    } catch (error) {
      if (error?.code === "ENOENT") {
        return null;
      }
      if (error instanceof SyntaxError) {
        throw storageError(
          "model_settings_invalid",
          "The session-local investigation model setting is not valid JSON.",
        );
      }
      throw error;
    }
  }

  async function writeModel(model, isCurrent = () => true) {
    if (!artifactRoot) {
      throw storageError(
        "artifact_storage_unavailable",
        "The current session does not expose a durable artifact workspace.",
      );
    }
    const normalizedModel = requireConfiguredInvestigationModel(model);
    const filePath = join(artifactRoot, MODEL_SETTINGS_FILE);
    const temporaryPath = `${filePath}.${randomUUID()}.tmp`;
    await mkdir(dirname(filePath), { recursive: true });
    await writeFile(
      temporaryPath,
      `${JSON.stringify({ schemaVersion: "1.0.0", model: normalizedModel }, null, 2)}\n`,
      "utf8",
    );
    if (!isCurrent()) {
      await rm(temporaryPath, { force: true });
      throw storageError("artifact_write_cancelled", "The model write belongs to a closed canvas instance.");
    }
    await rename(temporaryPath, filePath);
    return normalizedModel;
  }

  return {
    read,
    readArea,
    readModel,
    write,
    writeArea,
    writeModel,
    createDiagnosticCapture: () => createDiagnosticCapture(workspacePath, repositoryRoot),
    root,
  };
}

export function createDiagnosticCapture(workspacePath, repositoryRoot, {
  createId = randomUUID,
  now = () => new Date().toISOString(),
} = {}) {
  requireArtifactWorkspace(workspacePath, repositoryRoot);
  const artifactRoot = workspacePath
    ? join(workspacePath, "files", "aspnetcore-issue-triage")
    : null;
  const diagnosticRoot = artifactRoot ? join(artifactRoot, DIAGNOSTIC_DIRECTORY) : null;
  let sequence = 0;

  async function isEnabled() {
    if (!artifactRoot) {
      return false;
    }
    try {
      const settings = JSON.parse(
        await readFile(join(artifactRoot, DIAGNOSTIC_SETTINGS_FILE), "utf8"),
      );
      if (
        !settings
        || typeof settings !== "object"
        || Array.isArray(settings)
        || settings.schemaVersion !== DIAGNOSTIC_SCHEMA_VERSION
        || Object.keys(settings).length !== 2
        || typeof settings.enabled !== "boolean"
      ) {
        throw storageError(
          "diagnostic_settings_invalid",
          "The session-local diagnostic setting has an invalid shape.",
        );
      }
      return settings.enabled;
    } catch (error) {
      if (error?.code === "ENOENT") {
        return false;
      }
      if (error instanceof SyntaxError) {
        throw storageError(
          "diagnostic_settings_invalid",
          "The session-local diagnostic setting is not valid JSON.",
        );
      }
      throw error;
    }
  }

  function createRun(issueNumber) {
    if (!Number.isInteger(issueNumber) || issueNumber < 1) {
      throw storageError("invalid_issue", "issueNumber must be a positive integer");
    }
    const runId = createId();
    if (typeof runId !== "string" || !/^[A-Za-z0-9-]{16,64}$/.test(runId)) {
      throw storageError("diagnostic_identity_invalid", "The diagnostic run identity is invalid.");
    }

    async function capture({
      configuredModel = null,
      response = null,
      events = [],
      error = null,
      phase,
    }) {
      if (!(await isEnabled())) {
        return null;
      }
      if (!["response", "failure", "cleanup"].includes(phase)) {
        throw storageError("diagnostic_phase_invalid", "The diagnostic capture phase is invalid.");
      }
      const capturedAt = now();
      const envelope = createDiagnosticEnvelope({
        issueNumber,
        runId,
        sequence: ++sequence,
        capturedAt,
        phase,
        configuredModel,
        response,
        events,
        error,
      });
      const serialized = safeDiagnosticJson(envelope);
      if (Buffer.byteLength(serialized, "utf8") > MAX_DIAGNOSTIC_BYTES) {
        throw storageError(
          "diagnostic_capture_too_large",
          `The diagnostic capture exceeded the ${MAX_DIAGNOSTIC_BYTES}-byte limit.`,
        );
      }
      const filePath = join(
        diagnosticRoot,
        `dotnet-aspnetcore-issue-${issueNumber}-${runId}-${String(envelope.sequence).padStart(3, "0")}.json`,
      );
      const temporaryPath = `${filePath}.${randomUUID()}.tmp`;
      await mkdir(diagnosticRoot, { recursive: true });
      await writeFile(temporaryPath, `${serialized}\n`, "utf8");
      await rename(temporaryPath, filePath);
      return filePath;
    }

    return { capture, isEnabled, runId };
  }

  return { createRun, isEnabled, root: diagnosticRoot };
}

function createDiagnosticEnvelope({
  issueNumber,
  runId,
  sequence,
  capturedAt,
  phase,
  configuredModel,
  response,
  events,
  error,
}) {
  if (!Array.isArray(events)) {
    throw storageError("diagnostic_events_invalid", "The diagnostic event record must be an array.");
  }
  return {
    schemaVersion: DIAGNOSTIC_SCHEMA_VERSION,
    kind: "aspnetcore-issue-triage-diagnostic",
    repository: REPOSITORY,
    issueNumber,
    runId,
    sequence,
    capturedAt: requireDiagnosticString(capturedAt, "capturedAt"),
    phase,
    configuredModel: diagnosticString(configuredModel),
    response: normalizeDiagnosticResponse(response),
    events: events.map(normalizeDiagnosticEvent),
    error: normalizeDiagnosticError(error),
  };
}

function normalizeDiagnosticResponse(response) {
  const data = response?.data;
  return {
    data: {
      model: diagnosticString(data?.model),
      configuredModel: diagnosticString(data?.configuredModel),
      messageId: diagnosticString(data?.messageId),
      content: diagnosticString(data?.content),
    },
  };
}

function normalizeDiagnosticEvent(event) {
  if (!event || typeof event !== "object" || Array.isArray(event)) {
    throw storageError("diagnostic_event_invalid", "A diagnostic event record is invalid.");
  }
  const data = event.data;
  const normalized = {
    type: requireDiagnosticString(event.type, "event.type"),
    timestamp: diagnosticString(event.timestamp),
    id: diagnosticString(event.id),
    data: {},
  };
  if (data && typeof data === "object" && !Array.isArray(data)) {
    for (const key of [
      "toolName",
      "success",
      "model",
      "previousModel",
      "newModel",
      "messageId",
      "content",
      "status",
    ]) {
      if (Object.hasOwn(data, key)) {
        normalized.data[key] = typeof data[key] === "boolean"
          ? data[key]
          : diagnosticString(data[key]);
      }
    }
    if (Object.hasOwn(data, "error")) {
      normalized.data.error = normalizeDiagnosticError(data.error);
    }
  }
  return normalized;
}

function normalizeDiagnosticError(error) {
  if (!error) {
    return null;
  }
  if (typeof error === "string") {
    return { code: null, message: requireDiagnosticString(error, "error") };
  }
  if (typeof error !== "object" || Array.isArray(error)) {
    return { code: null, message: "The diagnostic operation failed." };
  }
  return {
    code: diagnosticString(error.code),
    message: requireDiagnosticString(
      typeof error.message === "string" ? error.message : "The diagnostic operation failed.",
      "error.message",
    ),
  };
}

function diagnosticString(value) {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value !== "string") {
    return null;
  }
  if (value.length > MAX_DIAGNOSTIC_STRING_LENGTH) {
    throw storageError(
      "diagnostic_capture_too_large",
      `A diagnostic string exceeded the ${MAX_DIAGNOSTIC_STRING_LENGTH}-character limit.`,
    );
  }
  return value;
}

function requireDiagnosticString(value, name) {
  const normalized = diagnosticString(value);
  if (!normalized) {
    throw storageError("diagnostic_value_invalid", `${name} must be a non-empty string.`);
  }
  return normalized;
}

function safeDiagnosticJson(value) {
  try {
    return JSON.stringify(value, (_key, nested) => {
      if (typeof nested === "bigint" || typeof nested === "function" || typeof nested === "symbol") {
        throw storageError("diagnostic_serialization_failed", "Diagnostic data is not safely serializable.");
      }
      return nested;
    }, 2);
  } catch (error) {
    if (error?.code?.startsWith("diagnostic_")) {
      throw error;
    }
    throw storageError("diagnostic_serialization_failed", `Diagnostic data could not be serialized: ${error.message}`);
  }
}

function requireArtifactWorkspace(workspacePath, repositoryRoot) {
  if (!workspacePath || !repositoryRoot) {
    return;
  }
  if (!isAbsolute(workspacePath) || !isAbsolute(repositoryRoot)) {
    throw storageError(
      "artifact_storage_invalid",
      "The durable artifact workspace and repository root must be absolute paths.",
    );
  }
  const fromRepository = relative(resolve(repositoryRoot), resolve(workspacePath));
  if (
    fromRepository === ""
    || (!fromRepository.startsWith("..") && !isAbsolute(fromRepository))
  ) {
    throw storageError(
      "artifact_storage_invalid",
      "Durable issue-triage artifacts must be stored outside the repository checkout.",
    );
  }
}

export function validateStoredReport(report, expectedIssueNumber) {
  if (!report || typeof report !== "object" || Array.isArray(report)) {
    throw storageError("report_invalid", "Stored investigation report must be an object.");
  }
  if (
    report.schemaVersion !== "1.0.0"
    || report.repository !== REPOSITORY
    || !Number.isInteger(report.issueNumber)
    || report.issueNumber < 1
    || report.issueNumber !== expectedIssueNumber
    || report.issueUrl !== `https://github.com/${REPOSITORY}/issues/${report.issueNumber}`
    || typeof report.area !== "string"
    || typeof report.createdAt !== "string"
    || typeof report.content !== "string"
    || !report.content
  ) {
    throw storageError("report_invalid", "Stored investigation report has an invalid shape.");
  }
  const model = requireConfiguredInvestigationModel(report.model);
  return {
    schemaVersion: "1.0.0",
    repository: REPOSITORY,
    issueNumber: report.issueNumber,
    issueUrl: report.issueUrl,
    area: report.area,
    createdAt: report.createdAt,
    model,
    messageId: typeof report.messageId === "string" ? report.messageId : "",
    skill: ".github/skills/investigate-issue/SKILL.md",
    skillDigest: typeof report.skillDigest === "string" ? report.skillDigest : "",
    content: report.content,
  };
}

function storageError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
