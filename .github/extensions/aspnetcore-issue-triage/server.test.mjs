import assert from "node:assert/strict";
import { Readable } from "node:stream";
import test from "node:test";

import {
  configureServer,
  isAllowedPostRequest,
  hasInstanceToken,
  parseItemRequest,
  parsePageRequest,
  parseRefreshRequest,
  readJsonBody,
  startInstance,
  stopInstance,
} from "./server.mjs";

test("refresh accepts only one supported public area", () => {
  assert.deepEqual(parseRefreshRequest({ area: "area-blazor" }), {
    area: "area-blazor",
  });
  assert.throws(
    () => parseRefreshRequest({ area: "area-runtime" }),
    (error) => error.code === "invalid_area",
  );
  assert.throws(
    () => parseRefreshRequest({ area: "area-blazor", source: "fixture" }),
    (error) => error.code === "invalid_refresh",
  );
});

test("selection and investigation requests accept only opaque item IDs", () => {
  assert.deepEqual(parseItemRequest({ itemId: "opaque-item-id-123" }), {
    itemId: "opaque-item-id-123",
  });
  assert.throws(
    () => parseItemRequest({ itemId: "opaque-item-id-123", number: 123 }),
    (error) => error.code === "invalid_selection",
  );
  assert.throws(
    () => parseItemRequest({ itemId: "../issues/123" }),
    (error) => error.code === "invalid_selection",
  );
});

test("page requests are explicit and bounded", () => {
  assert.deepEqual(
    parsePageRequest(new URLSearchParams("offset=50&limit=100")),
    { offset: 50, limit: 100 },
  );
  assert.throws(
    () => parsePageRequest(new URLSearchParams("offset=0&limit=1000")),
    (error) => error.code === "invalid_page",
  );
  assert.throws(
    () => parsePageRequest(new URLSearchParams("offset=0&limit=50&url=https://attacker.example")),
    (error) => error.code === "invalid_page",
  );
});

test("POST protection permits loopback same-origin and rejects forged origins", () => {
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "http://127.0.0.1:43123",
      "sec-fetch-site": "same-origin",
    },
  }), true);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "https://attacker.example",
      "sec-fetch-site": "cross-site",
    },
  }), false);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "attacker.example:43123",
      origin: "http://attacker.example:43123",
      "sec-fetch-site": "same-origin",
    },
  }), false);
});

test("every HTTP route requires an unguessable per-instance capability token", () => {
  const token = "a".repeat(43);
  assert.equal(
    hasInstanceToken(new URL(`http://127.0.0.1:43123/?token=${token}`), token),
    true,
  );
  assert.equal(
    hasInstanceToken(new URL("http://127.0.0.1:43123/"), token),
    false,
  );
  assert.equal(
    hasInstanceToken(new URL("http://127.0.0.1:43123/?token=wrong"), token),
    false,
  );
});

test("request bodies reject oversized and invalid JSON payloads", async () => {
  await assert.rejects(
    readJsonBody(Readable.from([JSON.stringify({ value: "x".repeat(5_000) })])),
    (error) => error.code === "request_too_large",
  );
  await assert.rejects(
    readJsonBody(Readable.from(["{not-json"])),
    (error) => error.code === "invalid_json",
  );
});

test("concurrent opens share one usable server and capability URL", async () => {
  let releaseArea;
  const areaReady = new Promise((resolve) => {
    releaseArea = resolve;
  });
  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => {
        await areaReady;
        return "area-blazor";
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });

  const first = startInstance("concurrent-open-instance", {}, () => {});
  const second = startInstance("concurrent-open-instance", {}, () => {});
  releaseArea();
  const [firstEntry, secondEntry] = await Promise.all([first, second]);
  assert.strictEqual(firstEntry, secondEntry);
  assert.equal(firstEntry.url, secondEntry.url);
  assert.equal((await fetch(firstEntry.url)).status, 200);
  assert.equal((await fetch(secondEntry.url)).status, 200);
  await stopInstance("concurrent-open-instance");
});

