import assert from "node:assert/strict";
import {
  access,
  mkdir,
  mkdtemp,
  readdir,
  readFile,
  rm,
  writeFile,
} from "node:fs/promises";
import { join, relative } from "node:path";
import test from "node:test";

import { cleanupIsolatedResources } from "./isolation.mjs";
import {
  DIAGNOSTIC_DIRECTORY,
  DIAGNOSTIC_SETTINGS_FILE,
  MAX_DIAGNOSTIC_BYTES,
  createReportStore,
} from "./storage.mjs";

const TEST_MODEL = "test-model";

async function testDirectories() {
  const workspace = await mkdtemp(join(process.cwd(), ".diagnostic-workspace-"));
  const repositoryRoot = await mkdtemp(join(process.cwd(), ".diagnostic-repository-"));
  return { workspace, repositoryRoot };
}

async function removeDirectories(...directories) {
  await Promise.all(directories.map((directory) => rm(directory, { recursive: true, force: true })));
}

async function enableDiagnostics(workspace) {
  await mkdir(join(workspace, "files", "aspnetcore-issue-triage"), { recursive: true });
  await writeFile(
    join(workspace, "files", "aspnetcore-issue-triage", DIAGNOSTIC_SETTINGS_FILE),
    JSON.stringify({ schemaVersion: "1.0.0", enabled: true }),
    "utf8",
  );
}

test("diagnostic capture is disabled by default and writes nothing", async () => {
  const { workspace, repositoryRoot } = await testDirectories();
  try {
    const store = createReportStore(workspace, repositoryRoot);
    const run = store.createDiagnosticCapture().createRun(123);

    assert.equal(await run.isEnabled(), false);
    assert.equal(
      await run.capture({
        phase: "failure",
        response: { data: { model: TEST_MODEL, content: "private response" } },
        events: [{ type: "tool.execution_complete", data: { toolName: "read", success: false } }],
        error: new Error("report validation failed"),
      }),
      null,
    );
    await assert.rejects(
      access(join(workspace, "files", "aspnetcore-issue-triage", DIAGNOSTIC_DIRECTORY)),
      (error) => error.code === "ENOENT",
    );
  } finally {
    await removeDirectories(workspace, repositoryRoot);
  }
});

test("enabled diagnostic capture stays in the artifact root and rejects checkout storage", async () => {
  const { workspace, repositoryRoot } = await testDirectories();
  try {
    await enableDiagnostics(workspace);
    const store = createReportStore(workspace, repositoryRoot);
    const run = store.createDiagnosticCapture().createRun(123);
    const filePath = await run.capture({
      phase: "response",
      configuredModel: TEST_MODEL,
      response: { data: { model: TEST_MODEL, messageId: "message-1", content: "response" } },
      events: [],
    });

    assert.ok(filePath);
    assert.ok(relative(join(workspace, "files", "aspnetcore-issue-triage"), filePath)
      .startsWith(`${DIAGNOSTIC_DIRECTORY}/`));
    assert.ok(relative(repositoryRoot, filePath).startsWith(".."));
    await assert.rejects(
      Promise.resolve().then(() => createReportStore(join(repositoryRoot, "artifacts"), repositoryRoot)),
      (error) => error.code === "artifact_storage_invalid",
    );
  } finally {
    await removeDirectories(workspace, repositoryRoot);
  }
});

test("failure response and events are persisted before isolated cleanup", async () => {
  const { workspace, repositoryRoot } = await testDirectories();
  try {
    await enableDiagnostics(workspace);
    const store = createReportStore(workspace, repositoryRoot);
    const run = store.createDiagnosticCapture().createRun(123);
    const response = {
      data: {
        model: TEST_MODEL,
        configuredModel: TEST_MODEL,
        messageId: "message-1",
        content: "untrusted report response",
      },
    };
    const events = [
      { type: "session.model_change", data: { previousModel: "old-model", newModel: TEST_MODEL } },
      { type: "tool.execution_complete", data: { toolName: "read", success: false, error: { code: "read_failed", message: "denied" } } },
    ];
    const filePath = await run.capture({
      phase: "failure",
      configuredModel: TEST_MODEL,
      response,
      events,
      error: { code: "investigation_contract_invalid", message: "report validation failed" },
    });
    let presentBeforeCleanup = false;
    await cleanupIsolatedResources(
      {
        disconnect: async () => {
          try {
            await access(filePath);
            presentBeforeCleanup = true;
          } catch {
            presentBeforeCleanup = false;
          }
        },
      },
      { stop: async () => {} },
      50,
    );

    assert.equal(presentBeforeCleanup, true);
    const captured = JSON.parse(await readFile(filePath, "utf8"));
    assert.equal(captured.phase, "failure");
    assert.deepEqual(captured.response.data, response.data);
    assert.deepEqual(captured.events, [
      {
        type: "session.model_change",
        timestamp: null,
        id: null,
        data: { previousModel: "old-model", newModel: TEST_MODEL },
      },
      {
        type: "tool.execution_complete",
        timestamp: null,
        id: null,
        data: {
          toolName: "read",
          success: false,
          error: { code: "read_failed", message: "denied" },
        },
      },
    ]);
    assert.deepEqual(captured.error, {
      code: "investigation_contract_invalid",
      message: "report validation failed",
    });
    assert.equal(Object.hasOwn(captured, "environment"), false);
    assert.equal(Object.hasOwn(captured, "process"), false);
  } finally {
    await removeDirectories(workspace, repositoryRoot);
  }
});

test("diagnostic outcomes use distinct files and preserve earlier captures", async () => {
  const { workspace, repositoryRoot } = await testDirectories();
  try {
    await enableDiagnostics(workspace);
    const store = createReportStore(workspace, repositoryRoot);
    const run = store.createDiagnosticCapture().createRun(123);
    const first = await run.capture({ phase: "response", response: { data: {} }, events: [] });
    const second = await run.capture({
      phase: "cleanup",
      response: { data: {} },
      events: [],
      error: { code: "investigation_cleanup_failed", message: "cleanup failed" },
    });

    assert.notEqual(first, second);
    assert.deepEqual(
      (await readdir(join(workspace, "files", "aspnetcore-issue-triage", DIAGNOSTIC_DIRECTORY))).sort(),
      [first, second].map((path) => path.split("/").pop()).sort(),
    );
  } finally {
    await removeDirectories(workspace, repositoryRoot);
  }
});

test("diagnostic capture rejects oversized untrusted content", async () => {
  const { workspace, repositoryRoot } = await testDirectories();
  try {
    await enableDiagnostics(workspace);
    const store = createReportStore(workspace, repositoryRoot);
    const run = store.createDiagnosticCapture().createRun(123);

    await assert.rejects(
      run.capture({
        phase: "response",
        response: {
          data: {
            content: "x".repeat(MAX_DIAGNOSTIC_BYTES),
          },
        },
        events: [],
      }),
      (error) => error.code === "diagnostic_capture_too_large",
    );
  } finally {
    await removeDirectories(workspace, repositoryRoot);
  }
});
