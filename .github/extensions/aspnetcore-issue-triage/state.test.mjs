import assert from "node:assert/strict";
import test from "node:test";

import { createTriageController } from "./state.mjs";
import { AREA_OPTIONS } from "./taxonomy.mjs";

const TEST_MODEL = "test-model";

function queue(area, numbers) {
  return {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    area,
    areaOptions: AREA_OPTIONS,
    generatedAt: "2026-09-06T18:00:00Z",
    predicate: `label:${area}`,
    coverage: {
      complete: true,
      sourceOpenAreaCount: numbers.length,
      retrievedOpenAreaCount: numbers.length,
      qualifyingCount: numbers.length,
      pages: 1,
      limitation: null,
      warnings: [],
      rateLimit: null,
      searchCapApplied: false,
      retrieval: "fixture",
    },
    issues: numbers.map((number) => ({
      number,
      title: `Issue ${number}`,
      url: `https://github.com/dotnet/aspnetcore/issues/${number}`,
      author: "reporter",
      createdAt: "2026-01-01T00:00:00Z",
      updatedAt: "2026-01-02T00:00:00Z",
      labels: [area],
      whyIncluded: ["Open issue"],
    })),
  };
}

function store() {
  const reports = new Map();
  let area = null;
  return {
    read: async (number) => reports.get(number) ?? null,
    readArea: async () => area,
    write: async (report) => {
      reports.set(report.issueNumber, report);
      return report;
    },
    writeArea: async (value) => {
      area = value;
      return value;
    },
  };
}

test("controller exposes every issue through explicit local pages", async () => {
  let id = 0;
  const controller = createTriageController({
    load: async ({ area }) => queue(area, Array.from({ length: 125 }, (_, index) => index + 1)),
    reportStore: store(),
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();

  assert.equal(Object.hasOwn(controller.getState().snapshot, "items"), false);
  const first = controller.getPage({ offset: 0, limit: 50 });
  const second = controller.getPage({ offset: 50, limit: 50 });
  const third = controller.getPage({ offset: 100, limit: 50 });
  assert.equal(first.total, 125);
  assert.equal(first.items.length, 50);
  assert.equal(second.items.length, 50);
  assert.equal(third.items.length, 25);
  assert.equal(third.nextOffset, null);
});

test("supported areas remain available when the initial retrieval fails", async () => {
  const controller = createTriageController({
    load: async () => {
      throw new Error("temporary GitHub failure");
    },
    reportStore: store(),
  });
  await assert.rejects(controller.initialize(), /temporary GitHub failure/);
  const state = controller.getState();
  assert.equal(state.snapshot, null);
  assert.equal(state.requestedArea, "area-blazor");
  assert.ok(state.areaOptions.some((option) => option.label === "area-blazor"));
});

test("scope switching reloads server data and rejects stale selections", async () => {
  let id = 0;
  const controller = createTriageController({
    load: async ({ area }) => queue(area, area === "area-blazor" ? [1] : [2]),
    reportStore: store(),
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const staleId = controller.getPage({ offset: 0, limit: 25 }).items[0].id;
  await controller.select({ itemId: staleId });
  await controller.refresh({ area: "area-mvc" });

  assert.equal(controller.getState().snapshot.area, "area-mvc");
  assert.equal(controller.getState().snapshot.selectedIssue, null);
  assert.throws(
    () => controller.queueInvestigation({ itemId: staleId }),
    (error) => error.code === "stale_selection",
  );
});

test("issue-bound draft saves reject a changed selection", async () => {
  const reports = new Map();
  const workspaces = new Map();
  for (const number of [1, 2]) {
    reports.set(number, {
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      issueNumber: number,
      issueUrl: `https://github.com/dotnet/aspnetcore/issues/${number}`,
      area: "area-blazor",
      createdAt: "2026-01-01T00:00:00Z",
      model: TEST_MODEL,
      messageId: `message-${number}`,
      skill: ".github/skills/investigate-issue/SKILL.md",
      skillDigest: "a".repeat(64),
      content: `report-${number}`,
    });
    workspaces.set(number, {
      issueNumber: number,
      draft: { content: `draft-${number}`, revision: 1 },
    });
  }
  const reportStore = {
    ...store(),
    read: async (number) => reports.get(number) ?? null,
    seedWorkspace: async (report) => workspaces.get(report.issueNumber),
    saveDraft: async (number, content, revision) => {
      const current = workspaces.get(number);
      assert.equal(current.draft.revision, revision);
      const next = {
        ...current,
        draft: { content, revision: revision + 1 },
      };
      workspaces.set(number, next);
      return next;
    },
  };
  let id = 0;
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [1, 2]),
    reportStore,
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const items = controller.getPage({ offset: 0, limit: 25 }).items;
  await controller.select({ itemId: items[0].id });
  await controller.select({ itemId: items[1].id });

  await assert.rejects(
    controller.saveDraft({
      issueNumber: 1,
      content: "draft-1-edited",
      provenance: "human",
      revision: 1,
    }),
    (error) => error.code === "stale_selection",
  );
  assert.equal(workspaces.get(2).draft.content, "draft-2");
});

test("queued investigations preserve durable reports by issue identity", async () => {
  let id = 0;
  const reports = store();
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [42]),
    reportStore: reports,
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const itemId = controller.getPage({ offset: 0, limit: 25 }).items[0].id;
  const { job } = controller.queueInvestigation({ itemId });
  controller.beginInvestigation(job);
  await controller.completeInvestigation(job, {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    issueNumber: 42,
    issueUrl: "https://github.com/dotnet/aspnetcore/issues/42",
    area: "area-blazor",
    createdAt: "2026-09-06T19:00:00Z",
    model: TEST_MODEL,
    messageId: "message-1",
    skill: ".github/skills/investigate-issue/SKILL.md",
    skillDigest: "a".repeat(64),
    content: "report",
  });
  assert.equal(controller.getState().snapshot.investigation.phase, "complete");

  await controller.select({ itemId });
  assert.equal(controller.getState().snapshot.selectedReport.issueNumber, 42);
});

test("an investigation report is not attached to a different selected issue", async () => {
  let id = 0;
  const reports = store();
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [1, 2]),
    reportStore: reports,
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const [first, second] = controller.getPage({ offset: 0, limit: 25 }).items;
  const { job } = controller.queueInvestigation({ itemId: first.id });
  controller.beginInvestigation(job);
  await controller.select({ itemId: second.id });
  await controller.completeInvestigation(job, {
    issueNumber: 1,
    content: "report for issue 1",
  });

  const state = controller.getState().snapshot;
  assert.equal(state.selectedIssue.number, 2);
  assert.equal(state.selectedReport, null);
  await controller.select({ itemId: first.id });
  assert.equal(controller.getState().snapshot.selectedReport.issueNumber, 1);
});

test("overlapping report reads cannot overwrite the latest selection", async () => {
  let id = 0;
  let releaseFirst;
  const firstRead = new Promise((resolve) => {
    releaseFirst = resolve;
  });
  const reports = store();
  reports.read = async (number) => {
    if (number === 1) {
      await firstRead;
    }
    return { issueNumber: number };
  };
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [1, 2]),
    reportStore: reports,
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const [first, second] = controller.getPage({ offset: 0, limit: 25 }).items;

  const selectFirst = controller.select({ itemId: first.id });
  await controller.select({ itemId: second.id });
  releaseFirst();
  await selectFirst;

  const state = controller.getState().snapshot;
  assert.equal(state.selectedIssue.number, 2);
  assert.equal(state.selectedReport.issueNumber, 2);
});

