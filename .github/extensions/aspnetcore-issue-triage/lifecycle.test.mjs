import assert from "node:assert/strict";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";

import { createForegroundHandoff } from "./handoff.mjs";
import { configureServer, startInstance, stopInstance } from "./server.mjs";
import { createTriageController } from "./state.mjs";
import { createAreaStore } from "./storage.mjs";

function queue(area = "area-blazor", count = 1) {
  return {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    area,
    generatedAt: "2026-09-07T00:00:00Z",
    predicate: `label:${area}`,
    coverage: { complete: true, sourceOpenAreaCount: count, retrievedOpenAreaCount: count },
    issues: Array.from({ length: count }, (_, index) => ({
      number: index + 1,
      title: `Untrusted issue ${index + 1}`,
      url: `https://github.com/dotnet/aspnetcore/issues/${index + 1}`,
      labels: [area],
      whyIncluded: ["Open issue", `Labeled ${area}`],
    })),
  };
}

const store = () => ({ readArea: async () => null, writeArea: async () => {} });
const deferred = () => Promise.withResolvers();

test("queue pages preserve full coverage without broadcasting all rows", async () => {
  const controller = createTriageController({
    load: async ({ area }) => queue(area, 125),
    areaStore: store(),
  });
  await controller.initialize();
  assert.equal(Object.hasOwn(controller.getState().snapshot, "items"), false);
  assert.deepEqual([0, 50, 100].map((offset) => controller.getPage({ offset, limit: 50 }).items.length), [50, 50, 25]);
  const before = controller.getState().revision;
  controller.select({ itemId: controller.getPage().items[0].id });
  assert.ok(controller.getState().revision > before);
});

test("selection during refresh cannot permanently block later refreshes", async () => {
  const waiting = deferred();
  let calls = 0;
  const controller = createTriageController({
    load: ({ area }) => ++calls === 2 ? waiting.promise : Promise.resolve(queue(area)),
    areaStore: store(),
  });
  await controller.initialize();
  const itemId = controller.getPage().items[0].id;
  const refresh = controller.refresh();
  assert.throws(() => controller.select({ itemId }), { code: "refresh_in_progress" });
  waiting.resolve(queue());
  await refresh;
  await controller.refresh({ area: "area-mvc" });
  assert.equal(calls, 3);
  assert.equal(controller.getState().snapshot.area, "area-mvc");
});

test("disposal during area persistence cannot publish a new snapshot", async () => {
  const entered = deferred();
  const waiting = deferred();
  const controller = createTriageController({
    load: async () => queue(),
    areaStore: { writeArea: async () => { entered.resolve(); await waiting.promise; } },
  });
  const initializing = controller.initialize();
  await entered.promise;
  controller.dispose();
  waiting.resolve();
  await assert.rejects(initializing, { code: "controller_disposed" });
  assert.equal(controller.getState().snapshot, null);
});

test("unknown send outcome remains unknown in controller state", async () => {
  const launch = createForegroundHandoff({
    readIssue: async () => ({
      repository: "dotnet/aspnetcore", number: 1,
      url: "https://github.com/dotnet/aspnetcore/issues/1",
      state: "open", milestone: null, labels: ["area-blazor"], isPullRequest: false,
    }),
    send: async () => { throw new Error("Connection closed after submission"); },
  });
  const controller = createTriageController({ load: async () => queue(), areaStore: store() });
  await controller.initialize();
  await controller.investigate({ itemId: controller.getPage().items[0].id, destination: "current" }, launch.dispatch);
  assert.equal(controller.getState().snapshot.handoff.phase, "unknown");
  assert.equal(controller.getState().snapshot.handoff.destination, "current");
});

test("selection change cancels a pending canonical read without sending", async () => {
  const entered = deferred();
  const release = deferred();
  let sends = 0;
  const launch = createForegroundHandoff({
    readIssue: async () => {
      entered.resolve();
      await release.promise;
      return {
        repository: "dotnet/aspnetcore", number: 1,
        url: "https://github.com/dotnet/aspnetcore/issues/1",
        state: "open", milestone: null, labels: ["area-blazor"], isPullRequest: false,
      };
    },
    send: async () => { sends++; return "receipt"; },
  });
  const controller = createTriageController({ load: async () => queue("area-blazor", 2), areaStore: store() });
  await controller.initialize();
  const [first, second] = controller.getPage().items;
  const pending = controller.investigate({ itemId: first.id, destination: "new-child" }, launch.dispatch);
  await entered.promise;
  controller.select({ itemId: second.id });
  release.resolve();
  await pending;
  assert.equal(sends, 0);
  assert.equal(controller.getState().snapshot.selectedIssue.number, 2);
  assert.equal(controller.getState().snapshot.handoff.phase, "idle");
});

test("late sent receipt cannot attach to a different selected issue", async () => {
  const entered = deferred();
  const release = deferred();
  const controller = createTriageController({ load: async () => queue("area-blazor", 2), areaStore: store() });
  await controller.initialize();
  const [first, second] = controller.getPage().items;
  const pending = controller.investigate({ itemId: first.id, destination: "new-child" }, async () => {
    entered.resolve();
    await release.promise;
    return { status: "sent", messageId: "receipt", queued: null };
  });
  await entered.promise;
  assert.equal(controller.getState().snapshot.handoff.destination, "new-child");
  controller.select({ itemId: second.id });
  release.resolve();
  await pending;
  assert.equal(controller.getState().snapshot.selectedIssue.number, 2);
  assert.equal(controller.getState().snapshot.handoff.phase, "idle");
  assert.equal(controller.getState().snapshot.handoff.destination, null);
});