test("failed startup releases its reservation for a later open", async () => {
  configureServer({
    load: async () => {
      throw new Error("unused");
    },
    store: {
      read: async () => null,
      readArea: async () => {
        throw new Error("startup failed");
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });
  await assert.rejects(
    startInstance("retry-open-instance", {}, () => {}),
    /startup failed/,
  );

  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => "area-blazor",
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });
  const entry = await startInstance("retry-open-instance", {}, () => {});
  assert.equal((await fetch(entry.url)).status, 200);
  await stopInstance("retry-open-instance");
});

test("start stop start stop operations execute in invocation order", async () => {
  let releaseFirstArea;
  const firstAreaReady = new Promise((resolve) => {
    releaseFirstArea = resolve;
  });
  let areaReads = 0;
  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => {
        areaReads += 1;
        if (areaReads === 1) {
          await firstAreaReady;
        }
        return "area-blazor";
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });

  const firstOpen = startInstance("start-stop-start-stop-instance", {}, () => {});
  const firstStop = stopInstance("start-stop-start-stop-instance");
  const secondOpen = startInstance("start-stop-start-stop-instance", {}, () => {});
  const secondStop = stopInstance("start-stop-start-stop-instance");
  releaseFirstArea();

  const firstEntry = await firstOpen;
  await firstStop;
  const secondEntry = await secondOpen;
  await secondStop;
  assert.equal(areaReads, 2);
  assert.notEqual(firstEntry.url, secondEntry.url);
  await assert.rejects(fetch(firstEntry.url));
  await assert.rejects(fetch(secondEntry.url));
});

test("a stopped initialization cannot overwrite a fresh instance area", async () => {
  let releaseStoppedLoad;
  const stoppedLoadGate = new Promise((resolve) => {
    releaseStoppedLoad = resolve;
  });
  let stoppedLoadStarted;
  const stoppedLoadObserved = new Promise((resolve) => {
    stoppedLoadStarted = resolve;
  });
  const writes = [];
  configureServer({
    load: async ({ area }) => {
      if (area === "area-blazor") {
        stoppedLoadStarted();
        await stoppedLoadGate;
      }
      return {
        schemaVersion: "1.0.0",
        repository: "dotnet/aspnetcore",
        area,
        areaOptions: [],
        generatedAt: "2026-09-06T20:00:00Z",
        predicate: `label:${area}`,
        coverage: {
          complete: true,
          sourceOpenAreaCount: 0,
          retrievedOpenAreaCount: 0,
          qualifyingCount: 0,
          uncertainMembershipCount: 0,
          pages: 1,
          limitation: null,
          warnings: [],
          rateLimit: null,
          searchCapApplied: false,
          retrieval: "fixture",
        },
        issues: [],
      };
    },
    store: {
      read: async () => null,
      readArea: async () => null,
      write: async (report) => report,
      writeArea: async (area, isCurrent) => {
        if (isCurrent()) {
          writes.push(area);
        }
        return area;
      },
    },
    scheduleInvestigation: () => {},
  });

  const firstEntry = await startInstance(
    "stopped-initialization-instance",
    { area: "area-blazor" },
    () => {},
  );
  await stoppedLoadObserved;
  const firstStop = stopInstance("stopped-initialization-instance");
  const secondOpen = startInstance(
    "stopped-initialization-instance",
    { area: "area-mvc" },
    () => {},
  );
  releaseStoppedLoad();
  await firstStop;
  const secondEntry = await secondOpen;
  await secondEntry.initialization;

  assert.deepEqual(writes, ["area-mvc"]);
  await assert.rejects(fetch(firstEntry.url));
  assert.equal((await fetch(secondEntry.url)).status, 200);
  await stopInstance("stopped-initialization-instance");
});