test("a pending old report read cannot overwrite a completed investigation", async () => {
  let id = 0;
  let releaseRead;
  const pendingRead = new Promise((resolve) => {
    releaseRead = resolve;
  });
  const reports = store();
  reports.read = async () => {
    await pendingRead;
    return { issueNumber: 1, content: "old" };
  };
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [1]),
    reportStore: reports,
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const item = controller.getPage({ offset: 0, limit: 25 }).items[0];
  const { job } = controller.queueInvestigation({ itemId: item.id });
  controller.beginInvestigation(job);
  const selection = controller.select({ itemId: item.id });
  await controller.completeInvestigation(job, { issueNumber: 1, content: "new" });
  releaseRead();
  await selection;

  assert.equal(controller.getState().snapshot.selectedReport.content, "new");
});

test("different scope refreshes are rejected while a refresh is active", async () => {
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const controller = createTriageController({
    load: async ({ area }) => {
      await pending;
      return queue(area, []);
    },
    reportStore: store(),
  });
  const refresh = controller.initialize();
  assert.throws(
    () => controller.refresh({ area: "area-mvc" }),
    (error) => error.code === "refresh_in_progress",
  );
  release();
  await refresh;
});

test("refresh and investigation cannot overlap", async () => {
  let id = 0;
  const controller = createTriageController({
    load: async ({ area }) => queue(area, [1]),
    reportStore: store(),
    createId: () => `opaque-item-id-${String(++id).padStart(4, "0")}`,
  });
  await controller.initialize();
  const item = controller.getPage({ offset: 0, limit: 25 }).items[0];
  const { job } = controller.queueInvestigation({ itemId: item.id });
  controller.beginInvestigation(job);
  assert.throws(
    () => controller.refresh({ area: "area-mvc" }),
    (error) => error.code === "investigation_in_progress",
  );

  controller.failInvestigation(job, new Error("finished"));
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const refreshing = createTriageController({
    load: async ({ area }) => {
      await pending;
      return queue(area, [1]);
    },
    reportStore: store(),
  });
  const refresh = refreshing.initialize();
  assert.throws(
    () => refreshing.queueInvestigation({ itemId: "opaque-item-id-0001" }),
    (error) => error.code === "refresh_in_progress",
  );
  release();
  await refresh;
});
