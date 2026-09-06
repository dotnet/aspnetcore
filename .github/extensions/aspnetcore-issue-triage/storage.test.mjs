import assert from "node:assert/strict";
import { access, mkdtemp, readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, relative } from "node:path";
import test from "node:test";

import { createReportStore } from "./storage.mjs";

const TEST_MODEL = "test-model";

test("reports persist under a durable issue identity, not a panel ID", async () => {
  const workspace = await mkdtemp(join(tmpdir(), "aspnetcore-triage-"));
  const store = createReportStore(workspace);
  const report = {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    issueNumber: 123,
    issueUrl: "https://github.com/dotnet/aspnetcore/issues/123",
    area: "area-blazor",
    createdAt: "2026-09-06T19:00:00Z",
    model: TEST_MODEL,
    messageId: "message-1",
    skill: ".github/skills/investigate-issue/SKILL.md",
    skillDigest: "a".repeat(64),
    content: "advisory report",
  };
  await store.write(report);

  assert.deepEqual(await store.read(123), report);
  const filePath = join(
    workspace,
    "files",
    "aspnetcore-issue-triage",
    "reports",
    "dotnet-aspnetcore-issue-123.json",
  );
  assert.match(await readFile(filePath, "utf8"), /"issueNumber": 123/);
  assert.doesNotMatch(filePath, /instance|panel/);
});

test("the selected area persists at session scope for provider rehydration", async () => {
  const workspace = await mkdtemp(join(tmpdir(), "aspnetcore-triage-"));
  const first = createReportStore(workspace);
  await first.writeArea("area-perf");

  const rehydrated = createReportStore(workspace);
  assert.equal(await rehydrated.readArea(), "area-perf");
});

test("the session-local model setting uses the fixed artifact root outside the checkout", async () => {
  const workspace = await mkdtemp(join(tmpdir(), "aspnetcore-triage-"));
  const repositoryRoot = await mkdtemp(join(tmpdir(), "aspnetcore-repo-"));
  const store = createReportStore(workspace, repositoryRoot);

  assert.equal(await store.readModel(), null);
  await store.writeModel(TEST_MODEL);

  assert.equal(await store.readModel(), TEST_MODEL);
  const settingsPath = join(
    workspace,
    "files",
    "aspnetcore-issue-triage",
    "model-settings.json",
  );
  assert.match(await readFile(settingsPath, "utf8"), /"model": "test-model"/);
  assert.ok(relative(repositoryRoot, settingsPath).startsWith(".."));

  await writeFile(settingsPath, "{", "utf8");
  await assert.rejects(
    store.readModel(),
    (error) => error.code === "model_settings_invalid",
  );
});

test("artifact storage rejects a workspace inside the repository checkout", () => {
  assert.throws(
    () => createReportStore("/repo/.copilot/artifacts", "/repo"),
    (error) => error.code === "artifact_storage_invalid",
  );
});

test("cancelled durable writes never replace session artifacts", async () => {
  const workspace = await mkdtemp(join(tmpdir(), "aspnetcore-triage-"));
  const store = createReportStore(workspace);
  await assert.rejects(
    store.writeArea("area-blazor", () => false),
    (error) => error.code === "artifact_write_cancelled",
  );
  assert.equal(await store.readArea(), null);

  const preferencesPath = join(
    workspace,
    "files",
    "aspnetcore-issue-triage",
    "preferences.json",
  );
  await assert.rejects(access(preferencesPath), (error) => error.code === "ENOENT");
});