test("stopping during initialization cannot resurrect a closed panel", async () => {
  const entered = deferred();
  const release = deferred();
  configureServer({
    load: async () => { entered.resolve(); await release.promise; return queue(); },
    store: store(),
    launch: { dispatch: async () => { throw new Error("Unexpected dispatch"); } },
  });
  const entry = await startInstance("stop-initializing", {}, () => {});
  await entered.promise;
  await stopInstance("stop-initializing");
  release.resolve();
  await entry.initialization;
  assert.equal(entry.controller.getState().snapshot, null);
  assert.equal(entry.server.listening, false);
});

test("reopened provider retains the selected area preference", async () => {
  let savedArea = null;
  configureServer({
    load: async ({ area }) => queue(area),
    store: {
      readArea: async () => savedArea,
      writeArea: async (area) => { savedArea = area; },
    },
    launch: { dispatch: async () => { throw new Error("Unexpected dispatch"); } },
  });
  const first = await startInstance("area-reload", { area: "area-blazor" }, () => {});
  await first.initialization;
  await first.controller.refresh({ area: "area-mvc" });
  await stopInstance("area-reload");
  const reopened = await startInstance("area-reload", { area: "area-blazor" }, () => {});
  try {
    await reopened.initialization;
    assert.equal(reopened.controller.getState().snapshot.area, "area-mvc");
  } finally {
    await stopInstance("area-reload");
  }
});

test("real HTTP handoff, retired routes, SSE, and repeated lifecycle", { timeout: 10_000 }, async () => {
  const prompts = [];
  configureServer({
    load: async ({ area }) => queue(area),
    store: store(),
    launch: createForegroundHandoff({
      readIssue: async () => ({
        repository: "dotnet/aspnetcore", number: 1,
        url: "https://github.com/dotnet/aspnetcore/issues/1",
        state: "open", milestone: null, labels: ["area-blazor"], isPullRequest: false,
      }),
      send: async ({ prompt }) => { prompts.push(prompt); return "receipt"; },
    }),
  });
  const [entry, again] = await Promise.all([
    startInstance("http-test", {}, () => {}),
    startInstance("http-test", {}, () => {}),
  ]);
  assert.equal(entry.url, again.url);
  const request = (path, body) => fetch(new URL(path + new URL(entry.url).search, entry.url), body === undefined ? {} : {
    method: "POST",
    headers: { Origin: new URL(entry.url).origin, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  try {
    await entry.initialization;
    const events = await request("/events");
    const reader = events.body.getReader();
    try {
      const first = await reader.read();
      assert.match(new TextDecoder().decode(first.value), /data:.*"snapshot"/);
    } finally {
      await reader.cancel();
    }
    const state = await (await request("/api/state")).json();
    const pageUrl = new URL("/api/issues", entry.url);
    pageUrl.searchParams.set("token", entry.token);
    pageUrl.searchParams.set("offset", "0");
    pageUrl.searchParams.set("limit", "25");
    const page = await (await fetch(pageUrl)).json();
    assert.equal(page.snapshotId, state.snapshot.id);
    const itemId = page.items[0].id;
    for (const body of [{ itemId }, { itemId, destination: "reuse" }, { itemId, destination: "current", issueNumber: 9 }]) {
      assert.equal((await request("/api/investigate", body)).status, 400);
    }
    assert.equal(prompts.length, 0);
    for (const destination of ["current", "new-child"]) {
      const receipt = await request("/api/investigate", { itemId, destination });
      assert.equal(receipt.status, 202);
      const handoff = (await receipt.json()).snapshot.handoff;
      assert.equal(handoff.phase, "sent");
      assert.equal(handoff.destination, destination);
    }
    assert.equal(prompts.length, 2);
    assert.match(prompts[0], /here in this foreground session/);
    assert.doesNotMatch(prompts[0], /create_session|open_issue_session/);
    assert.match(prompts[1], /create_session/);
    assert.match(prompts[1], /detached: false/);
    for (const prompt of prompts) {
      assert.doesNotMatch(prompt, /Untrusted issue|model:|open_issue_session/);
    }
    for (const path of ["draft", "undo", "discuss", "preview", "publish", "resolve-publication", "select-saved"]) {
      assert.equal((await request(`/api/${path}`, {})).status, 404, path);
    }
    assert.equal((await request("/api/saved")).status, 404);
    assert.equal(prompts.length, 2);
    assert.equal((await fetch(new URL("/api/state", entry.url))).status, 403);
  } finally {
    await stopInstance("http-test");
  }
});

test("area storage surfaces corrupt settings and leaves old artifacts unchanged", async () => {
  const root = await mkdtemp(join(tmpdir(), "triage-preferences-"));
  try {
    const areaStore = createAreaStore(root, join(root, "repo"));
    await areaStore.writeArea("area-blazor");
    const folder = join(root, "files", "aspnetcore-issue-triage");
    const historical = join(folder, "model-settings.json");
    await writeFile(historical, "inert historical fixture");
    await areaStore.writeArea("area-mvc");
    assert.equal(await readFile(historical, "utf8"), "inert historical fixture");
    await writeFile(join(folder, "preferences.json"), "{broken");
    await assert.rejects(areaStore.readArea(), { code: "preferences_invalid" });
    assert.throws(() => createAreaStore(join(root, "repo", "..nested"), join(root, "repo")));
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});
